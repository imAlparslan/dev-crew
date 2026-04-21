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
/usr/libexec/PlistBuddy -c "Set :0:BundleIsRelocatable false" "$COMPONENT_PLIST"
/usr/libexec/PlistBuddy -c "Set :0:BundleIsVersionChecked false" "$COMPONENT_PLIST"
/usr/libexec/PlistBuddy -c "Set :0:BundleHasBundleIdentifier true" "$COMPONENT_PLIST"
/usr/libexec/PlistBuddy -c "Set :0:BundleIdentifier $PACKAGE_ID" "$COMPONENT_PLIST"

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