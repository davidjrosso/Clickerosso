#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
: "${UNITY_EDITOR:?Set UNITY_EDITOR to the licensed Unity executable matching ProjectVersion.txt}"
[[ -x "$UNITY_EDITOR" ]] || { echo "UNITY_EDITOR is not executable" >&2; exit 2; }
mkdir -p TestResults/Unity
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" -buildTarget Android \
  -executeMethod ReactorDeCaos.Editor.ProjectSetup.BuildAndroid -quit \
  -logFile "$PWD/TestResults/Unity/android-build.log"
test -s Builds/Android/ReactorDeCaos-development.apk
