using System;
using System.IO;
using UnityEngine;

namespace Yokai
{
    [Serializable]
    public sealed class YokaiSaveData
    {
        public float checkpointX = 0f;
        public float checkpointY = 1f;
        public float checkpointZ = -16f;
        public bool shrineActivated = false;
        public bool bossDefeated = false;
        public int qualityProfile = -1;
        public float cameraSensitivity = 1f;
    }

    public static class YokaiSaveSystem
    {
        static YokaiSaveData data;
        static string SavePath { get { return Path.Combine(Application.persistentDataPath, "yokai_save_v10.json"); } }

        public static YokaiSaveData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        public static void Load()
        {
            data = null;
            try
            {
                if (File.Exists(SavePath))
                    data = JsonUtility.FromJson<YokaiSaveData>(File.ReadAllText(SavePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[YOKAI] Save recovery: " + e.Message);
            }
            if (data == null) data = new YokaiSaveData();
        }

        public static void Save()
        {
            if (data == null) data = new YokaiSaveData();
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[YOKAI] Save failed: " + e.Message);
            }
        }

        public static Vector3 GetCheckpoint()
        {
            return new Vector3(Data.checkpointX, Data.checkpointY, Data.checkpointZ);
        }

        public static void SetCheckpoint(Vector3 p)
        {
            Data.checkpointX = p.x;
            Data.checkpointY = p.y;
            Data.checkpointZ = p.z;
            Data.shrineActivated = true;
            Save();
        }

        public static void SetBossDefeated(bool value)
        {
            Data.bossDefeated = value;
            Save();
        }

        public static void ResetProgress()
        {
            data = new YokaiSaveData();
            Save();
        }
    }
}
