using System.Collections.Generic;
using UnityEngine;

namespace Yokai
{
    public sealed class YokaiProjectilePool : MonoBehaviour
    {
        public static YokaiProjectilePool Instance { get; private set; }
        public int warmCount = 18;

        readonly Queue<YokaiProjectile> pool = new Queue<YokaiProjectile>();

        void Awake()
        {
            Instance = this;
            for (int i=0;i<warmCount;i++)
                pool.Enqueue(CreateProjectile());
        }

        YokaiProjectile CreateProjectile()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Pooled_Spirit_Projectile";
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * .28f;

            Collider c = go.GetComponent<Collider>();
            if (c != null) Destroy(c);

            go.GetComponent<Renderer>().sharedMaterial =
                YokaiMaterialLibrary.Emissive("boss_projectile", new Color(.55f,.18f,.9f));

            YokaiProjectile projectile = go.AddComponent<YokaiProjectile>();
            go.SetActive(false);
            return projectile;
        }

        public YokaiProjectile Rent()
        {
            YokaiProjectile projectile = pool.Count > 0 ? pool.Dequeue() : CreateProjectile();
            projectile.gameObject.SetActive(true);
            return projectile;
        }

        public void Return(YokaiProjectile projectile)
        {
            if (projectile == null) return;
            projectile.transform.SetParent(transform, false);
            if (!pool.Contains(projectile)) pool.Enqueue(projectile);
        }
    }
}
