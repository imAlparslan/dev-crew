#!/usr/bin/env bash
set -euo pipefail

APP_PATH=""
CLI_PATH=""
VERSION=""
OUTPUT_DIR="artifacts/dist"
PACKAGE_ID="com.devcrew.macos"

# Parse arguments
while [[ $# -gt 0 ]]; do
  case "$1" in
    --app) APP_PATH="$2"; shift 2 ;;
    --cli) CLI_PATH="$2"; shift 2 ;;
    --version) VERSION="$2"; shift 2 ;;
    --output-dir) OUTPUT_DIR="$2"; shift 2 ;;
    --package-id) PACKAGE_ID="$2"; shift 2 ;;
    *) echo "UNKNOWN ARGUMENT: $1" >&2; exit 1 ;;
  esac
done

# Folder check
if [[ -z "$APP_PATH" || -z "$CLI_PATH" || -z "$VERSION" ]]; then
  echo "ERROR: --app, --cli and --version parameters are required!" >&2
  exit 1
fi

mkdir -p "$OUTPUT_DIR"
PKG_NAME="DevCrew-${VERSION}.pkg"
PKG_PATH="${OUTPUT_DIR}/${PKG_NAME}"

# temp directory
STAGING_ROOT="$(mktemp -d /tmp/devcrew-pkg-root.XXXXXX)"
COMPONENT_PLIST="$(mktemp /tmp/devcrew-component.XXXXXX.plist)"

cleanup() {
  rm -rf "$STAGING_ROOT"
  rm -f "$COMPONENT_PLIST"
}
trap cleanup EXIT

# Create file hierarchy
mkdir -p "${STAGING_ROOT}/Applications" "${STAGING_ROOT}/usr/local/bin"

cp -R "$APP_PATH" "${STAGING_ROOT}/Applications/DevCrew.app"
install -m 755 "$CLI_PATH" "${STAGING_ROOT}/usr/local/bin/crew"

# 1. Component Analysis
pkgbuild --analyze --root "$STAGING_ROOT" "$COMPONENT_PLIST"

# 2. Edit Component Settings (using PlistBuddy)
# Index 0 is usually /Applications/DevCrew.app. 
# These settings ensure it overwrites during updates and guarantees a fixed location.
set_or_add_plist_entry() {
  local key_path="$1"
  local value_type="$2"
  local value="$3"

  if /usr/libexec/PlistBuddy -c "Print ${key_path}" "$COMPONENT_PLIST" >/dev/null 2>&1; then
    /usr/libexec/PlistBuddy -c "Set ${key_path} ${value}" "$COMPONENT_PLIST"
  else
    /usr/libexec/PlistBuddy -c "Add ${key_path} ${value_type} ${value}" "$COMPONENT_PLIST"
  fi
}

set_or_add_plist_entry ":0:BundleIsRelocatable" "bool" "false"
set_or_add_plist_entry ":0:BundleIsVersionChecked" "bool" "false"
set_or_add_plist_entry ":0:BundleHasBundleIdentifier" "bool" "true"
set_or_add_plist_entry ":0:BundleIdentifier" "string" "$PACKAGE_ID"

# 3. Create the package
pkgbuild \
  --root "$STAGING_ROOT" \
  --component-plist "$COMPONENT_PLIST" \
  --identifier "$PACKAGE_ID" \
  --version "$VERSION" \
  --install-location "/" \
  "$PKG_PATH"

echo "------------------------------------------"
echo "✅ PKG successfully created: ${PKG_PATH}"
echo "📦 Package Identifier: ${PACKAGE_ID}"
echo "🚀 Version: ${VERSION}"
echo "------------------------------------------"