#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Yokai.EditorTools
{
    public static class YokaiProjectBootstrap
    {
        const string ScenePath = "Assets/Yokai/Scenes/YokaiVerticalSlice.unity";

        [MenuItem("YOKAI/Bootstrap Playable Vertical Slice")]
        public static void ConfigureProject()
        {
            Directory.CreateDirectory("Assets/Yokai/Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("YOKAI_VERTICAL_SLICE_BOOTSTRAP");
            root.AddComponent<YokaiWorldBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            PlayerSettings.companyName = "YOKAI Studio";
            PlayerSettings.productName = "YOKAI";
            PlayerSettings.bundleVersion = "1.9.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.runInBackground = true;

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.yokaistudio.yokai");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.bundleVersionCode = 190;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.MTRendering = true;

            QualitySettings.vSyncCount = 0;
            QualitySettings.shadowCascades = 1;
            QualitySettings.shadowDistance = 45f;
            QualitySettings.antiAliasing = 2;
            Application.targetFrameRate = 60;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[YOKAI] Playable vertical slice bootstrap complete.");
        }

        [MenuItem("YOKAI/Build Windows Development")]
        public static void BuildWindowsDevelopment()
        {
            ConfigureProject();
            YokaiReleaseValidator.ValidateOrThrow();
            Directory.CreateDirectory("Builds/Windows");

            BuildPlayerOptions options = new BuildPlayerOptions();
            options.scenes = new string[] { ScenePath };
            options.locationPathName = "Builds/Windows/YOKAI_V19_AIR_PHYSICS_RC.exe";
            options.target = BuildTarget.StandaloneWindows64;
            options.options = BuildOptions.Development;

            EnsureSucceeded(BuildPipeline.BuildPlayer(options), options.locationPathName);
        }

        [MenuItem("YOKAI/Build Android Development APK")]
        public static void BuildAndroidDevelopment()
        {
            BuildAndroid("Builds/Android/YOKAI_V19_AIR_PHYSICS_RC_DEV.apk", true);
        }

        [MenuItem("YOKAI/Build Android RELEASE APK")]
        public static void BuildAndroidRelease()
        {
            BuildAndroid("Builds/Android/YOKAI_V19_AIR_PHYSICS_RC.apk", false);
        }

        public static void BuildAndroidCi()
        {
            BuildAndroid("Builds/Android/YOKAI_V19_AIR_PHYSICS_RC.apk", false);
        }

        static void BuildAndroid(string outputPath, bool development)
        {
            ConfigureProject();
            YokaiReleaseValidator.ValidateOrThrow();
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory("Builds/Android");

            BuildPlayerOptions options = new BuildPlayerOptions();
            options.scenes = new string[] { ScenePath };
            options.locationPathName = outputPath;
            options.target = BuildTarget.Android;
            options.targetGroup = BuildTargetGroup.Android;
            options.options = development ? BuildOptions.Development : BuildOptions.None;

            EnsureSucceeded(BuildPipeline.BuildPlayer(options), outputPath);
        }

        static void EnsureSucceeded(BuildReport report, string outputPath)
        {
            if (report == null || report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("[YOKAI] Build failed: " + (report == null ? "no report" : report.summary.result.ToString()));

            FileInfo file = new FileInfo(outputPath);
            if (!file.Exists || file.Length <= 0)
                throw new BuildFailedException("[YOKAI] Build reported success but output is missing/empty: " + outputPath);

            Debug.Log("[YOKAI_BUILD_READY] " + outputPath + " | bytes=" + file.Length + " | time=" + report.summary.totalTime);
        }
    }
}
#endif
