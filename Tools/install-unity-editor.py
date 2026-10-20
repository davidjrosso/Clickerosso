#!/usr/bin/env python3
"""Install the project's pinned Linux Editor, verifying Unity's published hash."""
import base64
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import urllib.request

project = Path(__file__).resolve().parents[1]
version = (project / 'ProjectSettings/ProjectVersion.txt').read_text().splitlines()[0].split(': ', 1)[1]
root = Path('/workspace/toolchains')
target = root / ('Unity-' + version)
url = ('https://services.api.unity.com/unity/editor/release/v1/releases'
       '?limit=1&version=' + version + '&platform=LINUX&architecture=X86_64')
with urllib.request.urlopen(url, timeout=60) as response:
    releases = json.load(response)['results']
release = next(r for r in releases if r['version'] == version)
download = next(d for d in release['downloads'] if d['platform'] == 'LINUX' and d['architecture'] == 'X86_64')
algorithm, expected = download['integrity'].split('-', 1)
expected = base64.b64decode(expected).hex()
root.mkdir(parents=True, exist_ok=True)
archive = root / ('Unity-' + version + '.tar.xz')
if not archive.exists():
    required = download['downloadSize']['value'] + download['installedSize']['value']
    if shutil.disk_usage(root).free < required + 2 * 1024 ** 3:
        raise SystemExit('Not enough free space for the verified Editor installation.')
    temporary = archive.with_suffix('.download')
    print('Downloading official Unity Editor', version, flush=True)
    with urllib.request.urlopen(download['url'], timeout=60) as response, temporary.open('wb') as output:
        shutil.copyfileobj(response, output, length=1024 * 1024)
    temporary.rename(archive)
digest = hashlib.new(algorithm)
with archive.open('rb') as handle:
    for block in iter(lambda: handle.read(1024 * 1024), b''):
        digest.update(block)
if digest.hexdigest() != expected:
    raise SystemExit('Unity artifact checksum mismatch. Archive was not extracted.')
print('Official Unity checksum verified:', algorithm, flush=True)
target.mkdir(exist_ok=True)
subprocess.run(['tar', '-xJf', str(archive), '-C', str(target)], check=True)
editor = target / 'Editor/Unity'
if not editor.is_file():
    raise SystemExit('Expected Editor executable missing after extraction.')
print('UNITY_EDITOR=' + str(editor), flush=True)
print('Installation does not activate a Unity license or validate the project.', flush=True)
