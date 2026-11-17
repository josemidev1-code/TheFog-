using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static TheFog.EditorTools.Entorno.ArquiKit;

namespace TheFog.EditorTools.Entorno
{
    /// <summary>
    /// Pueblo de Valdara: plaza mayor con iglesia, ayuntamiento, bar y fuente, y calles en cruz con casas adosadas
    /// manchegas (cal, zócalo de color, teja árabe, rejas, balcones, faroles). Todo por piezas (ArquiKit), con
    /// variación por semilla. Trazado en EntornoLayout. Se puede repetir: rehace "PUEBLO DE VALDARA".
    /// </summary>
    public static class PuebloBuilder
    {
        const float T = 0.45f, Planta = 2.9f, Pendiente = 24f;
        const int Semilla = 1987;

        static System.Random rng;
        static Terrain terrain;
        static Material cal, calSucia, teja, madera, piedra, sillar, hierro, cristal, luzVentana, tabla, farolLuz;
        static Material[] zocalos, puertas;
        static int lucesPuestas;

        [MenuItem("THE FOG/Entorno/Construir pueblo de Valdara (escena abierta)")]
        public static void Build()
        {
            ArquiKit.MeshDir = "Assets/Art/Retro/Arquitectura/Mallas";
            ArquiKit.MatDir = "Assets/Art/Retro/Arquitectura/Materiales";
            EnsureFolders();
            Materials();
            rng = new System.Random(Semilla);
            lucesPuestas = 0;
            terrain = Object.FindAnyObjectByType<Terrain>();

            var old = GameObject.Find("PUEBLO DE VALDARA");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("PUEBLO DE VALDARA").transform;

            var P = EntornoLayout.Plaza; float px = EntornoLayout.PlazaMedioX, pz = EntornoLayout.PlazaMedioZ, c = EntornoLayout.CalleMedia;
            float zN = P.y + pz, zS = P.y - pz;   // bordes norte y sur de la plaza (-139, -161)

            // --- Plaza ---
            var plaza = Group("Plaza mayor", root);
            Ayuntamiento(plaza, new Vector2(-(px + c) / 2f, zN), new Vector2(0, -1), px - c);
            Iglesia(plaza, new Vector2((px + c) / 2f, zS), new Vector2(0, 1), px - c);
            Fila(Group("Plaza norte este", plaza), new Vector2(c, zN), new Vector2(px, zN), new Vector2(0, -1), 1);
            Fila(Group("Plaza sur oeste", plaza), new Vector2(-px, zS), new Vector2(-c, zS), new Vector2(0, 1), 1);
            Bar(plaza, new Vector2(-px, (zN + P.y + c) / 2f), new Vector2(1, 0), zN - (P.y + c));
            Fila(Group("Plaza oeste sur", plaza), new Vector2(-px, zS), new Vector2(-px, P.y - c), new Vector2(1, 0), 1);
            Fila(Group("Plaza este norte", plaza), new Vector2(px, P.y + c), new Vector2(px, zN), new Vector2(-1, 0), 1);
            Fila(Group("Plaza este sur", plaza), new Vector2(px, zS), new Vector2(px, P.y - c), new Vector2(-1, 0), 1);
            Fuente(plaza, P);
            foreach (var o in new[] { new Vector2(-8, 7), new Vector2(8, 7), new Vector2(-8, -7), new Vector2(8, -7) })
                Farola(plaza, P + o);

            // --- Calles (las filas empiezan donde acaban los edificios de la plaza) ---
            var calles = Group("Calles", root);
            Fila(Group("Calle norte, oeste", calles), new Vector2(-c, -110f), new Vector2(-c, zN + 10f), new Vector2(1, 0), 2);
            Fila(Group("Calle norte, este", calles), new Vector2(c, -110f), new Vector2(c, zN + 10f), new Vector2(-1, 0), 2);
            Fila(Group("Calle sur, oeste", calles), new Vector2(-c, zS - 10f), new Vector2(-c, -198f), new Vector2(1, 0), 2);
            Fila(Group("Calle sur, este", calles), new Vector2(c, zS - 19.5f), new Vector2(c, -198f), new Vector2(-1, 0), 2);
            Fila(Group("Calle oeste, norte", calles), new Vector2(-60f, P.y + c), new Vector2(-px - 10f, P.y + c), new Vector2(0, -1), 2);
            Fila(Group("Calle oeste, sur", calles), new Vector2(-60f, P.y - c), new Vector2(-px - 10f, P.y - c), new Vector2(0, 1), 2);
            Fila(Group("Calle este, norte", calles), new Vector2(px + 10f, P.y + c), new Vector2(60f, P.y + c), new Vector2(0, -1), 2);
            Fila(Group("Calle este, sur", calles), new Vector2(px + 10f, P.y - c), new Vector2(60f, P.y - c), new Vector2(0, 1), 2);

            foreach (var t in root.GetComponentsInChildren<Transform>()) if (t.GetComponent<Light>() == null) t.gameObject.isStatic = true;
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Debug.Log("[Pueblo] Valdara construido: " + root.GetComponentsInChildren<MeshRenderer>().Length + " piezas, " + lucesPuestas + " luces.");
        }

        static void Materials()
        {
            cal = Mat("Casa_CalExterior", "Plaster007", 1f / 2.5f, 0.05f, new Color(0.92f, 0.91f, 0.87f));
            calSucia = Mat("Pueblo_CalSucia", "Plaster007", 1f / 2.5f, 0.05f, new Color(0.78f, 0.76f, 0.70f));
            teja = Mat("Casa_TejaArabe", "RoofingTiles006", 1f / 1.6f, 0.08f, new Color(0.72f, 0.62f, 0.55f));
            madera = Mat("Casa_MaderaVieja", "Wood062", 1f / 1.2f, 0.1f, new Color(0.62f, 0.56f, 0.50f));
            tabla = Mat("Pueblo_TablasViejas", "Planks023A", 1f / 1.2f, 0.1f, new Color(0.55f, 0.50f, 0.45f));
            piedra = Mat("Casa_Piedra", "Concrete031", 1f / 1.5f, 0.05f, new Color(0.70f, 0.68f, 0.64f));
            sillar = Mat("Pueblo_Sillar", "Concrete031", 1f / 2f, 0.05f, new Color(0.80f, 0.72f, 0.58f));
            hierro = Mat("Casa_Hierro", "Metal053B", 1f, 0.3f, new Color(0.16f, 0.16f, 0.16f));
            cristal = Cristal("Casa_Cristal", new Color(0.32f, 0.38f, 0.38f, 0.35f), 0.85f);
            luzVentana = Emisivo("Pueblo_VentanaEncendida", new Color(1f, 0.74f, 0.42f), 1.6f);
            farolLuz = Emisivo("Casa_Bombilla", new Color(1f, 0.82f, 0.55f), 2.5f);
            zocalos = new[]
            {
                Mat("Casa_ZocaloAzul", "PaintedPlaster017", 1f / 2f, 0.05f, new Color(0.42f, 0.52f, 0.78f)),
                Mat("Pueblo_ZocaloOcre", "PaintedPlaster017", 1f / 2f, 0.05f, new Color(0.80f, 0.62f, 0.38f)),
                Mat("Pueblo_ZocaloPiedra", "Concrete031", 1f / 1.5f, 0.05f, new Color(0.55f, 0.53f, 0.50f)),
                Mat("Pueblo_ZocaloGris", "PaintedPlaster017", 1f / 2f, 0.05f, new Color(0.50f, 0.52f, 0.54f)),
            };
            puertas = new[]
            {
                Mat("Casa_PuertaVerde", "Planks023A", 1f / 1.2f, 0.12f, new Color(0.45f, 0.62f, 0.48f)),
                Mat("Pueblo_PuertaMarron", "Planks023A", 1f / 1.2f, 0.12f, new Color(0.55f, 0.42f, 0.32f)),
                Mat("Pueblo_PuertaAzul", "Planks023A", 1f / 1.2f, 0.12f, new Color(0.40f, 0.50f, 0.62f)),
            };
        }

        static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static bool P(float p) => rng.NextDouble() < p;
        static X Pick<X>(X[] arr) => arr[rng.Next(arr.Length)];

        static float Altura(Vector2 xz) => terrain != null ? terrain.SampleHeight(new Vector3(xz.x, 0, xz.y)) + terrain.transform.position.y : 0f;

        /// <summary>Raíz de un edificio: en el centro de su fachada, a ras de suelo, con la fachada mirando a la calle (-Z local).</summary>
        static Transform Raiz(Transform parent, string name, Vector2 fachada, Vector2 haciaCalle)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.position = new Vector3(fachada.x, Altura(fachada), fachada.y);
            t.rotation = Quaternion.LookRotation(new Vector3(-haciaCalle.x, 0, -haciaCalle.y), Vector3.up);
            return t;
        }

        // ---------- Filas de casas adosadas ----------
        static void Fila(Transform g, Vector2 a, Vector2 b, Vector2 haciaCalle, int maxPlantas)
        {
            Vector2 d = b - a; float len = d.magnitude; Vector2 dir = d / len;
            float s = 0f; int n = 0;
            while (len - s > 3.5f)
            {
                float w = Mathf.Min(R(5.5f, 9f), len - s);
                if (len - s - w < 3.5f) w = len - s; // la última casa llena el hueco
                Vector2 c = a + dir * (s + w / 2f);
                if (w > 4f && P(0.12f) && n > 0) Tapia(g, c, haciaCalle, dir, w);
                else Casa(g, "Casa " + (++n), c, haciaCalle, dir, w, R(8f, 11f), P(maxPlantas > 1 ? 0.45f : 0.3f) ? 2 : 1, P(0.12f));
                s += w;
            }
        }

        /// <summary>Casa adosada. a lo largo de la fila = dir; fondo hacia dentro de la manzana.</summary>
        static void Casa(Transform parent, string name, Vector2 fachada, Vector2 haciaCalle, Vector2 dir, float ancho, float fondo, int plantas, bool abandonada)
        {
            var g = Raiz(parent, name + (abandonada ? " (abandonada)" : ""), fachada, haciaCalle);
            // local +X a lo largo de la fila (cualquier sentido vale; la casa es simétrica en su trazado)
            float H = plantas * Planta + 0.35f;
            var muroMat = abandonada || P(0.25f) ? calSucia : cal;
            var zocalo = Pick(zocalos);
            var puerta = Pick(puertas);

            // Huecos de fachada por vanos
            int vanos = Mathf.Max(1, Mathf.FloorToInt(ancho / 1.9f));
            float paso = ancho / vanos;
            int vanoPuerta = rng.Next(vanos);
            bool balcon = plantas > 1 && P(0.5f);
            var huecos = new List<Hueco>();
            for (int i = 0; i < vanos; i++)
            {
                float sc = paso * (i + 0.5f);
                if (i == vanoPuerta) huecos.Add(new Hueco(sc, 1.05f, 0f, 2.15f));
                else huecos.Add(new Hueco(sc, 0.8f, 1.0f, 2.0f));
                for (int p = 1; p < plantas; p++)
                {
                    float y0 = p * Planta;
                    if (balcon && i == vanoPuerta) huecos.Add(new Hueco(sc, 0.9f, y0 + 0.1f, y0 + 2.2f));
                    else huecos.Add(new Hueco(sc, 0.75f, y0 + 0.9f, y0 + 2.0f));
                }
            }
            Muro(g, "Fachada", new Vector3(-ancho / 2f, 0, T / 2f), new Vector3(ancho / 2f, 0, T / 2f), T, 0, H, muroMat, true, huecos.ToArray());
            Muro(g, "Trasera", new Vector3(-ancho / 2f, 0, fondo - T / 2f), new Vector3(ancho / 2f, 0, fondo - T / 2f), T, 0, H, muroMat, true);
            Muro(g, "MedianeraA", new Vector3(-ancho / 2f + T / 2f, 0, T), new Vector3(-ancho / 2f + T / 2f, 0, fondo - T), T, 0, H, muroMat, true);
            Muro(g, "MedianeraB", new Vector3(ancho / 2f - T / 2f, 0, T), new Vector3(ancho / 2f - T / 2f, 0, fondo - T), T, 0, H, muroMat, true);
            BoxLocal(g, "Forjado", new Vector3(0, H - 0.1f, fondo / 2f), new Vector3(ancho - 2f * T, 0.2f, fondo - 2f * T), Quaternion.identity, muroMat, false);

            // Zócalo (salvo en la puerta)
            float hz = R(0.6f, 0.95f);
            float sp0 = paso * vanoPuerta + paso / 2f - 0.55f, sp1 = sp0 + 1.1f;
            if (sp0 > 0.05f) BoxLocal(g, "Zocalo", new Vector3(-ancho / 2f + sp0 / 2f, hz / 2f, -0.015f), new Vector3(sp0, hz, 0.03f), Quaternion.identity, zocalo, false);
            if (ancho - sp1 > 0.05f) BoxLocal(g, "Zocalo", new Vector3(-ancho / 2f + (sp1 + ancho) / 2f, hz / 2f, -0.015f), new Vector3(ancho - sp1, hz, 0.03f), Quaternion.identity, zocalo, false);

            // Carpinterías
            foreach (var h in huecos)
            {
                var c = new Vector3(-ancho / 2f + h.centro, 0, T / 2f);
                bool esPuerta = h.abajo < 0.01f;
                bool esBalcon = !esPuerta && h.arriba - h.abajo > 2f;
                Marco(g, c, Vector3.right, h.ancho, h.abajo, h.arriba);
                float alto = h.arriba - h.abajo;
                var centro = c + Vector3.up * (h.abajo + alto / 2f);
                if (esPuerta || esBalcon)
                {
                    BoxLocal(g, esPuerta ? "Puerta" : "PuertaBalcon", centro + Vector3.forward * 0.08f, new Vector3(h.ancho - 0.1f, alto - 0.06f, 0.06f), Quaternion.identity, abandonada ? tabla : puerta, false);
                    if (esBalcon) Balcon(g, c + Vector3.up * h.abajo, h.ancho + 0.6f);
                    if (esPuerta && P(0.35f)) Farol(g, new Vector3(c.x + (P(0.5f) ? 0.95f : -0.95f), 2.35f, -0.22f)); // al lado de la puerta, bajo el balcón
                }
                else if (abandonada)
                {
                    for (int k = 0; k < 3; k++)
                        BoxLocal(g, "Tabla", centro + new Vector3(0, (k - 1) * alto * 0.3f, -0.06f), new Vector3(h.ancho + 0.15f, 0.14f, 0.03f), Quaternion.Euler(0, 0, R(-8f, 8f)), tabla, false);
                }
                else
                {
                    bool encendida = P(0.22f);
                    BoxLocal(g, "Cristal", centro + Vector3.forward * 0.05f, new Vector3(h.ancho - 0.1f, alto - 0.1f, 0.012f), Quaternion.identity, encendida ? luzVentana : cristal, false);
                    if (h.abajo < 1.5f || P(0.3f)) Reja(g, "Reja", centro - Vector3.forward * 0.05f, Vector3.right, h.ancho + 0.08f, alto + 0.08f, hierro);
                    if (P(0.4f)) Contraventanas(g, centro, h.ancho, alto, puerta, P(0.5f));
                }
            }

            Tejado(g, ancho, fondo, H, Vuelo: 0.35f);
            if (P(0.6f))
            {
                var cx = R(-ancho / 2f + 0.8f, ancho / 2f - 0.8f); float ridge = H + (fondo / 2f) * Mathf.Tan(Pendiente * Mathf.Deg2Rad);
                BoxLocal(g, "Chimenea", new Vector3(cx, ridge - 0.2f, fondo * 0.62f), new Vector3(0.6f, 1.6f, 0.6f), Quaternion.identity, muroMat, false);
                BoxLocal(g, "Sombrerete", new Vector3(cx, ridge + 0.65f, fondo * 0.62f), new Vector3(0.78f, 0.06f, 0.78f), Quaternion.identity, piedra, false);
            }
            BoxLocal(g, "Escalon", new Vector3(-ancho / 2f + paso * (vanoPuerta + 0.5f), 0.06f, -0.22f), new Vector3(1.3f, 0.12f, 0.45f), Quaternion.identity, piedra, true);
        }

        /// <summary>Tapia de corral encalada con portón, entre casas.</summary>
        static void Tapia(Transform parent, Vector2 fachada, Vector2 haciaCalle, Vector2 dir, float ancho)
        {
            var g = Raiz(parent, "Corral", fachada, haciaCalle);
            float h = 2.4f;
            Muro(g, "Tapia", new Vector3(-ancho / 2f, 0, 0.15f), new Vector3(ancho / 2f, 0, 0.15f), 0.3f, 0, h, calSucia, true, new Hueco(ancho / 2f, 2.4f, 0f, 2.1f));
            BoxLocal(g, "Porton", new Vector3(0, 1.05f, 0.18f), new Vector3(2.4f, 2.1f, 0.06f), Quaternion.identity, tabla, true);
            BoxLocal(g, "Albardilla", new Vector3(0, h + 0.05f, 0.15f), new Vector3(ancho, 0.1f, 0.42f), Quaternion.identity, teja, false);
        }

        static void Marco(Transform g, Vector3 c, Vector3 along, float ancho, float abajo, float arriba)
        {
            float alto = arriba - abajo, t = 0.06f;
            BoxLocal(g, "Dintel", c + Vector3.up * (arriba - t / 2f), new Vector3(ancho, t, 0.08f), Quaternion.identity, madera, false);
            BoxLocal(g, "Jamba", c + Vector3.up * (abajo + alto / 2f) - along * (ancho / 2f - t / 2f), new Vector3(t, alto, 0.08f), Quaternion.identity, madera, false);
            BoxLocal(g, "Jamba", c + Vector3.up * (abajo + alto / 2f) + along * (ancho / 2f - t / 2f), new Vector3(t, alto, 0.08f), Quaternion.identity, madera, false);
            if (abajo > 0.05f) BoxLocal(g, "Alfeizar", c + Vector3.up * (abajo - 0.03f) - Vector3.forward * (T / 2f), new Vector3(ancho + 0.16f, 0.05f, 0.14f), Quaternion.identity, piedra, false);
        }

        static void Contraventanas(Transform g, Vector3 centro, float ancho, float alto, Material m, bool abiertas)
        {
            float z = -T / 2f - 0.03f;
            if (abiertas)
                foreach (float s in new[] { -1f, 1f })
                    BoxLocal(g, "Contraventana", centro + new Vector3(s * (ancho / 2f + ancho / 4f + 0.02f), 0, z), new Vector3(ancho / 2f, alto, 0.03f), Quaternion.identity, m, false);
            else
                BoxLocal(g, "ContraventanaCerrada", centro + new Vector3(0, 0, z), new Vector3(ancho, alto, 0.03f), Quaternion.identity, m, false);
        }

        static void Balcon(Transform g, Vector3 baseHueco, float ancho)
        {
            var f = baseHueco - Vector3.forward * (T / 2f); // plano de fachada
            BoxLocal(g, "LosaBalcon", f + new Vector3(0, -0.04f, -0.32f), new Vector3(ancho, 0.08f, 0.64f), Quaternion.identity, piedra, false);
            Reja(g, "Barandilla", f + new Vector3(0, 0.5f, -0.62f), Vector3.right, ancho - 0.05f, 0.95f, hierro);
            foreach (float s in new[] { -1f, 1f })
                Reja(g, "BarandillaLado", f + new Vector3(s * (ancho / 2f - 0.03f), 0.5f, -0.33f), Vector3.forward, 0.58f, 0.95f, hierro);
        }

        /// <summary>Farol fernandino de forja (modelado en Blender) clavado en la fachada (z = 0), con su luz.</summary>
        public static void Farol(Transform g, Vector3 pos)
        {
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Blender/FarolFernandino.glb");
            if (modelo != null)
            {
                var f = (GameObject)PrefabUtility.InstantiatePrefab(modelo, g);
                f.name = "FarolFernandino";
                f.transform.localPosition = new Vector3(pos.x, pos.y + 0.25f, 0f); // pletina en la pared
                f.transform.localRotation = Quaternion.Euler(0, 180f, 0);           // el brazo sale hacia la calle (-Z)
                Luz(g, new Vector3(pos.x, pos.y - 0.15f, -0.9f), 0.8f, 7f); // separada de la pared: sin mancha quemada
                return;
            }
            BoxLocal(g, "BrazoFarol", pos + new Vector3(0, 0.15f, 0.1f), new Vector3(0.04f, 0.04f, 0.35f), Quaternion.identity, hierro, false);
            BoxLocal(g, "Farol", pos, new Vector3(0.2f, 0.28f, 0.2f), Quaternion.identity, farolLuz, false);
            BoxLocal(g, "TejadilloFarol", pos + new Vector3(0, 0.17f, 0), new Vector3(0.28f, 0.05f, 0.28f), Quaternion.identity, hierro, false);
            Luz(g, pos + new Vector3(0, -0.1f, -0.15f), 1.3f, 8f);
        }

        static void Luz(Transform g, Vector3 localPos, float intensity, float range)
        {
            var l = new GameObject("Luz").AddComponent<Light>();
            l.transform.SetParent(g, false);
            l.transform.localPosition = localPos;
            l.type = LightType.Point; l.color = new Color(1f, 0.76f, 0.48f); l.intensity = intensity; l.range = range;
            l.shadows = LightShadows.None;
            lucesPuestas++;
        }

        /// <summary>Tejado a dos aguas con la cumbrera paralela a la fachada (eje X local). Fachada en z = 0, fondo hacia +Z.</summary>
        static void Tejado(Transform g, float ancho, float fondo, float H, float Vuelo, float pendiente = Pendiente, Material m = null)
        {
            m = m ?? teja;
            float a = pendiente * Mathf.Deg2Rad;
            float run = fondo / 2f + Vuelo, len = run / Mathf.Cos(a), grosor = 0.16f;
            float ridge = H + (fondo / 2f) * Mathf.Tan(a);
            foreach (float sz in new[] { -1f, 1f })
            {
                var n = new Vector3(0, Mathf.Cos(a), sz * Mathf.Sin(a));
                var center = new Vector3(0, ridge - (run / 2f) * Mathf.Tan(a), fondo / 2f + sz * run / 2f) + n * (grosor / 2f);
                BoxLocal(g, "Faldon", center, new Vector3(len, grosor, ancho + 0.02f), Quaternion.LookRotation(Vector3.right, n), m, false);
            }
            Viga(g, "Cumbrera", new Vector3(-ancho / 2f, ridge + 0.17f, fondo / 2f), new Vector3(ancho / 2f, ridge + 0.17f, fondo / 2f), 0.3f, 0.15f, m, false);
            float rise = (fondo / 2f) * Mathf.Tan(a);
            foreach (float x in new[] { -ancho / 2f + T / 2f, ancho / 2f - T / 2f })
            {
                var go = new GameObject("Hastial");
                go.transform.SetParent(g, false);
                go.transform.localPosition = new Vector3(x, H, fondo / 2f);
                go.AddComponent<MeshFilter>().sharedMesh = GableMesh(fondo, rise, T);
                go.AddComponent<MeshRenderer>().sharedMaterial = cal;
            }
        }

        // ---------- Edificios de la plaza ----------
        static void Ayuntamiento(Transform parent, Vector2 fachada, Vector2 haciaCalle, float ancho)
        {
            var g = Raiz(parent, "Ayuntamiento", fachada, haciaCalle);
            float fondo = 10f, H = 2 * 3.3f + 0.4f;
            var huecos = new List<Hueco> { new Hueco(ancho / 2f, 1.6f, 0f, 2.8f) };
            foreach (float s in new[] { ancho * 0.18f, ancho * 0.82f }) huecos.Add(new Hueco(s, 0.9f, 1.0f, 2.2f));
            foreach (float s in new[] { ancho * 0.2f, ancho * 0.5f, ancho * 0.8f }) huecos.Add(new Hueco(s, 1.0f, 3.4f, 5.7f));
            Muro(g, "Fachada", new Vector3(-ancho / 2f, 0, T / 2f), new Vector3(ancho / 2f, 0, T / 2f), T, 0, H, cal, true, huecos.ToArray());
            Muro(g, "Trasera", new Vector3(-ancho / 2f, 0, fondo - T / 2f), new Vector3(ancho / 2f, 0, fondo - T / 2f), T, 0, H, cal, true);
            Muro(g, "LadoA", new Vector3(-ancho / 2f + T / 2f, 0, T), new Vector3(-ancho / 2f + T / 2f, 0, fondo - T), T, 0, H, cal, true);
            Muro(g, "LadoB", new Vector3(ancho / 2f - T / 2f, 0, T), new Vector3(ancho / 2f - T / 2f, 0, fondo - T), T, 0, H, cal, true);
            BoxLocal(g, "Zocalo", new Vector3(0, 0.6f, -0.02f), new Vector3(ancho, 1.2f, 0.04f), Quaternion.identity, sillar, false);
            BoxLocal(g, "Imposta", new Vector3(0, 3.3f, -0.05f), new Vector3(ancho, 0.15f, 0.12f), Quaternion.identity, sillar, false);
            foreach (var h in huecos)
            {
                var c = new Vector3(-ancho / 2f + h.centro, 0, T / 2f);
                Marco(g, c, Vector3.right, h.ancho, h.abajo, h.arriba);
                float alto = h.arriba - h.abajo; var centro = c + Vector3.up * (h.abajo + alto / 2f);
                bool puerta = h.abajo < 0.01f, balconera = h.abajo > 3f;
                if (puerta || balconera) BoxLocal(g, "Hoja", centro + Vector3.forward * 0.08f, new Vector3(h.ancho - 0.1f, alto - 0.06f, 0.06f), Quaternion.identity, puertas[1], false);
                else { BoxLocal(g, "Cristal", centro + Vector3.forward * 0.05f, new Vector3(h.ancho - 0.1f, alto - 0.1f, 0.012f), Quaternion.identity, P(0.5f) ? luzVentana : cristal, false); Reja(g, "Reja", centro - Vector3.forward * 0.05f, Vector3.right, h.ancho + 0.08f, alto + 0.08f, hierro); }
            }
            Balcon(g, new Vector3(0, 3.4f, T / 2f), ancho - 1.2f); // balcón corrido
            Tejado(g, ancho, fondo, H, 0.4f);
            // Espadaña con reloj
            float ridge = H + (fondo / 2f) * Mathf.Tan(Pendiente * Mathf.Deg2Rad);
            BoxLocal(g, "Espadana", new Vector3(0, H + 1.1f, 0.25f), new Vector3(2.4f, 2.2f, 0.4f), Quaternion.identity, cal, false);
            BoxLocal(g, "Reloj", new Vector3(0, H + 1.2f, 0.02f), new Vector3(0.9f, 0.9f, 0.04f), Quaternion.identity, Emisivo("Pueblo_RelojEsfera", new Color(0.95f, 0.88f, 0.70f), 0.6f), false);
            BoxLocal(g, "Aguja", new Vector3(0.05f, H + 1.32f, -0.01f), new Vector3(0.04f, 0.32f, 0.02f), Quaternion.Euler(0, 0, -35f), hierro, false);
            BoxLocal(g, "Mastil", new Vector3(ancho * 0.35f, 4.6f, -0.6f), new Vector3(0.05f, 0.05f, 1.6f), Quaternion.Euler(-35f, 0, 0), hierro, false);
            Farol(g, new Vector3(-1.4f, 2.7f, -0.25f)); Farol(g, new Vector3(1.4f, 2.7f, -0.25f));
            BoxLocal(g, "Escalinata", new Vector3(0, 0.08f, -0.5f), new Vector3(3f, 0.16f, 1f), Quaternion.identity, sillar, true);
        }

        static void Iglesia(Transform parent, Vector2 fachada, Vector2 haciaCalle, float ancho)
        {
            var g = Raiz(parent, "Iglesia", fachada, haciaCalle);
            float fondo = 20f, H = 8.5f, t = 0.9f;
            var huecos = new[] { new Hueco(ancho / 2f, 2.0f, 0f, 3.4f), new Hueco(ancho / 2f, 1.0f, 5.2f, 6.6f) };
            Muro(g, "Fachada", new Vector3(-ancho / 2f, 0, t / 2f), new Vector3(ancho / 2f, 0, t / 2f), t, 0, H, sillar, true, huecos);
            Muro(g, "Cabecera", new Vector3(-ancho / 2f, 0, fondo - t / 2f), new Vector3(ancho / 2f, 0, fondo - t / 2f), t, 0, H, sillar, true);
            var vent = new[] { new Hueco(5f, 0.7f, 4.5f, 6.8f), new Hueco(10f, 0.7f, 4.5f, 6.8f), new Hueco(15f, 0.7f, 4.5f, 6.8f) };
            Muro(g, "NaveA", new Vector3(-ancho / 2f + t / 2f, 0, t), new Vector3(-ancho / 2f + t / 2f, 0, fondo - t), t, 0, H, sillar, true, vent);
            Muro(g, "NaveB", new Vector3(ancho / 2f - t / 2f, 0, t), new Vector3(ancho / 2f - t / 2f, 0, fondo - t), t, 0, H, sillar, true, vent);
            // Portada: arco aproximado con dovelas, y rosetón (ventana alta encendida, misa de difuntos)
            Marco(g, new Vector3(0, 0, t / 2f), Vector3.right, 2.0f, 0f, 3.4f);
            BoxLocal(g, "Portalon", new Vector3(0, 1.7f, t / 2f + 0.1f), new Vector3(1.9f, 3.35f, 0.08f), Quaternion.identity, puertas[1], false);
            for (int i = 0; i < 7; i++)
            {
                float ang = Mathf.PI * i / 6f;
                BoxLocal(g, "Dovela", new Vector3(Mathf.Cos(ang) * 1.35f, 3.4f + Mathf.Sin(ang) * 0.9f, -0.05f), new Vector3(0.45f, 0.35f, 0.12f), Quaternion.Euler(0, 0, ang * Mathf.Rad2Deg - 90f), sillar, false);
            }
            BoxLocal(g, "Roseton", new Vector3(0, 5.9f, t / 2f), new Vector3(0.9f, 1.3f, 0.02f), Quaternion.identity, luzVentana, false);
            // Tejado con la cumbrera a lo largo de la nave (girado 90°)
            var tj = Group("TejadoNave", g); tj.localPosition = new Vector3(0, 0, fondo / 2f); tj.localRotation = Quaternion.Euler(0, 90, 0);
            var tjIn = Group("Local", tj); tjIn.localPosition = new Vector3(0, 0, -ancho / 2f);
            Tejado(tjIn, fondo, ancho, H, 0.4f, 28f);
            // Campanario en la esquina, hacia la plaza
            var c = Group("Campanario", g); c.localPosition = new Vector3(-(ancho / 2f + 2.1f), 0, 2.2f); // lado de la plaza este, fuera de la calle sur
            float hc = 17f, w = 4.2f;
            foreach (var lado in new[] { 0, 90, 180, 270 })
            {
                var l = Group("Cara", c); l.localRotation = Quaternion.Euler(0, lado, 0);
                Muro(l, "Muro", new Vector3(-w / 2f, 0, -w / 2f + 0.35f), new Vector3(w / 2f, 0, -w / 2f + 0.35f), 0.7f, 0, hc, sillar, true,
                    new Hueco(w / 2f, 1.2f, hc - 3.4f, hc - 1.0f), new Hueco(w / 2f, 0.35f, hc * 0.45f, hc * 0.45f + 0.9f));
            }
            BoxLocal(c, "Cornisa", new Vector3(0, hc + 0.1f, 0), new Vector3(w + 0.4f, 0.2f, w + 0.4f), Quaternion.identity, sillar, false);
            for (int i = 0; i < 4; i++)
            {
                float s = w - i * 1.0f;
                BoxLocal(c, "Chapitel", new Vector3(0, hc + 0.45f + i * 0.5f, 0), new Vector3(s, 0.5f, s), Quaternion.identity, teja, false);
            }
            BoxLocal(c, "Cruz", new Vector3(0, hc + 2.9f, 0), new Vector3(0.06f, 1.2f, 0.06f), Quaternion.identity, hierro, false);
            BoxLocal(c, "CruzBrazo", new Vector3(0, hc + 3.1f, 0), new Vector3(0.6f, 0.06f, 0.06f), Quaternion.identity, hierro, false);
            BoxLocal(c, "Campana", new Vector3(0, hc - 2.3f, 0), new Vector3(0.9f, 1.0f, 0.9f), Quaternion.identity, Mat("Pueblo_Bronce", "Metal053B", 1f, 0.4f, new Color(0.42f, 0.36f, 0.24f)), false);
            BoxLocal(g, "Atrio", new Vector3(0, 0.1f, -1.5f), new Vector3(ancho + 2f, 0.2f, 3f), Quaternion.identity, sillar, true);
            Farol(g, new Vector3(-1.8f, 3.0f, -0.25f));
        }

        static void Bar(Transform parent, Vector2 fachada, Vector2 haciaCalle, float ancho)
        {
            var g = Raiz(parent, "Bar", fachada, haciaCalle);
            float fondo = 10f, H = Planta + 0.35f;
            var huecos = new[] { new Hueco(ancho * 0.3f, 1.1f, 0f, 2.2f), new Hueco(ancho * 0.72f, 2.2f, 0.9f, 2.2f) };
            Muro(g, "Fachada", new Vector3(-ancho / 2f, 0, T / 2f), new Vector3(ancho / 2f, 0, T / 2f), T, 0, H, cal, true, huecos);
            Muro(g, "Trasera", new Vector3(-ancho / 2f, 0, fondo - T / 2f), new Vector3(ancho / 2f, 0, fondo - T / 2f), T, 0, H, cal, true);
            Muro(g, "LadoA", new Vector3(-ancho / 2f + T / 2f, 0, T), new Vector3(-ancho / 2f + T / 2f, 0, fondo - T), T, 0, H, cal, true);
            Muro(g, "LadoB", new Vector3(ancho / 2f - T / 2f, 0, T), new Vector3(ancho / 2f - T / 2f, 0, fondo - T), T, 0, H, cal, true);
            BoxLocal(g, "Zocalo", new Vector3(-ancho / 2f + (ancho * 0.3f - 0.55f) / 2f, 0.45f, -0.015f), new Vector3(ancho * 0.3f - 0.55f, 0.9f, 0.03f), Quaternion.identity, zocalos[3], false);
            BoxLocal(g, "Zocalo", new Vector3((ancho * 0.3f + 0.55f + ancho) / 2f - ancho / 2f, 0.45f, -0.015f), new Vector3(ancho * 0.7f - 0.55f, 0.9f, 0.03f), Quaternion.identity, zocalos[3], false);
            foreach (var h in huecos)
            {
                var c = new Vector3(-ancho / 2f + h.centro, 0, T / 2f);
                Marco(g, c, Vector3.right, h.ancho, h.abajo, h.arriba);
                float alto = h.arriba - h.abajo; var centro = c + Vector3.up * (h.abajo + alto / 2f);
                BoxLocal(g, h.abajo < 0.01f ? "PuertaCristal" : "Escaparate", centro + Vector3.forward * 0.06f, new Vector3(h.ancho - 0.1f, alto - 0.08f, 0.012f), Quaternion.identity, luzVentana, false);
            }
            // Toldo y letrero sin texto (lámina iluminada)
            BoxLocal(g, "Toldo", new Vector3(ancho * 0.22f, 2.45f, -0.55f), new Vector3(ancho * 0.5f, 0.05f, 1.1f), Quaternion.Euler(-12f, 0, 0), Mat("Pueblo_Toldo", "Planks023A", 1f, 0.1f, new Color(0.55f, 0.30f, 0.25f)), false);
            BoxLocal(g, "Letrero", new Vector3(-ancho * 0.2f, 2.6f, -0.05f), new Vector3(1.6f, 0.45f, 0.06f), Quaternion.identity, Emisivo("Pueblo_LetreroBar", new Color(0.85f, 0.35f, 0.25f), 0.8f), false);
            Luz(g, new Vector3(0, 2.0f, 1.5f), 1.8f, 7f);
            Tejado(g, ancho, fondo, H, 0.35f);
            // Terraza: mesas y sillas de plástico (Poly Haven, CC0)
            var silla = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Optimized/PolyHaven/plastic_monobloc_chair_01/plastic_monobloc_chair_01.gltf");
            if (silla != null)
                for (int i = 0; i < 4; i++)
                {
                    var s = (GameObject)PrefabUtility.InstantiatePrefab(silla, g);
                    s.transform.localPosition = new Vector3(ancho * 0.1f + (i % 2) * 1.3f - 0.6f, 0, -1.8f - (i / 2) * 1.4f);
                    s.transform.localRotation = Quaternion.Euler(0, R(0f, 360f), 0);
                }
        }

        static void Fuente(Transform parent, Vector2 centro)
        {
            var g = Raiz(parent, "Fuente", centro, new Vector2(0, -1));
            for (int i = 0; i < 8; i++)
            {
                float ang = i * 45f;
                var dir = Quaternion.Euler(0, ang, 0) * Vector3.forward;
                BoxLocal(g, "Pilon", dir * 1.6f + Vector3.up * 0.3f, new Vector3(1.4f, 0.6f, 0.3f), Quaternion.Euler(0, ang, 0), sillar, true);
            }
            BoxLocal(g, "Agua", new Vector3(0, 0.45f, 0), new Vector3(3f, 0.04f, 3f), Quaternion.Euler(0, 22.5f, 0), Mat("Pueblo_AguaSucia", null, 1f, 0.95f, new Color(0.10f, 0.13f, 0.12f)), false);
            BoxLocal(g, "Pilar", new Vector3(0, 1.0f, 0), new Vector3(0.45f, 2f, 0.45f), Quaternion.identity, sillar, true);
            BoxLocal(g, "Taza", new Vector3(0, 2.05f, 0), new Vector3(0.9f, 0.15f, 0.9f), Quaternion.identity, sillar, false);
            foreach (var o in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                BoxLocal(g, "Cano", o * 0.3f + Vector3.up * 1.6f, new Vector3(0.05f, 0.05f, 0.25f), Quaternion.LookRotation(o), hierro, false);
        }

        static void Farola(Transform parent, Vector2 pos)
        {
            var g = Raiz(parent, "Farola", pos, new Vector2(0, -1));
            BoxLocal(g, "Base", new Vector3(0, 0.2f, 0), new Vector3(0.3f, 0.4f, 0.3f), Quaternion.identity, hierro, true);
            BoxLocal(g, "Fuste", new Vector3(0, 1.8f, 0), new Vector3(0.1f, 3.2f, 0.1f), Quaternion.identity, hierro, true);
            BoxLocal(g, "Farol", new Vector3(0, 3.6f, 0), new Vector3(0.3f, 0.4f, 0.3f), Quaternion.identity, farolLuz, false);
            BoxLocal(g, "Sombrero", new Vector3(0, 3.85f, 0), new Vector3(0.42f, 0.08f, 0.42f), Quaternion.identity, hierro, false);
            Luz(g, new Vector3(0, 3.4f, 0), 1.6f, 11f);
        }
    }
}
