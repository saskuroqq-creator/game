using UnityEngine;
namespace Yokai
{
    // Optional presentation layer. Missing/unready art keeps the working V14 fallback.
    [DefaultExecutionOrder(-50)]
    public sealed class YokaiAuthoredActor : MonoBehaviour
    {
        public GameObject visualPrefab;
        public string resourcePath;
        public Transform WeaponSocket { get; private set; }
        public Transform WeaponTip { get; private set; }
        public Transform ChestSocket { get; private set; }
        public Transform HeadSocket { get; private set; }
        Animator animator;
        YokaiEnemy enemy;
        YokaiBoss boss;
        YokaiAttributes attributes;
        Vector3 previousPosition;
        int landingHash, verticalHash, airborneHash, speedHash, attackingHash, phaseHash, deadHash, staggerHash, progressHash;
        void Start()
        {
            if (visualPrefab == null && !string.IsNullOrEmpty(resourcePath)) visualPrefab = Resources.Load<GameObject>(resourcePath);
            if (visualPrefab == null) return;
            GameObject instance = Instantiate(visualPrefab, transform, false);
            animator = instance.GetComponentInChildren<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("Yokai art prefab requires Animator + controller: " + resourcePath, this);
                Destroy(instance);
                return;
            }
            animator.applyRootMotion = false;
            foreach (var collider in instance.GetComponentsInChildren<Collider>()) collider.enabled = false;
            var fallback = transform.Find("HumanoidRig_3D");
            if (fallback != null) fallback.gameObject.SetActive(false);
            var pose = GetComponent<YokaiPoseAnimator>(); if (pose != null) pose.enabled = false;
            var grounding = GetComponent<YokaiFootGrounding>(); if (grounding != null) grounding.enabled = false;
            var visual = GetComponent<YokaiHumanoidVisual>(); if (visual != null) visual.enabled = false;
            WeaponSocket = Find(instance.transform, "WeaponSocket");
            WeaponTip = Find(instance.transform, "WeaponTip");
            ChestSocket = Find(instance.transform, "ChestSocket");
            HeadSocket = Find(instance.transform, "HeadSocket");
            var trail = GetComponent<YokaiWeaponTrail>();
            if (trail != null) trail.weapon = WeaponTip != null ? WeaponTip : WeaponSocket;
            var bridge = GetComponent<YokaiAnimatorBridge>(); if (bridge != null) bridge.animator = animator;
            enemy = GetComponent<YokaiEnemy>();
            boss = GetComponent<YokaiBoss>();
            attributes = GetComponent<YokaiAttributes>();
            previousPosition = transform.position;
            landingHash = Parameter("Landing", AnimatorControllerParameterType.Bool);
            verticalHash = Parameter("VerticalSpeed", AnimatorControllerParameterType.Float);
            airborneHash = Parameter("Airborne", AnimatorControllerParameterType.Bool);
            speedHash = Parameter("Speed", AnimatorControllerParameterType.Float);
            progressHash = Parameter("AttackNormalized", AnimatorControllerParameterType.Float);
            attackingHash = Parameter("Attacking", AnimatorControllerParameterType.Bool);
            deadHash = Parameter("Dead", AnimatorControllerParameterType.Bool);
            staggerHash = Parameter("Staggered", AnimatorControllerParameterType.Bool);
            phaseHash = Parameter("Phase", AnimatorControllerParameterType.Int);
        }
        void Update()
        {
            if (animator == null || enemy == null) return;
            float speed = Time.deltaTime > 0f ? Vector3.Distance(transform.position, previousPosition) / Time.deltaTime : 0f;
            previousPosition = transform.position;
            if (landingHash != 0) animator.SetBool(landingHash, enemy.IsLanding);
            if (verticalHash != 0) animator.SetFloat(verticalHash, enemy.VerticalSpeed);
            if (airborneHash != 0) animator.SetBool(airborneHash, enemy.IsAirborne);
            if (speedHash != 0) animator.SetFloat(speedHash, speed);
            if (progressHash != 0) animator.SetFloat(progressHash, enemy.AttackNormalized);
            if (attackingHash != 0) animator.SetBool(attackingHash, enemy.IsAttacking);
            if (deadHash != 0) animator.SetBool(deadHash, attributes != null && attributes.IsDead);
            if (staggerHash != 0) animator.SetBool(staggerHash, enemy.IsStaggered);
            if (phaseHash != 0) animator.SetInteger(phaseHash, boss != null ? boss.Phase : 1);
        }
        int Parameter(string name, AnimatorControllerParameterType type)
        {
            int hash = Animator.StringToHash(name);
            foreach (var p in animator.parameters) if (p.nameHash == hash && p.type == type) return hash;
            return 0;
        }
        static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
    }
}
