using System.Collections;
using UnityEngine;
namespace Yokai
{
    public enum YokaiSkill { CrescentDash, BladeStorm, SpiritWard }
    [RequireComponent(typeof(YokaiCombat))]
    public sealed class YokaiSkills : MonoBehaviour
    {
        public YokaiSkill ActiveSkill { get; private set; }
        public float dashCooldown = 6f;
        public float stormCooldown = 10f;
        public float wardCooldown = 14f;
        public float wardDuration = 5f;
        readonly float[] readyAt = new float[3];
        YokaiAttributes attributes;
        YokaiMotor motor;
        YokaiCombat combat;
        float wardUntil, wardCapacity;
        public int PresentationIndex { get; private set; }
        public float ActionDuration { get { return ActiveSkill == YokaiSkill.BladeStorm ? .95f : .55f; } }
        public float CooldownRemaining { get { return Mathf.Max(0f, readyAt[(int)ActiveSkill] - Time.time); } }
        public float SpiritCost { get { return ActiveSkill == YokaiSkill.CrescentDash ? 15f : ActiveSkill == YokaiSkill.BladeStorm ? 30f : 25f; } }
        public bool WardActive { get { return attributes != null && !attributes.IsDead && wardCapacity > 0f && Time.time < wardUntil; } }
        public string SkillName
        {
            get { return ActiveSkill == YokaiSkill.CrescentDash ? "CRESCENT DASH" :
                ActiveSkill == YokaiSkill.BladeStorm ? "BLADE STORM" : "SPIRIT WARD"; }
        }
        void Awake()
        {
            attributes = GetComponent<YokaiAttributes>();
            motor = GetComponent<YokaiMotor>();
            combat = GetComponent<YokaiCombat>();
        }
        public void Cycle()
        {
            if (combat.State == YokaiActionState.Skill) return;
            ActiveSkill = (YokaiSkill)(((int)ActiveSkill + 1) % 3);
            PresentationIndex = (int)ActiveSkill;
        }
        public bool TryCast(int expectedVersion)
        {
            int index = (int)ActiveSkill;
            float cost = SpiritCost;
            if (attributes.IsDead || Time.time < readyAt[index] || !attributes.ConsumeSpirit(cost)) return false;
            readyAt[index] = Time.time + (ActiveSkill == YokaiSkill.CrescentDash ? dashCooldown :
                ActiveSkill == YokaiSkill.BladeStorm ? stormCooldown : wardCooldown);
            PresentationIndex = index;
            StartCoroutine(CastRoutine(ActiveSkill, expectedVersion));
            return true;
        }
        bool Valid(int version) { return !attributes.IsDead && combat.ActionVersion == version; }
        IEnumerator CastRoutine(YokaiSkill skill, int version)
        {
            yield return new WaitForSeconds(skill == YokaiSkill.CrescentDash ? .12f : .22f);
            if (!Valid(version)) yield break;
            if (skill == YokaiSkill.CrescentDash)
            {
                Vector3 direction = transform.forward;
                motor.BeginBurst(direction, 11f, .22f);
                float until = Time.time + .22f;
                while (Time.time < until)
                {
                    if (!Valid(version)) yield break;
                    yield return null;
                }
                if (!Valid(version)) yield break;
                int hit = YokaiAreaDamage.Apply(transform, attributes.attackPower * 1.75f, 28f, 2.7f,
                    YokaiElement.Physical, true, combat.hittableMask, 130f);
                if (hit > 0) attributes.AddBladeFlow(12f);
                YokaiVfx.Ring(transform.position, new Color(.4f,.8f,1f), 2f, .25f);
            }
            else if (skill == YokaiSkill.BladeStorm)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (!Valid(version)) yield break;
                    int hit = YokaiAreaDamage.Apply(transform, attributes.attackPower * .7f, 13f, 3.2f,
                        YokaiElement.Physical, false, combat.hittableMask);
                    if (hit > 0) attributes.AddBladeFlow(4f);
                    YokaiVfx.Ring(transform.position, new Color(.8f,.85f,1f), 3.2f, .18f);
                    if (i < 2) yield return new WaitForSeconds(.2f);
                }
            }
            else
            {
                wardUntil = Time.time + wardDuration;
                wardCapacity = attributes.maxHealth * .3f;
                YokaiVfx.Ring(transform.position, new Color(.45f,.3f,1f), 1.3f, .45f);
            }
            YokaiAudio.Play("hunter_art", transform.position, .8f, 1f);
        }
        public float Absorb(float damage)
        {
            if (!WardActive) return damage;
            float absorbed = Mathf.Min(Mathf.Max(0f, damage), wardCapacity);
            wardCapacity -= absorbed;
            if (absorbed > 0f) YokaiVfx.Burst(transform.position + Vector3.up, new Color(.45f,.3f,1f), .25f, .2f);
            return Mathf.Max(0f, damage - absorbed);
        }
        public void ResetSkills()
        {
            StopAllCoroutines();
            wardUntil = wardCapacity = 0f;
            for (int i = 0; i < readyAt.Length; i++) readyAt[i] = 0f;
        }
        void OnDisable() { StopAllCoroutines(); }
    }
}
