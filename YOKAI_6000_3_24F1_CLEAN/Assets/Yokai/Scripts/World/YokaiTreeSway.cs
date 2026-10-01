using UnityEngine;

namespace Yokai
{
    public sealed class YokaiTreeSway : MonoBehaviour
    {
        public float amplitude = 1.25f;
        public float speed = .55f;

        Quaternion baseRotation;
        float seed;

        void Start()
        {
            baseRotation = transform.localRotation;
            seed = Random.Range(0f,10f);
        }

        void Update()
        {
            float x = Mathf.Sin(Time.time*speed + seed)*amplitude;
            float z = Mathf.Cos(Time.time*speed*.77f + seed)*amplitude*.55f;
            transform.localRotation = baseRotation * Quaternion.Euler(x,0f,z);
        }
    }
}
