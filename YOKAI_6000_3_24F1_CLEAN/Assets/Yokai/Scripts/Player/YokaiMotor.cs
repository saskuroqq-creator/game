using UnityEngine;

namespace Yokai
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class YokaiMotor : MonoBehaviour
    {
        public Transform cameraTransform;
        public float walkSpeed = 3.0f;
        public float runSpeed = 5.1f;
        public float sprintSpeed = 7.0f;
        public float acceleration = 22f;
        public float braking = 28f;
        public float rotationSharpness = 16f;
        public float gravity = -26f;
        public float dodgeSpeed = 10.0f;
        public float dodgeDuration = 0.43f;
        public float invulnerableStart = 0.055f;
        public float invulnerableEnd = 0.29f;
        public float perfectDodgeEnd = 0.145f;

        CharacterController controller;
        YokaiAttributes attributes;
        YokaiCombat combat;
        YokaiLockOn lockOn;
        Vector2 moveInput;
        float moveMagnitude;
        bool sprintHeld;
        Vector3 velocity;
        Vector3 externalVelocity;
        Vector3 dodgeDirection;
        float dodgeTimer = -1f;
        float stanceMoveMultiplier = 1f;
        public float jumpSpeed = 9.5f;
        public float airControl = .65f;
        bool jumpInProgress;
        bool airDodgeUsed;
        float airHoldRemaining;
        float hangBudget;
        float burstRemaining;
        Vector3 burstVelocity;
        public bool IsAirborne { get { return jumpInProgress || !Grounded; } }
        public float VerticalSpeed { get { return velocity.y; } }

        public bool IsDodging { get { return dodgeTimer >= 0f; } }
        public bool IsInvulnerable { get { return IsDodging && dodgeTimer >= invulnerableStart && dodgeTimer <= invulnerableEnd; } }
        public bool IsPerfectDodgeWindow { get { return IsDodging && dodgeTimer >= invulnerableStart && dodgeTimer <= perfectDodgeEnd; } }
        public bool Grounded { get { return controller != null && controller.isGrounded; } }
        public float PlanarSpeed { get { return new Vector3(velocity.x,0f,velocity.z).magnitude; } }
        public Vector2 LocalMove
        {
            get { Vector3 local = transform.InverseTransformDirection(DesiredDirection()); return new Vector2(local.x, local.z); }
        }
        public Vector2 LocalDodge
        {
            get { Vector3 local = transform.InverseTransformDirection(dodgeDirection); return new Vector2(local.x, local.z); }
        }
        public Vector2 MoveInput { get { return moveInput; } }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            attributes = GetComponent<YokaiAttributes>();
            combat = GetComponent<YokaiCombat>();
            lockOn = GetComponent<YokaiLockOn>();
            controller.slopeLimit = 50f;
            controller.stepOffset = 0.32f;
            controller.skinWidth = 0.035f;
            controller.minMoveDistance = 0f;
        }

        void Start()
        {
            // Runtime bootstrap adds combat after the motor's Awake.
            combat = GetComponent<YokaiCombat>();
            lockOn = GetComponent<YokaiLockOn>();
        }

        public void SetMove(Vector2 input)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);
            moveMagnitude = moveInput.magnitude;
        }

        public void SetSprint(bool value) { sprintHeld = value; }
        public void SetStanceMoveMultiplier(float value) { stanceMoveMultiplier = Mathf.Clamp(value, .75f, 1.25f); }

        public bool TryDodge()
        {
            if (IsDodging || attributes.IsDead || IsAirborne && airDodgeUsed || !attributes.ConsumeStamina(20f)) return false;
            if (IsAirborne) airDodgeUsed = true;
            Vector3 planar = DesiredDirection();
            dodgeDirection = planar.sqrMagnitude > .05f ? planar.normalized : transform.forward;
            velocity.x = velocity.z = 0f;
            dodgeTimer = 0f;
            return true;
        }

        public bool TryJump()
        {
            if (attributes.IsDead || IsAirborne || !attributes.ConsumeStamina(8f)) return false;
            Launch(jumpSpeed);
            return true;
        }

        public void Launch(float upwardSpeed)
        {
            if (attributes.IsDead) return;
            velocity.y = Mathf.Max(velocity.y, upwardSpeed);
            jumpInProgress = true;
            hangBudget = .4f;
        }

        public void HoldAir(float duration)
        {
            if (!IsAirborne || hangBudget <= 0f) return;
            float hold = Mathf.Min(duration, hangBudget);
            airHoldRemaining += hold;
            hangBudget -= hold;
        }

        public void BeginDive()
        {
            airHoldRemaining = 0f;
            hangBudget = 0f;
            velocity.y = -18f;
        }

        public void BeginBurst(Vector3 direction, float speed, float duration)
        {
            direction.y = 0f;
            velocity.x = velocity.z = 0f;
            burstVelocity = direction.normalized * speed;
            burstRemaining = duration;
        }
        public void StopBurst() { burstRemaining = 0f; }

        public void ResetMotion()
        {
            burstRemaining = 0f;
            jumpInProgress = airDodgeUsed = false;
            airHoldRemaining = hangBudget = 0f;
            dodgeTimer = -1f;
            velocity = externalVelocity = Vector3.zero;
            moveInput = Vector2.zero;
            moveMagnitude = 0f;
            sprintHeld = false;
        }

        public void AddImpulse(Vector3 impulse)
        {
            externalVelocity += impulse;
        }

        public void FaceDirection(Vector3 direction, float lerpFactor)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < .001f) return;
            Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = lerpFactor > 0f ? Quaternion.Slerp(transform.rotation, target, lerpFactor) : target;
        }

        void Update()
        {
            if (attributes.IsDead)
            {
                dodgeTimer = -1f;
                externalVelocity = Vector3.zero;
                ApplyVertical();
                return;
            }

            if (IsDodging)
            {
                dodgeTimer += Time.deltaTime;
                controller.Move((dodgeDirection * dodgeSpeed + externalVelocity) * Time.deltaTime);
                externalVelocity = Vector3.Lerp(externalVelocity, Vector3.zero, 8f * Time.deltaTime);
                ApplyVertical();
                if (dodgeTimer >= dodgeDuration) dodgeTimer = -1f;
                return;
            }

            if (burstRemaining > 0f && combat != null && combat.State == YokaiActionState.Skill)
            {
                float step = Mathf.Min(Time.deltaTime, burstRemaining);
                controller.Move(burstVelocity * step);
                burstRemaining -= step;
                ApplyVertical();
                return;
            }
            burstRemaining = 0f;
            Vector3 desired = DesiredDirection();
            bool wantsSprint = sprintHeld && (combat == null || combat.State == YokaiActionState.Free) && (lockOn == null || !lockOn.IsLocked) && !IsAirborne && moveMagnitude > .7f && attributes.stamina > 1f;
            if (wantsSprint) attributes.ConsumeStamina(10.5f * Time.deltaTime);

            float baseSpeed = wantsSprint ? sprintSpeed : (moveMagnitude > .5f ? runSpeed : walkSpeed);
            float targetSpeed = baseSpeed * moveMagnitude * stanceMoveMultiplier * (combat != null ? combat.MovementMultiplier : 1f);
            if (IsAirborne) targetSpeed *= airControl;
            Vector3 targetVelocity = desired * targetSpeed;

            Vector3 planarVelocity = new Vector3(velocity.x, 0f, velocity.z);
            float rate = targetVelocity.sqrMagnitude > planarVelocity.sqrMagnitude ? acceleration : braking;
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, rate * Time.deltaTime);
            velocity.x = planarVelocity.x;
            velocity.z = planarVelocity.z;

            if (desired.sqrMagnitude > .02f && (lockOn == null || !lockOn.IsLocked) &&
                (combat == null || combat.State == YokaiActionState.Free))
            {
                Quaternion q = Quaternion.LookRotation(desired, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, q, 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
            }

            controller.Move((new Vector3(velocity.x,0f,velocity.z) + externalVelocity) * Time.deltaTime);
            externalVelocity = Vector3.Lerp(externalVelocity, Vector3.zero, 7f * Time.deltaTime);
            ApplyVertical();
        }

        Vector3 DesiredDirection()
        {
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            return Vector3.ClampMagnitude(forward * moveInput.y + right * moveInput.x, 1f);
        }

        void ApplyVertical()
        {
            if (!controller.enabled) return;
            if (controller.isGrounded && velocity.y < 0f) velocity.y = -2f;
            else if (!attributes.IsDead && airHoldRemaining > 0f && velocity.y <= 0f)
            {
                velocity.y = -.8f;
                airHoldRemaining = Mathf.Max(0f, airHoldRemaining - Time.deltaTime);
            }
            else velocity.y += gravity * Time.deltaTime;
            CollisionFlags flags = controller.Move(Vector3.up * velocity.y * Time.deltaTime);
            if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0f) velocity.y = 0f;
            if (controller.isGrounded && velocity.y <= 0f)
            {
                jumpInProgress = airDodgeUsed = false;
                airHoldRemaining = hangBudget = 0f;
            }
        }
    }
}
