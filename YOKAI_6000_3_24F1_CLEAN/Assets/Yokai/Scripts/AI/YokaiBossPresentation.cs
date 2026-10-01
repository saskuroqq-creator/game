using UnityEngine;

namespace Yokai
{
    [RequireComponent(typeof(YokaiBoss))]
    public sealed class YokaiBossPresentation : MonoBehaviour
    {
        YokaiBoss boss;
        YokaiAttributes attributes;
        Renderer[] renderers;
        int lastPhase;
        float pulse;

        void Awake()
        {
            boss = GetComponent<YokaiBoss>();
            attributes = GetComponent<YokaiAttributes>();
            renderers = GetComponentsInChildren<Renderer>();
            lastPhase = 1;
        }

        void Update()
        {
            if (boss == null || attributes == null || attributes.IsDead) return;

            pulse += Time.deltaTime*(boss.Phase==3 ? 4f : 2f);

            if (boss.Phase != lastPhase)
            {
                lastPhase = boss.Phase;
                ApplyPhaseMaterials();
            }

            float scalePulse = 1f + Mathf.Sin(pulse)*.015f*(boss.Phase-1);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one*scalePulse, Time.deltaTime*3f);
        }

        void ApplyPhaseMaterials()
        {
            Color c = boss.Phase == 2 ? new Color(.72f,.12f,.04f) :
                      boss.Phase == 3 ? new Color(.46f,.08f,.72f) :
                      new Color(.18f,.08f,.07f);

            for (int i=0;i<renderers.Length;i++)
            {
                if (renderers[i] == null) continue;
                if (renderers[i].gameObject.name.Contains("Katana")) continue;
                renderers[i].sharedMaterial =
                    YokaiMaterialLibrary.Get("boss_phase_"+boss.Phase+"_"+i, c, .18f, .4f);
            }

            YokaiVfx.Burst(transform.position+Vector3.up*1.4f, YokaiVfx.ElementColor(
                boss.Phase==2 ? YokaiElement.Fire : YokaiElement.Shadow), .7f, .5f);
        }
    }
}
