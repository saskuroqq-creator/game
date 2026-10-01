using UnityEngine;

namespace Yokai
{
    public sealed class YokaiElementalStatus : MonoBehaviour
    {
        YokaiAttributes attributes;
        float burnTimer;
        float burnTick;
        float stormTimer;
        float spiritSlowTimer;
        float shadowTimer;

        public float MoveMultiplier { get { return spiritSlowTimer > 0f ? .7f : 1f; } }
        public void Clear() { burnTimer = burnTick = stormTimer = spiritSlowTimer = shadowTimer = 0f; }
        public bool IsShadowMarked { get { return shadowTimer > 0f; } }

        void Awake()
        {
            attributes = GetComponent<YokaiAttributes>();
        }

        void Update()
        {
            if (burnTimer > 0f)
            {
                burnTimer -= Time.deltaTime;
                burnTick -= Time.deltaTime;
                if (burnTick <= 0f)
                {
                    burnTick = .5f;
                    attributes.ApplyHealthDamage(3f);
                    YokaiVfx.Burst(transform.position + Vector3.up, YokaiVfx.ElementColor(YokaiElement.Fire), .12f, .15f);
                }
            }

            stormTimer -= Time.deltaTime;
            spiritSlowTimer -= Time.deltaTime;
            shadowTimer -= Time.deltaTime;
        }

        public void Apply(YokaiElement element)
        {
            if (element == YokaiElement.Fire)
            {
                burnTimer = Mathf.Max(burnTimer, 4f);
                burnTick = .08f;
            }
            else if (element == YokaiElement.Storm)
            {
                stormTimer = Mathf.Max(stormTimer, 2.5f);
                attributes.DamagePosture(18f, 2f);
            }
            else if (element == YokaiElement.Spirit)
            {
                spiritSlowTimer = Mathf.Max(spiritSlowTimer, 3f);
            }
            else if (element == YokaiElement.Shadow)
            {
                shadowTimer = Mathf.Max(shadowTimer, 5f);
            }
        }

        public float IncomingDamageMultiplier()
        {
            return IsShadowMarked ? 1.2f : 1f;
        }
    }
}
