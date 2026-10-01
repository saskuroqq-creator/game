using System;
using UnityEngine;

namespace Yokai
{
    public sealed class YokaiAttributes : MonoBehaviour
    {
        public float maxHealth = 120f;
        public float health = 120f;
        public float maxStamina = 100f;
        public float stamina = 100f;
        public float maxSpirit = 100f;
        public float spirit = 25f;
        public float maxPosture = 100f;
        public float posture = 0f;
        [Range(0f,100f)] public float bladeFlow = 0f;
        public float attackPower = 22f;
        public float defense = 5f;

        public float staminaRegen = 18f;
        public float postureRegen = 10f;
        public float bladeFlowDecay = 3.2f;
        public float bladeFlowGrace = 2.5f;

        public bool IsDead { get; private set; }
        public bool IsPostureBroken { get { return posture >= maxPosture - 0.01f; } }

        public event Action<float,float> HealthChanged;
        public event Action<float,float> StaminaChanged;
        public event Action<float,float> SpiritChanged;
        public event Action<float,float> PostureChanged;
        public event Action<float,float> BladeFlowChanged;
        public event Action PostureBroken;
        public event Action Died;
        public event Action Revived;

        public float staminaRegenDelay = .65f;
        float staminaRegenBlockedUntil;
        float lastFlowGain;
        float postureRegenBlockedUntil;

        void Awake()
        {
            ClampAll();
        }

        void Update()
        {
            if (IsDead) return;

            if (stamina < maxStamina && Time.time >= staminaRegenBlockedUntil)
            {
                stamina = Mathf.Min(maxStamina, stamina + staminaRegen * Time.deltaTime);
                if (StaminaChanged != null) StaminaChanged(stamina, maxStamina);
            }

            if (posture > 0f && Time.time >= postureRegenBlockedUntil)
            {
                posture = Mathf.Max(0f, posture - postureRegen * Time.deltaTime);
                if (PostureChanged != null) PostureChanged(posture, maxPosture);
            }

            if (bladeFlow > 0f && Time.time - lastFlowGain >= bladeFlowGrace)
            {
                bladeFlow = Mathf.Max(0f, bladeFlow - bladeFlowDecay * Time.deltaTime);
                if (BladeFlowChanged != null) BladeFlowChanged(bladeFlow, 100f);
            }
        }

        public void Configure(float hp, float sta, float spi, float pos, float atk, float def)
        {
            maxHealth = health = hp;
            maxStamina = stamina = sta;
            maxSpirit = Mathf.Max(1f, spi);
            spirit = Mathf.Min(spirit, maxSpirit);
            maxPosture = Mathf.Max(1f, pos);
            posture = 0f;
            attackPower = atk;
            defense = def;
            IsDead = false;
            ClampAll();
        }

        public bool ConsumeStamina(float value)
        {
            value = Mathf.Max(0f, value);
            if (IsDead || stamina + 0.001f < value) return false;
            stamina -= value;
            if (value > 0f) staminaRegenBlockedUntil = Time.time + staminaRegenDelay;
            if (StaminaChanged != null) StaminaChanged(stamina, maxStamina);
            return true;
        }

        public bool ConsumeSpirit(float value)
        {
            value = Mathf.Max(0f, value);
            if (IsDead || spirit + 0.001f < value) return false;
            spirit -= value;
            if (SpiritChanged != null) SpiritChanged(spirit, maxSpirit);
            return true;
        }

        public void AddSpirit(float value)
        {
            if (IsDead) return;
            spirit = Mathf.Clamp(spirit + value, 0f, maxSpirit);
            if (SpiritChanged != null) SpiritChanged(spirit, maxSpirit);
        }

        public void AddBladeFlow(float value)
        {
            if (IsDead) return;
            bladeFlow = Mathf.Clamp(bladeFlow + value, 0f, 100f);
            if (value > 0f) lastFlowGain = Time.time;
            if (BladeFlowChanged != null) BladeFlowChanged(bladeFlow, 100f);
        }

        public bool SpendBladeFlow(float value)
        {
            if (bladeFlow + 0.001f < value) return false;
            bladeFlow = Mathf.Max(0f, bladeFlow - value);
            if (BladeFlowChanged != null) BladeFlowChanged(bladeFlow, 100f);
            return true;
        }

        public void DamagePosture(float value, float regenDelay)
        {
            if (IsDead || value <= 0f) return;
            bool wasBroken = IsPostureBroken;
            posture = Mathf.Clamp(posture + value, 0f, maxPosture);
            postureRegenBlockedUntil = Mathf.Max(postureRegenBlockedUntil, Time.time + regenDelay);
            if (PostureChanged != null) PostureChanged(posture, maxPosture);
            if (!wasBroken && IsPostureBroken && PostureBroken != null) PostureBroken();
        }

        public void ResetPosture()
        {
            posture = 0f;
            if (PostureChanged != null) PostureChanged(posture, maxPosture);
        }

        public float ApplyHealthDamage(float rawDamage)
        {
            if (IsDead || rawDamage <= 0f) return 0f;
            float finalDamage = Mathf.Max(1f, rawDamage - defense);
            health = Mathf.Max(0f, health - finalDamage);
            if (HealthChanged != null) HealthChanged(health, maxHealth);

            if (health <= 0f)
            {
                IsDead = true;
                if (Died != null) Died();
            }
            return finalDamage;
        }

        public void Heal(float value)
        {
            if (IsDead || value <= 0f) return;
            health = Mathf.Min(maxHealth, health + value);
            if (HealthChanged != null) HealthChanged(health, maxHealth);
        }

        public void RestoreFull()
        {
            IsDead = false;
            health = maxHealth;
            stamina = maxStamina;
            posture = 0f;
            if (HealthChanged != null) HealthChanged(health, maxHealth);
            if (StaminaChanged != null) StaminaChanged(stamina, maxStamina);
            if (PostureChanged != null) PostureChanged(posture, maxPosture);
        }

        public void ReviveAt(float fraction)
        {
            if (!IsDead) return;
            IsDead = false;
            health = Mathf.Clamp(maxHealth * fraction, 1f, maxHealth);
            stamina = maxStamina * 0.5f;
            posture = 0f;
            if (HealthChanged != null) HealthChanged(health, maxHealth);
            if (StaminaChanged != null) StaminaChanged(stamina, maxStamina);
            if (PostureChanged != null) PostureChanged(posture, maxPosture);
            if (Revived != null) Revived();
        }

        void ClampAll()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            maxStamina = Mathf.Max(1f, maxStamina);
            maxSpirit = Mathf.Max(1f, maxSpirit);
            maxPosture = Mathf.Max(1f, maxPosture);
            health = Mathf.Clamp(health, 0f, maxHealth);
            stamina = Mathf.Clamp(stamina, 0f, maxStamina);
            spirit = Mathf.Clamp(spirit, 0f, maxSpirit);
            posture = Mathf.Clamp(posture, 0f, maxPosture);
            bladeFlow = Mathf.Clamp(bladeFlow, 0f, 100f);
        }
    }
}
