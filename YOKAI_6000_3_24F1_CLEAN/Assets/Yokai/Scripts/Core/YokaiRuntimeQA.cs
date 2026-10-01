using System.Collections;
using System.Linq;
using UnityEngine;

namespace Yokai
{
    public sealed class YokaiRuntimeQA : MonoBehaviour
    {
        IEnumerator Start()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(.25f);

            string failure = Validate();
            if (string.IsNullOrEmpty(failure))
                Debug.Log("[YOKAI_RUNTIME_QA_PASS] player/camera/mobile/combat/3D-art/encounters/boss/world ready");
            else
                Debug.LogError("[YOKAI_RUNTIME_QA_FAIL] " + failure);
        }

        string Validate()
        {
            YokaiGameSession session = YokaiGameSession.Instance;
            if (session == null) return "GameSession missing";
            if (session.player == null) return "Player missing";
            if (Camera.main == null) return "Main Camera missing";

            GameObject player = session.player.gameObject;
            if (player.GetComponent<CharacterController>() == null) return "Player CharacterController missing";
            if (player.GetComponent<YokaiAttributes>() == null) return "Player attributes missing";
            if (player.GetComponent<YokaiMotor>() == null) return "Player motor missing";
            if (player.GetComponent<YokaiCombat>() == null) return "Player combat missing";
            if (player.GetComponent<YokaiLockOn>() == null) return "Player lock-on missing";
            if (player.GetComponent<YokaiMobileHUD>() == null) return "Mobile HUD missing";
            if (player.GetComponent<YokaiHumanoidVisual>() == null) return "Player 3D visual missing";
            if (player.transform.Find("HumanoidRig_3D") == null) return "Player V1.4 3D rig missing";
            if (player.GetComponentsInChildren<MeshFilter>(true).Length < 12) return "Player 3D model is incomplete";

            GameObject artRoot = GameObject.Find("YOKAI_ART_WORLD_V14");
            if (artRoot == null) return "V1.4 3D world art root missing";
            if (artRoot.GetComponentsInChildren<MeshRenderer>(true).Length < 80) return "3D world art population incomplete";
            if (GameObject.Find("ZONE_01_CedarPass") == null || GameObject.Find("ZONE_02_ForsakenHamlet") == null ||
                GameObject.Find("ZONE_03_SpiritShrineBasin") == null || GameObject.Find("ZONE_04_OniCourtyard") == null)
                return "One or more V1.4 world zones missing";

            YokaiEncounterDirector director = session.director;
            if (director == null) return "EncounterDirector missing";
            if (director.firstEncounter == null || director.firstEncounter.Count != 3) return "First encounter must contain 3 enemies";
            if (director.eliteEncounter == null || director.eliteEncounter.Count != 3) return "Elite encounter must contain 3 enemies";
            if (director.firstEncounter.Any(x => x == null) || director.eliteEncounter.Any(x => x == null)) return "Encounter contains null enemy";
            foreach (YokaiEnemy e in director.firstEncounter.Concat(director.eliteEncounter))
            {
                if (e.GetComponent<YokaiHumanoidVisual>() == null || e.GetComponentsInChildren<MeshFilter>(true).Length < 10)
                    return "Enemy 3D model incomplete: " + e.name;
            }
            if (director.boss == null) return "Boss missing";
            if (director.boss.GetComponentsInChildren<MeshFilter>(true).Length < 16) return "Boss 3D model incomplete";
            if (director.gateOne == null || director.gateTwo == null) return "Encounter gates missing";

            if (Object.FindFirstObjectByType<YokaiProjectilePool>() == null) return "Projectile pool missing";
            if (Object.FindFirstObjectByType<YokaiPerformanceManager>() == null) return "Performance manager missing";
            if (Object.FindFirstObjectByType<YokaiAudioManager>() == null) return "Audio manager missing";

            RaycastHit hit;
            if (!Physics.Raycast(session.player.position + Vector3.up, Vector3.down, out hit, 4f, ~0, QueryTriggerInteraction.Ignore))
                return "No walkable ground below player spawn";

            return null;
        }
    }
}
