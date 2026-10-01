using System.Collections;
using UnityEngine;

namespace Yokai
{
    public sealed class YokaiElementReactive : MonoBehaviour, IYokaiElementReceiver
    {
        public float reactionDuration = 3.5f;

        Renderer[] renderers;
        Material[] originalMaterials;
        Coroutine reaction;

        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            originalMaterials = new Material[renderers.Length];
            for (int i=0;i<renderers.Length;i++)
                originalMaterials[i] = renderers[i] != null ? renderers[i].sharedMaterial : null;
        }

        public void ApplyElement(YokaiElement element)
        {
            if (reaction != null) StopCoroutine(reaction);
            reaction = StartCoroutine(ReactionRoutine(element));
        }

        IEnumerator ReactionRoutine(YokaiElement element)
        {
            Color color = YokaiVfx.ElementColor(element);
            float duration = reactionDuration;

            if (element == YokaiElement.Fire) duration = 5f;
            else if (element == YokaiElement.Storm) duration = 2.1f;
            else if (element == YokaiElement.Spirit) duration = 4f;
            else if (element == YokaiElement.Shadow) duration = 4.5f;

            for (int i=0;i<renderers.Length;i++)
            {
                if (renderers[i] != null)
                    renderers[i].sharedMaterial =
                        YokaiMaterialLibrary.Emissive("env_react_" + GetInstanceID() + "_" + element + "_" + i, color);
            }

            YokaiVfx.Burst(transform.position + Vector3.up*.8f, color, .22f, .25f);
            yield return new WaitForSeconds(duration);

            for (int i=0;i<renderers.Length;i++)
            {
                if (renderers[i] != null && i < originalMaterials.Length && originalMaterials[i] != null)
                    renderers[i].sharedMaterial = originalMaterials[i];
            }

            reaction = null;
        }
    }
}
