using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yokai
{
    [RequireComponent(typeof(YokaiAttributes))]
    [RequireComponent(typeof(YokaiMotor))]
    [RequireComponent(typeof(YokaiLockOn))]
    public sealed class YokaiCombat : MonoBehaviour, IYokaiDamageReceiver
    {
        public Transform attackOrigin;
        public LayerMask hittableMask = ~0;
        public float attackRadius = 1.05f;
        public float lightRange = 1.35f;
        public float heavyRange = 1.65f;
        public float parryWindow = .15f;
        public float guardDamageMultiplier = .24f;

        YokaiAttributes attributes;
        YokaiMotor motor;
        YokaiLockOn lockOn;
        YokaiHunterArts arts;
        YokaiHumanoidVisual visual;
        [Range(.1f,.5f)] public float inputBufferTime = .24f;
        [Range(30f,180f)] public float guardArc = 130f;
        public YokaiCombatTuning tuning;
        YokaiAttackDefinition[] defaults;
        int actionVersion;
        int bufferedAction;
        float bufferUntil;
        bool dodgeRewarded;
        readonly Collider[] hitBuffer = new Collider[96];
        readonly HashSet<int> struck = new HashSet<int>();
        public int ActionVersion { get { return actionVersion; } }
        public float MovementMultiplier
        {
            get
            {
                if (State == YokaiActionState.Free) return 1f;
                if (State == YokaiActionState.Guard || State == YokaiActionState.Parry) return .4f;
                if (State == YokaiActionState.AirLight || State == YokaiActionState.Jump) return .65f;
                if (State == YokaiActionState.Light) return .18f;
                if (State == YokaiActionState.Heavy) return .1f;
                return 0f;
            }
        }
        public float launcherCooldown = 2.5f;
        public float slamRadius = 2.8f;
        float launcherReadyAt;
        int airComboIndex;
        YokaiSkills skills;
        public int AirComboIndex { get { return airComboIndex; } }
        public float LauncherCooldownRemaining { get { return Mathf.Max(0f, launcherReadyAt - Time.time); } }
        int comboIndex;
        int healCharges = 3;
        float comboResetAt;
        float actionStart;
        float actionEnd;
        float parryEnd;
        bool guardHeld;

        public YokaiActionState State { get; private set; }
        public YokaiStance Stance { get; private set; }
        public int ComboIndex { get { return comboIndex; } }
        public bool FinisherReady
        {
            get
            {
                if (lockOn == null || !lockOn.IsLocked || lockOn.Current == null) return false;
                YokaiEnemy enemy = lockOn.Current.GetComponentInParent<YokaiEnemy>();
                return enemy != null && enemy.CanBeFinished &&
                    Vector3.Distance(transform.position, enemy.transform.position) <= 2.6f;
            }
        }
        public int HealCharges { get { return healCharges; } }
        public float ActionNormalized
        {
            get
            {
                if (actionEnd <= actionStart) return 1f;
                return Mathf.Clamp01((Time.time-actionStart)/(actionEnd-actionStart));
            }
        }

        public bool IsParryWindow { get { return State == YokaiActionState.Parry && Time.time <= parryEnd; } }

        void Awake()
        {
            State = YokaiActionState.Free;
            Stance = YokaiStance.Gale;
            attributes = GetComponent<YokaiAttributes>();
            motor = GetComponent<YokaiMotor>();
            lockOn = GetComponent<YokaiLockOn>();
            arts = GetComponent<YokaiHunterArts>();
            visual = GetComponent<YokaiHumanoidVisual>();
            skills = GetComponent<YokaiSkills>();
            if (attackOrigin == null) attackOrigin = transform;

            attributes.Died += OnDied;
            attributes.Revived += OnRevived;
            defaults = YokaiCombatTuning.Defaults();
            if (tuning == null) tuning = Resources.Load<YokaiCombatTuning>("YokaiArt/CombatTuning");
            ApplyStance();
        }

        void Start() { skills = GetComponent<YokaiSkills>(); }

        void Update()
        {
            if (attributes.IsDead) return;
            if (!motor.IsAirborne) airComboIndex = 0;

            if (State != YokaiActionState.Free && State != YokaiActionState.Guard && Time.time >= actionEnd)
                State = guardHeld ? YokaiActionState.Guard : YokaiActionState.Free;

            if (bufferedAction != 0)
            {
                if (Time.time > bufferUntil) bufferedAction = 0;
                else if ((bufferedAction <= 2 && CanAttack()) || (bufferedAction >= 3 && CanCancel()))
                {
                    int next = bufferedAction;
                    bufferedAction = 0;
                    if (next == 1) LightAttack();
                    else if (next == 2) HeavyAttack();
                    else if (next == 3) Dodge();
                    else Parry();
                }
            }

            if (comboResetAt > 0f && Time.time > comboResetAt)
                comboIndex = 0;

            if (lockOn.IsLocked && lockOn.Current != null && State != YokaiActionState.Dodge)
            {
                Vector3 d = lockOn.Current.position - transform.position;
                if (d.sqrMagnitude > .1f) motor.FaceDirection(d, .15f);
            }
        }

        public void LightAttack()
        {
            if (!CanAttack()) { Buffer(1); return; }
            if (motor.IsAirborne) { AirAttack(); return; }
            int nextIndex = comboResetAt > Time.time ? (comboIndex % 5) + 1 : 1;
            YokaiAttackDefinition move = tuning != null && tuning.lightCombo != null &&
                tuning.lightCombo.Length >= nextIndex && tuning.lightCombo[nextIndex-1] != null
                ? tuning.lightCombo[nextIndex-1] : defaults[nextIndex-1];
            if (!attributes.ConsumeStamina(Mathf.Max(0f, move.stamina))) return;
            comboIndex = nextIndex;


            float speed = Stance == YokaiStance.Gale ? 1.14f : (Stance == YokaiStance.Stone ? .9f : 1f);
            float duration = Mathf.Max(.15f, move.duration) / speed;
            SetState(YokaiActionState.Light, duration);
            comboResetAt = actionEnd + .38f;

            float damage = attributes.attackPower * Mathf.Max(0f, move.damageMultiplier);
            float posture = Mathf.Max(0f, move.posture);
            if (Stance == YokaiStance.Stone) posture *= 1.35f;
            if (Stance == YokaiStance.Spirit) attributes.AddSpirit(2f);

            YokaiAudio.Play("slash_light", transform.position, .72f, 1f + comboIndex*.035f);
            StartCoroutine(StrikeAfter(duration*Mathf.Clamp(move.contact, .1f, .8f), damage, posture, lightRange, false));
        }

        public void HeavyAttack()
        {
            if (!CanAttack()) { Buffer(2); return; }
            if (motor.IsAirborne) { AirSlam(); return; }

            if (TryFinisher()) return;
            if (!attributes.ConsumeStamina(22f)) return;

            float duration = Stance == YokaiStance.Stone ? .72f : .62f;
            SetState(YokaiActionState.Heavy, duration);

            float flow = attributes.bladeFlow;
            float damage = attributes.attackPower * 1.9f * (1f + flow/100f*.5f);
            float posture = 32f * (Stance == YokaiStance.Stone ? 1.35f : 1f);

            if (flow >= 70f)
            {
                damage *= 1.18f;
                posture *= 1.22f;
                attributes.SpendBladeFlow(35f);
            }

            YokaiAudio.Play("slash_heavy", transform.position, .88f, .94f);
            StartCoroutine(StrikeAfter(duration*.46f, damage, posture, heavyRange, true));
        }

        public void Jump()
        {
            if (!CanCancel() || !motor.TryJump()) return;
            guardHeld = false;
            SetState(YokaiActionState.Jump, .16f);
        }

        public void Launcher()
        {
            if (!CanAttack() || motor.IsAirborne || Time.time < launcherReadyAt || !attributes.ConsumeStamina(18f)) return;
            launcherReadyAt = Time.time + launcherCooldown;
            SetState(YokaiActionState.Launcher, .55f);
            StartCoroutine(StrikeAfter(.20f, attributes.attackPower * 1.25f, 24f, lightRange, true, motor.jumpSpeed));
        }

        void AirAttack()
        {
            if (!YokaiAirCombatRules.CanContinueCombo(airComboIndex) || !attributes.ConsumeStamina(9f)) return;
            airComboIndex++;
            comboIndex = 0;
            float duration = .36f + airComboIndex * .045f;
            SetState(YokaiActionState.AirLight, duration);
            motor.HoldAir(.12f);
            YokaiAudio.Play("slash_light", transform.position, .75f, 1.12f);
            StartCoroutine(StrikeAfter(duration * .32f, attributes.attackPower * (1.05f + airComboIndex * .12f),
                12f + airComboIndex * 4f, lightRange + .2f, false));
        }

        void AirSlam()
        {
            if (!attributes.ConsumeStamina(20f)) return;
            SetState(YokaiActionState.AirSlam, 2.8f);
            YokaiAreaDamage.SlamAirborne(transform, slamRadius, hittableMask);
            motor.BeginDive();
            StartCoroutine(SlamRoutine());
        }

        IEnumerator SlamRoutine()
        {
            int version = actionVersion;
            float timeout = Time.time + 2.5f;
            yield return null;
            while (!motor.Grounded && Time.time < timeout)
            {
                if (version != actionVersion || attributes.IsDead) yield break;
                yield return null;
            }
            if (version != actionVersion || attributes.IsDead) yield break;
            if (!motor.Grounded) { SetState(YokaiActionState.Free, .01f); yield break; }
            YokaiAreaDamage.Apply(transform, attributes.attackPower * 2.1f, 42f, slamRadius,
                YokaiElement.Physical, true, hittableMask);
            YokaiVfx.Ring(transform.position, new Color(1f,.55f,.12f), slamRadius, .4f);
            if (YokaiFeedbackManager.Instance != null) YokaiFeedbackManager.Instance.CameraImpulse(.6f);
            SetState(YokaiActionState.Landing, .38f);
        }

        public void CastSkill()
        {
            if (!CanAttack() || skills == null || !skills.TryCast(actionVersion + 1)) return;
            SetState(YokaiActionState.Skill, skills.ActionDuration);
        }

        public void CycleSkill() { if (skills != null) skills.Cycle(); }

        public void Dodge()
        {
            if (!CanCancel()) { Buffer(3); return; }
            guardHeld = false;
            if (motor.TryDodge())
            {
                YokaiAudio.Play("dodge", transform.position, .55f, 1f);
                dodgeRewarded = false;
                SetState(YokaiActionState.Dodge, motor.dodgeDuration);
            }
        }

        public void Parry()
        {
            if (!CanCancel()) { Buffer(4); return; }
            if (!attributes.ConsumeStamina(6f)) return;
            guardHeld = false;
            SetState(YokaiActionState.Parry, .28f);
            parryEnd = Time.time + parryWindow;
        }

        public void SetGuard(bool held)
        {
            guardHeld = held;
            if (attributes.IsDead) return;

            if (held)
            {
                if (State == YokaiActionState.Free || State == YokaiActionState.Guard)
                    State = YokaiActionState.Guard;
            }
            else if (State == YokaiActionState.Guard)
            {
                State = YokaiActionState.Free;
            }
        }

        public void CastHunterArt()
        {
            if (!CanAttack() || arts == null) return;
            if (arts.TryCast(actionVersion + 1))
            {
                YokaiAudio.Play("hunter_art", transform.position, .8f, 1f);
                SetState(YokaiActionState.Art, .75f);
                attributes.AddBladeFlow(8f);
            }
        }

        public void CycleHunterArt()
        {
            if (arts != null) arts.Cycle();
        }

        public void CycleStance()
        {
            if (!CanCancel()) return;
            Stance = (YokaiStance)(((int)Stance + 1) % 3);
            ApplyStance();
            YokaiVfx.Burst(transform.position + Vector3.up, StanceColor(), .25f, .3f);
        }

        public void Heal()
        {
            if (!CanCancel() || healCharges <= 0 || attributes.health >= attributes.maxHealth*.99f) return;
            SetState(YokaiActionState.Art, .8f);
            StartCoroutine(HealRoutine());
        }

        public void RefillHealing()
        {
            healCharges = 3;
        }

        IEnumerator HealRoutine()
        {
            int version = actionVersion;
            yield return new WaitForSeconds(.45f);
            if (version != actionVersion || attributes.IsDead) yield break;
            healCharges--;
            attributes.Heal(attributes.maxHealth*.42f);
            YokaiVfx.Burst(transform.position + Vector3.up, new Color(.35f,1f,.55f), .35f, .4f);
        }

        bool TryFinisher()
        {
            if (!lockOn.IsLocked || lockOn.Current == null) return false;
            YokaiEnemy enemy = lockOn.Current.GetComponentInParent<YokaiEnemy>();
            if (enemy == null || !enemy.CanBeFinished) return false;
            if (Vector3.Distance(transform.position, enemy.transform.position) > 2.6f) return false;

            SetState(YokaiActionState.Finisher, .9f);
            StartCoroutine(FinisherRoutine(enemy));
            return true;
        }

        IEnumerator FinisherRoutine(YokaiEnemy enemy)
        {
            int version = actionVersion;
            motor.FaceDirection(enemy.transform.position-transform.position, 0f);
            yield return new WaitForSeconds(.42f);
            if (version == actionVersion && !attributes.IsDead && enemy != null && enemy.CanBeFinished)
            {
                enemy.ExecuteFinisher(attributes.attackPower * 4.5f);
                attributes.AddBladeFlow(28f);
                attributes.AddSpirit(15f);
                YokaiVfx.Burst(enemy.transform.position + Vector3.up, new Color(1f,.8f,.25f), .65f, .45f);
            }
        }

        IEnumerator StrikeAfter(float delay, float damage, float posture, float range, bool heavy, float launchSpeed = 0f)
        {
            int version = actionVersion;
            yield return new WaitForSeconds(delay);
            if (version != actionVersion || attributes.IsDead) yield break;
            struck.Clear();
            bool followedLaunch = false;
            float activeUntil = Time.time + (heavy ? .16f : .1f);
            do
            {
                if (version != actionVersion || attributes.IsDead) yield break;
                Vector3 center = transform.position + Vector3.up;
                int count = Physics.OverlapSphereNonAlloc(center, range + attackRadius, hitBuffer,
                    hittableMask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    Collider c = hitBuffer[i];
                    if (c == null || c.transform.IsChildOf(transform)) continue;
                    YokaiEnemy enemy = c.GetComponentInParent<YokaiEnemy>();
                    if (enemy == null || !enemy.CanTarget) continue;
                    int id = enemy.GetInstanceID();
                    if (struck.Contains(id)) continue;
                    Vector3 point = c.ClosestPoint(center);
                    if (!YokaiCombatGeometry.InArc(transform.position, transform.forward, point,
                        range + attackRadius * .45f, heavy ? 150f : 120f)) continue;
                    if (!YokaiCombatGeometry.Visible(center, enemy.AimPoint.position, transform, enemy.transform)) continue;
                    struck.Add(id);
                    bool accepted = enemy.ReceiveHit(new YokaiHit(gameObject, damage, posture, point,
                        transform.forward, YokaiElement.Physical, heavy, false));
                    if (!accepted) continue;
                    if (launchSpeed > 0f && enemy.TryLaunch(launchSpeed, transform.forward) && !followedLaunch)
                    {
                        motor.Launch(launchSpeed);
                        followedLaunch = true;
                    }
                    if (State == YokaiActionState.AirLight) enemy.HoldAir(.12f);
                    attributes.AddBladeFlow(heavy ? 14f : 7f);
                    attributes.AddSpirit(heavy ? 7f : 3f);
                    YokaiVfx.Burst(point, heavy ? new Color(1f,.75f,.2f) : new Color(.9f,.9f,1f), heavy ? .28f : .16f, .2f);
                    if (YokaiFeedbackManager.Instance != null)
                    {
                        YokaiFeedbackManager.Instance.HitStop(heavy ? .055f : .028f, heavy ? .08f : .18f);
                        YokaiFeedbackManager.Instance.CameraImpulse(heavy ? .48f : .2f);
                    }
                }
                yield return null;
            } while (Time.time < activeUntil);
        }

        public bool ReceiveHit(YokaiHit hit)
        {
            if (attributes.IsDead) return false;

            if (motor.IsInvulnerable)
            {
                if (motor.IsPerfectDodgeWindow && !dodgeRewarded)
                {
                    dodgeRewarded = true;
                    attributes.AddBladeFlow(18f);
                    attributes.AddSpirit(6f);
                    YokaiVfx.Burst(transform.position + Vector3.up, new Color(.3f,.85f,1f), .32f, .32f);
                }
                return false;
            }

            bool frontal = YokaiCombatGeometry.InArc(transform.position, transform.forward,
                hit.source != null ? hit.source.transform.position : transform.position - hit.direction,
                float.MaxValue, guardArc);
            if (IsParryWindow && frontal && !hit.unblockable)
            {
                attributes.AddBladeFlow(24f);
                attributes.AddSpirit(10f);

                if (hit.source != null)
                {
                    YokaiEnemy enemy = hit.source.GetComponent<YokaiEnemy>();
                    if (enemy != null) enemy.Parried(34f);
                }

                YokaiAudio.Play("parry", transform.position, .92f, 1f);
                YokaiVfx.Ring(transform.position, new Color(.95f,.85f,.35f), 1.2f, .28f);
                if (YokaiFeedbackManager.Instance != null)
                {
                    YokaiFeedbackManager.Instance.HitStop(.065f, .06f);
                    YokaiFeedbackManager.Instance.CameraImpulse(.42f);
                }
                return false;
            }

            float damage = skills != null ? skills.Absorb(hit.damage) : hit.damage;
            float posture = hit.postureDamage;

            if (State == YokaiActionState.Guard && frontal && !hit.unblockable)
            {
                float guardCost = Mathf.Max(5f, hit.damage*.38f);
                if (attributes.ConsumeStamina(guardCost))
                {
                    damage *= guardDamageMultiplier;
                    posture *= .58f;
                    attributes.ApplyHealthDamage(damage);
                    attributes.DamagePosture(posture, 1.7f);
                    if (!attributes.IsDead && !attributes.IsPostureBroken) return true;
                    if (attributes.IsDead) return true;
                    SetState(YokaiActionState.Stagger, 1.15f);
                    guardHeld = false;
                    attributes.ResetPosture();
                    return true;
                }
                else
                {
                    guardHeld = false;
                    posture = attributes.maxPosture;
                }
            }

            attributes.ApplyHealthDamage(damage);
            attributes.DamagePosture(posture, 1.7f);
            if (visual != null) visual.PlayHitReaction();
            if (YokaiFeedbackManager.Instance != null)
                YokaiFeedbackManager.Instance.CameraImpulse(hit.heavy ? .38f : .18f);

            if (!attributes.IsDead)
            {
                SetState(attributes.IsPostureBroken ? YokaiActionState.Stagger : YokaiActionState.Hit,
                    attributes.IsPostureBroken ? 1.15f : .28f);
                if (attributes.IsPostureBroken) attributes.ResetPosture();
                motor.AddImpulse(hit.direction.normalized * (hit.heavy ? 3.4f : 1.3f));
            }

            return true;
        }

        bool CanAttack()
        {
            return !attributes.IsDead && !motor.IsDodging &&
                (State == YokaiActionState.Free || State == YokaiActionState.Guard ||
                 (State == YokaiActionState.Light && ActionNormalized > (tuning != null ? tuning.chainWindow : .62f)) ||
                 (State == YokaiActionState.AirLight && ActionNormalized > .56f) ||
                 (State == YokaiActionState.Launcher && ActionNormalized > .48f));
        }

        bool CanCancel()
        {
            return !attributes.IsDead &&
                (State == YokaiActionState.Free || State == YokaiActionState.Guard ||
                 (State == YokaiActionState.Light && ActionNormalized > (tuning != null ? tuning.lightCancel : .48f)) ||
                 (State == YokaiActionState.Heavy && ActionNormalized > (tuning != null ? tuning.heavyCancel : .72f)) ||
                 (State == YokaiActionState.AirLight && ActionNormalized > .48f) ||
                 (State == YokaiActionState.Launcher && ActionNormalized > .48f));
        }

        void ApplyStance()
        {
            if (Stance == YokaiStance.Gale) motor.SetStanceMoveMultiplier(1.08f);
            else if (Stance == YokaiStance.Stone) motor.SetStanceMoveMultiplier(.9f);
            else motor.SetStanceMoveMultiplier(1f);
        }

        Color StanceColor()
        {
            if (Stance == YokaiStance.Gale) return new Color(.25f,.75f,1f);
            if (Stance == YokaiStance.Stone) return new Color(1f,.55f,.18f);
            return new Color(.55f,.3f,1f);
        }

        void SetState(YokaiActionState state, float duration)
        {
            if (state != YokaiActionState.Skill) motor.StopBurst();
            actionVersion++;
            State = state;
            actionStart = Time.time;
            actionEnd = Time.time + Mathf.Max(.01f, duration);
        }

        void OnDied()
        {
            actionVersion++;
            bufferedAction = 0;
            StopAllCoroutines();
            motor.StopBurst();
            if (skills != null) skills.ResetSkills();
            State = YokaiActionState.Dead;
            guardHeld = false;
            if (YokaiGameSession.Instance != null)
                YokaiGameSession.Instance.PlayerDied();
        }

        void Buffer(int action)
        {
            if (attributes.IsDead || State == YokaiActionState.Hit || State == YokaiActionState.Stagger || State == YokaiActionState.Finisher) return;
            bufferedAction = action;
            bufferUntil = Time.time + inputBufferTime;
        }

        void OnDisable()
        {
            actionVersion++;
            bufferedAction = 0;
            StopAllCoroutines();
        }

        void OnDestroy()
        {
            if (attributes == null) return;
            attributes.Died -= OnDied;
            attributes.Revived -= OnRevived;
        }

        public void ResetActions()
        {
            StopAllCoroutines();
            if (arts != null) arts.Cancel();
            if (skills != null) skills.ResetSkills();
            airComboIndex = 0;
            launcherReadyAt = 0f;
            actionVersion++;
            bufferedAction = 0;
            State = YokaiActionState.Free;
            comboIndex = 0;
            comboResetAt = 0f;
            guardHeld = false;
            parryEnd = 0f;
            motor.ResetMotion();
            var status = GetComponent<YokaiElementalStatus>();
            if (status != null) status.Clear();
        }

        void OnRevived() { ResetActions(); }
    }
}
