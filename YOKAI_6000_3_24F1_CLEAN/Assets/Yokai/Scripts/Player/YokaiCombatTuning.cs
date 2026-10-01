using System;
using UnityEngine;
namespace Yokai
{
    [Serializable]
    public sealed class YokaiAttackDefinition
    {
        public float duration = .42f;
        public float stamina = 7f;
        public float damageMultiplier = 1f;
        public float posture = 13f;
        [Range(0f,1f)] public float contact = .34f;
    }
    [CreateAssetMenu(menuName = "Yokai/Combat Tuning")]
    public sealed class YokaiCombatTuning : ScriptableObject
    {
        public YokaiAttackDefinition[] lightCombo = Defaults();
        [Range(.45f,.95f)] public float chainWindow = .62f;
        [Range(.35f,.95f)] public float lightCancel = .48f;
        [Range(.5f,.98f)] public float heavyCancel = .72f;
        public static YokaiAttackDefinition[] Defaults()
        {
            return new[] {
                new YokaiAttackDefinition { duration=.385f, damageMultiplier=1f, posture=13f },
                new YokaiAttackDefinition { duration=.410f, damageMultiplier=1.16f, posture=16f },
                new YokaiAttackDefinition { duration=.440f, damageMultiplier=1.32f, posture=19f },
                new YokaiAttackDefinition { duration=.480f, damageMultiplier=1.42f, posture=22f, stamina=9f, contact=.38f },
                new YokaiAttackDefinition { duration=.580f, damageMultiplier=1.75f, posture=30f, stamina=12f, contact=.44f }
            };
        }
    }
}
