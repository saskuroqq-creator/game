using UnityEngine;

namespace Yokai
{
    public sealed class YokaiSpiritWisp : MonoBehaviour
    {
        public float spiritReward = 18f;
        public float bladeFlowReward = 8f;
        public float bobHeight = .18f;
        public float bobSpeed = 2.2f;
        public float pickupRadius = 1.25f;

        Vector3 basePosition;
        float seed;
        bool collected;

        void Start()
        {
            basePosition = transform.position;
            seed = Random.Range(0f, 10f);
        }

        void Update()
        {
            if (collected) return;

            transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time*bobSpeed + seed)*bobHeight);

            YokaiGameSession session = YokaiGameSession.Instance;
            if (session == null || session.player == null) return;

            if (Vector3.Distance(transform.position, session.player.position) <= pickupRadius)
            {
                collected = true;
                YokaiAttributes a = session.player.GetComponent<YokaiAttributes>();
                if (a != null)
                {
                    a.AddSpirit(spiritReward);
                    a.AddBladeFlow(bladeFlowReward);
                }

                YokaiVfx.Burst(transform.position, new Color(.28f,.9f,1f), .28f, .35f);
                Destroy(gameObject);
            }
        }
    }
}
