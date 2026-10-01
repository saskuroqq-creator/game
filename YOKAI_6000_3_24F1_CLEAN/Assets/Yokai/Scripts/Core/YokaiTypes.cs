using UnityEngine;

namespace Yokai
{
    public enum YokaiStance { Gale, Stone, Spirit }
    public enum YokaiElement { Physical, Fire, Storm, Spirit, Shadow }
    public enum YokaiEnemyArchetype { Grunt, Ronin, Stalker, Heavy, Elite, Boss }
    public enum YokaiActionState { Free, Light, Heavy, Dodge, Parry, Guard, Art, Finisher, Hit, Stagger, Dead, Jump, Launcher, AirLight, AirSlam, Skill, Landing }
    public enum YokaiGameStage { Explore, FirstEncounter, EliteHunt, Shrine, Boss, Victory }

    public struct YokaiHit
    {
        public GameObject source;
        public float damage;
        public float postureDamage;
        public Vector3 point;
        public Vector3 direction;
        public YokaiElement element;
        public bool heavy;
        public bool unblockable;

        public YokaiHit(GameObject source, float damage, float postureDamage, Vector3 point,
            Vector3 direction, YokaiElement element, bool heavy, bool unblockable)
        {
            this.source = source;
            this.damage = damage;
            this.postureDamage = postureDamage;
            this.point = point;
            this.direction = direction;
            this.element = element;
            this.heavy = heavy;
            this.unblockable = unblockable;
        }
    }

    public interface IYokaiDamageReceiver
    {
        bool ReceiveHit(YokaiHit hit);
    }

    public interface IYokaiTargetable
    {
        Transform AimPoint { get; }
        bool CanTarget { get; }
        float TargetPriority { get; }
    }

    public interface IYokaiElementReceiver
    {
        void ApplyElement(YokaiElement element);
    }
}
