using System.Collections.Generic;
using UnityEngine;

namespace Yokai
{
    public static class YokaiMaterialLibrary
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static Material Get(string key, Color color, float metallic, float smoothness)
        {
            Material found;
            if (cache.TryGetValue(key, out found) && found != null) return found;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");

            Material mat = new Material(shader);
            mat.name = "YOKAI_" + key;
            mat.enableInstancing = true;
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            cache[key] = mat;
            return mat;
        }

        public static Material Get(string key, Color color)
        {
            return Get(key, color, 0f, 0.35f);
        }

        public static Material Emissive(string key, Color color)
        {
            Material mat = Get(key, color, 0f, 0.3f);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 1.75f);
            }
            return mat;
        }
    }
}
