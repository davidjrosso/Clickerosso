#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
: "${UNITY_EDITOR:?Set UNITY_EDITOR to the licensed Unity executable matching ProjectVersion.txt}"
[[ -x "$UNITY_EDITOR" ]] || { echo "UNITY_EDITOR is not executable" >&2; exit 2; }
mkdir -p TestResults/Unity
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" \
  -executeMethod ReactorDeCaos.Editor.ProjectSetup.Prepare -quit \
  -logFile "$PWD/TestResults/Unity/prepare.log"
for platform in EditMode PlayMode; do
  result="$PWD/TestResults/Unity/$platform.xml"
  rm -f "$result"
  "$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" \
    -runTests -testPlatform "$platform" -testResults "$result" \
    -logFile "$PWD/TestResults/Unity/$platform.log"
  python3 - "$result" <<'PY'
import sys
import xml.etree.ElementTree as ET
root = ET.parse(sys.argv[1]).getroot()
passed = int(root.attrib.get('passed', 0))
failed = int(root.attrib.get('failed', 0))
skipped = int(root.attrib.get('skipped', 0))
print(f'{sys.argv[1]}: passed={passed}, failed={failed}, skipped={skipped}')
if root.attrib.get('result') != 'Passed' or passed == 0 or failed or skipped:
    raise SystemExit('Unity suite did not complete with all tests passing.')
PY
done
