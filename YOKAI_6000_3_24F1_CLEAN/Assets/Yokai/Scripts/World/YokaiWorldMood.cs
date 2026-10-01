using UnityEngine;

namespace Yokai
{
    public sealed class YokaiWorldMood : MonoBehaviour
    {
        Transform player;
        Color targetFog;
        Color targetAmbient;
        float targetDensity;
        int currentZone = -1;
        string zoneTitle = string.Empty;
        float titleUntil;
        GUIStyle style;

        void Start()
        {
            targetFog = RenderSettings.fogColor;
            targetAmbient = RenderSettings.ambientLight;
            targetDensity = RenderSettings.fogDensity;
        }

        void Update()
        {
            if (player == null && YokaiGameSession.Instance != null) player = YokaiGameSession.Instance.player;
            if (player == null) return;

            int zone;
            float z = player.position.z;
            if (z < 19f) zone = 0;
            else if (z < 52f) zone = 1;
            else if (z < 68f) zone = 2;
            else zone = 3;

            if (zone != currentZone)
            {
                currentZone = zone;
                if (zone == 0)
                {
                    zoneTitle = "CEDAR PASS";
                    targetFog = new Color(.10f,.15f,.17f);
                    targetAmbient = new Color(.18f,.22f,.25f);
                    targetDensity = .008f;
                }
                else if (zone == 1)
                {
                    zoneTitle = "FORSAKEN HAMLET";
                    targetFog = new Color(.13f,.145f,.15f);
                    targetAmbient = new Color(.21f,.20f,.20f);
                    targetDensity = .010f;
                }
                else if (zone == 2)
                {
                    zoneTitle = "SPIRIT SHRINE BASIN";
                    targetFog = new Color(.08f,.17f,.17f);
                    targetAmbient = new Color(.16f,.25f,.24f);
                    targetDensity = .009f;
                }
                else
                {
                    zoneTitle = "ONI COURTYARD";
                    targetFog = new Color(.18f,.065f,.085f);
                    targetAmbient = new Color(.24f,.14f,.17f);
                    targetDensity = .012f;
                }
                titleUntil = Time.unscaledTime + 2.5f;
            }

            float t = 1f - Mathf.Exp(-Time.deltaTime * .8f);
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetFog, t);
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetAmbient, t);
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetDensity, t);
        }

        void OnGUI()
        {
            if (Time.unscaledTime > titleUntil || string.IsNullOrEmpty(zoneTitle)) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = Mathf.Clamp(Screen.height / 34, 20, 38);
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = new Color(.92f,.88f,.78f,.92f);
            }
            float alpha = Mathf.Clamp01((titleUntil - Time.unscaledTime) * .7f);
            Color old = GUI.color;
            GUI.color = new Color(1f,1f,1f,alpha);
            GUI.Label(new Rect(0f, Screen.height*.12f, Screen.width, 48f), zoneTitle, style);
            GUI.color = old;
        }
    }
}
