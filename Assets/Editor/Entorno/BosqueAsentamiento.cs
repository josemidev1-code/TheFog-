using System.Collections.Generic;
using System.Linq;
using TheFog.Entorno;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TheFog.EditorTools.Entorno
{
    /// <summary>
    /// Lo que hay construido en el bosque: la casa de Daniel (claro central), la torre de vigilancia con la cabina
    /// de comunicaciones (claro del oeste), el generador al pie de la torre y el tendido de postes entre ambas.
    /// Casa, torre y muebles salen del mismo pack que el bosque (Elbolilloduro, CC0), así todo comparte estilo.
    /// </summary>
    public static partial class BosqueBuilder
    {
        const string TripoDir = "Assets/Models/Retro/Tripo/";
        const string PolyDir = "Assets/Models/Retro/PolyHaven/";

        static readonly Vector2 CasaPos = new Vector2(11f, -8f);
        static readonly Vector2 TorrePos = new Vector2(-100f, -8f);
        const float TorreYaw = 180f;   // la escalera arranca del lado por donde llega el sendero
        const bool TorreDeMetal = true; // estructura de metal oxidado (la cabina sigue siendo de madera)
        // x, z, radio de la explanada (también zona sin vegetación)
        static readonly Vector3[] Edificios = { new Vector3(11f, -8f, 6.5f), new Vector3(-100f, -8f, 9f) };

        // Lo que el constructor deja preparado para cablear en Daniel (BosqueJuego).
        static readonly List<Light> lucesGenerador = new List<Light>();
        static GameObject figuraSendero;
        static Vector3 zonaSilueta;
        static RadioBosque radioCasa, radioTorre;
        static GameObject generadorGo;

        static Vector2 inicioDaniel = new Vector2(0, -146);
        static Vector2 miraDaniel = new Vector2(-12, -110);

        // Sitios ocupados por enseres sueltos (x, z, radio): ahí tampoco crece hierba, que los atravesaría.
        static readonly List<Vector3> despejado = new List<Vector3>();

        static bool Despejado(Vector2 p)
        {
            foreach (var d in despejado)
                if ((p.x - d.x) * (p.x - d.x) + (p.y - d.y) * (p.y - d.y) < d.z * d.z) return true;
            return false;
        }

        static bool EnEdificio(Vector2 p, float margen)
        {
            foreach (var e in Edificios)
                if (Vector2.Distance(p, new Vector2(e.x, e.y)) < e.z + margen) return true;
            return false;
        }

        static void Asentamiento(Transform root)
        {
            lucesGenerador.Clear();
            despejado.Clear(); derechos.Clear();
            AssetDatabase.DeleteAsset(CorregidosDir);
            // ---------------- Casa de Daniel
            var casa = Edificio("Cabana", "CASA DE DANIEL", root, CasaPos, 0.25f);
            // El pack trae la estufa de lado, con la boca hacia la pared de la puerta: va en la esquina, mirando a la habitación.
            Recolocar(casa, "oven_003", 45f, new Vector2(1.02f, 1.74f));
            // La silla suelta del pack se quita: las dos sillas se colocan más abajo, cada una mirando a lo suyo.
            Quitar(casa, "Cube_031");
            // La cocina ocupa la pared de la ventana (encimera con fregadero y cocina de gas, sacadas de la cabina de la torre):
            // la estantería sobra y la cómoda del aparato pasa a la pared del fondo, entre la cocina y la cama.
            Quitar(casa, "shelve_003");
            Recolocar(casa, "drawer_01_003", 90f, new Vector2(-3.19f, -0.70f));
            var puertaCasa = casa.transform.Cast<Transform>().First(t => t.name.StartsWith("Door"));
            Vector3 frente = Vector3.ProjectOnPlane(Limites(puertaCasa.gameObject).center - Limites(casa).center, Vector3.up).normalized;
            Vector3 quiero = new Vector3(-CasaPos.x, 0f, -CasaPos.y).normalized; // la puerta mira al cruce de senderos
            casa.transform.rotation = Quaternion.FromToRotation(frente, quiero);
            Colisiones(casa);
            Puerta(casa, puertaCasa);
            Electrica(Luz("Luz interior", casa.transform, casa.transform.position + Vector3.up * 2.3f, new Color(1f, 0.70f, 0.40f), 3.4f, 8f, true));
            // Sin corriente, lo único que alumbra dentro son las brasas de la estufa.
            Luz("Brasas", casa.transform, casa.transform.TransformPoint(new Vector3(0.48f, 0.5f, 1.2f)), new Color(1f, 0.42f, 0.16f), 0.9f, 4.2f, true)
                .gameObject.AddComponent<LuzParpadeo>().cantidad = 0.35f;
            Vector3 fuera = casa.transform.position + quiero * 5.2f;
            Electrica(Luz("Luz porche", casa.transform, casa.transform.position + quiero * 3.6f + Vector3.up * 2.3f, new Color(1f, 0.78f, 0.5f), 4f, 9f, true));
            // Daniel empieza dentro, junto a la cama, mirando a la puerta.
            Vector3 ini = casa.transform.TransformPoint(new Vector3(-1.5f, 0f, -0.3f));
            inicioDaniel = new Vector2(ini.x, ini.z);
            miraDaniel = new Vector2(fuera.x, fuera.z);
            Physics.SyncTransforms();

            // Interior: mesa con la radio, silla, garrafa junto a la puerta, cosas de vivir ahí.
            var dentro = Group(casa.transform, "Enseres");
            Dentro(EdifDir + "Prop_Cocina.fbx", dentro, casa, new Vector3(-2.25f, SueloCasa, -1.865f), 0f, true).name = "Cocina";
            Dentro(EdifDir + "Prop_Cocina_Gas.fbx", dentro, casa, new Vector3(-0.655f, SueloCasa, -1.945f), 90f, true).name = "Cocina de gas";
            Dentro(EdifDir + "Prop_Table.fbx", dentro, casa, new Vector3(0.45f, SueloCasa, -2.0f), 90f, true);
            Silla(dentro, casa, new Vector2(0.35f, -1.34f), new Vector2(0.32f, -2.0f));  // sentado a la radio
            Silla(dentro, casa, new Vector2(-0.25f, 1.05f), new Vector2(0.66f, 1.38f));   // frente a la boca de la estufa
            Physics.SyncTransforms();
            radioCasa = Usable<RadioBosque>(Encima(PolyDir + "vintage_radio_transceiver/vintage_radio_transceiver_1k.gltf", dentro, casa, new Vector3(0.20f, 1.6f, -2.05f), 0f, "Radio de casa"), "escuchar la radio");
            Encima(EdifDir + "Prop_Cup.fbx", dentro, casa, new Vector3(0.52f, 1.6f, -1.80f), 40f, null);
            var cuaderno = Encima(EdifDir + "Prop_Documents.fbx", dentro, casa, new Vector3(0.80f, 1.6f, -2.02f), 15f, "Cuaderno del abuelo");
            Tumbar(cuaderno); // el modelo viene de canto; va tumbado sobre la mesa
            Nota(cuaderno, "leer el cuaderno", "Cuaderno del abuelo",
                "La encontramos a unos cuarenta metros de la galería vieja. No parecía una veta. Tampoco parecía una piedra.\n\nJulián quiso romper un trozo con el pico. Le dije que esperara.\n\nEsta mañana han cerrado la mina.\n\nDicen que volveremos a trabajar el lunes. Nadie ha explicado qué van a sacar mientras tanto.", false);
            // Sobre la cómoda: el aparato que hizo el abuelo (modelo exclusivo: imagen de ChatGPT → Tripo).
            const string detector = TripoDir + "detector/detector.gltf";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(detector) != null)
                Nota(Encima(detector, dentro, casa, new Vector3(-3.19f, 1.5f, -0.70f), 160f, "Detector del abuelo"), "mirar el aparato", "Aparato del abuelo",
                    "Una caja de chapa verde con un medidor de aguja y una sonda al final de un cable. Lo hizo el abuelo con piezas de otras cosas. Nunca explicó qué medía.\n\nAcercas la sonda a la ventana: la aguja no se mueve.\n\nTe la acercas a la mano.\n\nSe mueve. Solo un poco.", false);
            else
                Encima(EdifDir + "Prop_Book.fbx", dentro, casa, new Vector3(-3.19f, 1.5f, -0.70f), 160f, null);
            var lin = Usable<Recogible>(Encima(EdifDir + "Prop_flashlight.fbx", dentro, casa, new Vector3(-2.0f, 1.2f, 2.1f), 30f, "Linterna"), "coger la linterna");
            if (lin != null) lin.clave = "linterna";
            Dentro(EdifDir + "Prop_teapot.fbx", dentro, casa, new Vector3(0.91f, 1.01f, 1.63f), 65f, false); // sobre la plancha de la estufa
            var garrafa = Dentro(PolyDir + "plastic_jerrycan/plastic_jerrycan_1k.gltf", dentro, casa, new Vector3(1.35f, SueloCasa, -0.95f), 100f, true);
            garrafa.name = "Garrafa de combustible";
            Usable<Recogible>(garrafa, "coger la garrafa").clave = "garrafa";
            // Leña apilada contra la pared, al lado de la estufa; la mochila, apoyada entre la mesilla y la leña.
            Dentro(EdifDir + "Prop_firewood_01.fbx", dentro, casa, new Vector3(0.30f, SueloCasa, 2.20f), 90f, false);
            Dentro(EdifDir + "Prop_firewood.fbx", dentro, casa, new Vector3(-0.42f, SueloCasa, 2.14f), 80f, false);
            Dentro(EdifDir + "Prop_Backpack.fbx", dentro, casa, new Vector3(-1.25f, SueloCasa, 2.12f), 200f, false);
            // Sobre la encimera y la cocina: lo de comer y los cacharros, con modelos del mismo pack.
            const float enc = SueloCasa + 0.92f, gas = SueloCasa + 1.01f;
            Dentro(EdifDir + "Prop_dishes.fbx", dentro, casa, new Vector3(-2.42f, enc, -1.9f), 0f, false);
            Dentro(EdifDir + "Prop_dishes.fbx", dentro, casa, new Vector3(-2.42f, enc + 0.03f, -1.9f), 25f, false);
            Dentro(EdifDir + "Prop_Cup.fbx", dentro, casa, new Vector3(-2.15f, enc, -1.62f), 110f, false);
            Dentro(EdifDir + "Prop_pot.fbx", dentro, casa, new Vector3(-1.95f, enc, -2.05f), 30f, false);
            Dentro(EdifDir + "Prop_Food.fbx", dentro, casa, new Vector3(-1.68f, enc, -2.14f), 0f, false);
            Dentro(EdifDir + "Prop_Food.fbx", dentro, casa, new Vector3(-1.53f, enc, -2.1f), 40f, false);
            Dentro(EdifDir + "Prop_Food.fbx", dentro, casa, new Vector3(-1.6f, enc + 0.08f, -2.12f), 75f, false);
            Dentro(EdifDir + "Prop_Water_bottle.fbx", dentro, casa, new Vector3(-1.3f, enc, -2.15f), 0f, false);
            Dentro(EdifDir + "Prop_Water_bottle.fbx", dentro, casa, new Vector3(-1.19f, enc, -2.1f), 50f, false);
            Dentro(EdifDir + "Prop_canteen.fbx", dentro, casa, new Vector3(-2.2f, enc, -2.16f), 95f, false);
            Dentro(EdifDir + "Prop_Matches.fbx", dentro, casa, new Vector3(-1.2f, enc, -1.62f), 25f, false);
            Dentro(EdifDir + "Prop_frying_pan.fbx", dentro, casa, new Vector3(-0.66f, gas, -1.9f), 35f, false);
            Dentro(EdifDir + "Prop_tray.fbx", dentro, casa, new Vector3(1.35f, SueloCasa, -2.0f), 0f, false); // el cubo, en el rincón
            Nota(Dentro(EdifDir + "Prop_calendar.fbx", dentro, casa, new Vector3(-3.52f, 1.32f, -0.70f), 0f, false), "mirar el calendario", "Calendario",
                "Las últimas noches están tachadas. La de hoy, todavía no.", true);
            Nota(Dentro(EdifDir + "Prop_Photos.fbx", dentro, casa, new Vector3(-3.52f, 1.55f, 1.25f), 0f, false), "mirar la foto", "Fotografía",
                "Mina de Valdara. Nueve hombres en la bocamina, con los cascos en la mano. El abuelo es el segundo por la izquierda.\n\nDetrás, a lápiz:\n«Galería vieja. El último día que bajamos todos.»", false);

            // Alrededor de la casa: leña, pozo, carro, coche, herramientas.
            Vector3 lado = Vector3.Cross(Vector3.up, quiero);
            var alrededor = Group(root, "Alrededor de la casa");
            Prop(TripoDir + "lena/lena.gltf", alrededor, En(CasaPos, quiero, lado, -1.5f, 5.6f), Yaw(quiero) + 90f, 1f, true, 1.6f);
            Prop(TripoDir + "pozo/pozo.gltf", alrededor, En(CasaPos, quiero, lado, 8.5f, -8.5f), 20f, 1f, true, 2f);
            Prop(TripoDir + "carro/carro.gltf", alrededor, En(CasaPos, quiero, lado, -3f, -8.5f), Yaw(quiero) + 70f, 1f, true, 2.5f);
            Nota(Prop(EdifDir + "Coche.fbx", alrededor, En(CasaPos, quiero, lado, 9.5f, 6.5f), Yaw(quiero) + 160f, 1f, true, 3f), "mirar la furgoneta", "",
                "No arranca desde el martes. La batería estaba bien.", true);
            // La pala, apoyada en la pared del lado de la leñera (inclinada hasta tocar los troncos).
            {
                var pala = Prop(EdifDir + "Prop_Shovel.fbx", alrededor, En(CasaPos, quiero, lado, 0.6f, 2.72f + 0.27f), 0f, 1f, false, 0.3f);
                if (pala != null) pala.transform.rotation = Quaternion.AngleAxis(15f, casa.transform.right) * Quaternion.Euler(0f, casa.transform.eulerAngles.y + 90f, 0f);
            }
            Prop(EdifDir + "Prop_Box.fbx", alrededor, En(CasaPos, quiero, lado, 4.5f, -2.6f), 15f, 1f, true, 0.5f);
            Prop(EdifDir + "Prop_Box.fbx", alrededor, En(CasaPos, quiero, lado, 4.6f, -3.2f), -8f, 1f, true, 0.5f);

            // ---------------- Torre de vigilancia y comunicaciones
            var torre = Edificio("Torre_Vigia", "TORRE DE DANIEL", root, TorrePos, 0.05f);
            torre.transform.rotation = Quaternion.Euler(0f, TorreYaw, 0f);
            // La silla de la cabina venía de espaldas a la cómoda: va a la mesa auxiliar, al final de la encimera.
            Recolocar(torre, "Cube_003", 0f, new Vector2(-2.39f, -2.12f));
            // La cocina de la cabina (encimera en L con dos fregaderos, cocina de gas y platos) se va a la casa.
            // Arriba queda un banco de trabajo de dos módulos, sin fregadero, para la emisora, el monitor y los partes.
            foreach (var pieza in new[] { "Kitchen_table_001", "Stove", "dishes_003", "dishes_004", "dishes_005" }) Quitar(torre, pieza);
            Recolocar(torre, "Book_001", 0f, new Vector2(1.72f, -2.94f));
            Colisiones(torre);
            if (TorreDeMetal) Metal(torre, "Cube_016", "Cube_046");
            Physics.SyncTransforms();
            // Puesto de comunicaciones en la encimera de la cabina: emisora y monitor de las cámaras.
            var puesto = Group(torre.transform, "Puesto de comunicaciones");
            Dentro(EdifDir + "Prop_Banco_Torre.fbx", puesto, torre, new Vector3(0.76f, 17.73f, -2.765f), 0f, true).name = "Banco de trabajo";
            Physics.SyncTransforms();
            radioTorre = Usable<RadioBosque>(Encima(PolyDir + "vintage_radio_transceiver/vintage_radio_transceiver_1k.gltf", puesto, torre, new Vector3(0.58f, 20.2f, -2.85f), 0f, "Emisora de la torre"), "usar la emisora");
            if (radioTorre != null) radioTorre.deTorre = true;
            var monitor = Usable<MonitorCamaras>(Encima(PolyDir + "television_02/television_02_1k.gltf", puesto, torre, new Vector3(-0.12f, 20.2f, -2.9f), 0f, "Monitor de camaras"), "mirar las cámaras");
            var parte = torre.transform.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("Documents"));
            if (parte != null) Nota(parte.gameObject, "leer el parte", "Parte de incidencias",
                "PUESTO FORESTAL 03 — turno de noche\n\n22:40  Sin novedad.\n01:15  Niebla en el sendero. Sin novedad.\n03:10  Figura en CAM 03, junto a la casa. No la reconozco.\n03:12  Ya no está.\n\nLa letra es tuya. La fecha, de hace tres noches.", false);
            var archivador = torre.transform.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("Archivist"));
            if (archivador != null) Nota(archivador.gameObject, "abrir el archivador", "Hoja de mantenimiento",
                "PUESTO FORESTAL 03\n\nCircuito A: iluminación y vigilancia.\nCircuito B: sensores y equipo auxiliar de captación.\n\nEn caso de avería, mantener la alimentación de emergencia mientras se evalúa el estado de los filtros.\n\nADVERTENCIA: no accionar la purga manual sin autorización del responsable técnico.", false);
            Encima(EdifDir + "Prop_flashlight.fbx", puesto, torre, new Vector3(1.0f, 20.2f, -2.5f), 60f, null);
            var puertaTorre = torre.transform.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("Door"));
            if (puertaTorre != null) Puerta(torre, puertaTorre);
            var cabina = torre.transform.Cast<Transform>().First(t => t.name == "Cube_022"); // los cristales de la cabina
            var cb = Limites(cabina.gameObject);
            var lCabina = Luz("Luz cabina", torre.transform, new Vector3(cb.center.x, cb.max.y + 0.2f, cb.center.z), new Color(1f, 0.72f, 0.42f), 7f, 12f, true);
            var lPie = Luz("Luz pie de torre", torre.transform, torre.transform.position + new Vector3(0f, 3.2f, 0f), new Color(0.95f, 0.8f, 0.55f), 6f, 15f, true);

            // Generador al pie de la torre, con sus bidones.
            var gen = Group(root, "Generador");
            Vector2 g = TorrePos + new Vector2(7.6f, 6.8f);
            generadorGo = Prop(TripoDir + "generador/generador.gltf", gen, g, -70f, 1.5f, true, 1.2f);
            taken.Add(new Vector3(g.x, g.y, 6f)); // sin matas encima de los bidones
            Prop(EdifDir + "Prop_fuel_tank.fbx", gen, g + new Vector2(0.9f, 0.5f), 30f, 1f, true, 0.4f);
            Prop(EdifDir + "Prop_fuel_tank.fbx", gen, g + new Vector2(1.2f, -0.1f), -50f, 1f, true, 0.4f);
            Prop(PolyDir + "plastic_jerrycan/plastic_jerrycan_1k.gltf", gen, g + new Vector2(-0.8f, 0.7f), 110f, 1f, true, 0.4f);
            Prop(EdifDir + "Prop_toolbox.fbx", gen, g + new Vector2(0.2f, -0.9f), 12f, 1f, false, 0.3f);
            // Orden en que vuelve la luz al arrancar: generador, pie de torre, cabina, y por el tendido hasta la casa.
            var lGen = Luz("Luz generador", gen, Suelo(g, 0f) + Vector3.up * 1.6f, new Color(1f, 0.75f, 0.45f), 3f, 7f, false);
            var deCasa = lucesGenerador.ToArray();
            lucesGenerador.Clear();
            Electrica(lGen); Electrica(lPie); Electrica(lCabina);
            foreach (var l in deCasa) Electrica(l);
            lCabina.gameObject.AddComponent<LuzParpadeo>().cantidad = 0.08f;
            if (generadorGo != null)
            {
                var gb = generadorGo.AddComponent<GeneradorBosque>();
                gb.luces = lucesGenerador.ToArray();
                gb.cristales = torre.GetComponentsInChildren<Renderer>().Concat(casa.GetComponentsInChildren<Renderer>())
                    .Where(r => r.sharedMaterials.Any(m => m != null && m.name.StartsWith("Glass"))).ToArray();
            }

            // La silueta: una en el sendero, otra que solo se ve por la cámara de la casa.
            zonaSilueta = Suelo(new Vector2(-34f, 7.5f), 0f);
            figuraSendero = Figura(root, new Vector2(-52f, 10.8f), 0f, "Silueta del sendero");
            Vector3 porche = casa.transform.position + quiero * 4.6f - lado * 1.3f;
            var figuraCasa = Figura(root, new Vector2(porche.x, porche.z), Yaw(quiero), "Silueta de la casa");
            figuraCasa.SetActive(false);

            // Cámaras del monitor: sendero, bosque y casa.
            var cams = Group(root, "Camaras");
            var c1 = Camara(cams, "CAM 01", Suelo(new Vector2(-48f, 13.5f), 0f) + Vector3.up * 4.2f, Suelo(new Vector2(-20f, 5f), 0f) + Vector3.up);
            var c2 = Camara(cams, "CAM 02", torre.transform.position + new Vector3(-3f, 7f, 5f), Suelo(TorrePos + new Vector2(-22f, 30f), 0f) + Vector3.up * 2f);
            var c3 = Camara(cams, "CAM 03", casa.transform.position + quiero * 11f + lado * 3.5f + Vector3.up * 3.4f, casa.transform.position + quiero * 3f + Vector3.up * 1.3f);
            if (monitor != null)
            {
                monitor.camaras = new[] { c1, c2, c3 };
                monitor.nombres = new[] { "CAM 01 · SENDERO", "CAM 02 · MONTE", "CAM 03 · CASA" };
                monitor.figura = figuraCasa;
                monitor.puertaCasa = casa.GetComponentInChildren<PuertaAbatible>();
            }

            // Tendido de la torre a la casa, siguiendo el ramal: postes y cables (BosqueTendido.cs).
            Tendido(root, casa, torre, quiero);
        }

        const float SueloCasa = -0.028f; // altura del suelo de tablas respecto al origen de la cabaña

        /// <summary>Una silla del pack en el suelo de un edificio, mirando hacia un punto (coordenadas locales x, z).</summary>
        static GameObject Silla(Transform parent, GameObject edificio, Vector2 sitio, Vector2 mira)
        {
            Vector2 d = mira - sitio;
            return Dentro(EdifDir + "Prop_Cube.fbx", parent, edificio, new Vector3(sitio.x, SueloCasa, sitio.y), Mathf.Atan2(-d.y, d.x) * Mathf.Rad2Deg, true);
        }

        static void Quitar(GameObject edificio, string pieza)
        {
            var t = edificio.transform.Find(pieza);
            if (t != null) Object.DestroyImmediate(t.gameObject); else Debug.LogWarning("[Bosque] No encuentro la pieza " + pieza);
        }

        /// <summary>Tumba sobre su cara un objeto que el pack trae de canto, sin despegarlo de donde apoya.</summary>
        static void Tumbar(GameObject go)
        {
            if (go == null) return;
            var b = Limites(go); float apoyo = b.min.y;
            go.transform.RotateAround(b.center, go.transform.right, 90f);
            go.transform.position += Vector3.up * (apoyo - Limites(go).min.y);
        }

        /// <summary>Gira sobre sí misma una pieza que el pack trae ya colocada y lleva su centro a otro punto del edificio (x, z locales).</summary>
        static void Recolocar(GameObject edificio, string pieza, float yaw, Vector2 centro)
        {
            var t = edificio.transform.Find(pieza);
            var r = t != null ? t.GetComponent<Renderer>() : null;
            if (r == null) { Debug.LogWarning("[Bosque] No encuentro la pieza " + pieza); return; }
            t.RotateAround(r.bounds.center, Vector3.up, yaw);
            Vector3 c = r.bounds.center, destino = edificio.transform.TransformPoint(new Vector3(centro.x, 0f, centro.y));
            t.position += new Vector3(destino.x - c.x, 0f, destino.z - c.z);
        }

        const string CorregidosDir = KitDir + "Corregidos";
        static readonly Dictionary<Mesh, Mesh> derechos = new Dictionary<Mesh, Mesh>();

        /// <summary>
        /// Varias piezas del pack vienen espejadas, con las caras hacia dentro (la caja de herramientas se ve como una
        /// chapa, la linterna hueca, algunos marcos de ventana oscuros). Se detectan por el volumen con signo de la malla
        /// y se sustituyen por una copia con caras y normales del derecho, guardada en Corregidos/.
        /// </summary>
        static void CarasDelDerecho(GameObject go)
        {
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                var src = mf.sharedMesh;
                if (src == null) continue;
                if (!derechos.TryGetValue(src, out var ok))
                {
                    ok = null;
                    string origen = AssetDatabase.GetAssetPath(src);
                    float vol = origen.StartsWith(EdifDir) ? VolumenRelativo(src) : 0f;
                    if (vol < -0.05f)
                    {
                        EnsureFolder(CorregidosDir);
                        ok = Object.Instantiate(src);
                        ok.name = src.name + "_derecho";
                        if (vol < -0.3f)
                        {
                            // Del revés entera: se da la vuelta a todas las caras.
                            for (int s = 0; s < ok.subMeshCount; s++)
                            {
                                var tr = ok.GetTriangles(s);
                                for (int i = 0; i < tr.Length; i += 3) { int x = tr[i]; tr[i] = tr[i + 1]; tr[i + 1] = x; }
                                ok.SetTriangles(tr, s);
                            }
                            ok.normals = ok.normals.Select(n => -n).ToArray();
                        }
                        else
                        {
                            // Mezclada (marcos de ventana: unas caras bien y otras no): se le dan las dos caras, cada una con su normal.
                            int n0 = src.vertexCount;
                            var rend = mf.GetComponent<Renderer>();
                            var mats = rend != null ? rend.sharedMaterials : new Material[0];
                            ok.SetVertices(src.vertices.Concat(src.vertices).ToArray());
                            ok.SetNormals(src.normals.Concat(src.normals.Select(n => -n)).ToArray());
                            if (src.uv.Length == n0) ok.SetUVs(0, src.uv.Concat(src.uv).ToArray());
                            ok.subMeshCount = src.subMeshCount;
                            for (int s = 0; s < src.subMeshCount; s++)
                            {
                                var tr = src.GetTriangles(s);
                                bool cristal = s < mats.Length && mats[s] != null && mats[s].name.StartsWith("Glass"); // el cristal ya se dibuja por las dos caras
                                var todo = new List<int>(tr);
                                if (!cristal) for (int i = 0; i < tr.Length; i += 3) { todo.Add(tr[i + 1] + n0); todo.Add(tr[i] + n0); todo.Add(tr[i + 2] + n0); }
                                ok.SetTriangles(todo, s);
                            }
                            ok.RecalculateBounds();
                        }
                        AssetDatabase.CreateAsset(ok, AssetDatabase.GenerateUniqueAssetPath(
                            CorregidosDir + "/" + System.IO.Path.GetFileNameWithoutExtension(origen) + "_" + src.name.Replace('.', '_') + ".asset"));
                    }
                    derechos[src] = ok;
                }
                if (ok != null) mf.sharedMesh = ok;
            }
        }

        /// <summary>Volumen con signo de la malla dividido por el de su caja: negativo si las caras miran hacia dentro.</summary>
        static float VolumenRelativo(Mesh m)
        {
            var vs = m.vertices; var c = m.bounds.center; double v = 0;
            for (int s = 0; s < m.subMeshCount; s++)
            {
                var tr = m.GetTriangles(s);
                for (int i = 0; i < tr.Length; i += 3) v += Vector3.Dot(vs[tr[i]] - c, Vector3.Cross(vs[tr[i + 1]] - c, vs[tr[i + 2]] - c)) / 6.0;
            }
            var b = m.bounds.size; double caja = (double)b.x * b.y * b.z;
            return caja > 1e-9 ? (float)(v / caja) : 0f;
        }

        /// <summary>Coloca un modelo dentro de un edificio, en coordenadas locales de este (x hacia la puerta).</summary>
        static GameObject Dentro(string path, Transform parent, GameObject edificio, Vector3 local, float yawLocal, bool caja)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogWarning("[Bosque] Falta el modelo " + path); return new GameObject("FALTA"); }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = prefab.name;
            go.transform.SetPositionAndRotation(edificio.transform.TransformPoint(local),
                Quaternion.Euler(0f, edificio.transform.eulerAngles.y + yawLocal, 0f) * prefab.transform.rotation);
            CarasDelDerecho(go);
            if (caja)
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                    if (mf.sharedMesh != null) mf.gameObject.AddComponent<BoxCollider>();
            return go;
        }

        /// <summary>Apoya un modelo sobre lo que haya debajo del punto local dado (mesa, encimera, cómoda).</summary>
        static GameObject Encima(string path, Transform parent, GameObject edificio, Vector3 localDesde, float yawLocal, string nombre)
        {
            Vector3 desde = edificio.transform.TransformPoint(localDesde);
            RaycastHit hit;
            if (!Physics.Raycast(desde, Vector3.down, out hit, 3f)) { Debug.LogWarning("[Bosque] Nada debajo para apoyar " + path); return null; }
            var go = Dentro(path, parent, edificio, edificio.transform.InverseTransformPoint(hit.point), yawLocal, false);
            // El pivote puede no estar en la base: se baja o sube hasta que apoye.
            var b = Limites(go);
            go.transform.position += Vector3.up * (hit.point.y - b.min.y);
            if (nombre != null) go.name = nombre;
            return go;
        }

        /// <summary>Cambia la madera de la estructura por chapa y perfiles oxidados.</summary>
        static void Metal(GameObject edificio, params string[] piezas)
        {
            const string mp = "Assets/Models/Retro/Bosque/Materiales/Metal_Oxidado.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, mp); }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Retro/PolyHaven/rusted_wheel_rim_02/textures/rusted_wheel_rim_02_diff_1k.jpg");
            if (tex == null) tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Retro/Materiales/Metal053B/Metal053B_1K-JPG_Color.jpg");
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", new Color(0.62f, 0.5f, 0.42f));
            m.SetFloat("_Smoothness", 0.08f); m.SetFloat("_Metallic", 0f);
            m.SetFloat("_SpecularHighlights", 0f); m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            m.SetFloat("_EnvironmentReflections", 0f); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            EditorUtility.SetDirty(m);
            foreach (Transform t in edificio.transform)
            {
                if (!piezas.Contains(t.name)) continue;
                var r = t.GetComponent<Renderer>();
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
            }
        }

        static void Electrica(Light l) { l.enabled = false; lucesGenerador.Add(l); }

        /// <summary>Hace usable con la E un objeto ya colocado: le pone caja de colisión holgada y el componente.</summary>
        static T Usable<T>(GameObject go, string rotulo) where T : Interactuable
        {
            if (go == null) return null;
            if (go.GetComponentInChildren<Collider>() == null)
            {
                var b = Limites(go);
                var c = go.AddComponent<BoxCollider>();
                c.center = go.transform.InverseTransformPoint(b.center);
                Vector3 s = go.transform.InverseTransformVector(b.size);
                c.size = new Vector3(Mathf.Max(0.22f, Mathf.Abs(s.x)), Mathf.Max(0.22f, Mathf.Abs(s.y)), Mathf.Max(0.22f, Mathf.Abs(s.z)));
            }
            var u = go.AddComponent<T>();
            u.rotulo = rotulo;
            return u;
        }

        static void Nota(GameObject go, string rotulo, string titulo, string texto, bool pensamiento)
        {
            var e = Usable<Examinable>(go, rotulo);
            if (e == null) return;
            e.titulo = titulo; e.texto = texto; e.pensamiento = pensamiento;
        }

        static GameObject Figura(Transform parent, Vector2 xz, float yaw, string nombre)
        {
            const string mp = "Assets/Models/Retro/Bosque/Materiales/Silueta.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(m, mp); }
            m.SetColor("_BaseColor", new Color(0.018f, 0.022f, 0.02f));
            EditorUtility.SetDirty(m);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KitDir + "Figura/Silueta.fbx");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = nombre;
            go.transform.SetPositionAndRotation(Suelo(xz, 0.02f), Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation);
            go.transform.localScale = prefab.transform.localScale * 1.09f; // un poco más alto de lo normal
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; }
            taken.Add(new Vector3(xz.x, xz.y, 5f));
            return go;
        }

        static Camera Camara(Transform parent, string nombre, Vector3 pos, Vector3 mira)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(mira - pos));
            var c = go.AddComponent<Camera>();
            c.fieldOfView = 62f; c.nearClipPlane = 0.2f; c.farClipPlane = 80f;
            c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = RenderSettings.fogColor;
            c.enabled = false;
            return c;
        }

        static Vector2 En(Vector2 centro, Vector3 frente, Vector3 lado, float adelante, float derecha)
        {
            Vector3 p = frente * adelante + lado * derecha;
            return centro + new Vector2(p.x, p.z);
        }

        static float Yaw(Vector3 dir) => Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        static GameObject Edificio(string modelo, string nombre, Transform parent, Vector2 xz, float alzar)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EdifDir + modelo + ".fbx");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = nombre;
            go.transform.position = Suelo(xz, 0f) + Vector3.up * alzar;
            CarasDelDerecho(go);
            return go;
        }

        /// <summary>Estructura con MeshCollider (se puede entrar y subir), muebles con caja, objetos pequeños sin nada.</summary>
        static void Colisiones(GameObject edificio)
        {
            foreach (var mf in edificio.GetComponentsInChildren<MeshFilter>())
            {
                var r = mf.GetComponent<Renderer>();
                if (mf.sharedMesh == null || r == null || mf.name.StartsWith("Door")) continue;
                var mats = r.sharedMaterials.Where(m => m != null).Select(m => m.name).ToArray();
                bool estructura = !mf.name.StartsWith("shelve") && mats.Any(m => m.StartsWith("Wood") || m.StartsWith("Rooftile") || m.StartsWith("Glass"));
                var size = r.bounds.size;
                if (estructura) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                else if (Mathf.Max(size.x, size.y, size.z) > 0.55f) mf.gameObject.AddComponent<BoxCollider>();
                mf.gameObject.isStatic = true;
            }
        }

        /// <summary>Cuelga la hoja de una bisagra en su canto y le pone el giro con la E.</summary>
        static void Puerta(GameObject edificio, Transform hoja)
        {
            var mf = hoja.GetComponent<MeshFilter>();
            var b = mf.sharedMesh.bounds; // en local de la hoja
            bool anchoEnX = b.size.x > b.size.z;
            Vector3 eje = anchoEnX ? Vector3.right : Vector3.forward;
            Vector3 canto = b.center - eje * (anchoEnX ? b.extents.x : b.extents.z); canto.y = b.min.y;
            var bisagra = new GameObject("Bisagra " + hoja.name).transform;
            bisagra.SetParent(hoja.parent, false);
            bisagra.SetPositionAndRotation(hoja.TransformPoint(canto), hoja.rotation);
            hoja.SetParent(bisagra, true);
            hoja.gameObject.isStatic = false;
            hoja.gameObject.AddComponent<BoxCollider>();
            // Abre hacia fuera: el lado al que la hoja se aleja del centro del edificio.
            Vector3 centro = Limites(edificio).center, libre = hoja.TransformPoint(b.center);
            float mejor = 100f;
            float dMas = Vector3.Distance(bisagra.position + Quaternion.AngleAxis(100f, Vector3.up) * (libre - bisagra.position), centro);
            float dMenos = Vector3.Distance(bisagra.position + Quaternion.AngleAxis(-100f, Vector3.up) * (libre - bisagra.position), centro);
            if (dMenos > dMas) mejor = -100f;
            bisagra.gameObject.AddComponent<PuertaAbatible>().anguloAbierta = mejor;
        }

        static GameObject Prop(string path, Transform parent, Vector2 xz, float yaw, float scale, bool caja, float radio)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogWarning("[Bosque] Falta el modelo " + path); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = prefab.name;
            go.transform.SetPositionAndRotation(Suelo(xz, 0.03f), Quaternion.Euler(0f, yaw, 0f) * go.transform.rotation);
            go.transform.localScale = go.transform.localScale * scale;
            CarasDelDerecho(go);
            if (caja)
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                    if (mf.sharedMesh != null && mf.GetComponent<Collider>() == null) mf.gameObject.AddComponent<BoxCollider>();
            taken.Add(new Vector3(xz.x, xz.y, radio * 2f));
            despejado.Add(new Vector3(xz.x, xz.y, radio + 0.35f));
            return go;
        }

        static Light Luz(string nombre, Transform parent, Vector3 pos, Color color, float intensidad, float alcance, bool sombras)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = color; l.intensity = intensidad; l.range = alcance;
            l.shadows = sombras ? LightShadows.Soft : LightShadows.None;
            return l;
        }
    }
}
