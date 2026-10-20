using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEngine;

namespace ReactorDeCaos.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/ReactorDeCaos/Scenes/Main.unity";
        private const string FontPath = "Assets/ReactorDeCaos/Resources/Fonts/ReactorFont.asset";
        private const string SettingsPath = "Assets/ReactorDeCaos/Resources/Economy.asset";

        static ProjectSetup()
        {
            if (!Application.isBatchMode)
                EditorApplication.delayCall += () =>
                {
                    if (!EditorApplication.isPlayingOrWillChangePlaymode
                        && AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) == null)
                        Prepare();
                };
        }

        [MenuItem("Reactor de Caos/Prepare Project")]
        public static void Prepare()
        {
            EnsureTmpResources();
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) == null)
            {
                Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/ReactorDeCaos/Resources/Fonts/DejaVuSans.ttf");
                if (source == null) throw new FileNotFoundException("Bundled font is missing.");
                TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source);
                font.name = "ReactorFont";
                string characters = "";
                for (char c = ' '; c <= '~'; c++) characters += c;
                characters += "ÁÉÍÓÚÜÑáéíóúüñ¿¡×·—";
                if (!font.TryAddCharacters(characters, out string missing))
                    throw new InvalidOperationException("Font glyph generation failed: " + missing);
                AssetDatabase.CreateAsset(font, FontPath);
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (Texture2D atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
                font.atlasPopulationMode = AtlasPopulationMode.Static;
                EditorUtility.SetDirty(font);
            }
            if (AssetDatabase.LoadAssetAtPath<EconomySettings>(SettingsPath) == null)
            {
                var settings = ScriptableObject.CreateInstance<EconomySettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            PlayerSettings.companyName = "Clickerosso";
            PlayerSettings.productName = "Reactor de Caos";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.clickerosso.reactordecaos");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.runInBackground = false;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Reactor project prepared. Open Main.unity and press Play.");
        }

        private static void EnsureTmpResources()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro/Resources")) return;
            PackageInfo package = PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
            if (package == null) throw new InvalidOperationException("TextMeshPro package is not resolved.");
            string[] resources = Directory.GetFiles(package.resolvedPath, "TMP Essential Resources.unitypackage", SearchOption.AllDirectories);
            if (resources.Length != 1) throw new FileNotFoundException("TMP Essential Resources package not found.");
            AssetDatabase.ImportPackage(resources[0], false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [MenuItem("Reactor de Caos/Build Android Development APK")]
        public static void BuildAndroid()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Install Android Build Support, SDK, NDK and OpenJDK for this Editor.");
            Prepare();
            Directory.CreateDirectory("Builds/Android");
            EditorUserBuildSettings.buildAppBundle = false;
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Android/ReactorDeCaos-development.apk",
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Android build failed: " + report.summary.result);
            Debug.Log("APK built: " + report.summary.outputPath);
        }
    }
}
