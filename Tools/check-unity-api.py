#!/usr/bin/env python3
"""Compile our assemblies against installed Unity APIs; does NOT execute Unity."""
import json
import os
from pathlib import Path
import subprocess

project = Path(__file__).resolve().parents[1]
version = (project / 'ProjectSettings/ProjectVersion.txt').read_text().splitlines()[0].split(': ', 1)[1]
editor = Path(os.environ.get('UNITY_EDITOR', '/workspace/toolchains/Unity-' + version + '/Editor/Unity'))
data = editor.parent / 'Data'
sdk = Path(os.environ.get('REACTOR_DOTNET', '/workspace/toolchains/dotnet/dotnet'))
compiler = data / 'DotNetSdkRoslyn/csc.dll'
standard = data / 'NetStandard/ref/2.1.0/netstandard.dll'
template = data / 'Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.2d-cross-platform-5.1.0/ScriptAssemblies'
nunit = data / 'Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net40/unity-custom/nunit.framework.dll'
for file in [compiler, standard, template / 'UnityEngine.UI.dll', template / 'Unity.TextMeshPro.dll', nunit]:
    if not file.is_file():
        raise SystemExit('Required official Editor reference is missing: ' + str(file))

output = project / 'TestResults/UnityApi'
output.mkdir(parents=True, exist_ok=True)
engine = [data / 'Managed/UnityEngine.dll']
engine += [p for p in (data / 'Managed/UnityEngine').glob('UnityEngine*.dll')]
editor_refs = list((data / 'Managed').rglob('UnityEditor.*Module.dll'))
external = {name: template / (name + '.dll') for name in
            ['UnityEngine.UI', 'Unity.TextMeshPro', 'UnityEngine.TestRunner', 'UnityEditor.TestRunner']}
compiled = {}
order = ['Scripts/Core', 'Scripts/Persistence', 'Scripts/Runtime', 'Editor', 'Tests/EditMode', 'Tests/PlayMode']
for folder in order:
    location = project / 'Assets/ReactorDeCaos' / folder
    definition = json.loads(next(location.glob('*.asmdef')).read_text())
    name = definition['name']
    references = [standard] + list((data / 'NetStandard/compat/2.1.0/shims/netfx').glob('*.dll'))
    if not definition.get('noEngineReferences'):
        references += engine
    if definition.get('includePlatforms') == ['Editor']:
        references += editor_refs
    for reference in definition.get('references', []):
        references.append(compiled[reference] if reference in compiled else external[reference])
    if 'TestAssemblies' in definition.get('optionalUnityReferences', []):
        references += [nunit, external['UnityEngine.TestRunner'], external['UnityEditor.TestRunner']]
    artifact = output / (name + '.dll')
    response = output / (name + '.rsp')
    options = ['-nologo', '-target:library', '-nostdlib+', '-langversion:9.0',
               '-define:UNITY_6000_0_OR_NEWER;UNITY_6000_0;UNITY_5_3_OR_NEWER;UNITY_EDITOR;UNITY_EDITOR_LINUX;UNITY_INCLUDE_TESTS',
               '-out:"' + str(artifact) + '"']
    options += ['-reference:"' + str(ref) + '"' for ref in dict.fromkeys(references)]
    options += ['"' + str(source) + '"' for source in sorted(location.rglob('*.cs'))]
    response.write_text('\n'.join(options) + '\n')
    artifact.unlink(missing_ok=True)
    subprocess.run([str(sdk), str(compiler), '@' + str(response)], check=True)
    if not artifact.is_file():
        raise SystemExit('Compiler did not produce ' + str(artifact))
    compiled[name] = artifact
    print('Compiled against official Unity APIs:', name, flush=True)
print('API compilation passed. This does not validate import, scenes, rendering, PlayMode or Android.', flush=True)
