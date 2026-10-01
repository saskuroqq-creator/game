using UnityEngine;

namespace Yokai
{
    [RequireComponent(typeof(YokaiMotor))]
    [RequireComponent(typeof(YokaiCombat))]
    public sealed class YokaiPlayerController : MonoBehaviour
    {
        public YokaiCameraRig cameraRig;
        public YokaiMobileHUD mobileHud;

        YokaiMotor motor;
        YokaiCombat combat;
        YokaiLockOn lockOn;
        Vector2 touchMove;
        Vector2 touchLook;
        bool touchSprint;

        void Awake()
        {
            motor = GetComponent<YokaiMotor>();
            combat = GetComponent<YokaiCombat>();
            lockOn = GetComponent<YokaiLockOn>();
        }

        void Update()
        {
            if (!Application.isMobilePlatform && Input.GetKeyDown(KeyCode.Escape) && YokaiGameSession.Instance != null)
                YokaiGameSession.Instance.TogglePause();

            if (YokaiGameSession.Instance != null && YokaiGameSession.Instance.Paused)
            {
                motor.SetMove(Vector2.zero);
                motor.SetSprint(false);
                combat.SetGuard(false);
                return;
            }

            Vector2 move = touchMove;
            bool sprint = touchSprint;

            if (!Application.isMobilePlatform)
            {
                move += new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                sprint |= Input.GetKey(KeyCode.LeftShift);

                if (Input.GetMouseButton(1) && cameraRig != null)
                    cameraRig.AddLook(new Vector2(Input.GetAxis("Mouse X")*7f, Input.GetAxis("Mouse Y")*7f));

                if (Input.GetMouseButtonDown(0)) combat.LightAttack();
                if (Input.GetKeyDown(KeyCode.F)) combat.HeavyAttack();
                if (Input.GetKeyDown(KeyCode.Space)) combat.Dodge();
                if (Input.GetKeyDown(KeyCode.Q)) combat.Parry();
                combat.SetGuard(Input.GetKey(KeyCode.LeftAlt));
                if (Input.GetKeyDown(KeyCode.E)) combat.CastHunterArt();
                if (Input.GetKeyDown(KeyCode.R)) combat.CycleHunterArt();
                if (Input.GetKeyDown(KeyCode.Tab)) combat.CycleStance();
                if (Input.GetKeyDown(KeyCode.C)) ToggleLock();
                if (Input.GetKeyDown(KeyCode.Z)) SwitchTarget(-1);
                if (Input.GetKeyDown(KeyCode.X)) SwitchTarget(1);
                if (Input.GetKeyDown(KeyCode.H)) combat.Heal();
                if (Input.GetKeyDown(KeyCode.G)) Interact();
                if (Input.GetKeyDown(KeyCode.V)) combat.Jump();
                if (Input.GetKeyDown(KeyCode.T)) combat.Launcher();
                if (Input.GetKeyDown(KeyCode.B)) combat.CastSkill();
                if (Input.GetKeyDown(KeyCode.N)) combat.CycleSkill();
            }

            motor.SetMove(Vector2.ClampMagnitude(move,1f));
            motor.SetSprint(sprint);

            if (cameraRig != null && touchLook.sqrMagnitude > 0f)
            {
                cameraRig.AddLook(touchLook);
                touchLook = Vector2.zero;
            }
        }

        public void SetTouchMove(Vector2 value) { touchMove = value; }
        public void AddTouchLook(Vector2 value) { touchLook += value; }
        public void SetTouchSprint(bool value) { touchSprint = value; }
        public void Jump() { combat.Jump(); }
        public void Launcher() { combat.Launcher(); }
        public void Skill() { combat.CastSkill(); }
        public void CycleSkill() { combat.CycleSkill(); }
        public void Attack() { combat.LightAttack(); }
        public void Heavy() { combat.HeavyAttack(); }
        public void Dodge() { combat.Dodge(); }
        public void Parry() { combat.Parry(); }
        public void Guard(bool value) { combat.SetGuard(value); }
        public void Ability() { combat.CastHunterArt(); }
        public void CycleAbility() { combat.CycleHunterArt(); }
        public void Stance() { combat.CycleStance(); }
        public void Heal() { combat.Heal(); }
        public void Pause() { if (YokaiGameSession.Instance != null) YokaiGameSession.Instance.TogglePause(); }

        public void ToggleLock()
        {
            Transform cam = cameraRig != null ? cameraRig.transform : null;
            lockOn.Toggle(cam);
        }

        public void SwitchTarget(int direction)
        {
            if (lockOn == null || !lockOn.IsLocked) return;
            lockOn.Cycle(cameraRig != null ? cameraRig.transform : null, direction);
        }

        public void Interact()
        {
            Collider[] around = Physics.OverlapSphere(transform.position, 2.4f, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < around.Length; i++)
            {
                YokaiShrine shrine = around[i].GetComponentInParent<YokaiShrine>();
                if (shrine != null)
                {
                    shrine.Activate(this);
                    return;
                }
            }
        }
    }
}
