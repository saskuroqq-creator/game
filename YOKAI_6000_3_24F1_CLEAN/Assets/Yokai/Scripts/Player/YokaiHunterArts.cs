using System.Collections;
using UnityEngine;

namespace Yokai
{
    [RequireComponent(typeof(YokaiAttributes))]
    public sealed class YokaiHunterArts : MonoBehaviour
    {
        public YokaiElement ActiveArt { get; private set; }
        public float cooldown = 5f;
        public float spiritCost = 30f;
        public float radius = 3.5f;
        public LayerMask hitMask = ~0;

        YokaiAttributes attributes;
        float readyAt;

        public float CooldownRemaining { get { return Mathf.Max(0f, readyAt - Time.time); } }

        void Awake()
        {
            ActiveArt = YokaiElement.Fire;
            attributes = GetComponent<YokaiAttributes>();
        }

        public void Cycle()
        {
            if (ActiveArt == YokaiElement.Fire) ActiveArt = YokaiElement.Storm;
            else if (ActiveArt == YokaiElement.Storm) ActiveArt = YokaiElement.Spirit;
            else if (ActiveArt == YokaiElement.Spirit) ActiveArt = YokaiElement.Shadow;
            else ActiveArt = YokaiElement.Fire;
        }

        public bool TryCast(int expectedActionVersion = -1)
        {
            if (attributes.IsDead || Time.time < readyAt || !attributes.ConsumeSpirit(spiritCost)) return false;
            readyAt = Time.time + cooldown;
            StartCoroutine(CastRoutine(expectedActionVersion, ActiveArt));
            return true;
        }

        public void Cancel() { StopAllCoroutines(); }

        IEnumerator CastRoutine(int expectedVersion, YokaiElement castArt)
        {
            Color color = YokaiVfx.ElementColor(castArt);
            YokaiVfx.Telegraph(transform.position, radius * .4f, color, .3f);
            yield return new WaitForSeconds(.27f);

            YokaiCombat combat = GetComponent<YokaiCombat>();
            if (attributes.IsDead || expectedVersion >= 0 &&
                (combat == null || combat.ActionVersion != expectedVersion)) yield break;

            float damage = attributes.attackPower * 1.55f;
            float posture = 24f;

            if (castArt == YokaiElement.Fire) damage *= 1.25f;
            else if (castArt == YokaiElement.Storm) posture *= 1.7f;
            else if (castArt == YokaiElement.Spirit)
            {
                attributes.Heal(attributes.maxHealth * .12f);
                attributes.AddBladeFlow(10f);
                damage *= .65f;
            }

            Collider[] hits = Physics.OverlapSphere(transform.position, radius, hitMask, QueryTriggerInteraction.Ignore);
            System.Collections.Generic.HashSet<int> damaged = new System.Collections.Generic.HashSet<int>();
            System.Collections.Generic.HashSet<int> reacted = new System.Collections.Generic.HashSet<int>();

            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i];
                if (c == null || c.transform.root == transform.root) continue;

                MonoBehaviour[] list = c.GetComponentsInParent<MonoBehaviour>();
                for (int j = 0; j < list.Length; j++)
                {
                    IYokaiDamageReceiver receiver = list[j] as IYokaiDamageReceiver;
                    if (receiver != null && damaged.Add(list[j].GetInstanceID()))
                    {
                        receiver.ReceiveHit(new YokaiHit(gameObject, damage, posture, c.ClosestPoint(transform.position),
                            (c.transform.position-transform.position).normalized, castArt, true, false));
                    }

                    IYokaiElementReceiver elementReceiver = list[j] as IYokaiElementReceiver;
                    if (elementReceiver != null && reacted.Add(list[j].GetInstanceID()))
                        elementReceiver.ApplyElement(castArt);
                }
            }

            YokaiVfx.Ring(transform.position, color, radius, .5f);
            YokaiVfx.Burst(transform.position + Vector3.up, color, .6f, .4f);
        }
    }
}
