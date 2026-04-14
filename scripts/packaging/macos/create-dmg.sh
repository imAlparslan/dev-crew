#!/usr/bin/env bash
set -euo pipefail

APP_PATH=""
VERSION=""
OUTPUT_DIR="artifacts/dist"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --app)
      APP_PATH="$2"
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

if [[ -z "$VERSION" ]]; then
  echo "Missing required argument: --version <semantic-version>" >&2
  exit 1
fi

if [[ ! -d "$APP_PATH" ]]; then
  echo "App bundle not found: $APP_PATH" >&2
  exit 1
fi

mkdir -p "$OUTPUT_DIR"
DMG_NAME="DevCrew-${VERSION}.dmg"
DMG_PATH="${OUTPUT_DIR}/${DMG_NAME}"

STAGING_DIR="$(mktemp -d /tmp/devcrew-dmg-staging.XXXXXX)"
RW_DMG="$(mktemp /tmp/devcrew-rw.XXXXXX.dmg)"
MOUNT_POINT="$(mktemp -d /tmp/devcrew-dmg-mount.XXXXXX)"

cleanup() {
  hdiutil detach "$MOUNT_POINT" >/dev/null 2>&1 || true
  rm -rf "$STAGING_DIR" "$MOUNT_POINT"
  rm -f "$RW_DMG"
}
trap cleanup EXIT

cp -R "$APP_PATH" "${STAGING_DIR}/DevCrew.app"
ln -s /Applications "${STAGING_DIR}/Applications"

hdiutil create \
  -volname "DevCrew ${VERSION}" \
  -srcfolder "$STAGING_DIR" \
  -ov \
  -format UDRW \
  "$RW_DMG"

hdiutil convert "$RW_DMG" \
  -format ULFO \
  -o "$DMG_PATH"

hdiutil verify "$DMG_PATH"

hdiutil attach "$DMG_PATH" -mountpoint "$MOUNT_POINT" -readonly >/dev/null
if [[ ! -d "${MOUNT_POINT}/DevCrew.app" ]]; then
  echo "DevCrew.app not found in DMG" >&2
  exit 1
fi
if [[ ! -x "${MOUNT_POINT}/DevCrew.app/Contents/MacOS/DevCrew.Desktop" ]]; then
  echo "DevCrew.Desktop executable not found in DMG" >&2
  exit 1
fi

# Explicitly detach before returning to avoid noisy detach failures at trap time.
hdiutil detach "$MOUNT_POINT" >/dev/null

echo "DMG created at ${DMG_PATH}"