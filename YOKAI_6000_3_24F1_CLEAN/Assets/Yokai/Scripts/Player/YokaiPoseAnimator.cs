using UnityEngine;

namespace Yokai
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(YokaiHumanoidVisual))]
    public sealed class YokaiPoseAnimator : MonoBehaviour
    {
        YokaiHumanoidVisual visual;
        YokaiMotor motor;
        YokaiCombat combat;
        YokaiEnemy enemy;
        YokaiAttributes attributes;

        float gait;
        float breathe;
        float poseBlend;
        float lastSpeed;
        float deathBlend;
        Vector3 baseChestLocalPosition;

        void Awake()
        {
            visual = GetComponent<YokaiHumanoidVisual>();
            motor = GetComponent<YokaiMotor>();
            combat = GetComponent<YokaiCombat>();
            enemy = GetComponent<YokaiEnemy>();
            attributes = GetComponent<YokaiAttributes>();
            if (visual != null && visual.chest != null)
                baseChestLocalPosition = visual.chest.localPosition;
        }

        void LateUpdate()
        {
            if (visual == null || visual.chest == null || attributes == null) return;

            if (attributes.IsDead)
            {
                deathBlend = Mathf.MoveTowards(deathBlend, 1f, Time.deltaTime * 1.8f);
                ApplyDeathPose(deathBlend);
                return;
            }

            deathBlend = 0f;
            breathe += Time.deltaTime * 1.7f;

            float speed = motor != null ? motor.PlanarSpeed : EstimateEnemySpeed();
            lastSpeed = Mathf.Lerp(lastSpeed, speed, Time.deltaTime * 8f);
            gait += Time.deltaTime * Mathf.Lerp(2.4f, 9.2f, Mathf.Clamp01(lastSpeed/7f));

            YokaiActionState state = combat != null ? combat.State :
                (enemy != null && enemy.IsAttacking ? YokaiActionState.Light : YokaiActionState.Free);

            float actionN = combat != null ? combat.ActionNormalized : 0.5f;
            poseBlend = Mathf.MoveTowards(poseBlend, state == YokaiActionState.Free ? 0f : 1f, Time.deltaTime * 8f);

            ResetNeutral();

            if (state == YokaiActionState.Free || state == YokaiActionState.Guard)
                ApplyLocomotion(lastSpeed, state == YokaiActionState.Guard);

            if (combat != null)
            {
                if (state == YokaiActionState.Light) ApplyLight(actionN, combat.ComboIndex);
                else if (state == YokaiActionState.AirLight) ApplyLight(actionN, combat.AirComboIndex);
                else if (state == YokaiActionState.Launcher || state == YokaiActionState.AirSlam) ApplyHeavy(actionN);
                else if (state == YokaiActionState.Skill) ApplyArt(actionN);
                else if (state == YokaiActionState.Jump || state == YokaiActionState.Landing) ApplyAirPose(state == YokaiActionState.Landing);
                else if (state == YokaiActionState.Heavy) ApplyHeavy(actionN);
                else if (state == YokaiActionState.Dodge) ApplyDodge(actionN);
                else if (state == YokaiActionState.Parry) ApplyParry(actionN);
                else if (state == YokaiActionState.Guard) ApplyGuard();
                else if (state == YokaiActionState.Art) ApplyArt(actionN);
                else if (state == YokaiActionState.Finisher) ApplyFinisher(actionN);
                else if (state == YokaiActionState.Hit) ApplyHit(.55f);
                else if (state == YokaiActionState.Stagger) ApplyStagger(actionN);
            }
            else if (enemy != null && enemy.IsAttacking)
            {
                ApplyEnemyAttack(enemy.AttackNormalized * Mathf.PI * 2f);
            }

            if (combat != null && motor != null && motor.IsAirborne && state == YokaiActionState.Free) ApplyAirPose(false);
            if (enemy != null && enemy.IsAirborne) ApplyAirPose(false);
            else if (enemy != null && enemy.IsLanding) ApplyAirPose(true);
            ApplyBreathing();
        }

        float EstimateEnemySpeed()
        {
            if (enemy == null) return 0f;
            return enemy.IsAttacking || enemy.IsAirborne || enemy.IsLanding || enemy.IsStaggered ? 0f : 2.4f;
        }

        void ResetNeutral()
        {
            if (visual.hips != null) visual.hips.localRotation = Quaternion.identity;
            if (visual.chest != null)
            {
                visual.chest.localRotation = Quaternion.identity;
                visual.chest.localPosition = baseChestLocalPosition;
            }
            if (visual.head != null) visual.head.localRotation = Quaternion.identity;
            if (visual.leftUpperArm != null) visual.leftUpperArm.localRotation = Quaternion.Euler(0f,0f,7f);
            if (visual.rightUpperArm != null) visual.rightUpperArm.localRotation = Quaternion.Euler(0f,0f,-7f);
            if (visual.leftLowerArm != null) visual.leftLowerArm.localRotation = Quaternion.identity;
            if (visual.rightLowerArm != null) visual.rightLowerArm.localRotation = Quaternion.identity;
            if (visual.leftUpperLeg != null) visual.leftUpperLeg.localRotation = Quaternion.identity;
            if (visual.rightUpperLeg != null) visual.rightUpperLeg.localRotation = Quaternion.identity;
            if (visual.leftLowerLeg != null) visual.leftLowerLeg.localRotation = Quaternion.identity;
            if (visual.rightLowerLeg != null) visual.rightLowerLeg.localRotation = Quaternion.identity;
        }

        void ApplyLocomotion(float speed, bool guard)
        {
            float normalized = Mathf.Clamp01(speed / 7f);
            float walkSwing = Mathf.Sin(gait) * Mathf.Lerp(10f, 34f, normalized);
            float armSwing = walkSwing * .65f;

            visual.leftUpperLeg.localRotation = Quaternion.Euler(walkSwing,0f,0f);
            visual.rightUpperLeg.localRotation = Quaternion.Euler(-walkSwing,0f,0f);
            visual.leftLowerLeg.localRotation = Quaternion.Euler(Mathf.Max(0f,-walkSwing)*.7f,0f,0f);
            visual.rightLowerLeg.localRotation = Quaternion.Euler(Mathf.Max(0f,walkSwing)*.7f,0f,0f);

            if (!guard)
            {
                visual.leftUpperArm.localRotation = Quaternion.Euler(-armSwing,0f,8f);
                visual.rightUpperArm.localRotation = Quaternion.Euler(armSwing*.75f,0f,-8f);
            }

            float sprintLean = Mathf.InverseLerp(5.6f,7.2f,speed);
            visual.chest.localRotation = Quaternion.Euler(10f*sprintLean, Mathf.Sin(gait*.5f)*2f,0f);
            visual.hips.localRotation = Quaternion.Euler(-4f*sprintLean,Mathf.Sin(gait*.5f)*2f,0f);
        }

        void ApplyLight(float t, int combo)
        {
            float wind = Mathf.SmoothStep(0f,1f,Mathf.Clamp01(t/.35f));
            float strike = Mathf.SmoothStep(0f,1f,Mathf.Clamp01((t-.35f)/.4f));
            float angle = combo == 1 ? -42f : (combo == 2 ? 38f : -18f);
            float finish = combo == 3 ? 92f : 68f;

            visual.chest.localRotation = Quaternion.Euler(0f, Mathf.Lerp(angle, -angle*.8f, strike), 0f);
            visual.rightUpperArm.localRotation =
                Quaternion.Euler(Mathf.Lerp(-82f,finish,strike), Mathf.Lerp(-40f,35f,strike), -42f);
            visual.rightLowerArm.localRotation = Quaternion.Euler(Mathf.Lerp(-18f,-86f,wind),0f,0f);
            visual.leftUpperArm.localRotation = Quaternion.Euler(-18f,20f,22f);
            visual.hips.localRotation = Quaternion.Euler(0f,-visual.chest.localEulerAngles.y*.18f,0f);
        }

        void ApplyHeavy(float t)
        {
            float wind = Mathf.SmoothStep(0f,1f,Mathf.Clamp01(t/.42f));
            float release = Mathf.SmoothStep(0f,1f,Mathf.Clamp01((t-.42f)/.42f));
            visual.chest.localRotation = Quaternion.Euler(Mathf.Lerp(-8f,18f,release),Mathf.Lerp(-48f,48f,release),0f);
            visual.rightUpperArm.localRotation = Quaternion.Euler(Mathf.Lerp(-125f,105f,release),-22f,-32f);
            visual.rightLowerArm.localRotation = Quaternion.Euler(Mathf.Lerp(-45f,-12f,release),0f,0f);
            visual.leftUpperArm.localRotation = Quaternion.Euler(Mathf.Lerp(-85f,35f,release),28f,24f);
            visual.leftLowerArm.localRotation = Quaternion.Euler(-38f,0f,0f);
            visual.hips.localRotation = Quaternion.Euler(0f,Mathf.Lerp(-12f,18f,release),0f);
        }

        void ApplyDodge(float t)
        {
            float roll = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            visual.chest.localRotation = Quaternion.Euler(38f*roll,0f,18f*roll);
            visual.hips.localRotation = Quaternion.Euler(28f*roll,0f,-12f*roll);
            visual.leftUpperArm.localRotation = Quaternion.Euler(55f*roll,-15f,25f);
            visual.rightUpperArm.localRotation = Quaternion.Euler(-35f*roll,15f,-25f);
            visual.leftUpperLeg.localRotation = Quaternion.Euler(-45f*roll,0f,0f);
            visual.rightUpperLeg.localRotation = Quaternion.Euler(48f*roll,0f,0f);
        }

        void ApplyAirPose(bool landing)
        {
            visual.chest.localRotation = Quaternion.Euler(landing ? 24f : 8f, 0f, 0f);
            visual.leftUpperLeg.localRotation = Quaternion.Euler(-35f,0f,0f);
            visual.rightUpperLeg.localRotation = Quaternion.Euler(-18f,0f,0f);
            visual.leftLowerLeg.localRotation = Quaternion.Euler(55f,0f,0f);
            visual.rightLowerLeg.localRotation = Quaternion.Euler(42f,0f,0f);
        }

        void ApplyParry(float t)
        {
            visual.chest.localRotation = Quaternion.Euler(-4f,-14f,0f);
            visual.rightUpperArm.localRotation = Quaternion.Euler(-62f,-58f,-18f);
            visual.rightLowerArm.localRotation = Quaternion.Euler(-86f,0f,0f);
            visual.leftUpperArm.localRotation = Quaternion.Euler(-28f,22f,18f);
        }

        void ApplyGuard()
        {
            visual.chest.localRotation = Quaternion.Euler(-3f,-8f,0f);
            visual.rightUpperArm.localRotation = Quaternion.Euler(-52f,-36f,-18f);
            visual.leftUpperArm.localRotation = Quaternion.Euler(-38f,30f,18f);
            visual.leftLowerArm.localRotation = Quaternion.Euler(-48f,0f,0f);
        }

        void ApplyArt(float t)
        {
            float arc = Mathf.Sin(t*Mathf.PI);
            visual.chest.localRotation = Quaternion.Euler(-6f+16f*arc,-26f+52f*t,0f);
            visual.rightUpperArm.localRotation = Quaternion.Euler(-120f+190f*t,0f,-24f);
            visual.leftUpperArm.localRotation = Quaternion.Euler(-88f+68f*t,0f,24f);
            visual.head.localRotation = Quaternion.Euler(-8f*arc,0f,0f);
        }

        void ApplyFinisher(float t)
        {
            visual.chest.localRotation = Quaternion.Euler(0f,-58f+116f*t,0f);
            visual.rightUpperArm.localRotation = Quaternion.Euler(-140f+240f*t,-18f,-35f);
            visual.rightLowerArm.localRotation = Quaternion.Euler(-60f+35f*t,0f,0f);
            visual.leftUpperArm.localRotation = Quaternion.Euler(-45f+35f*t,30f,22f);
        }

        void ApplyHit(float amount)
        {
            visual.chest.localRotation = Quaternion.Euler(-18f*amount,0f,9f*amount);
            visual.head.localRotation = Quaternion.Euler(-10f*amount,8f*amount,0f);
        }

        void ApplyStagger(float t)
        {
            float wobble = Mathf.Sin(t*Mathf.PI*3f);
            visual.chest.localRotation = Quaternion.Euler(24f, wobble*14f, 10f*wobble);
            visual.leftUpperArm.localRotation = Quaternion.Euler(35f,0f,32f);
            visual.rightUpperArm.localRotation = Quaternion.Euler(42f,0f,-28f);
            visual.leftUpperLeg.localRotation = Quaternion.Euler(-12f,0f,0f);
            visual.rightUpperLeg.localRotation = Quaternion.Euler(18f,0f,0f);
        }

        void ApplyEnemyAttack(float phase)
        {
            float s = Mathf.Sin(phase);
            visual.chest.localRotation = Quaternion.Euler(-4f, s*18f,0f);
            visual.rightUpperArm.localRotation = Quaternion.Euler(-70f+s*55f,-28f,-28f);
            visual.leftUpperArm.localRotation = Quaternion.Euler(-18f,-8f,18f);
        }

        void ApplyDeathPose(float t)
        {
            visual.chest.localRotation = Quaternion.Euler(72f*t,0f,18f*t);
            visual.hips.localRotation = Quaternion.Euler(58f*t,0f,-10f*t);
            visual.leftUpperArm.localRotation = Quaternion.Euler(38f*t,0f,42f*t);
            visual.rightUpperArm.localRotation = Quaternion.Euler(-25f*t,0f,-48f*t);
            visual.leftUpperLeg.localRotation = Quaternion.Euler(-18f*t,0f,0f);
            visual.rightUpperLeg.localRotation = Quaternion.Euler(28f*t,0f,0f);
        }

        void ApplyBreathing()
        {
            float b = Mathf.Sin(breathe)*.8f;
            visual.chest.localPosition = baseChestLocalPosition + new Vector3(0f,b*.0015f,0f);
            visual.head.localRotation *= Quaternion.Euler(b*.6f,0f,0f);
        }
    }
}
