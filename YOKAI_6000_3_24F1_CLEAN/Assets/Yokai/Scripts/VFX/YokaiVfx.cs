using UnityEngine;

namespace Yokai
{
    public static class YokaiVfx
    {
        public static Color ElementColor(YokaiElement element)
        {
            if (element == YokaiElement.Fire) return new Color(1f, 0.23f, 0.04f);
            if (element == YokaiElement.Storm) return new Color(0.15f, 0.62f, 1f);
            if (element == YokaiElement.Spirit) return new Color(0.22f, 1f, 0.72f);
            if (element == YokaiElement.Shadow) return new Color(0.55f, 0.18f, 0.85f);
            return new Color(1f, 0.88f, 0.62f);
        }

        public static void Burst(Vector3 position, Color color, float scale, float lifetime)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "VFX_Burst";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = position;
            go.transform.localScale = Vector3.one * scale;
            go.GetComponent<Renderer>().sharedMaterial = YokaiMaterialLibrary.Emissive("burst_" + color.ToString(), color);
            YokaiVfxLife life = go.AddComponent<YokaiVfxLife>();
            life.Setup(lifetime, scale * 2.4f, true);
        }

        public static void Ring(Vector3 position, Color color, float radius, float lifetime)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "VFX_Ring";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = position + Vector3.up * 0.04f;
            go.transform.localScale = new Vector3(radius, 0.025f, radius);
            go.GetComponent<Renderer>().sharedMaterial = YokaiMaterialLibrary.Emissive("ring_" + color.ToString(), color);
            YokaiVfxLife life = go.AddComponent<YokaiVfxLife>();
            life.Setup(lifetime, radius * 1.18f, true);
        }

        public static void Telegraph(Vector3 position, float radius, Color color, float lifetime)
        {
            Ring(position, color, radius, lifetime);
        }
    }

    public sealed class YokaiVfxLife : MonoBehaviour
    {
        float remaining;
        float total;
        float targetScale;
        bool fadeScale;

        public void Setup(float lifetime, float finalScale, bool scaleOut)
        {
            remaining = total = Mathf.Max(0.05f, lifetime);
            targetScale = finalScale;
            fadeScale = scaleOut;
        }

        void Update()
        {
            remaining -= Time.deltaTime;
            if (fadeScale)
            {
                float t = 1f - Mathf.Clamp01(remaining / total);
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * targetScale, Time.deltaTime * (2f + t * 8f));
            }
            if (remaining <= 0f) Destroy(gameObject);
        }
    }
}
