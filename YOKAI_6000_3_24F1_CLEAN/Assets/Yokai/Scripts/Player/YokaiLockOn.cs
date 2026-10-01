using System.Collections.Generic;
using UnityEngine;

namespace Yokai
{
    public sealed class YokaiLockOn : MonoBehaviour
    {
        public float scanRadius = 20f;
        public float loseRadius = 25f;
        public float maxAngle = 80f;
        public LayerMask targetMask = ~0;

        IYokaiTargetable currentTarget;
        Transform current;

        public Transform Current { get { return current; } }
        public bool IsLocked { get { return current != null; } }

        public void Toggle(Transform cameraTransform)
        {
            if (current != null) Clear();
            else Acquire(cameraTransform);
        }

        public void Acquire(Transform cameraTransform)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, scanRadius, targetMask, QueryTriggerInteraction.Ignore);
            HashSet<int> seen = new HashSet<int>();
            Vector3 camForward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            float best = float.MaxValue;
            IYokaiTargetable selected = null;

            for (int i = 0; i < colliders.Length; i++)
            {
                MonoBehaviour[] behaviours = colliders[i].GetComponentsInParent<MonoBehaviour>();
                for (int j = 0; j < behaviours.Length; j++)
                {
                    IYokaiTargetable t = behaviours[j] as IYokaiTargetable;
                    if (t == null || !t.CanTarget) continue;
                    int id = behaviours[j].GetInstanceID();
                    if (!seen.Add(id)) break;

                    Vector3 to = t.AimPoint.position - transform.position;
                    float dist = to.magnitude;
                    float angle = Vector3.Angle(camForward, to);
                    if (angle <= maxAngle)
                    {
                        float score = dist + angle * .12f - t.TargetPriority * 3f;
                        if (score < best)
                        {
                            best = score;
                            selected = t;
                        }
                    }
                    break;
                }
            }

            Set(selected);
        }

        public void Cycle(Transform cameraTransform, int direction)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, scanRadius, targetMask, QueryTriggerInteraction.Ignore);
            System.Collections.Generic.List<IYokaiTargetable> targets = new System.Collections.Generic.List<IYokaiTargetable>();
            HashSet<int> seen = new HashSet<int>();

            for (int i=0;i<colliders.Length;i++)
            {
                MonoBehaviour[] behaviours = colliders[i].GetComponentsInParent<MonoBehaviour>();
                for (int j=0;j<behaviours.Length;j++)
                {
                    IYokaiTargetable t = behaviours[j] as IYokaiTargetable;
                    if (t == null || !t.CanTarget) continue;
                    int id = behaviours[j].GetInstanceID();
                    if (seen.Add(id)) targets.Add(t);
                    break;
                }
            }

            if (targets.Count == 0) { Clear(); return; }

            Vector3 camForward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            IYokaiTargetable best = null;
            float bestDelta = float.MaxValue;

            for (int i=0;i<targets.Count;i++)
            {
                IYokaiTargetable t = targets[i];
                if (currentTarget != null && object.ReferenceEquals(t, currentTarget)) continue;

                Vector3 to = t.AimPoint.position - transform.position;
                float signed = Vector3.SignedAngle(camForward, to, Vector3.up);

                if (direction > 0 && signed <= 2f) signed += 360f;
                if (direction < 0 && signed >= -2f) signed -= 360f;

                float score = direction > 0 ? signed : -signed;
                if (score >= 0f && score < bestDelta)
                {
                    bestDelta = score;
                    best = t;
                }
            }

            if (best == null)
            {
                Clear();
                Acquire(cameraTransform);
            }
            else Set(best);
        }

        void Update()
        {
            if (currentTarget == null || !currentTarget.CanTarget)
            {
                Clear();
                return;
            }

            if (current != null && Vector3.Distance(transform.position, current.position) > loseRadius)
                Clear();
        }

        void Set(IYokaiTargetable target)
        {
            currentTarget = target;
            current = target != null ? target.AimPoint : null;
        }

        public void Clear()
        {
            currentTarget = null;
            current = null;
        }
    }
}
