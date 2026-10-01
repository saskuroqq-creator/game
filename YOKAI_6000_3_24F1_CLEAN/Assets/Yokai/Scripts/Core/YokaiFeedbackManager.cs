using System.Collections;
using UnityEngine;

namespace Yokai
{
    public sealed class YokaiFeedbackManager : MonoBehaviour
    {
        public static YokaiFeedbackManager Instance { get; private set; }

        float normalFixedDelta;
        Coroutine hitStopRoutine;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            normalFixedDelta = Time.fixedDeltaTime;
        }

        public void HitStop(float seconds, float timeScale)
        {
            if (seconds <= 0f) return;
            if (hitStopRoutine != null) StopCoroutine(hitStopRoutine);
            hitStopRoutine = StartCoroutine(HitStopRoutine(seconds, Mathf.Clamp(timeScale, 0.02f, 1f)));
        }

        IEnumerator HitStopRoutine(float seconds, float scale)
        {
            Time.timeScale = scale;
            Time.fixedDeltaTime = normalFixedDelta * scale;

            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
                yield return null;

            Time.timeScale = 1f;
            Time.fixedDeltaTime = normalFixedDelta;
            hitStopRoutine = null;
        }

        public void CameraImpulse(float amount)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            YokaiCameraRig rig = cam.GetComponent<YokaiCameraRig>();
            if (rig != null) rig.AddTrauma(amount);
        }
    }
}
