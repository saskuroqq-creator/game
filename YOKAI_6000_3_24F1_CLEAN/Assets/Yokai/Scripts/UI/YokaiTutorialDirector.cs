using UnityEngine;

namespace Yokai
{
    public sealed class YokaiTutorialDirector : MonoBehaviour
    {
        public static YokaiTutorialDirector Instance { get; private set; }

        public string CurrentHint { get; private set; }
        public float HintAlpha { get; private set; }

        Transform player;
        YokaiCombat combat;
        YokaiLockOn lockOn;
        float showUntil;
        int tutorialStep;
        bool showedParry;
        bool showedArt;
        bool showedShrine;

        void Awake()
        {
            Instance = this;
        }

        public void Bind(Transform p)
        {
            player = p;
            combat = p != null ? p.GetComponent<YokaiCombat>() : null;
            lockOn = p != null ? p.GetComponent<YokaiLockOn>() : null;
            tutorialStep = 0;
            Show(Application.isMobilePlatform ?
                "Move with the left stick. Swipe the right side to control camera." :
                "WASD to move. Hold RMB and move the mouse to control camera.", 6f);
        }

        void Update()
        {
            if (player == null) return;

            if (Time.time <= showUntil) HintAlpha = Mathf.MoveTowards(HintAlpha,1f,Time.deltaTime*4f);
            else HintAlpha = Mathf.MoveTowards(HintAlpha,0f,Time.deltaTime*2f);

            float z = player.position.z;

            if (tutorialStep == 0 && z > -8f)
            {
                tutorialStep = 1;
                Show(Application.isMobilePlatform ?
                    "ATK chains light strikes. DODGE grants a short invulnerability window." :
                    "LMB chains light strikes. SPACE dodges with a short invulnerability window.", 6f);
            }

            if (!showedParry && z > 1f)
            {
                showedParry = true;
                Show(Application.isMobilePlatform ?
                    "Tap PARRY just before impact. Perfect parries break enemy Posture." :
                    "Tap Q just before impact. Perfect parries break enemy Posture.", 6f);
            }

            if (!showedArt && z > 20f)
            {
                showedArt = true;
                Show(Application.isMobilePlatform ?
                    "ART spends Spirit. ELEMENT cycles Fire, Storm, Spirit and Shadow." :
                    "E casts a Hunter Art. R cycles Fire, Storm, Spirit and Shadow.", 6f);
            }

            if (!showedShrine && z > 48f)
            {
                showedShrine = true;
                Show(Application.isMobilePlatform ?
                    "Use the shrine to save your checkpoint and refill healing." :
                    "Press G near the shrine to save your checkpoint and refill healing.", 7f);
            }
        }

        public void Show(string hint, float seconds)
        {
            CurrentHint = hint;
            showUntil = Time.time + seconds;
        }
    }
}
