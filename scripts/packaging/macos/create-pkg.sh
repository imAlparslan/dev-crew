#!/usr/bin/env bash
set -euo pipefail

APP_PATH=""
CLI_PATH=""
VERSION=""
OUTPUT_DIR="artifacts/dist"
PACKAGE_ID="com.devcrew.macos"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --app)
      APP_PATH="$2"
      shift 2
      ;;
    --cli)
      CLI_PATH="$2"
      shift 2
      ;;
    --version)
      VERSION="$2"
      shift 2
      ;;
    --output-dir)
      OUTPUT_DIR="$2"
      shift 2
      ;;
    --package-id)
      PACKAGE_ID="$2"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

if [[ -z "$APP_PATH" ]]; then
  echo "Missing required argument: --app <path-to-app-bundle>" >&2
  exit 1
fi

if [[ -z "$CLI_PATH" ]]; then
  echo "Missing required argument: --cli <path-to-cli-binary>" >&2
  exit 1
fi

if [[ -z "$VERSION" ]]; then
  echo "Missing required argument: --version <semantic-version>" >&2
  exit 1
fi

if [[ ! -d "$APP_PATH" ]]; then
  echo "App bundle not found: $APP_PATH" >&2
  exit 1
fi

if [[ ! -f "$CLI_PATH" ]]; then
  echo "CLI binary not found: $CLI_PATH" >&2
  exit 1
fi

mkdir -p "$OUTPUT_DIR"
PKG_NAME="DevCrew-${VERSION}.pkg"
PKG_PATH="${OUTPUT_DIR}/${PKG_NAME}"

STAGING_ROOT="$(mktemp -d /tmp/devcrew-pkg-root.XXXXXX)"
EXPAND_DIR="$(mktemp -u /tmp/devcrew-pkg-expand.XXXXXX)"

cleanup() {
  rm -rf "$STAGING_ROOT" "$EXPAND_DIR"
}
trap cleanup EXIT

mkdir -p "${STAGING_ROOT}/Applications" "${STAGING_ROOT}/usr/local/bin"

cp -R "$APP_PATH" "${STAGING_ROOT}/Applications/DevCrew.app"
install -m 755 "$CLI_PATH" "${STAGING_ROOT}/usr/local/bin/crew"

rm -f "$PKG_PATH"

pkgbuild \
  --root "$STAGING_ROOT" \
  --identifier "$PACKAGE_ID" \
  --version "$VERSION" \
  --install-location "/" \
  "$PKG_PATH"

pkgutil --expand "$PKG_PATH" "$EXPAND_DIR"

if [[ ! -f "$PKG_PATH" ]]; then
  echo "PKG was not created: $PKG_PATH" >&2
  exit 1
fi

if [[ ! -f "${EXPAND_DIR}/PackageInfo" ]]; then
  echo "PackageInfo not found after expanding PKG" >&2
  exit 1
fi

echo "PKG created at ${PKG_PATH}"