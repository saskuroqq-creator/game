using UnityEngine;

namespace Yokai
{
    public sealed class YokaiProjectile : MonoBehaviour
    {
        Vector3 direction;
        float speed;
        float damage;
        float posture;
        float life;
        GameObject owner;
        YokaiElement element;
        bool activeProjectile;

        public void Launch(GameObject source, Vector3 origin, Vector3 dir, float projectileSpeed,
            float projectileDamage, float postureDamage, YokaiElement hitElement, float lifetime)
        {
            owner = source;
            transform.position = origin;
            direction = dir.normalized;
            speed = projectileSpeed;
            damage = projectileDamage;
            posture = postureDamage;
            element = hitElement;
            life = lifetime;
            activeProjectile = true;
            gameObject.SetActive(true);
        }

        void Update()
        {
            if (!activeProjectile) return;

            float step = speed * Time.deltaTime;
            RaycastHit hit;
            if (Physics.SphereCast(transform.position, .18f, direction, out hit, step, ~0, QueryTriggerInteraction.Ignore))
            {
                if (owner != null && hit.transform.root == owner.transform.root)
                {
                    transform.position += direction * step;
                    return;
                }

                MonoBehaviour[] behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>();
                for (int i=0;i<behaviours.Length;i++)
                {
                    IYokaiDamageReceiver receiver = behaviours[i] as IYokaiDamageReceiver;
                    if (receiver != null)
                    {
                        receiver.ReceiveHit(new YokaiHit(owner, damage, posture, hit.point, direction, element, true, false));
                        break;
                    }
                }

                YokaiVfx.Burst(hit.point, YokaiVfx.ElementColor(element), .22f, .2f);
                Deactivate();
                return;
            }

            transform.position += direction * step;
            life -= Time.deltaTime;
            if (life <= 0f) Deactivate();
        }

        public void Deactivate()
        {
            activeProjectile = false;
            gameObject.SetActive(false);
            if (YokaiProjectilePool.Instance != null)
                YokaiProjectilePool.Instance.Return(this);
        }
    }
}
