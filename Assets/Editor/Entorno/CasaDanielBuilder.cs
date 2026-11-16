using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static TheFog.EditorTools.Entorno.ArquiKit;

namespace TheFog.EditorTools.Entorno
{
    /// <summary>
    /// Casa de Daniel jugable: casa de labranza manchega de una planta (referencia C en docs/entorno).
    /// Se entra por la puerta del sur a la cocina-comedor (chimenea al oeste); desde ella, dormitorio (noreste)
    /// y despensa (sureste). Muros de 50 cm con huecos reales, techo de vigas, tejado de teja árabe a dos aguas,
    /// zócalo azul, emparrado, poyo y farol en la puerta. Coordenadas locales: la fachada mira a -Z.
    /// Se puede repetir: rehace "CASA DE DANIEL".
    /// </summary>
    public static class CasaDanielBuilder
    {
        public static readonly Vector3 Posicion = new Vector3(-34f, 0f, -5f);

        const float W = 11f, D = 7f, T = 0.5f, H = 3.1f, Techo = 2.85f, Suelo = 0.12f;
        const float Pendiente = 27f, Vuelo = 0.4f;

        static Material cal, calInt, zocalo, teja, madera, puertaMat, piedra, hidraulico, barro, hierro, cristal, hollin, brasas, bombilla;

        [MenuItem("THE FOG/Entorno/Construir casa de Daniel (escena abierta)")]
        public static void Build()
        {
            ArquiKit.MeshDir = "Assets/Art/Retro/Arquitectura/Mallas";
            ArquiKit.MatDir = "Assets/Art/Retro/Arquitectura/Materiales";
            EnsureFolders();
            Materials();

            var old = GameObject.Find("CASA DE DANIEL");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("CASA DE DANIEL").transform;
            var terrain = Object.FindAnyObjectByType<Terrain>();
            var p = Posicion;
            if (terrain != null) p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
            root.position = p;

            Muros(Group("Muros", root));
            Suelos(Group("Suelos y techo", root));
            Tejado(Group("Tejado", root));
            Huecos(Group("Puertas y ventanas", root));
            Chimenea(Group("Chimenea", root));
            Fachada(Group("Fachada", root));
            Luces(Group("Luces", root));

            foreach (var t in root.GetComponentsInChildren<Transform>()) if (t.GetComponent<Light>() == null) t.gameObject.isStatic = true;
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Debug.Log("[Casa] Casa de Daniel construida en " + p);
        }

        static void Materials()
        {
            cal = Mat("Casa_CalExterior", "Plaster007", 1f / 2.5f, 0.05f, new Color(0.92f, 0.91f, 0.87f));
            calInt = Mat("Casa_CalInterior", "Plaster001", 1f / 2.5f, 0.05f, new Color(0.90f, 0.89f, 0.84f));
            zocalo = Mat("Casa_ZocaloAzul", "PaintedPlaster017", 1f / 2f, 0.05f, new Color(0.42f, 0.52f, 0.78f));
            teja = Mat("Casa_TejaArabe", "RoofingTiles006", 1f / 1.6f, 0.08f, new Color(0.72f, 0.62f, 0.55f));
            madera = Mat("Casa_MaderaVieja", "Wood062", 1f / 1.2f, 0.1f, new Color(0.62f, 0.56f, 0.50f));
            puertaMat = Mat("Casa_PuertaVerde", "Planks023A", 1f / 1.2f, 0.12f, new Color(0.45f, 0.62f, 0.48f));
            piedra = Mat("Casa_Piedra", "Concrete031", 1f / 1.5f, 0.05f, new Color(0.70f, 0.68f, 0.64f));
            hidraulico = Mat("Casa_SueloHidraulico", "Tiles131", 1f / 0.7f, 0.25f, new Color(0.85f, 0.82f, 0.78f));
            barro = Mat("Casa_SueloBarro", "Tiles047", 1f / 1.5f, 0.15f, new Color(0.78f, 0.62f, 0.55f));
            hierro = Mat("Casa_Hierro", "Metal053B", 1f, 0.3f, new Color(0.16f, 0.16f, 0.16f));
            hollin = Mat("Casa_Hollin", "Concrete031", 1f / 1.5f, 0.02f, new Color(0.12f, 0.11f, 0.10f));
            cristal = Cristal("Casa_Cristal", new Color(0.32f, 0.38f, 0.38f, 0.35f), 0.85f);
            brasas = Emisivo("Casa_Brasas", new Color(1f, 0.35f, 0.08f), 3f);
            bombilla = Emisivo("Casa_Bombilla", new Color(1f, 0.82f, 0.55f), 2.5f);
        }

        // ---------- Muros ----------
        static void Muros(Transform g)
        {
            float zs = -D / 2f + T / 2f, zn = D / 2f - T / 2f, xw = -W / 2f + T / 2f, xe = W / 2f - T / 2f;
            // Sur (fachada): ventana cocina, puerta, ventanuco despensa. s = x + 5.5
            Muro(g, "MuroSur", new Vector3(-W / 2f, 0, zs), new Vector3(W / 2f, 0, zs), T, 0, H, cal, true,
                new Hueco(1.7f, 0.8f, 1.0f, 2.0f), new Hueco(3.7f, 1.1f, 0f, 2.15f), new Hueco(8.5f, 0.6f, 1.5f, 2.1f));
            Muro(g, "MuroNorte", new Vector3(-W / 2f, 0, zn), new Vector3(W / 2f, 0, zn), T, 0, H, cal, true,
                new Hueco(3.3f, 0.8f, 1.0f, 2.0f), new Hueco(8.5f, 0.8f, 1.0f, 2.0f));
            Muro(g, "MuroOeste", new Vector3(xw, 0, -D / 2f + T), new Vector3(xw, 0, D / 2f - T), T, 0, H, cal, true);
            Muro(g, "MuroEste", new Vector3(xe, 0, -D / 2f + T), new Vector3(xe, 0, D / 2f - T), T, 0, H, cal, true,
                new Hueco(4.6f, 0.8f, 1.0f, 2.0f));
            // Tabiques (14 cm) hasta el techo
            Muro(g, "TabiqueCocina", new Vector3(1f, 0, -D / 2f + T), new Vector3(1f, 0, D / 2f - T), 0.14f, 0, Techo, calInt, true,
                new Hueco(1.4f, 0.8f, 0f, 2.05f), new Hueco(4.6f, 0.85f, 0f, 2.05f));
            Muro(g, "TabiqueDormitorio", new Vector3(1.07f, 0, 0.2f), new Vector3(W / 2f - T, 0, 0.2f), 0.14f, 0, Techo, calInt, true);
            // Hastiales encima de los muros este y oeste
            float rise = (D / 2f) * Mathf.Tan(Pendiente * Mathf.Deg2Rad);
            foreach (float x in new[] { xw, xe })
            {
                var go = new GameObject("Hastial");
                go.transform.SetParent(g, false);
                go.transform.localPosition = new Vector3(x, H, 0);
                go.AddComponent<MeshFilter>().sharedMesh = GableMesh(D, rise, T);
                go.AddComponent<MeshRenderer>().sharedMaterial = cal;
            }
        }

        // ---------- Suelos y techo ----------
        static void Suelos(Transform g)
        {
            float zi = D / 2f - T; // 3.0
            BoxLocal(g, "SueloCocina", new Vector3(-2f, Suelo / 2f, 0), new Vector3(6f, Suelo, 2f * zi), Quaternion.identity, hidraulico, true);
            BoxLocal(g, "SueloHabitaciones", new Vector3(3f, Suelo / 2f, 0), new Vector3(4f, Suelo, 2f * zi), Quaternion.identity, barro, true);
            BoxLocal(g, "Escalon", new Vector3(-1.8f, Suelo / 2f, -D / 2f - 0.25f), new Vector3(1.4f, Suelo, 0.5f), Quaternion.identity, piedra, true);
            BoxLocal(g, "Techo", new Vector3(0, Techo + 0.03f, 0), new Vector3(W - 2f * T, 0.06f, 2f * zi), Quaternion.identity, calInt, false);
            for (float x = -4.5f; x <= 4.51f; x += 1f)
                BoxLocal(g, "Viga", new Vector3(x, Techo - 0.09f, 0), new Vector3(0.14f, 0.18f, 2f * zi), Quaternion.identity, madera, false);
        }

        // ---------- Tejado ----------
        static void Tejado(Transform g)
        {
            float a = Pendiente * Mathf.Deg2Rad;
            float run = D / 2f + Vuelo, len = run / Mathf.Cos(a), grosor = 0.16f;
            float ridge = H + (D / 2f) * Mathf.Tan(a);
            foreach (float sz in new[] { -1f, 1f })
            {
                var n = new Vector3(0, Mathf.Cos(a), sz * Mathf.Sin(a));
                var center = new Vector3(0, ridge - (run / 2f) * Mathf.Tan(a), sz * run / 2f) + n * (grosor / 2f);
                BoxLocal(g, sz < 0 ? "FaldonSur" : "FaldonNorte", center, new Vector3(len, grosor, W + 2f * Vuelo),
                    Quaternion.LookRotation(Vector3.right, n), teja, false);
                // alero de madera bajo el vuelo
                BoxLocal(g, "Alero", new Vector3(0, H - 0.05f, sz * (D / 2f + Vuelo / 2f)), new Vector3(W + 2f * Vuelo, 0.04f, Vuelo), Quaternion.identity, madera, false);
            }
            Viga(g, "Cumbrera", new Vector3(-W / 2f - Vuelo, ridge + 0.17f, 0), new Vector3(W / 2f + Vuelo, ridge + 0.17f, 0), 0.32f, 0.16f, teja, false);
        }

        // ---------- Puertas y ventanas ----------
        static void Huecos(Transform g)
        {
            float zs = -D / 2f + T / 2f, zn = D / 2f - T / 2f, xe = W / 2f - T / 2f;
            Ventana(g, "VentanaCocinaSur", new Vector3(-3.8f, 0, zs), Vector3.right, Vector3.back, 0.8f, 1.0f, 2.0f, true);
            Ventana(g, "VentanucoDespensa", new Vector3(3.0f, 0, zs), Vector3.right, Vector3.back, 0.6f, 1.5f, 2.1f, true);
            Ventana(g, "VentanaCocinaNorte", new Vector3(-2.2f, 0, zn), Vector3.right, Vector3.forward, 0.8f, 1.0f, 2.0f, true);
            Ventana(g, "VentanaDormitorioNorte", new Vector3(3.0f, 0, zn), Vector3.right, Vector3.forward, 0.8f, 1.0f, 2.0f, true);
            Ventana(g, "VentanaDormitorioEste", new Vector3(xe, 0, 1.6f), Vector3.forward, Vector3.right, 0.8f, 1.0f, 2.0f, true);

            // Puerta principal: marco y hoja abierta hacia dentro (se puede entrar).
            Marco(g, "MarcoPuerta", new Vector3(-1.8f, 0, zs), Vector3.right, 1.1f, 0f, 2.15f);
            var bisagra = new GameObject("PuertaPrincipal").transform;
            bisagra.SetParent(g, false);
            bisagra.localPosition = new Vector3(-1.8f - 0.52f, Suelo, zs + 0.12f);
            bisagra.localRotation = Quaternion.LookRotation(Vector3.right) * Quaternion.Euler(0, -100f, 0);
            BoxLocal(bisagra, "Hoja", new Vector3(0, 1.02f, 0.52f), new Vector3(0.06f, 2.0f, 1.0f), Quaternion.identity, puertaMat, true);
            BoxLocal(bisagra, "Pomo", new Vector3(0.05f, 1.0f, 0.92f), new Vector3(0.05f, 0.05f, 0.05f), Quaternion.identity, hierro, false);
            // Marcos de los pasos interiores
            Marco(g, "MarcoPasoDespensa", new Vector3(1f, 0, -1.6f), Vector3.forward, 0.8f, 0f, 2.05f, 0.14f);
            Marco(g, "MarcoPasoDormitorio", new Vector3(1f, 0, 1.6f), Vector3.forward, 0.85f, 0f, 2.05f, 0.14f);
        }

        static void Ventana(Transform g, string name, Vector3 c, Vector3 along, Vector3 fuera, float ancho, float abajo, float arriba, bool reja)
        {
            var w = Group(name, g);
            Marco(w, "Marco", c, along, ancho, abajo, arriba);
            float alto = arriba - abajo;
            var rot = Quaternion.LookRotation(along, Vector3.up);
            BoxLocal(w, "Cristal", c + Vector3.up * (abajo + alto / 2f), new Vector3(0.012f, alto - 0.1f, ancho - 0.1f), rot, cristal, false);
            BoxLocal(w, "Parteluz", c + Vector3.up * (abajo + alto / 2f), new Vector3(0.04f, alto - 0.1f, 0.04f), rot, madera, false);
            BoxLocal(w, "Alfeizar", c + Vector3.up * (abajo - 0.03f) + fuera * (T / 2f), new Vector3(0.12f, 0.05f, ancho + 0.16f), rot, piedra, false);
            if (reja) Reja(w, "Reja", c + Vector3.up * (abajo + alto / 2f) + fuera * (T / 2f + 0.04f), along, ancho + 0.08f, alto + 0.08f, hierro);
        }

        static void Marco(Transform g, string name, Vector3 c, Vector3 along, float ancho, float abajo, float arriba, float fondo = 0.08f)
        {
            var m = Group(name, g);
            var rot = Quaternion.LookRotation(along, Vector3.up);
            float alto = arriba - abajo, t = 0.06f;
            BoxLocal(m, "Dintel", c + Vector3.up * (arriba - t / 2f), new Vector3(fondo, t, ancho), rot, madera, false);
            BoxLocal(m, "JambaA", c + Vector3.up * (abajo + alto / 2f) - along * (ancho / 2f - t / 2f), new Vector3(fondo, alto, t), rot, madera, false);
            BoxLocal(m, "JambaB", c + Vector3.up * (abajo + alto / 2f) + along * (ancho / 2f - t / 2f), new Vector3(fondo, alto, t), rot, madera, false);
            if (abajo > 0.05f) BoxLocal(m, "Peana", c + Vector3.up * (abajo + t / 2f), new Vector3(fondo, t, ancho), rot, madera, false);
        }

        // ---------- Chimenea ----------
        static void Chimenea(Transform g)
        {
            float x = -W / 2f + T + 0.35f; // pegada al muro oeste por dentro
            BoxLocal(g, "Hogar", new Vector3(x, Suelo + 0.05f, 0), new Vector3(0.7f, 0.1f, 1.5f), Quaternion.identity, piedra, true);
            BoxLocal(g, "JambaN", new Vector3(x, Suelo + 0.7f, 0.66f), new Vector3(0.7f, 1.4f, 0.18f), Quaternion.identity, calInt, true);
            BoxLocal(g, "JambaS", new Vector3(x, Suelo + 0.7f, -0.66f), new Vector3(0.7f, 1.4f, 0.18f), Quaternion.identity, calInt, true);
            float h0 = Suelo + 1.4f;
            BoxLocal(g, "Campana", new Vector3(x, (h0 + Techo) / 2f, 0), new Vector3(0.7f, Techo - h0, 1.5f), Quaternion.identity, calInt, true);
            BoxLocal(g, "Repisa", new Vector3(x + 0.05f, h0 + 0.04f, 0), new Vector3(0.82f, 0.07f, 1.7f), Quaternion.identity, madera, false);
            BoxLocal(g, "FondoHollin", new Vector3(-W / 2f + T + 0.02f, Suelo + 0.7f, 0), new Vector3(0.03f, 1.4f, 1.15f), Quaternion.identity, hollin, false);
            BoxLocal(g, "Brasas", new Vector3(x - 0.05f, Suelo + 0.13f, 0), new Vector3(0.35f, 0.06f, 0.5f), Quaternion.identity, brasas, false);
            // Cañón que sale por el tejado
            BoxLocal(g, "Canon", new Vector3(x, 4.35f, 0.5f), new Vector3(0.75f, 2.9f, 0.75f), Quaternion.identity, cal, false);
            BoxLocal(g, "Sombrerete", new Vector3(x, 5.95f, 0.5f), new Vector3(0.95f, 0.07f, 0.95f), Quaternion.identity, piedra, false);
            foreach (var o in new[] { new Vector2(-0.3f, -0.3f), new Vector2(0.3f, -0.3f), new Vector2(-0.3f, 0.3f), new Vector2(0.3f, 0.3f) })
                BoxLocal(g, "Pilarillo", new Vector3(x + o.x, 5.83f, 0.5f + o.y), new Vector3(0.08f, 0.18f, 0.08f), Quaternion.identity, cal, false);
        }

        // ---------- Fachada: zócalo, emparrado, poyo ----------
        static void Fachada(Transform g)
        {
            float hz = 0.7f, e = 0.03f;
            float zs = -D / 2f - e / 2f, zn = D / 2f + e / 2f, xw = -W / 2f - e / 2f, xe = W / 2f + e / 2f;
            BoxLocal(g, "ZocaloSurA", new Vector3((-W / 2f - e - 2.35f) / 2f, hz / 2f, zs), new Vector3(W / 2f + e - 2.35f, hz, e), Quaternion.identity, zocalo, false);
            BoxLocal(g, "ZocaloSurB", new Vector3((-1.25f + W / 2f + e) / 2f, hz / 2f, zs), new Vector3(W / 2f + e + 1.25f, hz, e), Quaternion.identity, zocalo, false);
            BoxLocal(g, "ZocaloNorte", new Vector3(0, hz / 2f, zn), new Vector3(W + 2f * e, hz, e), Quaternion.identity, zocalo, false);
            BoxLocal(g, "ZocaloOeste", new Vector3(xw, hz / 2f, 0), new Vector3(e, hz, D), Quaternion.identity, zocalo, false);
            BoxLocal(g, "ZocaloEste", new Vector3(xe, hz / 2f, 0), new Vector3(e, hz, D), Quaternion.identity, zocalo, false);

            // Emparrado delante de la puerta
            var p = Group("Emparrado", g);
            float zf = -D / 2f, z2 = zf - 2.7f, hp = 2.5f;
            foreach (float x in new[] { -4.0f, 0.4f })
                BoxLocal(p, "Poste", new Vector3(x, hp / 2f, z2), new Vector3(0.14f, hp, 0.14f), Quaternion.Euler(0, Random.Range(-4f, 4f), Random.Range(-1.5f, 1.5f)), madera, true);
            Viga(p, "Carrera", new Vector3(-4.3f, hp + 0.06f, z2), new Vector3(0.7f, hp + 0.06f, z2), 0.12f, 0.12f, madera, false);
            Viga(p, "Durmiente", new Vector3(-4.3f, hp, zf - 0.08f), new Vector3(0.7f, hp, zf - 0.08f), 0.1f, 0.1f, madera, false);
            foreach (float x in new[] { -4.0f, -2.5f, -1.0f, 0.4f })
                Viga(p, "Travesano", new Vector3(x, hp + 0.12f, zf), new Vector3(x, hp + 0.12f, z2 - 0.2f), 0.08f, 0.1f, madera, false);
            for (float z = zf - 0.3f; z > z2 - 0.1f; z -= 0.45f)
                Viga(p, "Cana", new Vector3(-4.2f, hp + 0.2f, z), new Vector3(0.6f, hp + 0.2f, z + Random.Range(-0.05f, 0.05f)), 0.035f, 0.035f, madera, false);

            // Poyo de piedra junto a la puerta
            BoxLocal(g, "Poyo", new Vector3(-0.1f, 0.2f, zf - 0.22f), new Vector3(1.6f, 0.4f, 0.42f), Quaternion.identity, piedra, true);
            BoxLocal(g, "PoyoLosa", new Vector3(-0.1f, 0.43f, zf - 0.24f), new Vector3(1.7f, 0.06f, 0.48f), Quaternion.identity, piedra, false);
        }

        // ---------- Luces ----------
        static void Luces(Transform g)
        {
            Bombilla(g, "LuzCocina", new Vector3(-2f, 2.5f, 0f), 2.2f, 7f, true);
            Bombilla(g, "LuzDormitorio", new Vector3(3f, 2.5f, 1.6f), 1.6f, 5f, false);
            Bombilla(g, "FarolPuerta", new Vector3(-1.8f, 2.45f, -D / 2f - 0.18f), 1.6f, 9f, true);
            var fuego = new GameObject("LuzBrasas").AddComponent<Light>();
            fuego.transform.SetParent(g, false);
            fuego.transform.localPosition = new Vector3(-W / 2f + T + 0.45f, Suelo + 0.4f, 0);
            fuego.type = LightType.Point; fuego.color = new Color(1f, 0.45f, 0.15f); fuego.intensity = 1.2f; fuego.range = 3.5f;
        }

        static void Bombilla(Transform g, string name, Vector3 pos, float intensity, float range, bool on)
        {
            var b = Group(name, g);
            b.localPosition = pos;
            BoxLocal(b, "Cable", new Vector3(0, (Techo - pos.y) / 2f + 0.03f, 0), new Vector3(0.01f, Mathf.Max(0.02f, Techo - pos.y), 0.01f), Quaternion.identity, hierro, false);
            var bulbo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(bulbo.GetComponent<Collider>());
            bulbo.name = "Bulbo"; bulbo.transform.SetParent(b, false); bulbo.transform.localScale = Vector3.one * 0.07f;
            bulbo.GetComponent<MeshRenderer>().sharedMaterial = on ? bombilla : cristal;
            var l = b.gameObject.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, 0.78f, 0.52f); l.intensity = intensity; l.range = range;
            l.shadows = LightShadows.Soft; l.enabled = on;
        }
    }
}
