using UnityEngine;

namespace Yokai
{
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(YokaiHumanoidVisual))]
    public sealed class YokaiFootGrounding : MonoBehaviour
    {
        public float rayStart = .55f;
        public float rayLength = 1.3f;
        public float footOffset = .08f;
        public LayerMask groundMask = ~0;

        YokaiHumanoidVisual visual;
        YokaiMotor motor;
        YokaiEnemy enemy;

        void Awake()
        {
            visual = GetComponent<YokaiHumanoidVisual>();
            motor = GetComponent<YokaiMotor>();
            enemy = GetComponent<YokaiEnemy>();
        }

        void LateUpdate()
        {
            if (visual == null || (motor != null && motor.IsAirborne) || (enemy != null && enemy.IsAirborne)) return;
            AlignLeg(visual.leftLowerLeg);
            AlignLeg(visual.rightLowerLeg);
        }

        void AlignLeg(Transform lowerLeg)
        {
            if (lowerLeg == null) return;

            Vector3 origin = lowerLeg.position + Vector3.up*rayStart;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, rayLength, groundMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0) return;

            bool found = false;
            RaycastHit best = new RaycastHit();
            float nearest = float.MaxValue;

            for (int i=0;i<hits.Length;i++)
            {
                if (hits[i].transform == null || hits[i].transform.root == transform.root) continue;
                if (hits[i].distance < nearest)
                {
                    nearest = hits[i].distance;
                    best = hits[i];
                    found = true;
                }
            }

            if (!found) return;

            Vector3 localNormal = lowerLeg.parent.InverseTransformDirection(best.normal);
            Quaternion target = Quaternion.FromToRotation(Vector3.up, localNormal);
            lowerLeg.localRotation = Quaternion.Slerp(lowerLeg.localRotation, target*lowerLeg.localRotation, Time.deltaTime*5f);
        }
    }
}
