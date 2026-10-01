using System.Collections.Generic;
using UnityEngine;

namespace Yokai
{
    public sealed class YokaiAttackCoordinator : MonoBehaviour
    {
        public static YokaiAttackCoordinator Instance { get; private set; }
        public int maxConcurrentAttackers = 2;

        readonly HashSet<int> attackers = new HashSet<int>();

        void Awake()
        {
            Instance = this;
        }

        public bool TryClaim(YokaiEnemy enemy)
        {
            if (enemy == null) return false;
            int id = enemy.GetInstanceID();
            if (attackers.Contains(id)) return true;
            if (attackers.Count >= maxConcurrentAttackers) return false;
            attackers.Add(id);
            return true;
        }

        public void Release(YokaiEnemy enemy)
        {
            if (enemy == null) return;
            attackers.Remove(enemy.GetInstanceID());
        }

        public void Clear()
        {
            attackers.Clear();
        }
    }
}
