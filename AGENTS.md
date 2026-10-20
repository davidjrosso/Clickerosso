# Reactor de Caos

## Scope
- Original offline Unity 2D incremental arcade game for Android, portrait.
- Use C#, uGUI and TextMeshPro. Do not replace Unity with another engine.
- MVP: tap income, combo, manual overload, three upgrades, two generators,
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
- Use the pinned Editor in ProjectSettings/ProjectVersion.txt. The initial
  version is provisional until the first real Editor import is validated.
- Do not claim Unity, Android or visual validation based on .NET tests.
- Report implementation, files, actual test results, blockers and reproduction
  steps at each phase. Record ongoing evidence in Docs/VALIDATION.md.

## Commands
- `bash Tools/install-dotnet.sh` installs a verified local SDK outside checkout.
- `bash Tools/test-core.sh` runs shared economy and persistence tests.
- `UNITY_EDITOR=/path/to/Unity bash Tools/test-unity.sh` runs EditMode/PlayMode.
- `UNITY_EDITOR=/path/to/Unity bash Tools/build-android.sh` builds a debug APK.
- Open `Assets/ReactorDeCaos/Scenes/Main.unity` and press Play.
- `Reactor de Caos/Prepare Project` prepares TMP font assets and Android settings.

## Assets and performance
- Original generated background provenance is in Art/PROVENANCE.md.
- Keep effect pools bounded; avoid per-frame allocations and uncapped emission.
- Test touch, safe areas, pause/resume and frame time on a real Android device.
- Never bypass package signatures, artifact hashes or TLS checks.
