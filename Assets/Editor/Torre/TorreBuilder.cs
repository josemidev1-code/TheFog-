using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TheFog.EditorTools.Torre
{
    /// <summary>
    /// Construye la estructura exterior de la torre de vigilancia en la escena abierta (TorreDaniel):
    /// patas de madera sobre zapatas, largueros, cruces de San Andrés, pasarela con barandilla y una
    /// torre de escalera adosada con tramos y rellanos. Todo con texturas CC0 de ambientCG (UV en metros)
    /// que pasan por el ajuste retro. Se puede ejecutar varias veces: rehace el grupo "Estructura".
    /// La cabina existente se sube para que su suelo quede a la altura de la pasarela.
    /// </summary>
    public static class TorreBuilder
    {
        // ------------------------------------------------------------------ medidas (metros)
        public const float DeckTop = 12f;          // altura del suelo de la pasarela
        const float BaseHalf = 3.6f;               // media anchura de la base (eje de las patas)
        const float TopHalf = 3.0f;                // media anchura arriba
        const float DeckHalf = 4.3f;               // media anchura de la pasarela
        const float DeckThick = 0.12f;
        const float Post = 0.32f;                  // sección de las patas
        const float RailH = 1.05f;

        // escalera
        const float FlightRise = 3f, Riser = 0.2f, Tread = 0.26f, LaneW = 1.0f, LandingL = 1.0f;
        static int Steps => Mathf.RoundToInt(FlightRise / Riser);           // 15
        static float FlightRun => Steps * Tread;                             // 3.9
        static float LaneAz0 => -DeckHalf;                                   // carril pegado a la pasarela
        static float LaneBz0 => -DeckHalf - LaneW;                           // carril exterior
        static float StairXHalf => FlightRun / 2f;                           // 1.95
        static float StairXOuter => StairXHalf + LandingL;                   // 2.95

        const string ArtDir = "Assets/Art/Torre";
        const string MeshDir = ArtDir + "/Mallas";
        const string MatDir = ArtDir + "/Materiales";
        const string TexDir = "Assets/Art/Retro/Materiales";

        static Material madera, tablones, hormigon;
        static Transform root;
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();

        [MenuItem("THE FOG/Torre/Construir estructura (escena abierta)")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var torre = GameObject.Find("TORRE DE VIGILANCIA");
            if (torre == null) { Debug.LogError("[Torre] No hay 'TORRE DE VIGILANCIA' en la escena abierta."); return; }
            if (scene.name != "TorreDaniel" &&
                !EditorUtility.DisplayDialog("Torre", $"La escena abierta es '{scene.name}', no TorreDaniel. ¿Construir aquí?", "Sí", "No"))
                return;

            EnsureFolders();
            Materials();
            meshes.Clear();

            // 1) Subir la cabina (y lo que va con ella) para que su suelo quede en DeckTop.
            var cabina = torre.transform.Find("CABINA");
            var suelo = cabina.Find("Arquitectura/Suelo");
            float delta = DeckTop - suelo.GetComponent<Renderer>().bounds.max.y;
            var estructuraVieja = torre.transform.Find("Estructura");
            Transform tejado = estructuraVieja != null ? estructuraVieja.Find("Tejado") : null;
            if (Mathf.Abs(delta) > 0.001f)
            {
                cabina.position += Vector3.up * delta;
                if (tejado != null) tejado.position += Vector3.up * delta;
                var daniel = GameObject.Find("Daniel");
                if (daniel != null) daniel.transform.position += Vector3.up * delta;
            }

            // 2) Rehacer la estructura. El tejado provisional se conserva hasta el paso 2 (envolvente de la cabina).
            if (tejado != null) tejado.SetParent(cabina, true);
            if (estructuraVieja != null) Object.DestroyImmediate(estructuraVieja.gameObject);
            root = new GameObject("Estructura").transform;
            root.SetParent(torre.transform, false);

            Legs();
            Bracing();
            Deck();
            Stairs();

            foreach (var r in root.GetComponentsInChildren<Transform>()) r.gameObject.isStatic = true;
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Torre] Estructura construida. Cabina movida {delta:F2} m. Piezas: {root.GetComponentsInChildren<MeshRenderer>().Length}.");
        }

        // ================================================================== partes
        static Vector3 LegAt(int sx, int sz, float y)
        {
            float h = Mathf.Lerp(BaseHalf, TopHalf, y / (DeckTop - 0.4f));
            return new Vector3(sx * h, y, sz * h);
        }

        static void Legs()
        {
            var g = Group("Patas");
            float top = DeckTop - DeckThick - 0.28f;
            foreach (int sx in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
            {
                var a = LegAt(sx, sz, 0.1f); var b = LegAt(sx, sz, top);
                Beam(g, "Pata", a, b, Post, Post, madera, true);
                Box(g, "Zapata", new Vector3(a.x, -0.1f, a.z), new Vector3(0.9f, 0.5f, 0.9f), Quaternion.identity, hormigon, true);
            }
        }

        static void Bracing()
        {
            var g = Group("Arriostramiento");
            float top = DeckTop - DeckThick - 0.28f;
            float[] levels = { 0.35f, 4f, 8f, top };
            int[,] faces = { { -1, -1, 1, -1 }, { 1, -1, 1, 1 }, { 1, 1, -1, 1 }, { -1, 1, -1, -1 } };
            for (int f = 0; f < 4; f++)
            {
                for (int l = 1; l < levels.Length; l++)
                {
                    // larguero horizontal en cada nivel (el inferior hace de solera)
                    var a = LegAt(faces[f, 0], faces[f, 1], levels[l]);
                    var b = LegAt(faces[f, 2], faces[f, 3], levels[l]);
                    Beam(g, "Larguero", a, b, 0.16f, 0.22f, madera, false);
                    // cruz de San Andrés entre niveles, un poco hacia fuera para no atravesar las patas
                    var a0 = LegAt(faces[f, 0], faces[f, 1], levels[l - 1] + 0.15f);
                    var b0 = LegAt(faces[f, 2], faces[f, 3], levels[l - 1] + 0.15f);
                    var a1 = LegAt(faces[f, 0], faces[f, 1], levels[l] - 0.15f);
                    var b1 = LegAt(faces[f, 2], faces[f, 3], levels[l] - 0.15f);
                    Vector3 outward = Vector3.Scale(new Vector3(Mathf.Sign(a.x + b.x), 0, Mathf.Sign(a.z + b.z)),
                                                    new Vector3(Mathf.Abs(a.x + b.x) > 0.1f ? 0.12f : 0, 0, Mathf.Abs(a.z + b.z) > 0.1f ? 0.12f : 0));
                    Beam(g, "Cruz", a0 + outward, b1 + outward, 0.1f, 0.18f, madera, false);
                    Beam(g, "Cruz", b0 + outward * 1.6f, a1 + outward * 1.6f, 0.1f, 0.18f, madera, false);
                }
                var s0 = LegAt(faces[f, 0], faces[f, 1], 0.35f); var s1 = LegAt(faces[f, 2], faces[f, 3], 0.35f);
                Beam(g, "Solera", s0, s1, 0.16f, 0.22f, madera, false);
            }
        }

        static void Deck()
        {
            var g = Group("Pasarela");
            float y = DeckTop - DeckThick / 2f;
            Box(g, "Tablero", new Vector3(0, y, 0), new Vector3(DeckHalf * 2, DeckThick, DeckHalf * 2), Quaternion.identity, tablones, true);
            // viguetas
            float jy = DeckTop - DeckThick - 0.14f;
            for (float z = -DeckHalf + 0.25f; z <= DeckHalf - 0.2f; z += 1.0f)
                Beam(g, "Vigueta", new Vector3(-DeckHalf, jy, z), new Vector3(DeckHalf, jy, z), 0.12f, 0.26f, madera, false);
            // vigas de borde
            foreach (int s in new[] { -1, 1 })
            {
                Beam(g, "Viga", new Vector3(-DeckHalf, jy - 0.02f, s * (DeckHalf - 0.08f)), new Vector3(DeckHalf, jy - 0.02f, s * (DeckHalf - 0.08f)), 0.16f, 0.3f, madera, false);
                Beam(g, "Viga", new Vector3(s * (DeckHalf - 0.08f), jy - 0.3f, -DeckHalf), new Vector3(s * (DeckHalf - 0.08f), jy - 0.3f, DeckHalf), 0.16f, 0.3f, madera, false);
            }
            // jabalcones: de cada pata a las esquinas de la pasarela
            foreach (int sx in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
            {
                var leg = LegAt(sx, sz, DeckTop - 2.2f);
                Beam(g, "Jabalcon", leg, new Vector3(sx * (DeckHalf - 0.2f), jy - 0.3f, sz * (TopHalf + 0.1f)), 0.12f, 0.16f, madera, false);
                Beam(g, "Jabalcon", leg, new Vector3(sx * (TopHalf + 0.1f), jy - 0.3f, sz * (DeckHalf - 0.2f)), 0.12f, 0.16f, madera, false);
            }
            // barandilla perimetral con hueco donde llega la escalera
            float e = DeckHalf - 0.05f;
            float gap0 = StairXHalf, gap1 = StairXOuter;
            Railing(g, new Vector3(-e, DeckTop, -e), new Vector3(gap0, DeckTop, -e));
            Railing(g, new Vector3(gap1, DeckTop, -e), new Vector3(e, DeckTop, -e));
            Railing(g, new Vector3(e, DeckTop, -e), new Vector3(e, DeckTop, e));
            Railing(g, new Vector3(e, DeckTop, e), new Vector3(-e, DeckTop, e));
            Railing(g, new Vector3(-e, DeckTop, e), new Vector3(-e, DeckTop, -e));
        }

        static void Stairs()
        {
            var g = Group("Escalera");
            float zA = LaneAz0 - LaneW / 2f, zB = LaneBz0 - LaneW / 2f;  // centros de carril
            float zOuter = LaneBz0 - LaneW, zInner = LaneAz0;
            float x0 = -StairXHalf, x1 = StairXHalf;

            // postes de la torre de escalera, de la zapata a la barandilla del último rellano
            float postTop = DeckTop + RailH;
            foreach (float x in new[] { -StairXOuter + 0.08f, StairXOuter - 0.08f })
            foreach (float z in new[] { zInner - 0.08f, zOuter + 0.08f })
            {
                Beam(g, "Poste", new Vector3(x, 0.1f, z), new Vector3(x, postTop, z), 0.22f, 0.22f, madera, true);
                Box(g, "Zapata", new Vector3(x, -0.1f, z), new Vector3(0.6f, 0.45f, 0.6f), Quaternion.identity, hormigon, true);
            }
            // tramos: (carril, x inicio, x fin, altura inicio)
            var flights = new (float z, float xa, float xb, float y)[]
            {
                (zB, x1, x0, 0f), (zA, x0, x1, 3f), (zB, x1, x0, 6f), (zA, x0, x1, 9f)
            };
            foreach (var f in flights) Flight(g, f.z, f.xa, f.xb, f.y);

            // rellanos en 3, 6 y 9 (alternos) y el de llegada en 12, que da a la pasarela
            Landing(g, -1, 3f, true); Landing(g, 1, 6f, true); Landing(g, -1, 9f, true);
            Landing(g, 1, 12f, false);

            // atado de la torre de escalera a las patas en cada rellano
            foreach (float y in new[] { 3f, 6f, 9f })
            foreach (int sx in new[] { -1, 1 })
                Beam(g, "Atado", new Vector3(sx * (StairXOuter - 0.08f), y - 0.3f, zInner - 0.08f), LegAt(sx, -1, y - 0.3f), 0.12f, 0.18f, madera, false);

            // cruces en la cara exterior de la torre de escalera
            for (int l = 0; l < 4; l++)
            {
                float ya = l * 3f + 0.4f, yb = l * 3f + 2.7f;
                Beam(g, "Cruz", new Vector3(-StairXOuter + 0.08f, ya, zOuter - 0.05f), new Vector3(StairXOuter - 0.08f, yb, zOuter - 0.05f), 0.08f, 0.14f, madera, false);
            }
        }

        static void Flight(Transform g, float zc, float xa, float xb, float y0)
        {
            var f = Group("Tramo " + y0.ToString("0"), g);
            float dir = Mathf.Sign(xb - xa);
            for (int i = 0; i < Steps; i++)
            {
                float x = xa + dir * (i + 0.5f) * Tread;
                float y = y0 + (i + 1) * Riser;
                Box(f, "Peldano", new Vector3(x, y - 0.025f, zc), new Vector3(Tread + 0.04f, 0.05f, LaneW - 0.12f), Quaternion.identity, tablones, false);
            }
            // zancas a ambos lados
            foreach (float s in new[] { -1f, 1f })
            {
                float z = zc + s * (LaneW / 2f - 0.04f);
                Beam(f, "Zanca", new Vector3(xa, y0 - 0.05f, z), new Vector3(xb, y0 + FlightRise - 0.05f, z), 0.06f, 0.28f, madera, false);
            }
            // rampa invisible para el CharacterController
            var ramp = new GameObject("RampaColision").transform; ramp.SetParent(f, false);
            Vector3 a = new Vector3(xa, y0 + 0.02f, zc), b = new Vector3(xb, y0 + FlightRise + 0.02f, zc);
            ramp.position = (a + b) / 2f - Vector3.up * 0.06f;
            ramp.rotation = Quaternion.LookRotation(b - a, Vector3.up);
            var c = ramp.gameObject.AddComponent<BoxCollider>();
            c.size = new Vector3(LaneW - 0.1f, 0.12f, (b - a).magnitude + 0.1f);
            // barandilla a ambos lados del tramo (por fuera, entre carriles y hacia la torre)
            float off = LaneW / 2f - 0.05f;
            Railing(f, new Vector3(xa, y0, zc - off), new Vector3(xb, y0 + FlightRise, zc - off));
            Railing(f, new Vector3(xa, y0, zc + off), new Vector3(xb, y0 + FlightRise, zc + off));
        }

        static void Landing(Transform g, int side, float y, bool bothLanes)
        {
            var l = Group("Rellano " + y.ToString("0"), g);
            float x0 = side * StairXHalf, x1 = side * StairXOuter;
            float z0 = LaneAz0, z1 = bothLanes ? LaneBz0 - LaneW : LaneBz0;
            var c = new Vector3((x0 + x1) / 2f, y - 0.06f, (z0 + z1) / 2f);
            Box(l, "Plataforma", c, new Vector3(Mathf.Abs(x1 - x0), 0.12f, Mathf.Abs(z1 - z0)), Quaternion.identity, tablones, true);
            Beam(l, "Viga", new Vector3(x1 - side * 0.08f, y - 0.25f, z0), new Vector3(x1 - side * 0.08f, y - 0.25f, z1), 0.12f, 0.22f, madera, false);
            Beam(l, "Viga", new Vector3(x0, y - 0.25f, z0 - 0.05f), new Vector3(x1, y - 0.25f, z0 - 0.05f), 0.12f, 0.22f, madera, false);
            Beam(l, "Viga", new Vector3(x0, y - 0.25f, z1 + 0.05f), new Vector3(x1, y - 0.25f, z1 + 0.05f), 0.12f, 0.22f, madera, false);
            // barandillas: lado exterior en x y lado exterior en z
            float xe = x1 - side * 0.05f, ze = z1 + 0.05f;
            Railing(l, new Vector3(xe, y, z0 - 0.05f), new Vector3(xe, y, ze));
            Railing(l, new Vector3(x0, y, ze), new Vector3(xe, y, ze));
        }

        static void Railing(Transform parent, Vector3 a, Vector3 b)
        {
            var r = Group("Barandilla", parent);
            float len = Vector3.Distance(new Vector3(a.x, 0, a.z), new Vector3(b.x, 0, b.z));
            int n = Mathf.Max(1, Mathf.CeilToInt(len / 1.4f));
            for (int i = 0; i <= n; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)n);
                Box(r, "Balaustre", p + Vector3.up * (RailH / 2f), new Vector3(0.09f, RailH, 0.09f), Quaternion.identity, madera, false);
            }
            Beam(r, "Pasamanos", a + Vector3.up * RailH, b + Vector3.up * RailH, 0.07f, 0.11f, madera, false);
            Beam(r, "Travesano", a + Vector3.up * (RailH * 0.5f), b + Vector3.up * (RailH * 0.5f), 0.05f, 0.09f, madera, false);
            // pared invisible para no caerse
            var w = new GameObject("ColisionBarandilla").transform; w.SetParent(r, false);
            w.position = (a + b) / 2f + Vector3.up * (RailH / 2f + 0.1f);
            var flat = new Vector3(b.x - a.x, 0, b.z - a.z);
            if (flat.sqrMagnitude > 0.0001f) w.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
            var c = w.gameObject.AddComponent<BoxCollider>();
            c.size = new Vector3(0.12f, RailH + 0.2f + Mathf.Abs(b.y - a.y), len);
        }

        // ================================================================== utilidades
        static Transform Group(string name, Transform parent = null)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent != null ? parent : root, false);
            return t;
        }

        static void Beam(Transform parent, string name, Vector3 a, Vector3 b, float w, float h, Material m, bool collide)
        {
            var d = b - a; float len = d.magnitude;
            if (len < 0.01f) return;
            var up = Mathf.Abs(Vector3.Dot(d.normalized, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
            Box(parent, name, (a + b) / 2f, new Vector3(w, h, len), Quaternion.LookRotation(d, up), m, collide);
        }

        static void Box(Transform parent, string name, Vector3 center, Vector3 size, Quaternion rot, Material m, bool collide)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center; go.transform.rotation = rot;
            go.AddComponent<MeshFilter>().sharedMesh = BoxMesh(size);
            go.AddComponent<MeshRenderer>().sharedMaterial = m;
            if (collide) go.AddComponent<BoxCollider>().size = size;
        }

        /// <summary>Caja centrada con UV en metros; la veta (u) sigue al eje Z local, que es el largo de las vigas.</summary>
        static Mesh BoxMesh(Vector3 s)
        {
            string key = $"Caja_{s.x:0.00}x{s.y:0.00}x{s.z:0.00}";
            if (meshes.TryGetValue(key, out var cached)) return cached;
            string path = $"{MeshDir}/{key}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { meshes[key] = existing; return existing; }

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
                // orden según la orientación de la cara
                if (Vector3.Dot(Vector3.Cross(uAxis, vAxis), normal) > 0) { t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2); }
                else { t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3); }
            }
            Face(Vector3.right, Vector3.forward, Vector3.up, s.z, s.y);
            Face(Vector3.left, Vector3.forward, Vector3.up, s.z, s.y);
            Face(Vector3.up, Vector3.forward, Vector3.right, s.z, s.x);
            Face(Vector3.down, Vector3.forward, Vector3.right, s.z, s.x);
            Face(Vector3.forward, Vector3.right, Vector3.up, s.x, s.y);
            Face(Vector3.back, Vector3.right, Vector3.up, s.x, s.y);
            var mesh = new Mesh { name = key };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
            mesh.RecalculateTangents(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            meshes[key] = mesh;
            return mesh;
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art/Torre")) AssetDatabase.CreateFolder("Assets/Art", "Torre");
            if (!AssetDatabase.IsValidFolder(MeshDir)) AssetDatabase.CreateFolder(ArtDir, "Mallas");
            if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder(ArtDir, "Materiales");
        }

        static void Materials()
        {
            madera = Mat("MaderaVigas", "Wood062", 1f / 1.2f, 0.12f, new Color(0.82f, 0.80f, 0.76f));
            tablones = Mat("Tablones", "Planks023A", 1f / 2.2f, 0.15f, new Color(0.85f, 0.82f, 0.78f));
            hormigon = Mat("Hormigon", "Concrete031", 1f / 2f, 0.08f, new Color(0.75f, 0.75f, 0.73f));
        }

        static Material Mat(string name, string acg, float tiling, float smooth, Color tint)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            string dir = $"{TexDir}/{acg}/{acg}_1K-JPG_";
            var col = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "Color.jpg");
            string norPath = dir + "NormalGL.jpg";
            var ti = AssetImporter.GetAtPath(norPath) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
            var nor = AssetDatabase.LoadAssetAtPath<Texture2D>(norPath);
            m.SetTexture("_BaseMap", col);
            m.SetColor("_BaseColor", tint);
            m.SetTextureScale("_BaseMap", Vector2.one * tiling);
            if (nor != null) { m.SetTexture("_BumpMap", nor); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f); }
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
