using System.Collections.Generic;
using UnityEngine;
namespace Yokai
{
    public static class YokaiAreaDamage
    {
        static readonly Collider[] hits = new Collider[96];
        static readonly HashSet<int> seen = new HashSet<int>();
        // Dive command has no damage; the existing landing shockwave owns damage.
        public static void SlamAirborne(Transform source, float radius, LayerMask mask)
        {
            Vector3 center = source.position + Vector3.up;
            int count = Physics.OverlapSphereNonAlloc(center, radius, hits, mask, QueryTriggerInteraction.Ignore);
            seen.Clear();
            for (int i = 0; i < count; i++)
            {
                if (hits[i] == null) continue;
                YokaiEnemy enemy = hits[i].GetComponentInParent<YokaiEnemy>();
                if (enemy == null || !enemy.CanTarget || !enemy.IsAirborne || !seen.Add(enemy.GetInstanceID())) continue;
                if (!YokaiCombatGeometry.Visible(center, enemy.AimPoint.position, source, enemy.transform)) continue;
                enemy.SlamDown(source.forward);
            }
        }

        public static int Apply(Transform source, float damage, float posture, float radius,
            YokaiElement element, bool heavy, LayerMask mask, float arc = 360f)
        {
            Vector3 center = source.position + Vector3.up;
            int count = Physics.OverlapSphereNonAlloc(center, radius, hits, mask, QueryTriggerInteraction.Ignore);
            seen.Clear();
            int accepted = 0;
            for (int i = 0; i < count; i++)
            {
                YokaiEnemy enemy = hits[i].GetComponentInParent<YokaiEnemy>();
                if (enemy == null || !enemy.CanTarget || seen.Contains(enemy.GetInstanceID())) continue;
                Vector3 point = hits[i].ClosestPoint(center);
                if (!YokaiCombatGeometry.InArc(source.position, source.forward, point, radius, arc) ||
                    !YokaiCombatGeometry.Visible(center, enemy.AimPoint.position, source, enemy.transform)) continue;
                seen.Add(enemy.GetInstanceID());
                Vector3 direction = enemy.transform.position - source.position;
                direction.y = 0f;
                if (direction.sqrMagnitude < .001f) direction = source.forward;
                if (enemy.ReceiveHit(new YokaiHit(source.gameObject, damage, posture, point,
                    direction.normalized, element, heavy, false))) accepted++;
            }
            return accepted;
        }
    }
}
