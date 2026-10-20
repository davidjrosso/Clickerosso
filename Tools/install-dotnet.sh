#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/workspace/toolchains/dotnet-home}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-/workspace/toolchains/nuget}"
export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_GENERATE_ASPNET_CERTIFICATE=false
python3 - <<'PY'
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import urllib.request

version = json.loads(Path('global.json').read_text())['sdk']['version']
base = Path('/workspace/toolchains')
base.mkdir(parents=True, exist_ok=True)
metadata = 'https://builds.dotnet.microsoft.com/dotnet/release-metadata/8.0/releases.json'
with urllib.request.urlopen(metadata, timeout=60) as response:
    releases = json.load(response)
artifacts = [
    artifact for release in releases['releases']
    for sdk in release.get('sdks', [release['sdk']])
    if sdk['version'] == version
    for artifact in sdk['files']
    if artifact['rid'] == 'linux-x64' and artifact['name'].endswith('.tar.gz')
]
if not artifacts:
    raise SystemExit('Pinned SDK not found in official release metadata.')
artifact = artifacts[0]
archive = base / ('dotnet-sdk-' + version + '-linux-x64.tar.gz')
def digest(path):
    value = hashlib.sha512()
    with path.open('rb') as handle:
        for block in iter(lambda: handle.read(1024 * 1024), b''):
            value.update(block)
    return value.hexdigest()
if not archive.exists() or digest(archive).lower() != artifact['hash'].lower():
    temporary = archive.with_suffix('.download')
    with urllib.request.urlopen(artifact['url'], timeout=60) as response, temporary.open('wb') as output:
        shutil.copyfileobj(response, output)
    if digest(temporary).lower() != artifact['hash'].lower():
        temporary.unlink()
        raise SystemExit('Official SDK SHA-512 verification failed.')
    os.replace(temporary, archive)
target = base / 'dotnet'
target.mkdir(exist_ok=True)
subprocess.run(['tar', '-xzf', str(archive), '-C', str(target)], check=True)
subprocess.run([str(target / 'dotnet'), '--version'], check=True)
print('SDK verified against the official SHA-512 and installed:', version)
PY
/workspace/toolchains/dotnet/dotnet restore Tests/CoreTests.csproj --locked-mode
