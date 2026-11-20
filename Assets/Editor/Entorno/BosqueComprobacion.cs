using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheFog.Entorno;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TheFog.EditorTools.Entorno
{
    /// <summary>
    /// Lista «OK / FALTA» de lo que necesita la primera versión jugable (el prólogo del bosque): objetos de la escena
    /// abierta con su componente, luces del generador en su orden, recursos de sonido e interfaz, el hueco del vídeo
    /// y qué modelos opcionales de Josemi existen ya. No cambia nada: solo escribe en la consola.
    /// Se ejecuta sola al acabar «2. Construir escena del bosque».
    /// </summary>
    public static class BosqueComprobacion
    {
        const string TripoDir = "Assets/Models/Retro/Tripo/";
        const string VideoPath = "Assets/StreamingAssets/Video/noticia_valdara.mp4";

        static readonly string[] OrdenLuces =
        {
            "Luz generador", "Luz interior", "Luz porche", "Luz poste 0", "Luz poste 1", "Luz poste 2", "Luz poste 3", "Luz poste 4",
            "Luz pie de torre", "Luz cabina",
        };

        // Modelos que Josemi todavía puede no haber hecho: nombre de carpeta y para qué sirve.
        static readonly string[][] Opcionales =
        {
            new[] { "televisor_80", "la tele de la cabaña (mientras tanto: television_02 de Poly Haven)" },
            new[] { "emisora", "la emisora de la torre (mientras tanto: vintage_radio_transceiver de Poly Haven)" },
            new[] { "diario", "el diario, sobre el escritorio (sin él, tecla J)" },
            new[] { "walkie", "el walkie, sobre el escritorio (sin él, tecla T)" },
            new[] { "mapa", "el mapa, en la pared junto a la puerta (sin él, tecla M)" },
            new[] { "interruptor", "el interruptor de la luz, junto a la puerta" },
            new[] { "cuadro_electrico", "el cuadro eléctrico, en la fachada del generador" },
            new[] { "bombilla", "la bombilla del techo de la cabaña" },
            new[] { "aplique", "el aplique del porche" },
            new[] { "farola_camino", "la farola de cada poste del sendero (sin ella la luz sale del poste)" },
        };

        static StringBuilder sb;
        static int faltan;

        [MenuItem("THE FOG/Bosque/3. Comprobar primera versión")]
        public static void Comprobar()
        {
            sb = new StringBuilder();
            faltan = 0;
            var escena = EditorSceneManager.GetActiveScene();
            sb.AppendLine("[Bosque] COMPROBACIÓN DE LA PRIMERA VERSIÓN — escena abierta: " + (string.IsNullOrEmpty(escena.path) ? "(sin guardar)" : escena.path));
            if (escena.path != "Assets/Scenes/Bosque.unity")
                sb.AppendLine("  AVISO  La escena abierta no es Assets/Scenes/Bosque.unity: lo de «escena» se refiere a la que está abierta.");

            // ---------------- Escena
            sb.AppendLine("ESCENA");
            var todos = Resources.FindObjectsOfTypeAll<Transform>().Where(t => t != null && t.gameObject.scene.IsValid()).ToArray();
            Objeto(todos, "CASA DE DANIEL");
            Objeto(todos, "TORRE DE DANIEL");
            ConAccionable(todos, "Televisor", "tele");
            ConAccionable(todos, "Cama", "cama");
            ConAccionable(todos, "Ventana", "ventana");
            ConRecogible(todos, "Linterna", "linterna");
            ConRecogible(todos, "Garrafa de combustible", "garrafa");
            Con<RadioBosque>(todos, "Radio de casa");
            Con<RadioBosque>(todos, "Emisora de la torre");
            for (int k = 0; k < 5; k++) Con<Light>(todos, "Luz poste " + k);

            var casa = Buscar(todos, "CASA DE DANIEL");
            var garrafa = Buscar(todos, "Garrafa de combustible");
            if (casa != null && garrafa != null && MundoBosque.DentroDeCasa(casa, garrafa.position))
                Linea(false, "El bidón está fuera de la cabaña", "sigue dentro (escena sin reconstruir: menú 2)");

            var gens = todos.Select(t => t.GetComponent<GeneradorBosque>()).Where(g => g != null).ToArray();
            if (gens.Length == 0) Linea(false, "GeneradorBosque", "no hay ninguno en la escena");
            else
            {
                var gb = gens[0];
                var nombres = (gb.luces ?? new Light[0]).Select(l => l != null ? l.name : "(vacía)").ToArray();
                bool diez = nombres.Length == 10;
                Linea(diez, "GeneradorBosque en «" + gb.name + "» con 10 luces", "tiene " + nombres.Length + " (escena sin reconstruir: menú 2)");
                if (diez)
                    Linea(nombres.SequenceEqual(OrdenLuces), "Orden de encendido: generador, interior, porche, postes 0-4, pie de torre, cabina",
                        "el orden es: " + string.Join(", ", nombres));
                if (casa != null)
                {
                    float d = Vector2.Distance(new Vector2(gb.transform.position.x, gb.transform.position.z), new Vector2(casa.position.x, casa.position.z));
                    Linea(d < 12f, "El generador está junto a la cabaña", "está a " + d.ToString("0") + " m de ella (escena sin reconstruir: menú 2)");
                }
                if (gb.name.StartsWith("FALTA")) Linea(false, "Modelo del generador", "no se importó generador_daniel: queda un punto usable e invisible");
                if (gens.Length > 1) sb.AppendLine("  AVISO  Hay " + gens.Length + " GeneradorBosque en la escena; el juego usa el primero que encuentre.");
            }

            var daniel = Buscar(todos, "Daniel");
            Linea(daniel != null && daniel.GetComponent<BosqueJuego>() != null, "BosqueJuego en Daniel", daniel == null ? "no hay ningún objeto «Daniel»" : "Daniel no lleva BosqueJuego");

            var sinModelo = todos.Where(t => t.name.StartsWith("FALTA")).Select(t => t.name).Distinct().ToArray();
            Linea(sinModelo.Length == 0, "Ningún modelo ausente en la escena", "puntos vacíos: " + string.Join(", ", sinModelo));

            // ---------------- Recursos
            sb.AppendLine("RECURSOS (Assets/Resources/)");
            foreach (var r in new[] { "Audio/Sint/generador_bucle", "Audio/Pasos/madera_1", "Audio/Ambiente/viento_bucle", "Audio/Musica/tension" })
                Linea(Resources.Load<AudioClip>(r) != null, r, "no se carga como AudioClip");
            foreach (var r in new[] { "UI/fog/panel", "UI/papel", "UI/walkie", "UI/mapa" })
                Linea(Resources.Load<TextAsset>(r) != null, r + ".bytes", "no se carga como TextAsset");
            foreach (var f in new[] { "BarlowCondensed-Medium", "BarlowCondensed-SemiBold", "SpecialElite-Regular", "VT323-Regular" })
                Linea(Resources.Load<Font>("Fuentes/" + f) != null, "Fuentes/" + f, "no se carga como Font");

            // ---------------- Vídeo
            sb.AppendLine("VÍDEO DEL INFORMATIVO (opcional)");
            Linea(System.Type.GetType("UnityEngine.Video.VideoPlayer, UnityEngine.VideoModule") != null, "Módulo de vídeo de Unity",
                "no está activo (Packages/manifest.json debe llevar com.unity.modules.video); sin él se ven los rótulos");
            Opcional(System.IO.File.Exists(VideoPath), VideoPath, "no existe todavía: se verán los rótulos de la cadena con subtítulos");

            // ---------------- Modelos opcionales
            sb.AppendLine("MODELOS OPCIONALES DE JOSEMI (" + TripoDir + "<nombre>/<nombre>.gltf)");
            foreach (var m in Opcionales)
                Opcional(AssetDatabase.LoadAssetAtPath<GameObject>(TripoDir + m[0] + "/" + m[0] + ".gltf") != null, m[0], "sin hacer — " + m[1]);

            sb.AppendLine(faltan == 0 ? "RESULTADO: todo lo necesario está." : "RESULTADO: faltan " + faltan + " cosas (líneas FALTA).");
            if (faltan == 0) Debug.Log(sb.ToString()); else Debug.LogWarning(sb.ToString());
            sb = null;
        }

        static Transform Buscar(IEnumerable<Transform> todos, string nombre) => todos.FirstOrDefault(t => t.name == nombre);

        static void Linea(bool ok, string que, string siFalta)
        {
            if (!ok) faltan++;
            sb.AppendLine(ok ? "  OK     " + que : "  FALTA  " + que + ": " + siFalta);
        }

        /// <summary>Lo que no es obligatorio: no cuenta como falta.</summary>
        static void Opcional(bool hay, string que, string siNo)
        {
            sb.AppendLine(hay ? "  OK     " + que : "  --     " + que + ": " + siNo);
        }

        static void Objeto(Transform[] todos, string nombre)
        {
            Linea(Buscar(todos, nombre) != null, nombre, "no está en la escena");
        }

        static void Con<T>(Transform[] todos, string nombre) where T : Component
        {
            var t = Buscar(todos, nombre);
            if (t == null) { Linea(false, nombre, "no está en la escena (escena sin reconstruir: menú 2)"); return; }
            Linea(t.GetComponentInChildren<T>(true) != null, nombre + " (" + typeof(T).Name + ")", "está, pero sin " + typeof(T).Name);
        }

        static void ConAccionable(Transform[] todos, string nombre, string clave)
        {
            var t = Buscar(todos, nombre);
            if (t == null) { Linea(false, nombre, "no está en la escena (escena sin reconstruir: menú 2)"); return; }
            var a = t.GetComponent<Accionable>();
            Linea(a != null && a.clave == clave && t.GetComponentInChildren<Collider>(true) != null, nombre + " (Accionable «" + clave + "»)",
                a == null ? "está, pero sin Accionable" : a.clave != clave ? "su clave es «" + a.clave + "»" : "no tiene collider");
        }

        static void ConRecogible(Transform[] todos, string nombre, string clave)
        {
            var t = Buscar(todos, nombre);
            if (t == null) { Linea(false, nombre, "no está en la escena"); return; }
            var r = t.GetComponent<Recogible>();
            Linea(r != null && r.clave == clave, nombre + " (Recogible «" + clave + "»)", r == null ? "está, pero sin Recogible" : "su clave es «" + r.clave + "»");
        }
    }
}
