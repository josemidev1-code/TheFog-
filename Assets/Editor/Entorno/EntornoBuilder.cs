using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TheFog.EditorTools.Entorno
{
    /// <summary>
    /// Coloca los modelos de Tripo del entorno de Daniel sobre el terreno: torre, casa con pozo, leña y muros de
    /// piedra seca, postes de luz hacia el pueblo y monte (encinas, pinos, árboles secos, rocas y matorral).
    /// Rehace el grupo "ENTORNO TRIPO" cada vez. Los modelos que aún no existan se saltan.
    /// Reparto con semilla fija (1987): mismo resultado cada vez.
    /// </summary>
    public static class EntornoBuilder
    {
        const string ModelDir = "Assets/Models/Retro/Tripo/";
        // Trazado común (torre, casa, senderos, pueblo): EntornoLayout.
        static Vector2 Torre => EntornoLayout.Torre;
        static Vector2 Casa => EntornoLayout.Casa;
        static Vector2[][] Senderos => EntornoLayout.Senderos;

        static Terrain terrain;
        static Transform root;
        static readonly List<Vector3> taken = new List<Vector3>(); // xz + radio ocupado

        [MenuItem("THE FOG/Entorno/Colocar entorno Tripo (escena abierta)")]
        public static void Build()
        {
            terrain = Object.FindAnyObjectByType<Terrain>();
            if (terrain == null) { Debug.LogError("[Entorno] Falta el terreno: THE FOG → Entorno → Construir suelo."); return; }
            var old = GameObject.Find("ENTORNO TRIPO");
            if (old != null) Object.DestroyImmediate(old);
            root = new GameObject("ENTORNO TRIPO").transform;
            taken.Clear();
            Random.InitState(1987);

            // --- Torre y casa ---
            var g = Group("Torre");
            Place("torre", g, Torre, 0f, Col.Mesh, 6f);
            Place("generador", g, new Vector2(4.6f, -1.5f), -70f, Col.Box, 1f);

            g = Group("Casa de Daniel");
            // La casa es jugable y la levanta CasaDanielBuilder (grupo "CASA DE DANIEL"); aquí solo se reserva su sitio.
            // Los modelos "casa" de Tripo (cerrados, sin interior) quedan para el pueblo.
            taken.Add(new Vector3(Casa.x, Casa.y, 8f));
            Place("pozo", g, Casa + new Vector2(7f, -9f), 15f, Col.Mesh, 1.6f);
            Place("lena", g, Casa + new Vector2(-6.4f, -1.2f), 90f, Col.Box, 1.5f);
            Place("lena", g, Casa + new Vector2(-6.5f, 1.5f), 84f, Col.Box, 1.5f);
            Place("carro", g, Casa + new Vector2(-8f, -8.5f), 200f, Col.Mesh, 2.2f);
            Place("gallinero", g, Casa + new Vector2(-7f, 9.5f), 160f, Col.Mesh, 2.5f);

            // Muros de piedra seca: cerca de la finca con hueco donde entra el sendero, algún tramo caído.
            g = Group("Muros");
            Wall(g, Casa + new Vector2(-12f, -12f), Casa + new Vector2(-2.6f, -12f));    // sur, hasta el hueco del camino (x ≈ -33)
            Wall(g, Casa + new Vector2(3.2f, -12f), Casa + new Vector2(12f, -12f));
            Wall(g, Casa + new Vector2(-12f, -12f), Casa + new Vector2(-12f, 13f));      // oeste
            Wall(g, Casa + new Vector2(-12f, 13f), Casa + new Vector2(12f, 13f));        // norte

            // Postes de luz: de la torre a la casa y de la casa hacia el pueblo, a un lado del camino.
            g = Group("Postes de luz");
            Poles(g, Senderos[0], 3.2f, 14f);
            Poles(g, Senderos[1], 3.5f, 32f);

            // --- Monte ---
            g = Group("Monte");
            Scatter("encina", g, 26, 22f, 160f, 4f, 0.8f, 1.25f, Col.Trunk);
            Scatter("pino", g, 30, 25f, 160f, 3f, 0.8f, 1.3f, Col.Trunk);
            Scatter("arbol_seco", g, 9, 20f, 120f, 3f, 0.7f, 1.1f, Col.Trunk);
            Scatter("roca", g, 45, 12f, 150f, 2f, 0.5f, 1.6f, Col.Mesh);
            Scatter("matorral", g, 240, 9f, 150f, 0.8f, 0.6f, 1.5f, Col.None);

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            int n = root.GetComponentsInChildren<MeshRenderer>().Length;
            Debug.Log("[Entorno] Colocados " + n + " modelos de Tripo.");
        }

        enum Col { None, Box, Mesh, Trunk }

        static Transform Group(string name)
        {
            var t = new GameObject(name).transform; t.SetParent(root, false); return t;
        }

        static GameObject Load(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + name + "/" + name + ".gltf");

        static GameObject Place(string model, Transform parent, Vector2 xz, float yaw, Col col, float radius, float scale = 1f, bool alignToSlope = false)
        {
            var prefab = Load(model);
            if (prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = model;
            Vector3 p = new Vector3(xz.x, 0f, xz.y);
            p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
            var rot = Quaternion.Euler(0f, yaw, 0f);
            if (alignToSlope)
            {
                var td = terrain.terrainData; var tp = terrain.transform.position;
                var n = td.GetInterpolatedNormal((p.x - tp.x) / td.size.x, (p.z - tp.z) / td.size.z);
                rot = Quaternion.FromToRotation(Vector3.up, Vector3.Slerp(Vector3.up, n, 0.6f)) * rot;
            }
            // Un poco hundido para que no flote en pendiente.
            go.transform.SetPositionAndRotation(p + Vector3.down * 0.06f * scale, rot);
            go.transform.localScale = Vector3.one * scale;
            go.isStatic = true;
            foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
            AddCollider(go, col, scale);
            taken.Add(new Vector3(xz.x, xz.y, radius * scale));
            return go;
        }

        static void AddCollider(GameObject go, Col col, float scale)
        {
            var mf = go.GetComponentInChildren<MeshFilter>();
            if (mf == null || col == Col.None) return;
            var host = mf.gameObject;
            switch (col)
            {
                case Col.Mesh:
                    host.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                    break;
                case Col.Box:
                    host.AddComponent<BoxCollider>();
                    break;
                case Col.Trunk:
                    var c = host.AddComponent<CapsuleCollider>();
                    var b = mf.sharedMesh.bounds;
                    c.radius = Mathf.Clamp(Mathf.Min(b.size.x, b.size.z) * 0.06f, 0.15f, 0.45f);
                    c.height = b.size.y * 0.5f;
                    c.center = new Vector3(b.center.x, b.min.y + c.height * 0.5f, b.center.z);
                    break;
            }
        }

        static void Wall(Transform parent, Vector2 a, Vector2 b)
        {
            var prefab = Load("muro_piedra");
            if (prefab == null) return;
            var size = prefab.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.size;
            float seg = Mathf.Max(size.x, size.z) * 0.96f;
            bool alongX = size.x >= size.z;
            Vector2 d = b - a; float len = d.magnitude; Vector2 dir = d / len;
            float baseYaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg + (alongX ? -90f : 0f);
            int count = Mathf.Max(1, Mathf.RoundToInt(len / seg));
            for (int i = 0; i < count; i++)
            {
                if (Random.value < 0.12f) continue; // tramo caído
                Vector2 c = a + dir * (seg * (i + 0.5f));
                Place("muro_piedra", parent, c, baseYaw + Random.Range(-3f, 3f) + (Random.value < 0.5f ? 180f : 0f), Col.Mesh, 0.1f, Random.Range(0.95f, 1.05f));
            }
        }

        static void Poles(Transform parent, Vector2[] line, float side, float spacing)
        {
            float acc = spacing * 0.5f;
            for (int i = 0; i < line.Length - 1; i++)
            {
                Vector2 a = line[i], b = line[i + 1]; Vector2 d = b - a; float len = d.magnitude; Vector2 dir = d / len;
                Vector2 normal = new Vector2(dir.y, -dir.x);
                for (; acc < len; acc += spacing)
                {
                    Vector2 p = a + dir * acc + normal * side;
                    if (!Free(p, 0.6f)) continue; // nada de postes dentro de la casa o del pozo
                    float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg + 90f; // cruceta perpendicular al camino
                    Place("poste_luz", parent, p, yaw + Random.Range(-4f, 4f), Col.Trunk, 0.5f);
                }
                acc -= len;
            }
        }

        static void Scatter(string model, Transform parent, int count, float minR, float maxR, float radius, float minScale, float maxScale, Col col)
        {
            if (Load(model) == null) return;
            int placed = 0;
            for (int tries = 0; placed < count && tries < count * 40; tries++)
            {
                float ang = Random.value * Mathf.PI * 2f;
                float r = Mathf.Lerp(minR, maxR, Mathf.Sqrt(Random.value));
                // Repartido alrededor de la torre y de la casa.
                Vector2 center = Random.value < 0.6f ? Torre : Casa;
                Vector2 p = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
                if (Vector2.Distance(p, Torre) < minR || Vector2.Distance(p, Casa) < Mathf.Max(minR, 16f)) continue;
                if (PathDistance(p) < radius + 2.2f) continue;
                if (EntornoLayout.PuebloDistance(p) < radius + 6f) continue; // el pueblo lo amuebla PuebloBuilder
                float s = Random.Range(minScale, maxScale);
                if (!Free(p, radius * s)) continue;
                Place(model, parent, p, Random.Range(0f, 360f), col, radius, s, model == "roca" || model == "matorral");
                placed++;
            }
        }

        static bool Free(Vector2 p, float r)
        {
            foreach (var t in taken)
                if (Vector2.Distance(p, new Vector2(t.x, t.y)) < r + t.z) return false;
            return true;
        }

        static float PathDistance(Vector2 p) => Mathf.Min(EntornoLayout.SenderoDistance(p), EntornoLayout.CalleDistance(p));
    }
}
