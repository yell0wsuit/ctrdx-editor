#!/bin/bash
# Builds the macOS .app bundle and a .dmg for the Cut the Rope DX Level Editor.
# Usage: ./bundle_macos.sh <version>
# Must run on macOS (arm64): NativeAOT does not support cross-OS compilation.

set -euo pipefail

APP_NAME="CtrDxEditor"
DISPLAY_NAME="Cut the Rope DX: Level Editor"
BUNDLE_ID="page.yell0wsuit.ctrdx.editor"
RID="osx-arm64"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT="$PROJECT_ROOT/src/CtrDxEditor.Desktop/CtrDxEditor.Desktop.csproj"
PUBLISH_DIR="$PROJECT_ROOT/publish/$RID"
RELEASE_DIR="$PROJECT_ROOT/publish/release_github"
APP_DIR="$PUBLISH_DIR/$APP_NAME.app"
ICON_SOURCE="$SCRIPT_DIR/icons/CtrDxEditorIcon.icns"
TEMPLATE="$SCRIPT_DIR/templates/macos/Info.plist"

VERSION="${1:-}"
if [ -z "$VERSION" ]; then
    echo "Error: version is required. Usage: $0 <version>" >&2
    exit 1
fi

if [ "$(uname -s)" != "Darwin" ]; then
    echo "Error: this script must run on macOS." >&2
    exit 1
fi

echo "=== Building $DISPLAY_NAME v$VERSION for macOS arm64 ==="

echo "[1/5] Publishing $RID..."
rm -rf "$PUBLISH_DIR"
dotnet publish "$PROJECT" \
    -c Release \
    -r "$RID" \
    -p:VersionPrefix="$VERSION" \
    -p:VersionSuffix= \
    -o "$PUBLISH_DIR"

if [ ! -f "$PUBLISH_DIR/$APP_NAME" ]; then
    echo "Error: expected executable not found at $PUBLISH_DIR/$APP_NAME" >&2
    exit 1
fi

echo "[2/5] Creating .app bundle..."
mkdir -p "$APP_DIR/Contents/MacOS"
mkdir -p "$APP_DIR/Contents/Resources"

# The editor downloads its assets at runtime, so there is no content to split out:
# everything published goes straight into Contents/MacOS.
find "$PUBLISH_DIR" -maxdepth 1 -type f -exec cp {} "$APP_DIR/Contents/MacOS/" \;
chmod +x "$APP_DIR/Contents/MacOS/$APP_NAME"

if [ -f "$ICON_SOURCE" ]; then
    cp "$ICON_SOURCE" "$APP_DIR/Contents/Resources/$APP_NAME.icns"
else
    echo "Warning: icon not found at $ICON_SOURCE, bundle will use the default icon"
fi

sed -e "s/{{APP_NAME}}/$APP_NAME/g" \
    -e "s/{{DISPLAY_NAME}}/$DISPLAY_NAME/g" \
    -e "s/{{BUNDLE_ID}}/$BUNDLE_ID/g" \
    -e "s/{{VERSION}}/$VERSION/g" \
    "$TEMPLATE" > "$APP_DIR/Contents/Info.plist"

echo "[3/5] Codesigning..."
# Every dylib must be signed before the bundle, or macOS kills the app on launch.
find "$APP_DIR" -name '*.dylib' -print0 | xargs -0 -I {} codesign --force --sign - {}
codesign --force --sign - "$APP_DIR"

echo "[4/5] Clearing quarantine..."
xattr -dr com.apple.quarantine "$APP_DIR" || true

echo "[5/5] Packaging .dmg..."
mkdir -p "$RELEASE_DIR"
DMG_PATH="$RELEASE_DIR/$APP_NAME-v$VERSION-macOS-arm64.dmg"
rm -f "$DMG_PATH"
hdiutil create -volname "$APP_NAME" -srcfolder "$APP_DIR" -ov -format UDZO "$DMG_PATH"

echo ""
echo "=== Build complete ==="
echo "App bundle: $APP_DIR"
echo "DMG:        $DMG_PATH ($(du -h "$DMG_PATH" | cut -f1))"
