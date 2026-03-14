#!/bin/bash

echo -ne "\033]0;Ritsuko Updater\007"

clear
echo "====================================="
echo " Ritsuko Update Service "
echo "====================================="
echo
echo "Script launched at: $(date)"
echo

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$SCRIPT_DIR/.."

echo "Resolved script directory: $SCRIPT_DIR"
echo "Resolved project root: $PROJECT_ROOT"
echo

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
echo "Waiting 5 seconds..."
sleep 5
echo
echo "ready for next step"
echo
echo "Press ENTER to close this window."

read