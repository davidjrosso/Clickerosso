using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace ReactorDeCaos.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/ReactorDeCaos/Scenes/Main.unity";
        private const string FontPath = "Assets/ReactorDeCaos/Resources/Fonts/ReactorFont.asset";
        private const string SettingsPath = "Assets/ReactorDeCaos/Resources/Economy.asset";
        private const string EssentialPackageName = "TMP Essential Resources";
        private static bool preparing;
        private static bool exitWhenFinished;
        private static double importDeadline;
        private static Action afterPreparation;

        static ProjectSetup()
        {
            if (!Application.isBatchMode)
                EditorApplication.delayCall += AutomaticPrepare;
        }

        private static void AutomaticPrepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || preparing) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutomaticPrepare;
                return;
            }
            if (!HasTmpResources() || AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) == null)
                Prepare();
        }

        [MenuItem("Reactor de Caos/Prepare Project")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Detén Play antes de ejecutar Tools > Reactor de Caos > Prepare Project.");
                return;
            }
            BeginPreparation(null, false);
        }

        [MenuItem("Tools/Reactor de Caos/Prepare Project")]
        public static void PrepareFromTools()
        {
            Prepare();
        }

        [MenuItem("Tools/Reactor de Caos/Open Main Scene")]
        public static void OpenMainScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Detén Play antes de abrir la escena principal.");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        // Batch entry points own the exit code; callers must NOT pass -quit,
        // because importing a unitypackage completes asynchronously.
        public static void PrepareBatch()
        {
            BeginPreparation(null, true);
        }

        public static bool HasTmpResources()
        {
            return Resources.Load<TMP_Settings>("TMP Settings") != null
                && Shader.Find("TextMeshPro/Distance Field") != null;
        }

        private static void BeginPreparation(Action next, bool batch)
        {
            if (preparing)
            {
                Debug.Log("Reactor preparation is already running. Wait for it to finish.");
                return;
            }
            preparing = true;
            exitWhenFinished = batch;
            afterPreparation = next;
            importDeadline = EditorApplication.timeSinceStartup + 300;
            EditorApplication.update += CheckImportTimeout;
            try
            {
                if (HasTmpResources())
                {
                    CompletePreparation();
                    return;
                }
                PackageInfo package = PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
                if (package == null) throw new InvalidOperationException("TextMeshPro package is not resolved.");
                string[] resources = Directory.GetFiles(package.resolvedPath,
                    EssentialPackageName + ".unitypackage", SearchOption.AllDirectories);
                if (resources.Length != 1)
                    throw new FileNotFoundException("TMP Essentials not found. Use Window > Text Mesh Pro > Import TMP Essential Resources.");
                AssetDatabase.importPackageCompleted += OnEssentialsImported;
                AssetDatabase.importPackageFailed += OnImportFailed;
                AssetDatabase.importPackageCancelled += OnImportCancelled;
                Debug.Log("Importing TMP Essential Resources. Reactor setup will continue when the import finishes.");
                AssetDatabase.ImportPackage(resources[0], false);
            }
            catch (Exception error) { FailPreparation(error); }
        }

        private static void OnEssentialsImported(string packageName)
        {
            if (packageName != EssentialPackageName) return;
            // Like TMP's official importer, notify its settings consumers after import.
            TMPro_EventManager.ON_RESOURCES_LOADED();
            EditorApplication.delayCall += CompletePreparation;
        }

        private static void OnImportFailed(string packageName, string error)
        {
            if (packageName == EssentialPackageName)
                FailPreparation(new InvalidOperationException("TMP Essentials import failed: " + error));
        }

        private static void OnImportCancelled(string packageName)
        {
            if (packageName == EssentialPackageName)
                FailPreparation(new InvalidOperationException("TMP Essentials import was cancelled. Run Prepare Project to retry."));
        }

        private static void CheckImportTimeout()
        {
            if (preparing && EditorApplication.timeSinceStartup > importDeadline)
                FailPreparation(new TimeoutException("TMP preparation timed out. Check the Console, import TMP Essentials and run Prepare Project."));
        }

        private static void CompletePreparation()
        {
            if (!preparing) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += CompletePreparation;
                return;
            }
            try
            {
                if (!HasTmpResources())
                    throw new InvalidOperationException("TMP Settings or the Distance Field shader is still missing. Import TMP Essential Resources and run Prepare Project.");
                PrepareAssets();
                Action next = afterPreparation;
                StopWaiting();
                next?.Invoke();
                if (exitWhenFinished) EditorApplication.Exit(0);
            }
            catch (Exception error) { FailPreparation(error); }
        }

        private static void StopWaiting()
        {
            preparing = false;
            afterPreparation = null;
            AssetDatabase.importPackageCompleted -= OnEssentialsImported;
            AssetDatabase.importPackageFailed -= OnImportFailed;
            AssetDatabase.importPackageCancelled -= OnImportCancelled;
            EditorApplication.update -= CheckImportTimeout;
            EditorApplication.delayCall -= CompletePreparation;
        }

        private static void FailPreparation(Exception error)
        {
            StopWaiting();
            Debug.LogException(error);
            if (exitWhenFinished) EditorApplication.Exit(1);
        }

        private static void PrepareAssets()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) == null)
            {
                TMP_FontAsset font = ReactorFont.CreateRuntimeFont();
                font.name = "ReactorFont";
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

        [MenuItem("Reactor de Caos/Build Android Development APK")]
        public static void BuildAndroid()
        {
            BeginPreparation(BuildAndroidPrepared, false);
        }

        public static void BuildAndroidBatch()
        {
            BeginPreparation(BuildAndroidPrepared, true);
        }

        private static void BuildAndroidPrepared()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Install Android Build Support, SDK, NDK and OpenJDK for this Editor.");
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
