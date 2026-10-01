using System.Collections.Generic;
using UnityEngine;

namespace Yokai
{
    public sealed class YokaiAtmosphere : MonoBehaviour
    {
        readonly List<Transform> fireflies = new List<Transform>();
        readonly List<Vector3> anchors = new List<Vector3>();
        float seed;

        void Start()
        {
            seed = Random.Range(0f,100f);
            int count = 28;
            if (YokaiPerformanceManager.Instance != null)
            {
                if (YokaiPerformanceManager.Instance.ActiveProfile == YokaiQualityProfile.Performance30) count = 18;
                else if (YokaiPerformanceManager.Instance.ActiveProfile == YokaiQualityProfile.High60) count = 36;
            }
            BuildFireflies(count);
        }

        void BuildFireflies(int count)
        {
            Mesh fireflyMesh = YokaiMeshLibrary.LowSphere("firefly",6,3);
            Material fireflyMat = YokaiMaterialLibrary.Emissive("firefly_shared", new Color(.42f,.92f,.65f));
            for (int i=0;i<count;i++)
            {
                GameObject go = YokaiArtUtility.MeshPart("SpiritFirefly", transform, fireflyMesh, Vector3.zero,
                    Vector3.one*Random.Range(.055f,.095f), Quaternion.identity, fireflyMat);
                Renderer renderer = go.GetComponent<Renderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                float z = Random.Range(-10f,90f);
                float x = Random.Range(-7f,7f);
                float y = Random.Range(.5f,2.8f);
                go.transform.localPosition = new Vector3(x,y,z);

                fireflies.Add(go.transform);
                anchors.Add(go.transform.localPosition);
            }
        }

        void Update()
        {
            for (int i=0;i<fireflies.Count;i++)
            {
                Transform t = fireflies[i];
                if (t == null) continue;

                Vector3 a = anchors[i];
                float time = Time.time*(.5f + (i%5)*.08f) + seed + i*.73f;
                t.localPosition = a + new Vector3(
                    Mathf.Sin(time*1.31f)*.45f,
                    Mathf.Sin(time*1.87f)*.22f,
                    Mathf.Cos(time*.93f)*.38f);
            }
        }
    }
}
