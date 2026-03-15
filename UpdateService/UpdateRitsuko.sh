#!/bin/bash

echo -ne "\033]0;Ritsuko Updater\007"

clear
echo "====================================="
echo " Ritsuko Update Service "
echo "====================================="
echo
echo "Script launched at: $(date)"
echo

APPLE_CODESIGN_IDENTITY="Developer ID Application: Rico Balakit (D9JNXBPA9M)"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
BUILD_DIR="$HOME/RitsukoBuild"
APP_PATH="$BUILD_DIR/Ritsuko.app"
UNITY_PATH="/Applications/Unity/Hub/Editor/6000.0.61f1/Unity.app/Contents/MacOS/Unity"

echo "Resolved script directory: $SCRIPT_DIR"
echo "Resolved project root: $PROJECT_ROOT"
echo "Resolved build directory: $BUILD_DIR"
echo "Resolved app path: $APP_PATH"
echo "Resolved Unity path: $UNITY_PATH"
echo

echo "Preparing update..."
echo "Git operations will begin in 3 seconds."
echo
sleep 3

echo "Changing to project root..."
cd "$PROJECT_ROOT" || exit 1
echo "Current directory: $(pwd)"
echo

echo "Resetting git workspace to HEAD..."
git reset --hard HEAD
echo

echo "Removing untracked files and directories..."
git clean -fd
echo

echo "Pulling latest changes from remote..."
git pull --ff-only
echo

echo "Git sync step complete."
echo
sleep 1
echo

echo "Ensuring external build directory exists..."
mkdir -p "$BUILD_DIR"
echo

echo "Removing previous built app, if it exists..."
rm -rf "$APP_PATH"
echo

echo "Starting Unity CLI build..."
echo

"$UNITY_PATH" \
  -batchmode \
  -quit \
  -projectPath "$PROJECT_ROOT" \
  -buildTarget StandaloneOSX \
  -executeMethod BuildRitsuko.BuildMac \
  -logFile -

UNITY_EXIT_CODE=$?
echo
echo "Unity build exit code: $UNITY_EXIT_CODE"
echo

if [ "$UNITY_EXIT_CODE" -ne 0 ]; then
    echo "Unity CLI build failed."
    echo
    echo "Waiting 5 seconds..."
    sleep 5
    echo
    echo "ready for next step"
    echo
    echo "Press ENTER to close this window."
    read
    exit "$UNITY_EXIT_CODE"
fi

if [ ! -d "$APP_PATH" ]; then
    echo "Unity CLI build reported success, but the built app was not found at:"
    echo "$APP_PATH"
    echo
    echo "Waiting 5 seconds..."
    sleep 5
    echo
    echo "ready for next step"
    echo
    echo "Press ENTER to close this window."
    read
    exit 1
fi

echo "Signing built app..."
codesign --force --deep --options runtime --timestamp --sign "$APPLE_CODESIGN_IDENTITY" "$APP_PATH"
CODESIGN_EXIT_CODE=$?
echo

if [ "$CODESIGN_EXIT_CODE" -ne 0 ]; then
    echo "Code signing failed."
    echo
    echo "Waiting 5 seconds..."
    sleep 5
    echo
    echo "Press ENTER to close this window."
    read
    exit "$CODESIGN_EXIT_CODE"
fi

echo "Verifying signature..."
codesign --verify --deep --strict --verbose=2 "$APP_PATH"
VERIFY_EXIT_CODE=$?
echo

if [ "$VERIFY_EXIT_CODE" -ne 0 ]; then
    echo "Code signature verification failed."
    echo
    echo "Waiting 5 seconds..."
    sleep 5
    echo
    echo "Press ENTER to close this window."
    read
    exit "$VERIFY_EXIT_CODE"
fi

echo "Gatekeeper assessment..."
spctl -a -vv "$APP_PATH"
echo

echo "Unity CLI build completed successfully."
echo
echo "Waiting 5 seconds before launch..."
sleep 5
echo

echo "Launching built app..."
open "$APP_PATH"
echo

echo "Update flow complete."
sleep 1
exit 0