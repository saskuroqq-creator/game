using System.Collections.Generic;
using UnityEngine;

namespace Yokai
{
    public sealed class YokaiEncounterDirector : MonoBehaviour
    {
        public Transform player;
        public List<YokaiEnemy> firstEncounter = new List<YokaiEnemy>();
        public List<YokaiEnemy> eliteEncounter = new List<YokaiEnemy>();
        public YokaiBoss boss;
        public GameObject gateOne;
        public GameObject gateTwo;

        bool firstStarted;
        bool eliteStarted;
        bool bossStarted;
        bool firstCleared;
        bool eliteCleared;

        void Start()
        {
            Subscribe(firstEncounter);
            Subscribe(eliteEncounter);
            if (boss != null) boss.Died += OnEnemyDied;
        }

        void Subscribe(List<YokaiEnemy> list)
        {
            for (int i=0;i<list.Count;i++)
                if (list[i] != null) list[i].Died += OnEnemyDied;
        }

        void Update()
        {
            if (player == null) return;

            float z = player.position.z;

            if (!firstStarted && z > -1f)
            {
                firstStarted = true;
                Activate(firstEncounter);
                if (YokaiGameSession.Instance != null)
                    YokaiGameSession.Instance.SetStage(YokaiGameStage.FirstEncounter, "Break the yokai ambush.");
            }

            if (firstStarted && !firstCleared && AllDead(firstEncounter))
            {
                firstCleared = true;
                if (gateOne != null) gateOne.SetActive(false);
                if (YokaiGameSession.Instance != null)
                    YokaiGameSession.Instance.SetStage(YokaiGameStage.EliteHunt, "Hunt the red ronin beyond the cedar pass.");
            }

            if (firstCleared && !eliteStarted && z > 24f)
            {
                eliteStarted = true;
                Activate(eliteEncounter);
            }

            if (eliteStarted && !eliteCleared && AllDead(eliteEncounter))
            {
                eliteCleared = true;
                if (gateTwo != null) gateTwo.SetActive(false);
                if (YokaiGameSession.Instance != null)
                    YokaiGameSession.Instance.SetStage(YokaiGameStage.Shrine, "Reach the spirit shrine and restore your strength.");
            }

            if (eliteCleared && !bossStarted && z > 63f)
            {
                bossStarted = true;
                if (boss != null) boss.Activate();

                YokaiLockOn lockOn = player.GetComponent<YokaiLockOn>();
                if (lockOn != null && Camera.main != null)
                    lockOn.Acquire(Camera.main.transform);

                if (YokaiGameSession.Instance != null)
                    YokaiGameSession.Instance.SetStage(YokaiGameStage.Boss, "Defeat Oni Warden Kagane.");
            }
        }


        public void ResetAfterPlayerDeath()
        {
            if (YokaiAttackCoordinator.Instance != null)
                YokaiAttackCoordinator.Instance.Clear();

            if (firstStarted && !firstCleared)
            {
                ResetList(firstEncounter);
                Activate(firstEncounter);
            }

            if (eliteStarted && !eliteCleared)
            {
                ResetList(eliteEncounter);
                Activate(eliteEncounter);
            }

            if (bossStarted && boss != null && !YokaiSaveSystem.Data.bossDefeated)
            {
                boss.ResetEnemy();
                boss.Activate();
            }
        }

        void ResetList(List<YokaiEnemy> list)
        {
            for (int i=0;i<list.Count;i++)
                if (list[i] != null) list[i].ResetEnemy();
        }

        void Activate(List<YokaiEnemy> list)
        {
            for (int i=0;i<list.Count;i++)
                if (list[i] != null) list[i].Activate();
        }

        bool AllDead(List<YokaiEnemy> list)
        {
            if (list.Count == 0) return true;
            for (int i=0;i<list.Count;i++)
                if (list[i] != null && !list[i].IsDead) return false;
            return true;
        }

        void OnEnemyDied(YokaiEnemy enemy)
        {
        }
    }
}
