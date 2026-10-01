using UnityEngine;

namespace Yokai
{
    [RequireComponent(typeof(YokaiCombat))]
    public sealed class YokaiWeaponTrail : MonoBehaviour
    {
        public Transform weapon;
        public float width = .08f;
        public float lifetime = .15f;

        YokaiCombat combat;
        TrailRenderer trail;
        GameObject trailObject;

        void Start()
        {
            combat = GetComponent<YokaiCombat>();

            YokaiHumanoidVisual visual = GetComponent<YokaiHumanoidVisual>();
            if (weapon == null && visual != null) weapon = visual.weapon;
            if (weapon == null) return;

            trailObject = new GameObject("WeaponTrail");
            trailObject.transform.SetParent(weapon, false);
            trailObject.transform.localPosition = weapon.name == "WeaponTip" ? Vector3.zero : new Vector3(0f,0f,.45f);

            trail = trailObject.AddComponent<TrailRenderer>();
            trail.time = lifetime;
            trail.startWidth = width;
            trail.endWidth = 0f;
            trail.minVertexDistance = .035f;
            trail.autodestruct = false;
            trail.emitting = false;
            trail.material = YokaiMaterialLibrary.Emissive("weapon_trail", new Color(.75f,.88f,1f));
        }

        void LateUpdate()
        {
            if (trail == null || combat == null) return;
            bool active =
                combat.State == YokaiActionState.Light ||
                combat.State == YokaiActionState.Heavy ||
                combat.State == YokaiActionState.Art ||
                combat.State == YokaiActionState.Finisher;
            trail.emitting = active && combat.ActionNormalized > .16f && combat.ActionNormalized < .72f;
        }
    }
}
