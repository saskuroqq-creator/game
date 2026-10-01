using UnityEngine;

namespace Yokai
{
    [RequireComponent(typeof(YokaiEnemy))]
    public sealed class YokaiEnemyIndicator : MonoBehaviour
    {
        YokaiEnemy enemy;
        YokaiAttributes attributes;
        Camera cam;

        void Awake()
        {
            enemy = GetComponent<YokaiEnemy>();
            attributes = GetComponent<YokaiAttributes>();
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
        }

        void OnGUI()
        {
            if (enemy == null || attributes == null || enemy.IsDead || cam == null) return;
            if (enemy.archetype == YokaiEnemyArchetype.Boss) return;

            Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up*2.15f);
            if (screen.z <= 0f) return;

            float distance = Vector3.Distance(cam.transform.position, transform.position);
            if (distance > 20f) return;

            float width = Mathf.Clamp(145f - distance*3f, 72f, 132f);
            float x = screen.x - width*.5f;
            float y = Screen.height - screen.y;

            Color old = GUI.color;

            GUI.color = new Color(.04f,.04f,.04f,.82f);
            GUI.DrawTexture(new Rect(x,y,width,8f),Texture2D.whiteTexture);
            GUI.color = enemy.archetype == YokaiEnemyArchetype.Elite ? new Color(.82f,.12f,.08f) : new Color(.62f,.08f,.07f);
            GUI.DrawTexture(new Rect(x,y,width*(attributes.health/attributes.maxHealth),8f),Texture2D.whiteTexture);

            GUI.color = new Color(.04f,.04f,.04f,.82f);
            GUI.DrawTexture(new Rect(x,y+10f,width,4f),Texture2D.whiteTexture);
            GUI.color = new Color(.95f,.62f,.12f);
            GUI.DrawTexture(new Rect(x,y+10f,width*(attributes.posture/attributes.maxPosture),4f),Texture2D.whiteTexture);

            GUI.color = old;
        }
    }
}
