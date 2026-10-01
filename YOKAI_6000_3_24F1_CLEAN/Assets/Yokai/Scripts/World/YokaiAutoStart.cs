using UnityEngine;

namespace Yokai
{
    public static class YokaiAutoStart
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureBootstrap()
        {
            if (Object.FindFirstObjectByType<YokaiWorldBootstrap>() != null) return;
            GameObject go = new GameObject("YOKAI_WORLD_BOOTSTRAP");
            go.AddComponent<YokaiWorldBootstrap>();
        }
    }
}
