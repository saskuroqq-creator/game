using System.Collections;
using UnityEngine;

namespace Yokai
{
    public sealed class YokaiGameSession : MonoBehaviour
    {
        public static YokaiGameSession Instance { get; private set; }

        public Transform player;
        public YokaiEncounterDirector director;
        public YokaiGameStage Stage { get; private set; }
        public bool Victory { get; private set; }
        public bool Paused { get; private set; }
        public string ObjectiveText { get; private set; }
        public float StageChangedAt { get; private set; }

        bool respawning;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Stage = YokaiGameStage.Explore;
            ObjectiveText = "Follow the lantern road.";
            StageChangedAt = Time.time;
            SetPaused(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (Time.timeScale == 0f) Time.timeScale = 1f;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) YokaiSaveSystem.Save();
        }

        void OnApplicationQuit()
        {
            YokaiSaveSystem.Save();
        }

        public void SetPlayer(Transform t)
        {
            player = t;
        }

        public void SetStage(YokaiGameStage stage, string objective)
        {
            Stage = stage;
            ObjectiveText = objective;
            StageChangedAt = Time.time;
        }

        public void RestoreSavedVictory()
        {
            Victory = true;
            SetStage(YokaiGameStage.Victory, "Oni Warden already defeated. Explore the shrine road.");
        }

        public void TogglePause()
        {
            SetPaused(!Paused);
        }

        public void SetPaused(bool value)
        {
            Paused = value;
            Time.timeScale = value ? 0f : 1f;
            AudioListener.pause = value;
        }

        public void RestartFromCheckpoint()
        {
            SetPaused(false);
            if (player == null) return;

            StopAllCoroutines();
            respawning = false;

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.position = YokaiSaveSystem.GetCheckpoint();
            if (cc != null) cc.enabled = true;

            YokaiAttributes a = player.GetComponent<YokaiAttributes>();
            YokaiCombat c = player.GetComponent<YokaiCombat>();
            if (a != null)
            {
                if (a.IsDead) a.ReviveAt(1f);
                else a.RestoreFull();
            }
            if (c != null) { c.ResetActions(); c.RefillHealing(); }
            if (director != null) director.ResetAfterPlayerDeath();
        }

        public void NewGame()
        {
            SetPaused(false);
            YokaiSaveSystem.ResetProgress();
            UnityEngine.SceneManagement.Scene active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (active.buildIndex >= 0) UnityEngine.SceneManagement.SceneManager.LoadScene(active.buildIndex);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(active.name);
        }

        public void PlayerDied()
        {
            if (!respawning)
                StartCoroutine(RespawnRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            respawning = true;
            SetPaused(false);
            yield return new WaitForSeconds(1.65f);

            if (player != null)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.position = YokaiSaveSystem.GetCheckpoint();
                if (cc != null) cc.enabled = true;

                YokaiAttributes a = player.GetComponent<YokaiAttributes>();
                YokaiCombat c = player.GetComponent<YokaiCombat>();
                if (a != null) a.ReviveAt(.7f);
                if (c != null) { c.ResetActions(); c.RefillHealing(); }
            }

            if (director != null)
                director.ResetAfterPlayerDeath();

            respawning = false;
        }

        public void BossDefeated(YokaiBoss boss)
        {
            Victory = true;
            SetPaused(false);
            SetStage(YokaiGameStage.Victory, "The Oni Warden has fallen.");
        }
    }
}
