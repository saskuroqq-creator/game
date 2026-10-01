using System.Collections.Generic;
using UnityEngine;
namespace Yokai
{
    [RequireComponent(typeof(YokaiCombat))]
    [RequireComponent(typeof(YokaiMotor))]
    public sealed class YokaiAnimatorBridge : MonoBehaviour
    {
        public Animator animator;
        public bool useRootMotionForAuthoredAttacks = false;
        YokaiCombat combat;
        YokaiMotor motor;
        YokaiLockOn lockOn;
        Animator cachedAnimator;
        RuntimeAnimatorController cachedController;
        readonly Dictionary<int, AnimatorControllerParameterType> parameters = new Dictionary<int, AnimatorControllerParameterType>();
        int previousVersion = -1;
        void Awake()
        {
            combat = GetComponent<YokaiCombat>();
            motor = GetComponent<YokaiMotor>();
            lockOn = GetComponent<YokaiLockOn>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }
        void Update()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            if (cachedAnimator != animator || cachedController != animator.runtimeAnimatorController)
            {
                cachedAnimator = animator;
                cachedController = animator.runtimeAnimatorController;
                parameters.Clear();
                foreach (var parameter in animator.parameters) parameters[parameter.nameHash] = parameter.type;
            }
            animator.applyRootMotion = false; // CharacterController remains authoritative.
            Float("Speed", motor.PlanarSpeed);
            Float("MoveX", motor.LocalMove.x);
            Float("MoveY", motor.LocalMove.y);
            Float("DodgeX", motor.LocalDodge.x);
            Float("DodgeY", motor.LocalDodge.y);
            Float("ActionNormalized", combat.ActionNormalized);
            Integer("ActionState", (int)combat.State);
            Integer("Stance", (int)combat.Stance);
            Integer("AttackIndex", combat.ComboIndex);
            Integer("AirComboIndex", combat.AirComboIndex);
            var skills = GetComponent<YokaiSkills>();
            Integer("SkillIndex", skills != null ? skills.PresentationIndex : 0);
            Float("VerticalSpeed", motor.VerticalSpeed);
            Bool("Airborne", motor.IsAirborne);
            Integer("ActionVersion", combat.ActionVersion);
            Bool("Grounded", motor.Grounded);
            Bool("LockedOn", lockOn != null && lockOn.IsLocked);
            if (previousVersion != combat.ActionVersion)
            {
                previousVersion = combat.ActionVersion;
                int hash = Animator.StringToHash("ActionChanged");
                if (Has(hash, AnimatorControllerParameterType.Trigger)) animator.SetTrigger(hash);
            }
        }
        bool Has(int hash, AnimatorControllerParameterType type)
        {
            AnimatorControllerParameterType found;
            return parameters.TryGetValue(hash, out found) && found == type;
        }
        void Float(string name, float value) { int h = Animator.StringToHash(name); if (Has(h, AnimatorControllerParameterType.Float)) animator.SetFloat(h, value); }
        void Integer(string name, int value) { int h = Animator.StringToHash(name); if (Has(h, AnimatorControllerParameterType.Int)) animator.SetInteger(h, value); }
        void Bool(string name, bool value) { int h = Animator.StringToHash(name); if (Has(h, AnimatorControllerParameterType.Bool)) animator.SetBool(h, value); }
    }
}
