#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/workspace/toolchains/dotnet-home}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-/workspace/toolchains/nuget}"
export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
sdk="${REACTOR_DOTNET:-/workspace/toolchains/dotnet/dotnet}"
if [[ ! -x "$sdk" ]]; then sdk="$(command -v dotnet)"; fi
destination="${1:-TestResults/Balance}"
mkdir -p "$destination"
python3 - "$destination" <<'PY'
import hashlib, json, sys
from pathlib import Path
import yaml
source = Path('Assets/ReactorDeCaos/Resources/Economy.asset')
data = yaml.safe_load('\n'.join(source.read_text().splitlines()[3:]))['MonoBehaviour']['values']
assert all(key in data for key in ['generators', 'upgrades', 'milestones']), 'Expected phase-2 catalog in Economy.asset'
output = Path(sys.argv[1])
(output / 'economy.json').write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n')
(output / 'source.sha256').write_text(hashlib.sha256(source.read_bytes()).hexdigest() + '  ' + str(source) + '\n')
PY
"$sdk" restore Tools/Balance/Balance.csproj --locked-mode
"$sdk" run --project Tools/Balance/Balance.csproj --no-restore --configuration Release -- "$destination/economy.json" "$destination"
