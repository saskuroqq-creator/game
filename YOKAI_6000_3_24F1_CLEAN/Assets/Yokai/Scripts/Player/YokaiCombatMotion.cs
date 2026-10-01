using UnityEngine;

namespace Yokai
{
    [RequireComponent(typeof(YokaiMotor))]
    [RequireComponent(typeof(YokaiCombat))]
    [RequireComponent(typeof(YokaiLockOn))]
    public sealed class YokaiCombatMotion : MonoBehaviour
    {
        public float lightLungeSpeed = 2.8f;
        public float heavyLungeSpeed = 4.1f;
        public float artLungeSpeed = 2.2f;
        public float maxWarpDistance = 2.9f;

        YokaiMotor motor;
        YokaiCombat combat;
        YokaiLockOn lockOn;

        void Awake()
        {
            motor = GetComponent<YokaiMotor>();
            combat = GetComponent<YokaiCombat>();
            lockOn = GetComponent<YokaiLockOn>();
        }

        void Update()
        {
            if (lockOn == null || !lockOn.IsLocked || lockOn.Current == null) return;

            Vector3 to = lockOn.Current.position - transform.position;
            to.y = 0f;
            float distance = to.magnitude;
            if (distance <= .85f || distance > maxWarpDistance) return;

            float n = combat.ActionNormalized;
            float speed = 0f;

            if (combat.State == YokaiActionState.Light && n >= .08f && n <= .48f)
                speed = lightLungeSpeed;
            else if (combat.State == YokaiActionState.Heavy && n >= .16f && n <= .58f)
                speed = heavyLungeSpeed;
            else if (combat.State == YokaiActionState.Art && n >= .12f && n <= .45f)
                speed = artLungeSpeed;

            if (speed > 0f)
            {
                motor.FaceDirection(to, .26f);
                motor.AddImpulse(to.normalized * speed * Time.deltaTime * 4.5f);
            }
        }
    }
}
