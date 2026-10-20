# Reactor de Caos

## Scope
- Original offline Unity 2D incremental arcade game for Android, portrait.
- Use C#, uGUI and TextMeshPro. Do not replace Unity with another engine.
- MVP: tap income, combo, manual overload, three upgrades, four configurable generators,
  milestones, local saves, original art and restrained feedback.
- No ads, purchases, accounts, servers, prestige or offline rewards.

## Working agreement
- Work in this existing checkout. Cloud tasks are already isolated; do not
  create Git worktrees unless the user explicitly requests one.
- Preserve user changes. Commit coherent local milestones; do not push without
  authorization. Never commit Library, builds, credentials or licenses.
- Keep economy in `Assets/ReactorDeCaos/Scripts/Core`, independent of Unity.
- Update tests when changing economy or persistence behavior.
- Configuration is data; UI and animation must not calculate rewards.
- Keep generator/milestone IDs stable across saves. `coil` and `plasma` retain
  phase-1 ownership; additional generators use ID-based records. Claims are
  persisted before celebrations. Visual stage changes grant no hidden income.
- Use the pinned Editor in ProjectSettings/ProjectVersion.txt. The initial
  version is provisional until the first real Editor import is validated.
- Do not claim Unity, Android or visual validation based on .NET tests.
- Report implementation, files, actual test results, blockers and reproduction
  steps at each phase. Record ongoing evidence in Docs/VALIDATION.md.

## Commands
- `bash Tools/install-dotnet.sh` installs a verified local SDK outside checkout.
- `bash Tools/test-core.sh` runs shared economy and persistence tests.
- `bash Tools/simulate-balance.sh` runs three 30-minute profiles against the real
  Economy.asset. Requires Python/PyYAML (`Tools/requirements-balance.txt`); results
  go to TestResults/Balance. See Docs/PHASE2.md for assumptions and pacing budgets.
- `python3 Tools/install-unity-editor.py` installs the pinned Linux Editor with
  the official artifact checksum. It does not activate a license.
- `python3 Tools/check-unity-api.py` compiles all assemblies using the installed
  Editor's real reference DLLs. This is not an Editor import or PlayMode run.
- `UNITY_EDITOR=/path/to/Unity bash Tools/test-unity.sh` runs EditMode/PlayMode.
- `UNITY_EDITOR=/path/to/Unity bash Tools/build-android.sh` builds a debug APK.
- Open `Assets/ReactorDeCaos/Scenes/Main.unity` and press Play.
- `Tools/Reactor de Caos/Open Main Scene` opens that scene after offering to save.
- `Tools/Reactor de Caos/Prepare Project` prepares TMP font assets and Android settings
  from Unity's top menu bar; the original `Reactor de Caos` menu also remains.
- Playback falls back to a dynamic font from the bundled DejaVuSans.ttf if the
  generated ReactorFont asset is absent. TMP Essentials are still required.

## Assets and performance
- Original generated background provenance is in Art/PROVENANCE.md.
- Keep effect pools bounded; avoid per-frame allocations and uncapped emission.
- Test touch, safe areas, pause/resume and frame time on a real Android device.
- Never bypass package signatures, artifact hashes or TLS checks.
