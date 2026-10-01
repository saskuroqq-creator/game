using System;
using System.Collections;
using UnityEngine;

namespace Yokai
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(YokaiAttributes))]
    [RequireComponent(typeof(YokaiElementalStatus))]
    public class YokaiEnemy : MonoBehaviour, IYokaiDamageReceiver, IYokaiTargetable
    {
        public YokaiEnemyArchetype archetype = YokaiEnemyArchetype.Grunt;
        public Transform target;
        public float activationRadius = 16f;
        public float preferredRange = 2.2f;
        public float moveSpeed = 2.8f;
        public float turnSpeed = 8f;
        public float attackCooldown = 1.55f;
        public float attackDamage = 15f;
        public float postureDamage = 16f;
        public float telegraphTime = .48f;
        public float attackRadius = 1.2f;
        public bool startsActive = false;
        public bool autoActivateByDistance = true;

        protected CharacterController controller;
        protected YokaiAttributes attributes;
        protected YokaiElementalStatus status;
        protected Transform aimPoint;
        protected bool activeCombat;
        protected bool attacking;
        protected bool dead;
        protected float nextAttack;
        protected float staggerUntil;
        protected float verticalSpeed;
        protected float orbitSeed;
        protected YokaiHumanoidVisual visual;
        protected int attackVersion;
        public float AttackNormalized { get; protected set; }
        public bool IsStaggered { get { return !dead && Time.time < staggerUntil; } }
        bool launched;
        float airHoldRemaining;
        float hangBudget;
        Vector3 reactionVelocity;
        bool slammed;
        float landingUntil;
        public float VerticalSpeed { get { return verticalSpeed; } }
        public bool IsLanding { get { return !dead && Time.time < landingUntil; } }
        public bool IsAirborne { get { return launched; } }
        Vector3 spawnPosition;
        Quaternion spawnRotation;

        public event Action<YokaiEnemy> Died;

        public Transform AimPoint { get { return aimPoint != null ? aimPoint : transform; } }
        public bool CanTarget { get { return !dead && gameObject.activeInHierarchy; } }
        public float TargetPriority { get { return archetype == YokaiEnemyArchetype.Boss ? 4f : (archetype == YokaiEnemyArchetype.Elite ? 2f : 1f); } }
        public bool IsDead { get { return dead; } }
        public bool IsAttacking { get { return attacking; } }
        public bool CanBeFinished
        {
            get
            {
                if (dead || launched || IsLanding || !attributes.IsPostureBroken) return false;
                if (archetype == YokaiEnemyArchetype.Boss)
                    return attributes.health <= attributes.maxHealth * .18f;
                return true;
            }
        }

        protected virtual void Awake()
        {
            controller = GetComponent<CharacterController>();
            attributes = GetComponent<YokaiAttributes>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            status = GetComponent<YokaiElementalStatus>();
            visual = GetComponent<YokaiHumanoidVisual>();
            orbitSeed = UnityEngine.Random.Range(0f, 100f);

            GameObject p = new GameObject("AimPoint");
            p.transform.SetParent(transform, false);
            p.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            aimPoint = p.transform;

            attributes.Died += HandleDeath;
            attributes.PostureBroken += HandlePostureBreak;
            activeCombat = startsActive;
        }

        public virtual void Configure(YokaiEnemyArchetype type, Transform player)
        {
            archetype = type;
            target = player;

            if (type == YokaiEnemyArchetype.Grunt)
            {
                attributes.Configure(82f,80f,1f,70f,16f,3f);
                moveSpeed=2.75f; preferredRange=2f; attackCooldown=1.7f; attackDamage=14f; postureDamage=14f; telegraphTime=.5f;
            }
            else if (type == YokaiEnemyArchetype.Ronin)
            {
                attributes.Configure(105f,95f,1f,90f,20f,5f);
                moveSpeed=3.25f; preferredRange=2.1f; attackCooldown=1.35f; attackDamage=18f; postureDamage=18f; telegraphTime=.38f;
            }
            else if (type == YokaiEnemyArchetype.Stalker)
            {
                attributes.Configure(76f,110f,1f,65f,18f,2f);
                moveSpeed=3.75f; preferredRange=1.9f; attackCooldown=1.15f; attackDamage=16f; postureDamage=12f; telegraphTime=.3f;
            }
            else if (type == YokaiEnemyArchetype.Heavy)
            {
                attributes.Configure(155f,80f,1f,145f,25f,9f);
                moveSpeed=2.05f; preferredRange=2.5f; attackCooldown=2.1f; attackDamage=27f; postureDamage=30f; telegraphTime=.72f;
            }
            else if (type == YokaiEnemyArchetype.Elite)
            {
                attributes.Configure(245f,110f,1f,175f,29f,10f);
                moveSpeed=3f; preferredRange=2.45f; attackCooldown=1.45f; attackDamage=26f; postureDamage=25f; telegraphTime=.46f;
            }

            activeCombat = startsActive;
        }

        public virtual void Activate()
        {
            activeCombat = true;
        }

        protected virtual void Update()
        {
            if (dead) return;
            // Reactions continue even when the target disappears or combat deactivates.
            if (launched || reactionVelocity.sqrMagnitude > .001f || IsLanding)
            { Gravity(); return; }
            if (target == null) { Gravity(); return; }
            YokaiAttributes targetStats = target.GetComponent<YokaiAttributes>();
            if (targetStats != null && targetStats.IsDead) { InterruptAttack(); Gravity(); return; }
            if (attributes.IsPostureBroken && Time.time >= staggerUntil) attributes.ResetPosture();

            float distance = Vector3.Distance(transform.position, target.position);
            if (!activeCombat && autoActivateByDistance && distance <= activationRadius) activeCombat = true;
            if (!activeCombat) { Gravity(); return; }

            if (launched) { Gravity(); return; }

            if (Time.time < staggerUntil)
            {
                Gravity();
                return;
            }

            if (attacking)
            {
                Gravity();
                return;
            }

            Vector3 to = target.position - transform.position;
            to.y = 0f;
            distance = to.magnitude;

            if (distance > preferredRange * 1.12f)
                Approach(to.normalized, 1f);
            else if (distance < preferredRange * .72f)
                Approach(-to.normalized, .55f);
            else
                Orbit(to.normalized);

            if (distance <= preferredRange * 1.15f && Time.time >= nextAttack &&
                YokaiCombatGeometry.Visible(AimPoint.position, target.position + Vector3.up, transform, target))
            {
                YokaiAttackCoordinator coordinator = YokaiAttackCoordinator.Instance;
                if (coordinator == null || coordinator.TryClaim(this))
                    StartCoroutine(AttackRoutine());
            }

            Gravity();
        }

        protected virtual void Approach(Vector3 dir, float speedScale)
        {
            if (dir.sqrMagnitude < .01f) return;
            float elementalSlow = status != null ? status.MoveMultiplier : 1f;
            controller.Move(dir * moveSpeed * speedScale * elementalSlow * Time.deltaTime);
            Quaternion q = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, turnSpeed * Time.deltaTime);
        }

        protected virtual void Orbit(Vector3 toward)
        {
            Vector3 side = Vector3.Cross(Vector3.up, toward) * (Mathf.Sin(Time.time*.65f + orbitSeed) > 0f ? 1f : -1f);
            Vector3 dir = (toward*.12f + side*.88f).normalized;
            Approach(dir, .55f);
            FaceTarget();
        }

        protected void FaceTarget()
        {
            if (target == null) return;
            Vector3 d = target.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < .01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), turnSpeed*Time.deltaTime);
        }

        protected virtual IEnumerator AttackRoutine()
        {
            int version = ++attackVersion;
            attacking = true;
            AttackNormalized = 0f;
            FaceTarget();

            float localTelegraph = telegraphTime;
            float localDamage = attackDamage;
            float localPosture = postureDamage;
            bool unblockable = false;
            int swings = 1;

            if (archetype == YokaiEnemyArchetype.Ronin)
            {
                localTelegraph *= .88f;
                swings = 2;
            }
            else if (archetype == YokaiEnemyArchetype.Stalker)
            {
                localTelegraph *= .7f;
                localDamage *= .88f;
                swings = 2;
            }
            else if (archetype == YokaiEnemyArchetype.Heavy)
            {
                localTelegraph *= 1.18f;
                localDamage *= 1.25f;
                localPosture *= 1.35f;
                unblockable = true;
            }
            else if (archetype == YokaiEnemyArchetype.Elite)
            {
                localTelegraph *= .82f;
                localDamage *= 1.08f;
                swings = 3;
            }

            Color telegraph = unblockable ? new Color(1f,.08f,.04f) : new Color(1f,.55f,.16f);
            YokaiVfx.Telegraph(transform.position, preferredRange*.58f, telegraph, localTelegraph);
            float began = Time.time;
            while (Time.time < began + localTelegraph)
            {
                if (!AttackValid(version)) yield break;
                AttackNormalized = Mathf.Clamp01((Time.time - began) / (localTelegraph + .3f));
                if (Time.time - began < localTelegraph * .65f) FaceTarget();
                yield return null;
            }

            for (int swingIndex=0; swingIndex<swings; swingIndex++)
            {
                if (!AttackValid(version)) yield break;

                if (archetype == YokaiEnemyArchetype.Stalker && swingIndex == 0)
                {
                    Vector3 dash = target.position-transform.position;
                    dash.y = 0f;
                    if (dash.sqrMagnitude > .1f)
                        controller.Move(dash.normalized * Mathf.Min(1.2f, dash.magnitude*.25f));
                }

                if (YokaiCombatGeometry.InArc(transform.position, transform.forward, target.position,
                    preferredRange * 1.48f, archetype == YokaiEnemyArchetype.Heavy ? 150f : 110f) &&
                    YokaiCombatGeometry.Visible(AimPoint.position, target.position + Vector3.up, transform, target))
                {
                    IYokaiDamageReceiver receiver = FindReceiver(target);
                    if (receiver != null)
                    {
                        float swingDamage = localDamage * (1f + swingIndex*.07f);
                        float swingPosture = localPosture * (1f + swingIndex*.05f);
                        receiver.ReceiveHit(new YokaiHit(gameObject, swingDamage, swingPosture, target.position,
                            (target.position-transform.position).normalized, YokaiElement.Physical,
                            archetype == YokaiEnemyArchetype.Heavy || archetype == YokaiEnemyArchetype.Elite,
                            unblockable && swingIndex == swings-1));
                    }
                }

                if (swings > 1 && swingIndex < swings-1)
                    yield return new WaitForSeconds(archetype == YokaiEnemyArchetype.Stalker ? .18f : .24f);
            }

            if (!AttackValid(version)) yield break;
            AttackNormalized = 1f;
            yield return new WaitForSeconds(.25f);
            if (!AttackValid(version)) yield break;
            nextAttack = Time.time + attackCooldown;
            attacking = false;
            if (YokaiAttackCoordinator.Instance != null) YokaiAttackCoordinator.Instance.Release(this);
        }


        public virtual void ResetEnemy()
        {
            StopAllCoroutines();
            InterruptAttack();
            if (status != null) status.Clear();
            gameObject.SetActive(true);
            dead = false;
            attacking = false;
            launched = false;
            airHoldRemaining = hangBudget = verticalSpeed = 0f;
            reactionVelocity = Vector3.zero;
            slammed = false;
            landingUntil = 0f;
            staggerUntil = 0f;
            nextAttack = Time.time + .8f;
            activeCombat = startsActive;

            if (controller != null) controller.enabled = false;
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;

            if (controller != null) controller.enabled = true;
            if (attributes != null)
            {
                attributes.RestoreFull();
                attributes.ResetPosture();
            }
        }

        public virtual bool ReceiveHit(YokaiHit hit)
        {
            if (dead) return false;

            float multiplier = status != null ? status.IncomingDamageMultiplier() : 1f;
            if (Time.time < staggerUntil) multiplier *= archetype == YokaiEnemyArchetype.Boss ? 1.18f : 1.28f;
            attributes.ApplyHealthDamage(hit.damage * multiplier);
            attributes.DamagePosture(hit.postureDamage, 2f);
            if (hit.element != YokaiElement.Physical && status != null) status.Apply(hit.element);

            // Heavy bodies retain armor against light hits; posture breaks always interrupt.
            if (!dead && (hit.heavy || archetype != YokaiEnemyArchetype.Heavy && archetype != YokaiEnemyArchetype.Boss))
            {
                InterruptAttack();
                staggerUntil = Mathf.Max(staggerUntil, Time.time + (hit.heavy ? .3f : .12f));
            }
            if (visual != null) visual.PlayHitReaction();

            if (hit.heavy && !dead)
            {
                Vector3 away = hit.direction; away.y = 0f;
                float resistance = archetype == YokaiEnemyArchetype.Boss ? .12f :
                    (archetype == YokaiEnemyArchetype.Heavy ? .35f : 1f);
                reactionVelocity = Vector3.ClampMagnitude(reactionVelocity + away.normalized * 2.4f * resistance, 5f);
            }

            return true;
        }

        public bool TryLaunch(float upwardSpeed, Vector3 direction = default(Vector3))
        {
            if (dead || launched || IsLanding || upwardSpeed <= 0f || !YokaiAirCombatRules.CanLaunch(archetype, attributes.IsPostureBroken)) return false;
            InterruptAttack();
            activeCombat = true;
            verticalSpeed = Mathf.Clamp(upwardSpeed, 1f, 12f);
            direction.y = 0f;
            reactionVelocity = direction.normalized * 1.25f;
            airHoldRemaining = 0f;
            slammed = false;
            launched = true;
            hangBudget = .4f;
            staggerUntil = Mathf.Max(staggerUntil, Time.time + .9f);
            return true;
        }

        public void HoldAir(float duration)
        {
            if (dead || !launched || slammed || duration <= 0f || hangBudget <= 0f) return;
            float hold = Mathf.Min(duration, hangBudget);
            airHoldRemaining += hold;
            hangBudget -= hold;
        }

        public bool SlamDown(Vector3 direction)
        {
            if (dead || !launched) return false;
            InterruptAttack();
            slammed = true;
            airHoldRemaining = hangBudget = 0f;
            verticalSpeed = -22f;
            direction.y = 0f;
            reactionVelocity = direction.normalized * 2f;
            return true;
        }

        protected bool AttackValid(int version)
        {
            return version == attackVersion && !dead && target != null && attacking;
        }

        protected void InterruptAttack()
        {
            attackVersion++;
            attacking = false;
            AttackNormalized = 0f;
            nextAttack = Mathf.Max(nextAttack, Time.time + .35f);
            if (YokaiAttackCoordinator.Instance != null) YokaiAttackCoordinator.Instance.Release(this);
        }

        public void Parried(float posture)
        {
            if (dead) return;
            InterruptAttack();
            staggerUntil = Mathf.Max(staggerUntil, Time.time + .65f);
            ApplyPostureDamage(posture);
        }

        protected virtual void OnDisable()
        {
            InterruptAttack();
            StopAllCoroutines();
        }

        protected virtual void OnDestroy()
        {
            if (attributes == null) return;
            attributes.Died -= HandleDeath;
            attributes.PostureBroken -= HandlePostureBreak;
        }

        public void ApplyPostureDamage(float value)
        {
            if (dead) return;
            attributes.DamagePosture(value, 2.4f);
        }

        public void ExecuteFinisher(float damage)
        {
            if (!CanBeFinished) return;
            attributes.ApplyHealthDamage(Mathf.Max(damage, attributes.health + attributes.defense + 1f));
        }

        protected virtual void HandlePostureBreak()
        {
            staggerUntil = Mathf.Max(staggerUntil, Time.time + (archetype == YokaiEnemyArchetype.Boss ? 1.4f : 2.1f));
            InterruptAttack();
            YokaiVfx.Ring(transform.position, new Color(1f,.82f,.25f), 1.25f, .35f);
        }

        protected virtual void HandleDeath()
        {
            if (dead) return;
            dead = true;
            InterruptAttack();
            if (YokaiAttackCoordinator.Instance != null) YokaiAttackCoordinator.Instance.Release(this);
            if (!launched) controller.enabled = false;
            YokaiVfx.Burst(transform.position + Vector3.up, new Color(.55f,.12f,.12f), .65f, .6f);
            if (Died != null) Died(this);
            StartCoroutine(DeathFade());
        }

        IEnumerator DeathFade()
        {
            // Air kills fall using the same collision solver before the dissolve.
            float timeout = Time.time + 3f;
            while (launched && Time.time < timeout)
            { Gravity(); yield return null; }
            launched = false;
            reactionVelocity = Vector3.zero;
            controller.enabled = false;
            float t = 0f;
            Vector3 start = transform.position;
            while (t < 1f)
            {
                t += Time.deltaTime / .8f;
                transform.position = start + Vector3.down * t * .6f;
                yield return null;
            }
            gameObject.SetActive(false);
        }

        void Gravity() { TickReaction(Time.deltaTime); }

        void TickReaction(float deltaTime)
        {
            if (controller == null || !controller.enabled) return;
            float remaining = Mathf.Clamp(deltaTime, 0f, .34f);
            // Short collision steps keep fast dives stable on low frame rate phones.
            while (remaining > 0f)
            {
                float dt = Mathf.Min(remaining, 1f / 60f);
                remaining -= dt;
                bool wasAirborne = launched;
                if (!launched && controller.isGrounded && verticalSpeed <= 0f) verticalSpeed = -2f;
                else if (airHoldRemaining > 0f && verticalSpeed <= 0f && !slammed)
                {
                    float hold = Mathf.Min(dt, airHoldRemaining);
                    airHoldRemaining -= hold;
                    verticalSpeed = -.8f - 26f * (dt - hold);
                }
                else verticalSpeed = Mathf.Max(-32f, verticalSpeed - 26f * dt);
                CollisionFlags flags = controller.Move((reactionVelocity + Vector3.up * verticalSpeed) * dt);
                if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0f) verticalSpeed = 0f;
                if ((flags & CollisionFlags.Sides) != 0) reactionVelocity = Vector3.zero;
                reactionVelocity = Vector3.MoveTowards(reactionVelocity, Vector3.zero, (launched ? 1.8f : 12f) * dt);
                if (((flags & CollisionFlags.Below) != 0 || controller.isGrounded) && verticalSpeed <= 0f)
                {
                    launched = false;
                    verticalSpeed = -2f;
                    airHoldRemaining = hangBudget = 0f;
                    if (wasAirborne)
                    {
                        float recovery = slammed ? .85f : .42f;
                        landingUntil = Time.time + recovery;
                        staggerUntil = Mathf.Max(staggerUntil, landingUntil);
                        nextAttack = Mathf.Max(nextAttack, landingUntil + .25f);
                        if (!dead && Application.isPlaying) YokaiVfx.Ring(transform.position, slammed ? new Color(1f,.5f,.12f) : new Color(.65f,.6f,.5f), slammed ? 1.1f : .5f, .22f);
                    }
                    slammed = false;
                }
            }
        }

        IYokaiDamageReceiver FindReceiver(Transform root)
        {
            MonoBehaviour[] list = root.GetComponents<MonoBehaviour>();
            for (int i=0;i<list.Length;i++)
            {
                IYokaiDamageReceiver r = list[i] as IYokaiDamageReceiver;
                if (r != null) return r;
            }
            return null;
        }
    }
}
