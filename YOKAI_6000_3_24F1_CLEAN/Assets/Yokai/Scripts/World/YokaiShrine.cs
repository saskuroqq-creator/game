using UnityEngine;

namespace Yokai
{
    public sealed class YokaiShrine : MonoBehaviour
    {
        public Transform checkpointPoint;
        public bool activated;

        Renderer[] renderers;

        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
        }

        public void Activate(YokaiPlayerController player)
        {
            if (player == null) return;

            activated = true;
            Vector3 point = checkpointPoint != null ? checkpointPoint.position : transform.position + transform.forward*1.5f;
            YokaiSaveSystem.SetCheckpoint(point);

            YokaiAttributes a = player.GetComponent<YokaiAttributes>();
            YokaiCombat combat = player.GetComponent<YokaiCombat>();
            if (a != null)
            {
                a.RestoreFull();
                a.AddSpirit(a.maxSpirit);
                a.AddBladeFlow(-100f);
            }
            if (combat != null) combat.RefillHealing();

            for (int i=0;i<renderers.Length;i++)
            {
                if (renderers[i] != null)
                    renderers[i].sharedMaterial = YokaiMaterialLibrary.Emissive("shrine_active", new Color(.2f,.85f,.68f));
            }

            YokaiVfx.Ring(transform.position, new Color(.2f,.9f,.7f), 2.2f, .7f);

            if (YokaiGameSession.Instance != null)
                YokaiGameSession.Instance.SetStage(YokaiGameStage.Boss, "Enter the red gate and defeat the Oni Warden.");
        }
    }
}
