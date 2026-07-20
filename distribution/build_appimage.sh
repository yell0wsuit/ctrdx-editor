#!/bin/bash
# Builds a Linux AppImage for the Cut the Rope DX Level Editor.
# Usage: ./build_appimage.sh <version>
# Must run on Linux x64: NativeAOT does not support cross-OS compilation.
#
# Requirements: .NET 10 SDK, clang, zlib1g-dev, wget.

set -euo pipefail

APP_NAME="CtrDxEditor"
APP_ID="page.yell0wsuit.ctrdx.editor"
DISPLAY_NAME="Cut the Rope DX: Level Editor"
DESCRIPTION="A standalone level editor for Cut the Rope: DX."
RID="linux-x64"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT="$PROJECT_ROOT/src/CtrDxEditor.Desktop/CtrDxEditor.Desktop.csproj"
PUBLISH_DIR="$PROJECT_ROOT/publish/$RID"
RELEASE_DIR="$PROJECT_ROOT/publish/release_github"
BUILD_DIR="$SCRIPT_DIR/appimage_build"
APPDIR="$BUILD_DIR/$APP_NAME.AppDir"
TOOLS_DIR="$SCRIPT_DIR/tools"
TEMPLATES_DIR="$SCRIPT_DIR/templates/linux"
ICON_SOURCE="$SCRIPT_DIR/icons/CtrDxEditorIcon_512.png"

VERSION="${1:-}"
if [ -z "$VERSION" ]; then
    echo "Error: version is required. Usage: $0 <version>" >&2
    exit 1
fi

echo "=== Building $DISPLAY_NAME v$VERSION AppImage ==="

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

echo "[2/5] Creating AppDir..."
rm -rf "$BUILD_DIR"
mkdir -p "$APPDIR/usr/bin"
mkdir -p "$APPDIR/usr/share/applications"
mkdir -p "$APPDIR/usr/share/icons/hicolor/512x512/apps"
mkdir -p "$APPDIR/usr/share/metainfo"

echo "[3/5] Copying application files..."
cp -r "$PUBLISH_DIR"/. "$APPDIR/usr/bin/"
chmod +x "$APPDIR/usr/bin/$APP_NAME"

cat > "$APPDIR/AppRun" << EOF
#!/bin/bash
SELF=\$(readlink -f "\$0")
HERE=\${SELF%/*}
export PATH="\${HERE}/usr/bin:\${PATH}"
cd "\${HERE}/usr/bin"
exec "./$APP_NAME" "\$@"
EOF
chmod +x "$APPDIR/AppRun"

echo "[4/5] Writing metadata..."
RELEASE_DATE=$(date +%Y-%m-%d)

sed -e "s/{{DISPLAY_NAME}}/$DISPLAY_NAME/g" \
    -e "s/{{DESCRIPTION}}/$DESCRIPTION/g" \
    -e "s/{{APP_NAME}}/$APP_NAME/g" \
    "$TEMPLATES_DIR/appimage.desktop" > "$APPDIR/$APP_NAME.desktop"
cp "$APPDIR/$APP_NAME.desktop" "$APPDIR/usr/share/applications/"

sed -e "s/{{APP_ID}}/$APP_ID/g" \
    -e "s/{{DISPLAY_NAME}}/$DISPLAY_NAME/g" \
    -e "s/{{DESCRIPTION}}/$DESCRIPTION/g" \
    -e "s/{{APP_NAME}}/$APP_NAME/g" \
    -e "s/{{VERSION}}/$VERSION/g" \
    -e "s/{{RELEASE_DATE}}/$RELEASE_DATE/g" \
    "$TEMPLATES_DIR/appimage.metainfo.xml" > "$APPDIR/usr/share/metainfo/$APP_ID.metainfo.xml"

if [ -f "$ICON_SOURCE" ]; then
    cp "$ICON_SOURCE" "$APPDIR/$APP_NAME.png"
    cp "$ICON_SOURCE" "$APPDIR/usr/share/icons/hicolor/512x512/apps/$APP_NAME.png"
    ln -sf "$APP_NAME.png" "$APPDIR/.DirIcon"
else
    echo "Warning: icon not found at $ICON_SOURCE, AppImage will use the default icon"
fi

echo "[5/5] Building AppImage..."
APPIMAGETOOL="$TOOLS_DIR/appimagetool-x86_64.AppImage"
if [ ! -f "$APPIMAGETOOL" ]; then
    echo "Downloading appimagetool..."
    mkdir -p "$TOOLS_DIR"
    wget -q --show-progress -O "$APPIMAGETOOL" \
        "https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage"
    chmod +x "$APPIMAGETOOL"
fi

mkdir -p "$RELEASE_DIR"
APPIMAGE_PATH="$RELEASE_DIR/$APP_NAME-v$VERSION-Linux-x86_64.AppImage"
rm -f "$APPIMAGE_PATH"

# GitHub's ubuntu runners have no libfuse2, so appimagetool cannot mount itself.
# Extracting and running sidesteps that without installing anything.
ARCH=x86_64 APPIMAGE_EXTRACT_AND_RUN=1 "$APPIMAGETOOL" "$APPDIR" "$APPIMAGE_PATH"

rm -rf "$BUILD_DIR"

echo ""
echo "=== Build complete ==="
echo "AppImage: $APPIMAGE_PATH ($(du -h "$APPIMAGE_PATH" | cut -f1))"
