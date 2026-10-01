using UnityEngine;

namespace Yokai
{
    public enum YokaiQualityProfile { Performance30, Balanced45, High60 }

    public sealed class YokaiPerformanceManager : MonoBehaviour
    {
        public static YokaiPerformanceManager Instance { get; private set; }
        public YokaiQualityProfile ActiveProfile { get; private set; }
        public float SmoothedFps { get; private set; }

        float sampleTime;
        int sampleFrames;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            int stored = YokaiSaveSystem.Data.qualityProfile;
            if (stored >= 0 && stored <= 2) Apply((YokaiQualityProfile)stored);
            else AutoSelect();
            SmoothedFps = Application.targetFrameRate;
        }

        void Update()
        {
            sampleFrames++;
            sampleTime += Time.unscaledDeltaTime;
            if (sampleTime >= 1f)
            {
                float fps = sampleFrames / Mathf.Max(0.001f, sampleTime);
                SmoothedFps = Mathf.Lerp(SmoothedFps, fps, 0.35f);
                sampleFrames = 0;
                sampleTime = 0f;
            }
        }

        public void AutoSelect()
        {
            int ram = SystemInfo.systemMemorySize;
            int vram = SystemInfo.graphicsMemorySize;
            if (ram <= 6000 || vram <= 1800) Apply(YokaiQualityProfile.Performance30);
            else if (ram <= 9000 || vram <= 3500) Apply(YokaiQualityProfile.Balanced45);
            else Apply(YokaiQualityProfile.High60);
        }

        public void Apply(YokaiQualityProfile profile)
        {
            ActiveProfile = profile;
            QualitySettings.vSyncCount = 0;
            if (profile == YokaiQualityProfile.Performance30)
            {
                Application.targetFrameRate = 30;
                QualitySettings.shadowDistance = 28f;
                QualitySettings.lodBias = 0.7f;
                QualitySettings.antiAliasing = 0;
            }
            else if (profile == YokaiQualityProfile.Balanced45)
            {
                Application.targetFrameRate = 45;
                QualitySettings.shadowDistance = 42f;
                QualitySettings.lodBias = 1f;
                QualitySettings.antiAliasing = 2;
            }
            else
            {
                Application.targetFrameRate = 60;
                QualitySettings.shadowDistance = 55f;
                QualitySettings.lodBias = 1.25f;
                QualitySettings.antiAliasing = 2;
            }
            YokaiSaveSystem.Data.qualityProfile = (int)profile;
            YokaiSaveSystem.Save();
        }

        public void CycleProfile()
        {
            Apply((YokaiQualityProfile)(((int)ActiveProfile + 1) % 3));
        }
    }
}
