using UnityEngine;

namespace Yokai
{
    public sealed class YokaiCameraRig : MonoBehaviour
    {
        public Transform target;
        public YokaiLockOn lockOn;
        public Vector3 pivotOffset = new Vector3(0f,1.45f,0f);
        public float distance = 4.4f;
        public float lockDistance = 5.0f;
        public float sensitivity = 115f;
        public float pitchMin = -28f;
        public float pitchMax = 56f;
        public float smooth = 15f;
        public float collisionRadius = .22f;
        public LayerMask collisionMask = ~0;

        float yaw;
        float pitch = 12f;
        Vector2 queuedLook;
        float trauma;

        public void Attach(Transform followTarget, YokaiLockOn targetLock)
        {
            target = followTarget;
            lockOn = targetLock;
            yaw = transform.eulerAngles.y;
        }

        public void AddLook(Vector2 delta)
        {
            queuedLook += delta;
        }

        public void AddTrauma(float amount)
        {
            trauma = Mathf.Clamp01(trauma + Mathf.Max(0f, amount));
        }

        void LateUpdate()
        {
            if (target == null) return;

            if (lockOn != null && lockOn.IsLocked && lockOn.Current != null)
            {
                Vector3 targetPoint = target.position + pivotOffset;
                Vector3 enemyPoint = lockOn.Current.position;
                Vector3 between = Vector3.Lerp(targetPoint, enemyPoint, .28f);
                Vector3 toEnemy = enemyPoint - targetPoint;
                yaw = Mathf.Atan2(toEnemy.x, toEnemy.z) * Mathf.Rad2Deg;
                pitch = Mathf.Lerp(pitch, 10f, 8f * Time.deltaTime);
                PlaceCamera(between, lockDistance);
            }
            else
            {
                yaw += queuedLook.x * sensitivity * .012f;
                pitch = Mathf.Clamp(pitch - queuedLook.y * sensitivity * .012f, pitchMin, pitchMax);
                PlaceCamera(target.position + pivotOffset, distance);
            }

            queuedLook = Vector2.zero;
            trauma = Mathf.MoveTowards(trauma, 0f, Time.unscaledDeltaTime * 1.9f);
        }

        void PlaceCamera(Vector3 pivot, float desiredDistance)
        {
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 back = -(rot * Vector3.forward);
            float finalDistance = desiredDistance;
            RaycastHit[] hits = Physics.SphereCastAll(pivot, collisionRadius, back, desiredDistance, collisionMask, QueryTriggerInteraction.Ignore);
            float nearest = desiredDistance;
            for (int i=0;i<hits.Length;i++)
            {
                if (target != null && hits[i].transform != null && hits[i].transform.root == target.root)
                    continue;
                if (hits[i].distance < nearest) nearest = hits[i].distance;
            }
            if (nearest < desiredDistance)
                finalDistance = Mathf.Max(.6f, nearest - .12f);

            Vector3 desired = pivot + back * finalDistance;

            if (trauma > 0.001f)
            {
                float amp = trauma * trauma;
                desired += new Vector3(
                    (Mathf.PerlinNoise(Time.unscaledTime*31f, 2.1f)-.5f)*.18f*amp,
                    (Mathf.PerlinNoise(7.2f, Time.unscaledTime*27f)-.5f)*.13f*amp,
                    0f);
            }

            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-smooth * Time.deltaTime));
            transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        }
    }
}
