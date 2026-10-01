using System.Collections;
using UnityEngine;

namespace Yokai
{
    public sealed class YokaiBoss : YokaiEnemy
    {
        public int Phase { get; private set; }
        int patternIndex;
        float baseMove;
        float baseCooldown;
        float baseDamage;
        bool transitionInvulnerable;
        bool introPlayed;

        protected override void Awake()
        {
            base.Awake();
            Phase = 1;
        }

        public void ConfigureBoss(Transform player)
        {
            archetype = YokaiEnemyArchetype.Boss;
            target = player;
            attributes.Configure(920f,140f,1f,260f,34f,13f);
            moveSpeed = 2.55f;
            preferredRange = 2.8f;
            attackCooldown = 1.8f;
            attackDamage = 31f;
            postureDamage = 30f;
            telegraphTime = .58f;
            activationRadius = 23f;
            startsActive = false;
            activeCombat = false;
            autoActivateByDistance = false;
            baseMove = moveSpeed;
            baseCooldown = attackCooldown;
            baseDamage = attackDamage;
        }

        public override void Activate()
        {
            if (introPlayed)
            {
                base.Activate();
                return;
            }

            introPlayed = true;
            StartCoroutine(IntroRoutine());
        }

        IEnumerator IntroRoutine()
        {
            transitionInvulnerable = true;
            activeCombat = false;
            attacking = false;

            YokaiAudio.Play("boss_telegraph", transform.position, .9f, .88f);
            if (YokaiAudioManager.Instance != null) YokaiAudioManager.Instance.SetBossMode(true);
            YokaiVfx.Ring(transform.position, new Color(.72f,.05f,.04f), 5f, 1.0f);
            if (YokaiFeedbackManager.Instance != null)
                YokaiFeedbackManager.Instance.CameraImpulse(.55f);

            yield return new WaitForSeconds(.95f);

            activeCombat = true;
            transitionInvulnerable = false;
            nextAttack = Time.time + .75f;
        }

        protected override void Update()
        {
            if (!dead)
            {
                float ratio = attributes.health / Mathf.Max(1f, attributes.maxHealth);
                int nextPhase = ratio > .66f ? 1 : (ratio > .33f ? 2 : 3);
                if (nextPhase != Phase)
                {
                    Phase = nextPhase;
                    StartCoroutine(PhaseTransitionRoutine());
                }

                moveSpeed = baseMove * (Phase == 1 ? 1f : (Phase == 2 ? 1.12f : 1.25f));
                attackCooldown = baseCooldown * (Phase == 1 ? 1f : (Phase == 2 ? .82f : .68f));
                attackDamage = baseDamage * (1f + (Phase-1)*.16f);
            }

            base.Update();
        }

        IEnumerator PhaseTransitionRoutine()
        {
            transitionInvulnerable = true;
            InterruptAttack();
            staggerUntil = Time.time + .75f;

            Color color = Phase == 2 ? new Color(.8f,.18f,.1f) : new Color(.55f,.12f,.85f);
            YokaiVfx.Ring(transform.position, color, 4.4f, .8f);
            YokaiVfx.Burst(transform.position + Vector3.up*1.3f, color, .75f, .55f);

            if (YokaiFeedbackManager.Instance != null)
                YokaiFeedbackManager.Instance.CameraImpulse(.6f);

            yield return new WaitForSeconds(.72f);
            transitionInvulnerable = false;
            nextAttack = Time.time + .45f;
        }

        public override bool ReceiveHit(YokaiHit hit)
        {
            if (transitionInvulnerable) return false;
            return base.ReceiveHit(hit);
        }

        public override void ResetEnemy()
        {
            StopAllCoroutines();
            base.ResetEnemy();
            Phase = 1;
            patternIndex = 0;
            transitionInvulnerable = false;
            introPlayed = false;
            activeCombat = false;
            attacking = false;
            moveSpeed = baseMove;
            attackCooldown = baseCooldown;
            attackDamage = baseDamage;
            nextAttack = Time.time + .8f;
            if (YokaiAudioManager.Instance != null) YokaiAudioManager.Instance.SetBossMode(false);
        }

        protected override IEnumerator AttackRoutine()
        {
            int version = ++attackVersion;
            attacking = true;
            AttackNormalized = 0f;
            FaceTarget();

            patternIndex = (patternIndex + 1) % 3;
            float telegraph = telegraphTime;
            float radius = preferredRange*.65f;
            Color color = new Color(1f,.2f,.08f);

            if (patternIndex == 1)
            {
                telegraph *= 1.15f;
                radius = 1.2f;
                color = new Color(.95f,.55f,.1f);
            }
            else if (patternIndex == 2)
            {
                telegraph *= .9f;
                radius = 3.8f;
                color = new Color(.55f,.18f,.9f);
            }

            YokaiAudio.Play("boss_telegraph", transform.position, .72f, Phase == 3 ? 1.12f : 1f);
            YokaiVfx.Telegraph(transform.position, radius, color, telegraph);
            float began = Time.time;
            while (Time.time < began + telegraph)
            {
                if (!AttackValid(version)) yield break;
                AttackNormalized = Mathf.Clamp01((Time.time - began) / (telegraph + .45f));
                if (Time.time - began < telegraph * .65f) FaceTarget();
                yield return null;
            }

            if (AttackValid(version))
            {
                if (patternIndex == 0)
                {
                    MeleeHit(attackDamage, postureDamage, preferredRange*1.45f, false);
                }
                else if (patternIndex == 1)
                {
                    Vector3 leap = transform.forward * 2.1f;
                    leap.y = 0f;
                    if (leap.magnitude > 1f) controller.Move(leap.normalized * Mathf.Min(2.1f, leap.magnitude*.45f));
                    MeleeHit(attackDamage*1.35f, postureDamage*1.55f, preferredRange*1.65f, true);
                    YokaiVfx.Ring(transform.position, new Color(1f,.45f,.08f), 2.4f, .4f);
                }
                else
                {
                    if (Phase >= 2) SpiritProjectileBurst();
                    else AreaBurst();
                }
            }

            if (!AttackValid(version)) yield break;
            AttackNormalized = 1f;
            yield return new WaitForSeconds(Phase == 3 ? .3f : .5f);
            if (!AttackValid(version)) yield break;
            nextAttack = Time.time + attackCooldown;
            attacking = false;
            if (YokaiAttackCoordinator.Instance != null) YokaiAttackCoordinator.Instance.Release(this);
        }

        void MeleeHit(float damage, float posture, float range, bool unblockable)
        {
            if (target == null || !YokaiCombatGeometry.InArc(transform.position, transform.forward,
                target.position, range, unblockable ? 160f : 140f) ||
                !YokaiCombatGeometry.Visible(AimPoint.position, target.position + Vector3.up, transform, target)) return;
            MonoBehaviour[] list = target.GetComponents<MonoBehaviour>();
            for (int i=0;i<list.Length;i++)
            {
                IYokaiDamageReceiver r = list[i] as IYokaiDamageReceiver;
                if (r != null)
                {
                    r.ReceiveHit(new YokaiHit(gameObject, damage, posture, target.position,
                        (target.position-transform.position).normalized, YokaiElement.Physical, true, unblockable));
                    break;
                }
            }
        }

        void AreaBurst()
        {
            YokaiVfx.Ring(transform.position, new Color(.55f,.16f,.9f), 4.2f, .55f);
            if (target == null || Vector3.Distance(transform.position,target.position) > 4.4f ||
                !YokaiCombatGeometry.Visible(AimPoint.position, target.position + Vector3.up, transform, target)) return;
            MonoBehaviour[] list = target.GetComponents<MonoBehaviour>();
            for (int i=0;i<list.Length;i++)
            {
                IYokaiDamageReceiver r = list[i] as IYokaiDamageReceiver;
                if (r != null)
                {
                    r.ReceiveHit(new YokaiHit(gameObject, attackDamage*1.1f, postureDamage*1.15f, target.position,
                        (target.position-transform.position).normalized, YokaiElement.Shadow, true, false));
                    break;
                }
            }
        }


        void SpiritProjectileBurst()
        {
            YokaiVfx.Ring(transform.position, new Color(.55f,.16f,.9f), 4.2f, .55f);

            if (YokaiProjectilePool.Instance == null)
            {
                AreaBurst();
                return;
            }

            int count = Phase == 2 ? 7 : 11;
            float speed = Phase == 2 ? 7.2f : 8.8f;
            Vector3 origin = transform.position + Vector3.up * 1.45f;
            Vector3 forward = target != null ? (target.position-origin).normalized : transform.forward;

            for (int i=0;i<count;i++)
            {
                float angle = (360f/count)*i;
                Quaternion spread = Quaternion.AngleAxis(angle, Vector3.up);
                Vector3 dir = spread * new Vector3(forward.x, 0f, forward.z).normalized;
                dir.y = Phase == 3 ? .08f : .03f;

                YokaiProjectile projectile = YokaiProjectilePool.Instance.Rent();
                projectile.Launch(gameObject, origin, dir, speed, attackDamage*.72f, postureDamage*.62f,
                    YokaiElement.Shadow, 3.2f);
            }

            if (target != null && Vector3.Distance(transform.position,target.position) < 2.4f)
                MeleeHit(attackDamage*.8f, postureDamage*.7f, 2.5f, false);
        }

        protected override void HandleDeath()
        {
            base.HandleDeath();
            if (YokaiAudioManager.Instance != null) YokaiAudioManager.Instance.SetBossMode(false);
            YokaiSaveSystem.SetBossDefeated(true);
            if (YokaiGameSession.Instance != null)
                YokaiGameSession.Instance.BossDefeated(this);
        }
    }
}
