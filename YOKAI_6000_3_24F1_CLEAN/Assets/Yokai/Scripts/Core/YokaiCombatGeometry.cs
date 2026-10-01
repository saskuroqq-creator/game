using UnityEngine;
namespace Yokai
{
    public static class YokaiCombatGeometry
    {
        static readonly RaycastHit[] visibilityHits = new RaycastHit[128];
        public static bool InArc(Vector3 origin, Vector3 forward, Vector3 point, float range, float arc)
        {
            Vector3 delta = point - origin;
            if (Mathf.Abs(delta.y) > 2.4f) return false;
            delta.y = forward.y = 0f;
            if (delta.sqrMagnitude > range * range) return false;
            return delta.sqrMagnitude < .0001f || Vector3.Angle(forward, delta) <= arc * .5f;
        }
        public static bool Visible(Vector3 from, Vector3 to, Transform source, Transform target)
        {
            // Check every hit to avoid treating the attacker's own controller as a wall.
            int count = Physics.RaycastNonAlloc(from, to - from, visibilityHits, Vector3.Distance(from, to),
                ~0, QueryTriggerInteraction.Ignore);
            if (count == visibilityHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Transform t = visibilityHits[i].transform;
                if (t.IsChildOf(source) || t.IsChildOf(target)) continue;
                // Other combatants are cleavable; static collision blocks the blade.
                if (t.GetComponentInParent<IYokaiDamageReceiver>() != null) continue;
                return false;
            }
            return true;
        }
    }
}
