using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Yokai
{
    /// <summary>
    /// Small deterministic low-poly mesh library used by the mobile art pass.
    /// Meshes are cached and shared across all characters and world props.
    /// </summary>
    public static class YokaiMeshLibrary
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        public static Mesh Frustum(string key, int sides, float bottomRadius, float topRadius, float height)
        {
            Mesh found;
            if (cache.TryGetValue(key, out found) && found != null) return found;
            sides = Mathf.Clamp(sides, 3, 24);
            var vertices = new List<Vector3>(sides * 2 + 2);
            var triangles = new List<int>(sides * 12);
            float y0 = -height * .5f;
            float y1 = height * .5f;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                vertices.Add(new Vector3(c * bottomRadius, y0, s * bottomRadius));
                vertices.Add(new Vector3(c * topRadius, y1, s * topRadius));
            }
            int bottomCenter = vertices.Count;
            vertices.Add(new Vector3(0f, y0, 0f));
            int topCenter = vertices.Count;
            vertices.Add(new Vector3(0f, y1, 0f));

            for (int i = 0; i < sides; i++)
            {
                int n = (i + 1) % sides;
                int b0 = i * 2, t0 = b0 + 1, b1 = n * 2, t1 = b1 + 1;
                triangles.Add(b0); triangles.Add(t0); triangles.Add(t1);
                triangles.Add(b0); triangles.Add(t1); triangles.Add(b1);
                triangles.Add(bottomCenter); triangles.Add(b1); triangles.Add(b0);
                triangles.Add(topCenter); triangles.Add(t0); triangles.Add(t1);
            }
            return Cache(key, vertices.ToArray(), triangles.ToArray());
        }

        public static Mesh TaperedBox(string key, float bottomX, float bottomZ, float topX, float topZ, float height)
        {
            Mesh found;
            if (cache.TryGetValue(key, out found) && found != null) return found;
            float y0 = -height * .5f, y1 = height * .5f;
            float bx = bottomX * .5f, bz = bottomZ * .5f, tx = topX * .5f, tz = topZ * .5f;
            Vector3[] v =
            {
                new Vector3(-bx,y0,-bz), new Vector3(bx,y0,-bz), new Vector3(bx,y0,bz), new Vector3(-bx,y0,bz),
                new Vector3(-tx,y1,-tz), new Vector3(tx,y1,-tz), new Vector3(tx,y1,tz), new Vector3(-tx,y1,tz)
            };
            int[] t =
            {
                0,1,2, 0,2,3,
                4,6,5, 4,7,6,
                0,4,5, 0,5,1,
                1,5,6, 1,6,2,
                2,6,7, 2,7,3,
                3,7,4, 3,4,0
            };
            return Cache(key, v, t);
        }

        public static Mesh LowSphere(string key, int segments, int rings)
        {
            Mesh found;
            if (cache.TryGetValue(key, out found) && found != null) return found;
            segments = Mathf.Clamp(segments, 6, 20);
            rings = Mathf.Clamp(rings, 3, 10);
            var v = new List<Vector3>();
            var t = new List<int>();
            v.Add(Vector3.up * .5f);
            for (int r = 1; r < rings; r++)
            {
                float phi = Mathf.PI * r / rings;
                float y = Mathf.Cos(phi) * .5f;
                float radius = Mathf.Sin(phi) * .5f;
                for (int s = 0; s < segments; s++)
                {
                    float a = Mathf.PI * 2f * s / segments;
                    v.Add(new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius));
                }
            }
            int bottom = v.Count;
            v.Add(Vector3.down * .5f);
            for (int s = 0; s < segments; s++)
            {
                int n = (s + 1) % segments;
                t.Add(0); t.Add(1 + n); t.Add(1 + s);
            }
            for (int r = 0; r < rings - 2; r++)
            {
                int row = 1 + r * segments;
                int next = row + segments;
                for (int s = 0; s < segments; s++)
                {
                    int n = (s + 1) % segments;
                    t.Add(row + s); t.Add(next + n); t.Add(next + s);
                    t.Add(row + s); t.Add(row + n); t.Add(next + n);
                }
            }
            int lastRow = 1 + (rings - 2) * segments;
            for (int s = 0; s < segments; s++)
            {
                int n = (s + 1) % segments;
                t.Add(lastRow + s); t.Add(lastRow + n); t.Add(bottom);
            }
            return Cache(key, v.ToArray(), t.ToArray());
        }

        public static Mesh Blade(string key)
        {
            Mesh found;
            if (cache.TryGetValue(key, out found) && found != null) return found;
            Vector3[] v =
            {
                new Vector3(-.055f,-.5f,-.04f), new Vector3(.055f,-.5f,-.04f), new Vector3(.055f,-.5f,.04f), new Vector3(-.055f,-.5f,.04f),
                new Vector3(-.025f,.40f,-.025f), new Vector3(.025f,.40f,-.025f), new Vector3(.025f,.40f,.025f), new Vector3(-.025f,.40f,.025f),
                new Vector3(0f,.58f,0f)
            };
            int[] t =
            {
                0,1,2, 0,2,3,
                0,4,5, 0,5,1,
                1,5,6, 1,6,2,
                2,6,7, 2,7,3,
                3,7,4, 3,4,0,
                4,8,5, 5,8,6, 6,8,7, 7,8,4
            };
            return Cache(key, v, t);
        }

        public static Mesh Roof(string key)
        {
            Mesh found;
            if (cache.TryGetValue(key, out found) && found != null) return found;
            Vector3[] v =
            {
                new Vector3(-.5f,0f,-.5f), new Vector3(.5f,0f,-.5f), new Vector3(-.5f,0f,.5f), new Vector3(.5f,0f,.5f),
                new Vector3(0f,.32f,-.5f), new Vector3(0f,.32f,.5f)
            };
            int[] t =
            {
                0,4,1, 2,3,5,
                0,2,5, 0,5,4,
                4,5,3, 4,3,1,
                0,1,3, 0,3,2
            };
            return Cache(key, v, t);
        }

        public static Mesh Rock(string key, int seed)
        {
            Mesh found;
            if (cache.TryGetValue(key, out found) && found != null) return found;
            const int sides = 8;
            var v = new List<Vector3>();
            var t = new List<int>();
            for (int r = 0; r < 3; r++)
            {
                float y = r == 0 ? -.42f : (r == 1 ? .05f : .48f);
                float baseRadius = r == 1 ? .58f : .34f;
                for (int i = 0; i < sides; i++)
                {
                    float n = Hash01(seed * 31 + r * 13 + i * 7);
                    float a = i * Mathf.PI * 2f / sides + (n - .5f) * .18f;
                    float radius = baseRadius * Mathf.Lerp(.82f, 1.18f, n);
                    v.Add(new Vector3(Mathf.Cos(a) * radius, y + (n - .5f) * .1f, Mathf.Sin(a) * radius));
                }
            }
            for (int r = 0; r < 2; r++)
            {
                int aRow = r * sides, bRow = (r + 1) * sides;
                for (int i = 0; i < sides; i++)
                {
                    int n = (i + 1) % sides;
                    t.Add(aRow + i); t.Add(bRow + n); t.Add(bRow + i);
                    t.Add(aRow + i); t.Add(aRow + n); t.Add(bRow + n);
                }
            }
            for (int i = 1; i < sides - 1; i++)
            {
                t.Add(0); t.Add(i); t.Add(i + 1);
                int top = sides * 2;
                t.Add(top); t.Add(top + i + 1); t.Add(top + i);
            }
            return Cache(key, v.ToArray(), t.ToArray());
        }

        static float Hash01(int n)
        {
            uint x = (uint)n;
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            return (x & 0x00ffffff) / 16777215f;
        }

        static Mesh Cache(string key, Vector3[] vertices, int[] triangles)
        {
            Mesh mesh = new Mesh();
            mesh.name = "YOKAI_MESH_" + key;
            if (vertices.Length > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            cache[key] = mesh;
            return mesh;
        }
    }
}
