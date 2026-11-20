using System.Collections.Generic;
using System.Linq;
using TheFog.Entorno;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TheFog.EditorTools.Entorno
{
    /// <summary>
    /// Lo que hay construido en el bosque: la casa de Daniel (claro central) con el generador en el costado de la leñera,
    /// la torre de vigilancia con la cabina de comunicaciones (claro del oeste) y el tendido de postes entre ambas.
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
            // la estantería del pack sobra.
            Quitar(casa, "shelve_003");
            // Cama, mesilla y cómoda del pack: en su lugar van los muebles que hizo Josemi en Tripo (cama de hierro,
            // mesita con despertador y librería), colocados más abajo.
            foreach (var pieza in new[] { "Bed_003", "drawer_003", "drawer_d_003", "drawer_01_003" }) Quitar(casa, pieza);
            var puertaCasa = casa.transform.Cast<Transform>().First(t => t.name.StartsWith("Door"));
            Vector3 frente = Vector3.ProjectOnPlane(Limites(puertaCasa.gameObject).center - Limites(casa).center, Vector3.up).normalized;
            Vector3 quiero = new Vector3(-CasaPos.x, 0f, -CasaPos.y).normalized; // la puerta mira al cruce de senderos
            casa.transform.rotation = Quaternion.FromToRotation(frente, quiero);
            Colisiones(casa);
            Puerta(casa, puertaCasa);
            var lInterior = Luz("Luz interior", casa.transform, casa.transform.position + Vector3.up * 2.3f, new Color(1f, 0.70f, 0.40f), 3.4f, 8f, true);
            Electrica(lInterior);
            // Sin corriente, lo único que alumbra dentro son las brasas de la estufa.
            Luz("Brasas", casa.transform, casa.transform.TransformPoint(new Vector3(0.48f, 0.5f, 1.2f)), new Color(1f, 0.42f, 0.16f), 0.9f, 4.2f, true)
                .gameObject.AddComponent<LuzParpadeo>().cantidad = 0.35f;
            Vector3 fuera = casa.transform.position + quiero * 5.2f;
            var lPorche = Luz("Luz porche", casa.transform, casa.transform.position + quiero * 3.6f + Vector3.up * 2.3f, new Color(1f, 0.78f, 0.5f), 4f, 9f, true);
            Electrica(lPorche);
            // Daniel empieza dentro, junto a la cama, mirando a la puerta.
            Vector3 ini = casa.transform.TransformPoint(new Vector3(-1.5f, 0f, -0.3f));
            inicioDaniel = new Vector2(ini.x, ini.z);
            miraDaniel = new Vector2(fuera.x, fuera.z);
            Physics.SyncTransforms();

            // Interior: escritorio con la radio, sillas, la tele sobre la encimera, cosas de vivir ahí.
            var dentro = Group(casa.transform, "Enseres");
            Dentro(EdifDir + "Prop_Cocina.fbx", dentro, casa, new Vector3(-2.25f, SueloCasa, -1.865f), 0f, true).name = "Cocina";
            Dentro(EdifDir + "Prop_Cocina_Gas.fbx", dentro, casa, new Vector3(-0.655f, SueloCasa, -1.945f), 90f, true).name = "Cocina de gas";
            // Muebles de Josemi (Tripo, preparados con tools/tripo_prepare_usuario.sh). Con giro 0 miran hacia la pared del fondo.
            var cama = Mueble("cama", dentro, casa, new Vector3(-2.84f, SueloCasa, 1.36f), 0f, true);    // cabecero contra la pared de la ventana
            cama.name = "Cama";
            Accion(cama, BosqueGuion.RotCama, "cama");
            Mueble("mesita_noche", dentro, casa, new Vector3(-1.93f, SueloCasa, 2.11f), -90f, true);
            Mueble("libreria", dentro, casa, new Vector3(-3.28f, SueloCasa, -0.75f), 180f, true);      // pared del fondo, entre la cocina y la cama
            Mueble("planta_colgante", dentro, casa, new Vector3(-3.24f, 1.38f, -0.42f), 0f, false);   // la maceta, sobre la librería
            Mueble("escritorio", dentro, casa, new Vector3(0.5f, SueloCasa, -1.935f), 90f, true);       // el rincón de la radio
            Mueble("corcho", dentro, casa, new Vector3(0.5f, 1.15f, -2.31f), 90f, false);               // en la pared, sobre el escritorio
            Mueble("radiador", dentro, casa, new Vector3(-0.8f, SueloCasa, 2.225f), -90f, true);        // bajo la ventana
            var ventana = Mueble("ventana_cortinas", dentro, casa, new Vector3(-0.8f, 0.955f, 2.145f), -90f, false); // por dentro, tapando la ventana del pack
            ventana.transform.localScale *= 1.16f;
            ventana.name = "Ventana";
            Accion(ventana, BosqueGuion.RotVentana, "ventana");
            Mueble("alfombra", dentro, casa, new Vector3(-1.0f, SueloCasa, 0.2f), 0f, false);
            Silla(dentro, casa, new Vector2(0.5f, -1.2f), new Vector2(0.5f, -2.0f));     // sentado a la radio
            Silla(dentro, casa, new Vector2(-0.25f, 1.05f), new Vector2(0.66f, 1.38f));   // frente a la boca de la estufa
            Physics.SyncTransforms();
            // El televisor, sobre el tramo liso de la encimera (el otro tramo es el fregadero). Mientras Josemi no haga
            // «televisor_80» se usa el de Poly Haven (el mismo modelo que el monitor de la torre). La luz azulada la pone el juego.
            // television_02 mira a +z con giro 0; los modelos de Tripo miran a -x con giro 0, por eso 90.
            bool tvPropia = Hay(Tripo("televisor_80"));
            var tele = Apoyado(tvPropia ? Tripo("televisor_80") : PolyDir + "television_02/television_02_1k.gltf", dentro, casa,
                new Vector3(-1.60f, SueloCasa + 0.92f, -1.95f), tvPropia ? 90f : 0f, "Televisor");
            Accion(tele, BosqueGuion.RotTeleMirar, "tele");
            radioCasa = Usable<RadioBosque>(Encima(TripoDir + "radio_antigua/radio_antigua.gltf", dentro, casa, new Vector3(0.45f, 1.6f, -2.0f), 0f, "Radio de casa"), BosqueGuion.RotRadioEscuchar);
            Encima(EdifDir + "Prop_Cup.fbx", dentro, casa, new Vector3(-0.05f, 1.6f, -2.12f), 40f, null);
            var cuaderno = Encima(EdifDir + "Prop_Documents.fbx", dentro, casa, new Vector3(0.05f, 1.6f, -1.82f), 15f, "Cuaderno del abuelo");
            Tumbar(cuaderno); // el modelo viene de canto; va tumbado sobre el escritorio
            Nota(cuaderno, "leer el cuaderno", "Cuaderno del abuelo",
                "La encontramos a unos cuarenta metros de la galería vieja. No parecía una veta. Tampoco parecía una piedra.\n\nJulián quiso romper un trozo con el pico. Le dije que esperara.\n\nEsta mañana han cerrado la mina.\n\nDicen que volveremos a trabajar el lunes. Nadie ha explicado qué van a sacar mientras tanto.", false);
            // En el escritorio, sobre el tapete: el aparato que hizo el abuelo (modelo exclusivo: imagen de ChatGPT → Tripo).
            Nota(Encima(TripoDir + "detector/detector.gltf", dentro, casa, new Vector3(1.0f, 1.6f, -1.98f), 160f, "Detector del abuelo"), "mirar el aparato", "Aparato del abuelo",
                "Una caja de chapa verde con un medidor de aguja y una sonda al final de un cable. Lo hizo el abuelo con piezas de otras cosas. Nunca explicó qué medía.\n\nAcercas la sonda a la ventana: la aguja no se mueve.\n\nTe la acercas a la mano.\n\nSe mueve. Solo un poco.", false);
            // Modelos de Josemi que aún pueden no existir (diario, walkie, mapa, interruptor, cuadro, bombilla, aplique).
            Physics.SyncTransforms();
            Opcionales(dentro, casa, lInterior, lPorche, quiero);
            // La linterna, en la mesita, al lado del despertador (el tablero queda a 0,61 m; el reloj forma parte del modelo).
            var lin = Usable<Recogible>(Mueble("linterna", dentro, casa, new Vector3(-1.88f, SueloCasa + 0.61f, 2.02f), 60f, false), BosqueGuion.RotLinterna);
            if (lin != null) { lin.name = "Linterna"; lin.clave = "linterna"; }
            Dentro(EdifDir + "Prop_teapot.fbx", dentro, casa, new Vector3(0.91f, 1.01f, 1.63f), 65f, false); // sobre la plancha de la estufa
            // Leña apilada contra la pared, al lado de la estufa; la mochila, apoyada en la pared del fondo, entre la librería y la cama.
            Dentro(EdifDir + "Prop_firewood_01.fbx", dentro, casa, new Vector3(0.30f, SueloCasa, 2.20f), 90f, false);
            Dentro(EdifDir + "Prop_Backpack.fbx", dentro, casa, new Vector3(-3.30f, SueloCasa, 0.08f), 110f, false);
            // Sobre la encimera y la cocina: lo de comer y los cacharros, con modelos del mismo pack.
            const float enc = SueloCasa + 0.92f, gas = SueloCasa + 1.01f;
            Dentro(EdifDir + "Prop_dishes.fbx", dentro, casa, new Vector3(-2.42f, enc, -1.9f), 0f, false);
            Dentro(EdifDir + "Prop_dishes.fbx", dentro, casa, new Vector3(-2.42f, enc + 0.03f, -1.9f), 25f, false);
            Dentro(EdifDir + "Prop_Cup.fbx", dentro, casa, new Vector3(-2.15f, enc, -1.62f), 110f, false);
            Dentro(EdifDir + "Prop_pot.fbx", dentro, casa, new Vector3(-1.95f, enc, -2.05f), 30f, false);
            // Las tres latas, delante de los platos: su sitio de antes es ahora el del televisor.
            Dentro(EdifDir + "Prop_Food.fbx", dentro, casa, new Vector3(-2.47f, enc, -1.60f), 0f, false);
            Dentro(EdifDir + "Prop_Food.fbx", dentro, casa, new Vector3(-2.32f, enc, -1.57f), 40f, false);
            Dentro(EdifDir + "Prop_Food.fbx", dentro, casa, new Vector3(-2.40f, enc + 0.08f, -1.585f), 75f, false);
            Dentro(EdifDir + "Prop_Water_bottle.fbx", dentro, casa, new Vector3(-1.3f, enc, -2.15f), 0f, false);
            Dentro(EdifDir + "Prop_Water_bottle.fbx", dentro, casa, new Vector3(-1.19f, enc, -2.1f), 50f, false);
            Dentro(EdifDir + "Prop_canteen.fbx", dentro, casa, new Vector3(-2.2f, enc, -2.16f), 95f, false);
            Dentro(EdifDir + "Prop_Matches.fbx", dentro, casa, new Vector3(-1.2f, enc, -1.62f), 25f, false);
            Dentro(EdifDir + "Prop_frying_pan.fbx", dentro, casa, new Vector3(-0.66f, gas, -1.9f), 35f, false);
            Dentro(EdifDir + "Prop_tray.fbx", dentro, casa, new Vector3(1.58f, SueloCasa, -2.08f), 0f, false); // el cubo, en el rincón
            Nota(Dentro(EdifDir + "Prop_calendar.fbx", dentro, casa, new Vector3(-3.52f, 1.32f, 0.08f), 0f, false), "mirar el calendario", "Calendario",
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
            // El bidón de gasolina, fuera, junto a la furgoneta (a la vista desde el porche, a 1,5 m de su costado).
            var bidon = Prop(TripoDir + "bidon/bidon.gltf", alrededor, En(CasaPos, quiero, lado, 8.6f, 3.9f), 35f, 1f, true, 0.4f);
            if (bidon != null) { bidon.name = "Garrafa de combustible"; Usable<Recogible>(bidon, BosqueGuion.RotBidon).clave = "garrafa"; }
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
            // La emisora de Josemi cuando exista («emisora», mira a -x: giro 90); mientras tanto, la de Poly Haven.
            bool emPropia = Hay(Tripo("emisora"));
            string em = emPropia ? Tripo("emisora") : PolyDir + "vintage_radio_transceiver/vintage_radio_transceiver_1k.gltf";
            radioTorre = Usable<RadioBosque>(Encima(em, puesto, torre, new Vector3(0.58f, 20.2f, -2.85f), emPropia ? 90f : 0f, "Emisora de la torre"), BosqueGuion.RotEmisoraUsar);
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

            // Generador de la casa del guarda, en el costado de la leñera: da corriente a la casa, al sendero y a la torre.
            // Es el modelo de Josemi (el generador anterior sigue en Tripo/generador por si se quiere volver).
            var gen = Group(root, "Generador");
            Vector2 g = En(CasaPos, quiero, lado, 1.2f, 4.4f);                       // mundo ≈ (12.62, -3.74), a 1,7 m de la pared
            generadorGo = Prop(TripoDir + "generador_daniel/generador_daniel.gltf", gen, g, Yaw(quiero) + 20f, 1.3f, true, 1.2f);
            if (generadorGo == null)
            {   // sin el modelo no se pone otro: queda un punto usable e invisible para que la partida se pueda acabar
                generadorGo = new GameObject("FALTA generador_daniel");
                generadorGo.transform.SetParent(gen, false);
                generadorGo.transform.position = Suelo(g, 0f);
                var caja = generadorGo.AddComponent<BoxCollider>(); caja.center = new Vector3(0f, 0.4f, 0f); caja.size = Vector3.one * 0.8f;
            }
            taken.Add(new Vector3(g.x, g.y, 3f));
            Prop(EdifDir + "Prop_toolbox.fbx", gen, En(CasaPos, quiero, lado, 0.5f, 5.0f), 12f, 1f, false, 0.3f);
            var lGen = Luz("Luz generador", gen, Suelo(g, 0f) + Vector3.up * 1.6f, new Color(1f, 0.75f, 0.45f), 3f, 7f, false);
            var deCasa = lucesGenerador.ToArray();                                   // Luz interior, Luz porche (las dos únicas apuntadas hasta aquí)
            lCabina.gameObject.AddComponent<LuzParpadeo>().cantidad = 0.08f;
            var gb = generadorGo.AddComponent<GeneradorBosque>();
            gb.cristales = torre.GetComponentsInChildren<Renderer>().Concat(casa.GetComponentsInChildren<Renderer>())
                .Where(r => r.sharedMaterials.Any(m => m != null && m.name.StartsWith("Glass"))).ToArray();

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

            // Orden en que vuelve la luz al arrancar el generador: él mismo, la casa, el sendero poste a poste y la torre.
            // Electrica las deja todas apagadas en la escena guardada; al empezar la partida el juego enciende solo las tres
            // de la red del pueblo (interior, porche y cabina).
            lucesGenerador.Clear();
            Electrica(lGen);
            foreach (var l in deCasa) Electrica(l);
            foreach (var l in lucesCamino) Electrica(l);
            Electrica(lPie); Electrica(lCabina);
            gb.luces = lucesGenerador.ToArray();
        }

        /// <summary>
        /// Modelos de Josemi que todavía pueden no existir. Cada uno se coloca solo si está su archivo
        /// (Assets/Models/Retro/Tripo/nombre/nombre.gltf): sin modelo no se pone nada ni se sustituye por otro.
        /// Posiciones provisionales, a ajustar por él. Medidas del muro: la pared de la puerta ocupa x local de 1,62 a 2,06
        /// (hueco de la puerta en z de -0,43 a 0,55, ventanas en z de ±1,13 a ±2,11); la fachada del generador tiene la cara
        /// exterior en z ≈ -2,73. Por eso lo de dentro va en x = 1,58, el aplique en x = 2,10 y el cuadro en z = -2,80.
        /// </summary>
        static void Opcionales(Transform dentro, GameObject casa, Light lInterior, Light lPorche, Vector3 quiero)
        {
            if (Hay(Tripo("diario")))        // sobre el escritorio
                Accion(Encima(Tripo("diario"), dentro, casa, new Vector3(0.62f, 1.6f, -1.68f), 20f, "Diario"), BosqueGuion.RotDiario, "diario");
            if (Hay(Tripo("walkie")))        // sobre el escritorio
                Accion(Encima(Tripo("walkie"), dentro, casa, new Vector3(1.08f, 1.6f, -1.66f), 60f, "Walkie"), BosqueGuion.RotWalkie, "walkie");
            if (Hay(Tripo("mapa")))
            {   // pared de la puerta, por dentro, entre la puerta y la ventana
                var mapa = Dentro(Tripo("mapa"), dentro, casa, new Vector3(1.58f, 1.45f, -0.79f), 0f, false);
                mapa.name = "Mapa";
                Accion(mapa, BosqueGuion.RotMapa, "mapa");
            }
            if (Hay(Tripo("interruptor")))
            {   // pared de la puerta, por dentro, al otro lado
                var interruptor = Dentro(Tripo("interruptor"), dentro, casa, new Vector3(1.58f, 1.25f, 0.84f), 0f, false);
                interruptor.name = "Interruptor";
                Accion(interruptor, BosqueGuion.RotInterruptor, "interruptor");
            }
            if (Hay(Tripo("cuadro_electrico")))
            {   // fachada lateral, por fuera, sobre el generador
                var cuadro = Dentro(Tripo("cuadro_electrico"), dentro, casa, new Vector3(1.2f, 1.35f, -2.80f), -90f, false);
                cuadro.name = "Cuadro electrico";
                Accion(cuadro, BosqueGuion.RotCuadro, "cuadro");
            }
            if (Hay(Tripo("bombilla")))
            {   // colgada en el centro de la habitación: la luz de dentro sale de ella
                var bombilla = Dentro(Tripo("bombilla"), dentro, casa, new Vector3(0f, 2.05f, 0f), 0f, false);
                bombilla.name = "Bombilla";
                if (lInterior != null) lInterior.transform.position = Limites(bombilla).center;
            }
            if (Hay(Tripo("aplique")))
            {   // fachada de la puerta, por fuera: la luz del porche sale de 0,25 m delante de él
                var aplique = Dentro(Tripo("aplique"), dentro, casa, new Vector3(2.10f, 2.0f, 0.84f), 180f, false);
                aplique.name = "Aplique";
                if (lPorche != null) lPorche.transform.position = Limites(aplique).center + quiero * 0.25f;
            }
        }

        static bool Hay(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        static string Tripo(string nombre) => TripoDir + nombre + "/" + nombre + ".gltf";

        /// <summary>Coloca un modelo con su base a la altura local dada (encimera, pared). null si el modelo no existe.</summary>
        static GameObject Apoyado(string path, Transform parent, GameObject edificio, Vector3 local, float yawLocal, string nombre)
        {
            if (!Hay(path)) return null;
            var go = Dentro(path, parent, edificio, local, yawLocal, false);
            go.transform.position += Vector3.up * (edificio.transform.TransformPoint(local).y - Limites(go).min.y);
            if (nombre != null) go.name = nombre;
            return go;
        }

        /// <summary>Hace usable un objeto que solo avisa al director del juego (Accionable) con su clave.</summary>
        static void Accion(GameObject go, string rotulo, string clave)
        {
            var a = Usable<Accionable>(go, rotulo);
            if (a != null) a.clave = clave;
        }

        const float SueloCasa = -0.028f; // altura del suelo de tablas respecto al origen de la cabaña

        /// <summary>Un modelo de Tripo (Assets/Models/Retro/Tripo/nombre/nombre.gltf) dentro de un edificio; el pivote está en el centro de la base.</summary>
        static GameObject Mueble(string nombre, Transform parent, GameObject edificio, Vector3 local, float yawLocal, bool caja)
        {
            return Dentro(TripoDir + nombre + "/" + nombre + ".gltf", parent, edificio, local, yawLocal, caja);
        }

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
            if (prefab == null)
            {   // sin el modelo no se pone otro: queda un punto vacío en su sitio (si es usable, Usable le da una caja de 0,3 m)
                Debug.LogWarning("[Bosque] Falta el modelo " + path);
                var falta = new GameObject("FALTA " + System.IO.Path.GetFileNameWithoutExtension(path));
                falta.transform.SetParent(parent, false);
                falta.transform.position = edificio.transform.TransformPoint(local);
                return falta;
            }
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
