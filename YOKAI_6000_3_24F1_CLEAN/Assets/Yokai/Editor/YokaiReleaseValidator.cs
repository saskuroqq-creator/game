#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Yokai.EditorTools
{
    public static class YokaiReleaseValidator
    {
        const string ScenePath = "Assets/Yokai/Scenes/YokaiVerticalSlice.unity";

        [MenuItem("YOKAI/Validate Release Candidate")]
        public static void ValidateMenu()
        {
            ValidateOrThrow();
            Debug.Log("[YOKAI_RELEASE_QA_PASS] V1.9 3D art release candidate source validation passed.");
        }

        public static void ValidateOrThrow()
        {
            YokaiCombatRegression.Validate();
            YokaiAirPhysicsRegression.Validate();
            YokaiMobileLayoutRegression.Validate();
            string[] scripts =
            {
                "Assets/Yokai/Scripts/Player/YokaiCombat.cs",
                "Assets/Yokai/Scripts/Player/YokaiMotor.cs",
                "Assets/Yokai/Scripts/Player/YokaiPlayerController.cs",
                "Assets/Yokai/Scripts/UI/YokaiMobileHUD.cs",
                "Assets/Yokai/Scripts/AI/YokaiEnemy.cs",
                "Assets/Yokai/Scripts/AI/YokaiBoss.cs",
                "Assets/Yokai/Scripts/World/YokaiEncounterDirector.cs",
                "Assets/Yokai/Scripts/World/YokaiWorldBootstrap.cs",
                "Assets/Yokai/Scripts/Core/YokaiRuntimeQA.cs",
                "Assets/Yokai/Scripts/Art/YokaiMeshLibrary.cs",
                "Assets/Yokai/Scripts/Art/YokaiArtUtility.cs",
                "Assets/Yokai/Scripts/Art/YokaiWorldArt.cs",
                "Assets/Yokai/Scripts/World/YokaiWorldMood.cs"
            };
            foreach (string path in scripts)
                if (!File.Exists(path)) throw new InvalidOperationException("Release: missing source " + path);

            string[] audio =
            {
                "slash_light","slash_heavy","dodge","parry","hunter_art","boss_telegraph","ambient_forest","ambient_boss"
            };
            foreach (string name in audio)
            {
                string path = "Assets/Yokai/Resources/Audio/" + name + ".wav";
                if (!File.Exists(path)) throw new InvalidOperationException("Release: missing audio " + path);
                if (AssetDatabase.LoadAssetAtPath<AudioClip>(path) == null)
                    throw new InvalidOperationException("Release: audio import failed " + path);
            }

            if (!File.Exists(ScenePath)) throw new InvalidOperationException("Release: bootstrap scene missing");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            YokaiWorldBootstrap bootstrap = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                bootstrap = root.GetComponentInChildren<YokaiWorldBootstrap>(true);
                if (bootstrap != null) break;
            }
            if (bootstrap == null) throw new InvalidOperationException("Release: YokaiWorldBootstrap missing from scene");

            EditorBuildSettingsScene[] enabled = EditorBuildSettings.scenes.Where(x => x.enabled).ToArray();
            if (enabled.Length != 1 || enabled[0].path != ScenePath)
                throw new InvalidOperationException("Release: expected exactly one enabled vertical-slice scene");

            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
                throw new InvalidOperationException("Release: Android must use IL2CPP");
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
                throw new InvalidOperationException("Release: ARM64 target missing");
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != "com.yokaistudio.yokai")
                throw new InvalidOperationException("Release: Android application identifier incorrect");
            if (PlayerSettings.Android.bundleVersionCode < 190)
                throw new InvalidOperationException("Release: Android version code is stale (V1.9 requires >= 190)");

            string manifest = File.ReadAllText("Packages/manifest.json");
            foreach (string required in new[]{"com.unity.modules.physics","com.unity.modules.audio","com.unity.modules.animation","com.unity.modules.imgui","com.unity.modules.jsonserialize"})
                if (!manifest.Contains(required)) throw new InvalidOperationException("Release: package manifest missing " + required);

            Debug.Log("[YOKAI_RELEASE_QA] scene/bootstrap/audio/android/package validation PASS");
        }
    }
}
#endif
