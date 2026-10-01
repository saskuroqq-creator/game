using UnityEngine;

namespace Yokai
{
    public static class YokaiArtUtility
    {
        public static GameObject MeshPart(string name, Transform parent, Mesh mesh, Vector3 localPosition,
            Vector3 localScale, Quaternion localRotation, Material material, bool boxCollider = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = localScale;
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            if (boxCollider)
            {
                BoxCollider c = go.AddComponent<BoxCollider>();
                c.center = mesh.bounds.center;
                c.size = mesh.bounds.size;
            }
            return go;
        }

        public static LODGroup AddTwoLevelLod(GameObject root, Renderer[] high, Renderer[] low, float lowStart = .10f, float cull = .018f)
        {
            LODGroup group = root.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.CrossFade;
            group.animateCrossFading = false;
            group.SetLODs(new[]
            {
                new LOD(Mathf.Clamp(lowStart, .03f, .5f), high),
                new LOD(Mathf.Clamp(cull, .005f, lowStart - .005f), low)
            });
            group.RecalculateBounds();
            return group;
        }
    }
}
