using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TheFog.EditorTools.Entorno
{
    /// <summary>
    /// Piezas de arquitectura jugable (se puede entrar): cajas y hastiales con UV en metros, muros con huecos de
    /// puerta y ventana, rejas y materiales CC0 de Assets/Art/Retro/Materiales con el mismo ajuste retro.
    /// Las mallas se guardan como assets para que la escena no dependa del código.
    /// </summary>
    public static class ArquiKit
    {
        public const string TexDir = "Assets/Art/Retro/Materiales";
        public static string MeshDir = "Assets/Art/Retro/Arquitectura/Mallas";
        public static string MatDir = "Assets/Art/Retro/Arquitectura/Materiales";
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        public struct Hueco
        {
            public float centro, ancho, abajo, arriba;   // a lo largo del muro (m desde el inicio) y alturas
            public Hueco(float centro, float ancho, float abajo, float arriba) { this.centro = centro; this.ancho = ancho; this.abajo = abajo; this.arriba = arriba; }
        }

        public static void EnsureFolders()
        {
            Ensure(MeshDir); Ensure(MatDir);
        }

        static void Ensure(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            Ensure(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        public static Transform Group(string name, Transform parent)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        /// <summary>Muro recto de a a b (puntos del eje a ras de suelo, en local del padre) con huecos.</summary>
        public static void Muro(Transform parent, string name, Vector3 a, Vector3 b, float grosor, float y0, float y1,
            Material m, bool collide, params Hueco[] huecos)
        {
            var g = Group(name, parent);
            Vector3 d = b - a; float len = d.magnitude; Vector3 dir = d / len;
            var rot = Quaternion.LookRotation(dir, Vector3.up);
            var list = new List<Hueco>(huecos); list.Sort((p, q) => p.centro.CompareTo(q.centro));
            float s = 0f; int i = 0;
            void Pieza(float s0, float s1, float h0, float h1)
            {
                if (s1 - s0 < 0.005f || h1 - h0 < 0.005f) return;
                var c = a + dir * ((s0 + s1) / 2f) + Vector3.up * ((h0 + h1) / 2f);
                BoxLocal(g, name + "_" + (i++), c, new Vector3(grosor, h1 - h0, s1 - s0), rot, m, collide);
            }
            foreach (var h in list)
            {
                float hs0 = h.centro - h.ancho / 2f, hs1 = h.centro + h.ancho / 2f;
                Pieza(s, hs0, y0, y1);
                Pieza(hs0, hs1, y0, h.abajo);   // antepecho
                Pieza(hs0, hs1, h.arriba, y1);  // dintel
                s = hs1;
            }
            Pieza(s, len, y0, y1);
        }

        public static GameObject BoxLocal(Transform parent, string name, Vector3 localCenter, Vector3 size, Quaternion localRot, Material m, bool collide)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localCenter;
            go.transform.localRotation = localRot;
            go.AddComponent<MeshFilter>().sharedMesh = BoxMesh(size);
            go.AddComponent<MeshRenderer>().sharedMaterial = m;
            if (collide) go.AddComponent<BoxCollider>().size = size;
            go.isStatic = true;
            return go;
        }

        /// <summary>Viga entre dos puntos locales.</summary>
        public static GameObject Viga(Transform parent, string name, Vector3 a, Vector3 b, float w, float h, Material m, bool collide)
        {
            var d = b - a; float len = d.magnitude;
            var up = Mathf.Abs(Vector3.Dot(d.normalized, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
            return BoxLocal(parent, name, (a + b) / 2f, new Vector3(w, h, len), Quaternion.LookRotation(d, up), m, collide);
        }

        /// <summary>Reja de barrotes delante de un hueco: plano local XY, centrada en c, mirando hacia normal.</summary>
        public static void Reja(Transform parent, string name, Vector3 c, Vector3 alongWall, float ancho, float alto, Material m)
        {
            var g = Group(name, parent);
            var rot = Quaternion.LookRotation(alongWall, Vector3.up);
            int n = Mathf.Max(3, Mathf.RoundToInt(ancho / 0.12f));
            for (int k = 0; k <= n; k++)
            {
                float s = -ancho / 2f + ancho * k / n;
                BoxLocal(g, "Barrote", c + alongWall * s, new Vector3(0.022f, alto, 0.022f), rot, m, false);
            }
            foreach (float y in new[] { -alto / 2f + 0.08f, alto / 2f - 0.08f })
                BoxLocal(g, "Pletina", c + Vector3.up * y, new Vector3(0.012f, 0.035f, ancho + 0.04f), rot, m, false);
        }

        /// <summary>Caja centrada con UV en metros (la u sigue el eje Z local: el largo).</summary>
        public static Mesh BoxMesh(Vector3 s)
        {
            string key = $"Caja2_{s.x:0.000}x{s.y:0.000}x{s.z:0.000}"; // "2": versión con las caras bien orientadas
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;
            string path = $"{MeshDir}/{key}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { cache[key] = existing; return existing; }

            var h = s / 2f;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            void Face(Vector3 normal, Vector3 uAxis, Vector3 vAxis, float uLen, float vLen)
            {
                int i = v.Count;
                Vector3 c = Vector3.Scale(normal, h);
                Vector3 du = uAxis * (uLen / 2f), dv = vAxis * (vLen / 2f);
                v.Add(c - du - dv); v.Add(c + du - dv); v.Add(c + du + dv); v.Add(c - du + dv);
                for (int k = 0; k < 4; k++) n.Add(normal);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(uLen, 0)); uv.Add(new Vector2(uLen, vLen)); uv.Add(new Vector2(0, vLen));
                // Unity: la cara visible es la que tiene normal = Cross(p1 - p0, p2 - p0).
                if (Vector3.Dot(Vector3.Cross(uAxis, vAxis), normal) > 0) { t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3); }
                else { t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2); }
            }
            Face(Vector3.right, Vector3.forward, Vector3.up, s.z, s.y);
            Face(Vector3.left, Vector3.forward, Vector3.up, s.z, s.y);
            Face(Vector3.up, Vector3.forward, Vector3.right, s.z, s.x);
            Face(Vector3.down, Vector3.forward, Vector3.right, s.z, s.x);
            Face(Vector3.forward, Vector3.right, Vector3.up, s.x, s.y);
            Face(Vector3.back, Vector3.right, Vector3.up, s.x, s.y);
            return Save(key, path, v, n, uv, t);
        }

        /// <summary>Hastial: triángulo (base ancho en Z, alto en Y) extruido en X con grosor. Base a y = 0.</summary>
        public static Mesh GableMesh(float ancho, float alto, float grosor)
        {
            string key = $"Hastial_{ancho:0.00}x{alto:0.00}x{grosor:0.00}";
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;
            string path = $"{MeshDir}/{key}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { cache[key] = existing; return existing; }
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            float hx = grosor / 2f, hz = ancho / 2f;
            // caras laterales (±X)
            foreach (float sx in new[] { 1f, -1f })
            {
                int i = v.Count;
                v.Add(new Vector3(sx * hx, 0, -hz)); v.Add(new Vector3(sx * hx, 0, hz)); v.Add(new Vector3(sx * hx, alto, 0));
                for (int k = 0; k < 3; k++) n.Add(new Vector3(sx, 0, 0));
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(ancho, 0)); uv.Add(new Vector2(ancho / 2f, alto));
                if (sx > 0) { t.Add(i); t.Add(i + 2); t.Add(i + 1); } else { t.Add(i); t.Add(i + 1); t.Add(i + 2); }
            }
            // faldones del triángulo (quedan bajo el tejado, pero cierran la pieza)
            foreach (float sz in new[] { 1f, -1f })
            {
                int i = v.Count;
                Vector3 p0 = new Vector3(-hx, 0, sz * hz), p1 = new Vector3(hx, 0, sz * hz), p2 = new Vector3(hx, alto, 0), p3 = new Vector3(-hx, alto, 0);
                var nn = Vector3.Cross(p1 - p0, p3 - p0).normalized; if (Vector3.Dot(nn, new Vector3(0, 0.5f, sz)) < 0) nn = -nn;
                v.Add(p0); v.Add(p1); v.Add(p2); v.Add(p3);
                for (int k = 0; k < 4; k++) n.Add(nn);
                float sl = Mathf.Sqrt(hz * hz + alto * alto);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(grosor, 0)); uv.Add(new Vector2(grosor, sl)); uv.Add(new Vector2(0, sl));
                if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), nn) > 0) { t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3); }
                else { t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2); }
            }
            return Save(key, path, v, n, uv, t);
        }

        static Mesh Save(string key, string path, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t)
        {
            var mesh = new Mesh { name = key };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
            mesh.RecalculateTangents(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            cache[key] = mesh;
            return mesh;
        }

        /// <summary>Material URP/Lit con textura de ambientCG (Assets/Art/Retro/Materiales/&lt;id&gt;). tiling = repeticiones por metro.</summary>
        public static Material Mat(string name, string acg, float tiling, float smooth, Color tint)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            if (!string.IsNullOrEmpty(acg))
            {
                string dir = $"{TexDir}/{acg}/{acg}_1K-JPG_";
                var col = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "Color.jpg");
                string norPath = dir + "NormalGL.jpg";
                var ti = AssetImporter.GetAtPath(norPath) as TextureImporter;
                if (ti != null && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
                var nor = AssetDatabase.LoadAssetAtPath<Texture2D>(norPath);
                m.SetTexture("_BaseMap", col);
                if (nor != null) { m.SetTexture("_BumpMap", nor); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f); }
            }
            m.SetColor("_BaseColor", tint);
            m.SetTextureScale("_BaseMap", Vector2.one * tiling);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Material transparente (cristal sucio).</summary>
        public static Material Cristal(string name, Color c, float smooth)
        {
            var m = Mat(name, null, 1f, smooth, c);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.SetOverrideTag("RenderType", "Transparent");
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Material que brilla (bombilla, brasas).</summary>
        public static Material Emisivo(string name, Color c, float intensidad)
        {
            var m = Mat(name, null, 1f, 0.3f, c);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * intensidad);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
