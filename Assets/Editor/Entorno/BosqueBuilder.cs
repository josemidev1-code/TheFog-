using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TheFog.EditorTools.Entorno
{
    /// <summary>
    /// Escena propia del bosque (Assets/Scenes/Bosque.unity), aparte de Valdara.
    /// Todo lo que hay sobre el terreno son modelos 3D del pack "Vegetation" de Elbolilloduro (CC0, estilo PSX):
    /// pinos, árboles de hoja, arbustos, hierba, rocas, troncos caídos, tocones y ramas.
    /// - "Importar kit": crea los materiales (recorte alfa en ramas y hierba) y los asigna a los FBX.
    /// - "Construir escena": terreno propio, sendero con tres claros, bosque con semilla fija (1987),
    ///   y copia de Daniel, la luna, el post-proceso y los sistemas desde TorreDaniel (misma cámara y niebla).
    /// Se puede repetir: rehace la escena entera.
    /// </summary>
    public static partial class BosqueBuilder
    {
        const string KitDir = "Assets/Models/Retro/Bosque/";
        const string ModelDir = KitDir + "Modelos/";
        const string EdifDir = KitDir + "Edificios/";
        const string TexDir = KitDir + "Texturas/";
        const string MatDir = KitDir + "Materiales/";
        const string TerrainDir = "Assets/Art/Retro/Suelo/Terreno";
        const string ScenePath = "Assets/Scenes/Bosque.unity";
        const string TorrePath = "Assets/Scenes/TorreDaniel.unity";

        const float Size = 500f, MaxH = 40f, Below = 8f;
        const int HeightRes = 513, AlphaRes = 1024;
        const float Radio = 200f;      // hasta dónde llega el bosque
        const float Celda = 6.2f;      // separación media entre árboles
        const float Lejos = 130f;      // plano lejano de la cámara (la niebla ya lo tapa todo antes)

        // Trazado: sendero principal de sur a norte y un ramal al oeste. Tres claros.
        static readonly Vector2[][] Senderos =
        {
            new[] { new Vector2(0, -150), new Vector2(-12, -110), new Vector2(10, -70), new Vector2(-5, -30), new Vector2(0, 0),
                    new Vector2(25, 35), new Vector2(62, 62), new Vector2(70, 105), new Vector2(40, 150) },
            new[] { new Vector2(0, 0), new Vector2(-45, 10), new Vector2(-90, -5) },
        };
        static readonly Vector3[] Claros = { new Vector3(3, -3, 17f), new Vector3(-96, -6, 17f), new Vector3(62, 62, 8f) }; // x, z, radio
        static readonly Vector2 Salida = new Vector2(0, -146);

        static Terrain terrain;
        static readonly List<Vector3> taken = new List<Vector3>(); // x, z, radio
        static readonly Dictionary<Mesh, Vector3> troncos = new Dictionary<Mesh, Vector3>(); // centro x, z, radio

        // ---------------------------------------------------------------- Kit

        [MenuItem("THE FOG/Bosque/1. Importar kit de modelos")]
        public static void ImportarKit()
        {
            EnsureFolder(MatDir.TrimEnd('/'));
            var mats = new Dictionary<string, Material>();
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexDir.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                bool alfa = name.StartsWith("Branch") || name.StartsWith("Tree_B") || name.StartsWith("Grass_P");
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                if (ti.alphaIsTransparency != alfa || ti.mipMapsPreserveCoverage != alfa)
                {
                    ti.alphaIsTransparency = alfa;
                    ti.mipMapsPreserveCoverage = alfa;   // que las ramas no adelgacen a lo lejos
                    ti.alphaTestReferenceValue = 0.4f;
                    ti.SaveAndReimport();
                }
                var mp = MatDir + name + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, mp); }
                m.shader = lit;
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
                m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Smoothness", 0f); m.SetFloat("_Metallic", 0f);
                m.SetFloat("_SpecularHighlights", 0f); m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                m.SetFloat("_EnvironmentReflections", 0f); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                if (alfa)
                {
                    m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.4f); m.EnableKeyword("_ALPHATEST_ON");
                    m.SetFloat("_Cull", 0f); // ramas y hierba se ven por las dos caras
                    m.SetOverrideTag("RenderType", "TransparentCutout");
                    m.renderQueue = (int)RenderQueue.AlphaTest;
                }
                EditorUtility.SetDirty(m);
                mats[name] = m;
            }
            // Cristal: el pack no trae textura; vidrio sucio, casi opaco, que deja pasar la luz de dentro.
            foreach (var gname in new[] { "Glass", "Glass.001" })
            {
                var gp = MatDir + gname.Replace('.', '_') + ".mat";
                var g = AssetDatabase.LoadAssetAtPath<Material>(gp);
                if (g == null) { g = new Material(lit); AssetDatabase.CreateAsset(g, gp); }
                g.shader = lit;
                g.SetFloat("_Surface", 1f); g.SetFloat("_Blend", 0f); g.SetOverrideTag("RenderType", "Transparent");
                g.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); g.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                g.SetFloat("_ZWrite", 0f); g.SetFloat("_Cull", 0f);
                g.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); g.renderQueue = (int)RenderQueue.Transparent;
                g.SetColor("_BaseColor", new Color(0.33f, 0.38f, 0.33f, 0.42f));
                g.SetFloat("_Smoothness", 0.25f); g.SetFloat("_Metallic", 0f);
                g.SetFloat("_EnvironmentReflections", 0f); g.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                EditorUtility.SetDirty(g);
                mats[gname] = g;
            }
            AssetDatabase.SaveAssets();

            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ModelDir.TrimEnd('/'), EdifDir.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mi = (ModelImporter)AssetImporter.GetAtPath(path);
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                mi.importAnimation = false; mi.importCameras = false; mi.importLights = false;
                mi.animationType = ModelImporterAnimationType.None;
                foreach (var m in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
                {
                    if (mats.TryGetValue(m.name, out var real) || mats.TryGetValue(m.name.Split('.')[0], out real)) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), real);
                    else Debug.LogWarning("[Bosque] " + path + ": no hay textura para el material " + m.name);
                }
                mi.SaveAndReimport();
                n++;
            }
            Debug.Log("[Bosque] Kit importado: " + mats.Count + " materiales, " + n + " modelos.");
        }

        // ---------------------------------------------------------------- Escena

        [MenuItem("THE FOG/Bosque/2. Construir escena del bosque")]
        public static void Build()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) { Debug.LogError("[Bosque] Guarda antes la escena abierta."); return; }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + "Pino_A.fbx") == null) { Debug.LogError("[Bosque] Falta el kit en " + ModelDir); return; }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CopiarDeTorre(scene);
            SceneManager.SetActiveScene(scene);

            Terreno();
            taken.Clear(); troncos.Clear();
            Random.InitState(1987);
            var root = new GameObject("BOSQUE").transform;
            Asentamiento(new GameObject("ASENTAMIENTO").transform);
            int nArb = Arboles(Group(root, "Arboles"));
            int nRoc = Rocas(Group(root, "Rocas"));
            int nTro = Troncos(Group(root, "Troncos y tocones"));
            int nMat = Matorral(Group(root, "Arbustos"));
            int nHie = Hierba(Group(root, "Hierba"));
            Limites(Group(root, "Limites (invisibles)"));

            var daniel = GameObject.Find("Daniel");
            if (daniel != null)
            {
                var p = Suelo(inicioDaniel, 0f); p.y += 0.7f; // cae al suelo de la casa
                var mira = miraDaniel - inicioDaniel;
                daniel.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, Mathf.Atan2(mira.x, mira.y) * Mathf.Rad2Deg, 0f));
                var cam = daniel.GetComponentInChildren<Camera>();
                if (cam != null) cam.farClipPlane = Lejos;
                var cc = daniel.GetComponent<CharacterController>();
                if (cc != null) { cc.stepOffset = 0.45f; cc.slopeLimit = 50f; } // los peldaños de la torre miden más que un bordillo
                var pixel = daniel.GetComponentInChildren<TheFog.Pixel.PixelArtCamera>();
                if (pixel != null) { pixel.fidelidad = 0.45f; pixel.tramado = 0.25f; }
                var juego = daniel.GetComponent<TheFog.Entorno.BosqueJuego>();
                if (juego == null) juego = daniel.AddComponent<TheFog.Entorno.BosqueJuego>();
                juego.fuenteHud = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Puesto/Fuentes/BarlowCondensed-Medium.ttf");
                juego.fuenteNota = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Puesto/Fuentes/SpecialElite-Regular.ttf");
                juego.fuenteMonitor = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Puesto/Fuentes/VT323-Regular.ttf");
                juego.figuraSendero = figuraSendero; juego.zonaSilueta = zonaSilueta;
                juego.radioCasa = radioCasa; juego.radioTorre = radioTorre;
                juego.generador = generadorGo != null ? generadorGo.transform : null;
                foreach (var ayuda in Object.FindObjectsByType<TheFog.Entorno.AyudaControles>(FindObjectsInactive.Include))
                    ayuda.texto = "WASD  andar      RATÓN  mirar      MAYÚS  correr      E  usar      F  linterna      ESC  soltar el ratón";
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[Bosque] Escena construida: " + nArb + " árboles, " + nRoc + " rocas, " + nTro + " troncos/tocones/ramas, "
                      + nMat + " arbustos, " + nHie + " matas de hierba.");
        }

        /// <summary>Daniel, la luna, el post-proceso, los sistemas y la niebla, iguales que en TorreDaniel.</summary>
        static void CopiarDeTorre(Scene dst)
        {
            var src = EditorSceneManager.OpenScene(TorrePath, OpenSceneMode.Additive);
            foreach (var name in new[] { "AMBIENTE", "Daniel", "SISTEMAS" })
            {
                var go = src.GetRootGameObjects().FirstOrDefault(g => g.name == name);
                if (go == null) { Debug.LogWarning("[Bosque] TorreDaniel no tiene " + name); continue; }
                var copy = Object.Instantiate(go);
                copy.name = name;
                SceneManager.MoveGameObjectToScene(copy, dst);
            }
            SceneManager.SetActiveScene(src);
            bool fog = RenderSettings.fog; var fogMode = RenderSettings.fogMode; var fogColor = RenderSettings.fogColor;
            float fogDensity = RenderSettings.fogDensity, fogStart = RenderSettings.fogStartDistance, fogEnd = RenderSettings.fogEndDistance;
            var ambMode = RenderSettings.ambientMode; var amb = RenderSettings.ambientLight;
            var ambSky = RenderSettings.ambientSkyColor; var ambEq = RenderSettings.ambientEquatorColor; var ambGr = RenderSettings.ambientGroundColor;
            float ambInt = RenderSettings.ambientIntensity, refInt = RenderSettings.reflectionIntensity;
            var sky = RenderSettings.skybox; var shadow = RenderSettings.subtractiveShadowColor;
            SceneManager.SetActiveScene(dst);
            RenderSettings.fog = fog; RenderSettings.fogMode = fogMode; RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity; RenderSettings.fogStartDistance = fogStart; RenderSettings.fogEndDistance = fogEnd;
            RenderSettings.ambientMode = ambMode; RenderSettings.ambientLight = amb;
            RenderSettings.ambientSkyColor = ambSky; RenderSettings.ambientEquatorColor = ambEq; RenderSettings.ambientGroundColor = ambGr;
            RenderSettings.ambientIntensity = ambInt; RenderSettings.reflectionIntensity = refInt;
            RenderSettings.skybox = sky; RenderSettings.subtractiveShadowColor = shadow;
            EditorSceneManager.CloseScene(src, true);
        }

        // ---------------------------------------------------------------- Trazado

        static float PathDistance(Vector2 p)
        {
            float d = float.MaxValue;
            foreach (var s in Senderos)
                for (int i = 0; i < s.Length - 1; i++) d = Mathf.Min(d, EntornoLayout.SegmentDistance(p, s[i], s[i + 1]));
            return d;
        }

        /// <summary>Distancia al borde del claro más cercano (negativa dentro).</summary>
        static float ClaroDistance(Vector2 p)
        {
            float d = float.MaxValue;
            foreach (var c in Claros) d = Mathf.Min(d, Vector2.Distance(p, new Vector2(c.x, c.y)) - c.z);
            return d;
        }

        /// <summary>0 = despejado (sendero, claro, borde), 1 = bosque cerrado.</summary>
        static float Densidad(Vector2 p)
        {
            float d = Smooth(2.4f, 6f, PathDistance(p));
            d *= Smooth(-1f, 5f, ClaroDistance(p));
            d *= 1f - Smooth(Radio - 20f, Radio, p.magnitude);
            float n = Mathf.PerlinNoise(p.x * 0.03f + 71f, p.y * 0.03f + 19f);
            return d * Mathf.Lerp(0.35f, 1f, Smooth(0.25f, 0.5f, n));
        }

        /// <summary>Zonas de árboles muertos: 0 = bosque vivo, 1 = todo seco.</summary>
        static float Muerte(Vector2 p) => Smooth(0.45f, 0.7f, Mathf.PerlinNoise(p.x * 0.017f + 5f, p.y * 0.017f + 133f));

        // ---------------------------------------------------------------- Terreno

        static void Terreno()
        {
            EnsureFolder(TerrainDir);
            var layers = new[]
            {
                Layer("Bosque_Suelo", "Assets/Art/Retro/Suelo/forrest_ground_03/forrest_ground_03", 3.0f),
                Layer("Bosque_Sendero", "Assets/Art/Retro/Suelo/dry_ground_rocks/dry_ground_rocks", 2.5f),
                Layer("Bosque_Hierba", "Assets/Art/Retro/Suelo/grass_path_3/grass_path_3", 3.5f),
            };
            var tdPath = TerrainDir + "/SueloBosque.asset";
            var td = AssetDatabase.LoadAssetAtPath<TerrainData>(tdPath);
            if (td == null) { td = new TerrainData(); AssetDatabase.CreateAsset(td, tdPath); }
            td.heightmapResolution = HeightRes;
            td.alphamapResolution = AlphaRes;
            td.baseMapResolution = 1024;
            td.size = new Vector3(Size, MaxH, Size);
            td.terrainLayers = layers;

            var h = new float[HeightRes, HeightRes];
            for (int zi = 0; zi < HeightRes; zi++)
            for (int xi = 0; xi < HeightRes; xi++)
            {
                float x = -Size / 2f + Size * xi / (HeightRes - 1), z = -Size / 2f + Size * zi / (HeightRes - 1);
                h[zi, xi] = Mathf.Clamp01((Elevation(x, z) + Below) / MaxH);
            }
            td.SetHeights(0, 0, h);

            var a = new float[AlphaRes, AlphaRes, 3];
            for (int zi = 0; zi < AlphaRes; zi++)
            for (int xi = 0; xi < AlphaRes; xi++)
            {
                float x = -Size / 2f + Size * (xi + 0.5f) / AlphaRes, z = -Size / 2f + Size * (zi + 0.5f) / AlphaRes;
                var p = new Vector2(x, z);
                float jitter = (Mathf.PerlinNoise(x * 0.6f, z * 0.6f) - 0.5f) * 0.8f;
                float path = 1f - Smooth(0.5f, 1.3f, PathDistance(p) + jitter);
                foreach (var e in Edificios) path = Mathf.Max(path, 1f - Smooth(e.z - 1f, e.z + 2.5f, Vector2.Distance(p, new Vector2(e.x, e.y)) + jitter * 2f));
                float n = Mathf.PerlinNoise(x * 0.05f + 31f, z * 0.05f + 17f) * 0.8f + Mathf.PerlinNoise(x * 0.25f, z * 0.25f + 5f) * 0.2f;
                float hierba = Mathf.Max(Smooth(0.55f, 0.7f, n) * 0.8f, 1f - Smooth(-2f, 3f, ClaroDistance(p) + jitter * 2f)) * (1f - path);
                a[zi, xi, 0] = Mathf.Max(0f, 1f - path - hierba);
                a[zi, xi, 1] = path;
                a[zi, xi, 2] = hierba;
            }
            td.SetAlphamaps(0, 0, a);
            EditorUtility.SetDirty(td);

            var go = Terrain.CreateTerrainGameObject(td);
            go.name = "TERRENO";
            go.transform.position = new Vector3(-Size / 2f, -Below, -Size / 2f);
            terrain = go.GetComponent<Terrain>();
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/TerrainLit.mat");
            if (mat != null) terrain.materialTemplate = mat;
            terrain.heightmapPixelError = 1f; // a 3 se veían escalones rectos en las explanadas
            terrain.basemapDistance = 200f;
            terrain.drawInstanced = true;
            terrain.shadowCastingMode = ShadowCastingMode.TwoSided;
            AssetDatabase.SaveAssets();
        }

        /// <summary>Altura en metros sobre y = 0: lomas suaves, casi llano en senderos y claros, más alto hacia el borde.</summary>
        static float Elevation(float x, float z)
        {
            float h = ElevacionLibre(x, z);
            // Explanadas: suelo llano bajo la casa y la torre.
            foreach (var e in Edificios)
            {
                float t = 1f - Smooth(e.z + 1.5f, e.z + 9f, Vector2.Distance(new Vector2(x, z), new Vector2(e.x, e.y)));
                if (t > 0f) h = Mathf.Lerp(h, ElevacionLibre(e.x, e.y), t);
            }
            return h;
        }

        static float ElevacionLibre(float x, float z)
        {
            var p = new Vector2(x, z);
            float hills = (Mathf.PerlinNoise(x * 0.012f + 13f, z * 0.012f + 7f) - 0.45f) * 11f
                        + (Mathf.PerlinNoise(x * 0.05f + 3f, z * 0.05f + 91f) - 0.5f) * 1.8f;
            hills += Smooth(150f, 240f, p.magnitude) * Mathf.PerlinNoise(x * 0.02f + 50f, z * 0.02f) * 20f;
            float flat = Mathf.Lerp(0.45f, 1f, Smooth(2f, 14f, PathDistance(p)));
            flat *= Mathf.Lerp(0.25f, 1f, Smooth(0f, 16f, ClaroDistance(p)));
            return hills * flat;
        }

        static TerrainLayer Layer(string name, string texBase, float tile)
        {
            var path = TerrainDir + "/" + name + ".terrainlayer";
            var l = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (l == null) { l = new TerrainLayer(); AssetDatabase.CreateAsset(l, path); }
            l.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texBase + "_Color.jpg");
            l.normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texBase + "_NormalGL.jpg");
            l.tileSize = new Vector2(tile, tile);
            l.smoothness = 0.05f; l.metallic = 0f;
            if (l.diffuseTexture == null) Debug.LogWarning("[Bosque] Falta la textura " + texBase + "_Color.jpg");
            EditorUtility.SetDirty(l);
            return l;
        }

        // ---------------------------------------------------------------- Poblado

        static int Arboles(Transform parent)
        {
            var pinos = Modelos("Pino_A", "Pino_B", "Pino_C", "Pino_D");
            var pinosSecos = Modelos("Pino_A_Seco", "Pino_B_Seco", "Pino_C_Seco", "Pino_D_Seco");
            var hoja = Modelos("Arbol_A", "Arbol_B", "Arbol_C", "Arbol_D", "Arbol_E", "Arbol_F", "Arbol_G");
            var hojaSeca = Modelos("Arbol_A_Seco", "Arbol_B_Seco", "Arbol_C_Seco", "Arbol_D_Seco", "Arbol_E_Seco", "Arbol_F_Seco", "Arbol_G_Seco");
            int n = 0;
            for (float x = -Radio; x <= Radio; x += Celda)
            for (float z = -Radio; z <= Radio; z += Celda)
            {
                var p = new Vector2(x, z) + new Vector2(Random.Range(-0.42f, 0.42f), Random.Range(-0.42f, 0.42f)) * Celda;
                float roll = Random.value, tipo = Random.value, seco = Random.value, s = Random.Range(0.7f, 1.05f), yaw = Random.Range(0f, 360f);
                int which = Random.Range(0, 1000);
                if (roll > Densidad(p) || !Free(p, 2.2f)) continue;
                // Manchas de pinar y manchas de bosque de hoja, con mezcla en los bordes.
                float pinar = Smooth(0.38f, 0.55f, Mathf.PerlinNoise(p.x * 0.011f + 201f, p.y * 0.011f + 47f));
                bool esPino = tipo < Mathf.Lerp(0.15f, 0.92f, pinar);
                bool esSeco = seco < Mathf.Lerp(0.18f, 0.92f, Muerte(p));
                var lista = esPino ? (esSeco ? pinosSecos : pinos) : (esSeco ? hojaSeca : hoja);
                var go = Plantar(lista[which % lista.Count], parent, p, yaw, s, 0.2f, 0f);
                taken.Add(new Vector3(p.x, p.y, 2.2f));
                // Calle del tendido: bajo los cables no hay árboles. Se quita después de plantar para no alterar el resto del bosque.
                if (BajoElTendido(p, CalleTendido)) { Object.DestroyImmediate(go); continue; }
                Tronco(go);
                Lod(go, 0.03f);
                n++;
            }
            return n;
        }

        static int Rocas(Transform parent)
        {
            var grandes = Modelos("Roca_01", "Roca_02", "Roca_03", "Roca_09", "Roca_01_Musgo", "Roca_02_Musgo", "Roca_03_Musgo", "Roca_09_Musgo");
            var todas = new List<GameObject>();
            for (int i = 0; i <= 10; i++) { todas.AddRange(Modelos("Roca_" + i.ToString("00"), "Roca_" + i.ToString("00") + "_Musgo")); }
            int n = 0;
            // Peñas grandes: hitos para orientarse. Unas cuantas rodean el claro del oeste.
            for (int i = 0; i < 7; i++)
            {
                float ang = (i / 7f) * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
                if (Mathf.Abs(Mathf.DeltaAngle(ang * Mathf.Rad2Deg, 0f)) < 35f) continue; // hueco por donde entra el ramal
                var c = new Vector2(Claros[1].x, Claros[1].y) + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (Claros[1].z + Random.Range(1f, 4f));
                n += Roca(grandes[Random.Range(0, grandes.Count)], parent, c, Random.Range(0.45f, 0.8f), 3.5f);
            }
            for (int tries = 0, k = 0; k < 16 && tries < 3000; tries++)
            {
                var p = Random.insideUnitCircle * (Radio - 25f);
                float s = Random.Range(0.5f, 1f);
                var prefab = grandes[Random.Range(0, grandes.Count)];
                if (PathDistance(p) < 9f || ClaroDistance(p) < 6f || !Free(p, 5f)) continue;
                n += Roca(prefab, parent, p, s, 5f); k++;
            }
            // Rocas medianas y pequeñas por todo el bosque, también al borde del sendero.
            for (int tries = 0, k = 0; k < 300 && tries < 20000; tries++)
            {
                var p = Random.insideUnitCircle * (Radio - 10f);
                float s = Random.Range(0.1f, 0.4f);
                var prefab = todas[Random.Range(0, todas.Count)];
                if (PathDistance(p) < 1.8f || ClaroDistance(p) < -4f || !Free(p, 1.2f)) continue;
                n += Roca(prefab, parent, p, s, 1.2f); k++;
            }
            return n;
        }

        static int Roca(GameObject prefab, Transform parent, Vector2 p, float s, float radio)
        {
            var go = Plantar(prefab, parent, p, Random.Range(0f, 360f), s, 0f, 8f);
            var b = Limites(go);
            go.transform.position -= Vector3.up * b.size.y * 0.18f; // medio enterrada
            if (b.size.y > 0.45f) Malla(go);
            Lod(go, b.size.y > 2f ? 0.02f : 0.012f);
            taken.Add(new Vector3(p.x, p.y, radio));
            return 1;
        }

        static int Troncos(Transform parent)
        {
            var troncosCaidos = Modelos("Tronco_A", "Tronco_A_Musgo", "Tronco_B", "Tronco_B_Musgo", "Tronco_C", "Tronco_C_Musgo");
            var tocones = Modelos("Tocon_A", "Tocon_A_Musgo");
            var rama = Modelos("Rama_Caida");
            int n = 0;
            for (int tries = 0, k = 0; k < 110 && tries < 12000; tries++)
            {
                var p = Random.insideUnitCircle * (Radio - 15f);
                float s = Random.Range(0.8f, 1.3f);
                var prefab = troncosCaidos[Random.Range(0, troncosCaidos.Count)];
                if (PathDistance(p) < 3f || ClaroDistance(p) < -3f || !Free(p, 2.4f)) continue;
                var go = Plantar(prefab, parent, p, Random.Range(0f, 360f), s, 0.08f, 5f);
                Malla(go); Lod(go, 0.012f);
                taken.Add(new Vector3(p.x, p.y, 2.4f)); n++; k++;
            }
            for (int tries = 0, k = 0; k < 90 && tries < 12000; tries++)
            {
                var p = Random.insideUnitCircle * (Radio - 15f);
                float s = Random.Range(0.8f, 1.5f);
                var prefab = tocones[Random.Range(0, tocones.Count)];
                if (PathDistance(p) < 2.2f || !Free(p, 1f)) continue;
                var go = Plantar(prefab, parent, p, Random.Range(0f, 360f), s, 0.05f, 4f);
                Malla(go); Lod(go, 0.012f);
                taken.Add(new Vector3(p.x, p.y, 1f)); n++; k++;
            }
            for (int tries = 0, k = 0; k < 90 && tries < 12000; tries++)
            {
                var p = Random.insideUnitCircle * (Radio - 15f);
                float s = Random.Range(0.5f, 1f);
                if (PathDistance(p) < 1.5f || !Free(p, 1.5f)) continue;
                var go = Plantar(rama[0], parent, p, Random.Range(0f, 360f), s, 0.05f, 0f);
                SinSombra(go); Lod(go, 0.015f);
                n++; k++;
            }
            return n;
        }

        static int Matorral(Transform parent)
        {
            var vivos = Modelos("Arbusto_A");
            var secos = Modelos("Arbusto_A_Seco", "Arbusto_B_Seco", "Arbusto_C_Seco");
            int n = 0;
            for (int tries = 0; n < 520 && tries < 30000; tries++)
            {
                var p = Random.insideUnitCircle * (Radio - 8f);
                float s = Random.Range(0.3f, 0.6f), seco = Random.value, yaw = Random.Range(0f, 360f);
                int which = Random.Range(0, 1000);
                if (PathDistance(p) < 2f || ClaroDistance(p) < -2f || !Free(p, 0.7f)) continue;
                bool esSeco = seco < Mathf.Lerp(0.35f, 0.95f, Muerte(p));
                var prefab = esSeco ? secos[which % secos.Count] : vivos[0];
                var go = Plantar(prefab, parent, p, yaw, prefab.name.StartsWith("Arbusto_A") ? s : s * 1.8f, 0.05f, 0f);
                SinSombra(go); Lod(go, 0.02f);
                n++;
            }
            return n;
        }

        static int Hierba(Transform parent)
        {
            var viva = Modelos("Hierba_A", "Hierba_B", "Hierba_C");
            var seca = Modelos("Hierba_A_Seca", "Hierba_B_Seca", "Hierba_C_Seca");
            var manchas = Modelos("Hierba_Mancha", "Hierba_Mancha_Seca");
            int n = 0;
            // Más hierba donde entra luz: junto al sendero y en los claros.
            for (int tries = 0; n < 3000 && tries < 120000; tries++)
            {
                var p = Random.insideUnitCircle * (Radio - 20f);
                float keep = Random.value, s = Random.Range(0.45f, 0.9f), seco = Random.value, yaw = Random.Range(0f, 360f);
                int which = Random.Range(0, 1000);
                float dPath = PathDistance(p), dClaro = ClaroDistance(p);
                if (dPath < 1.1f || EnEdificio(p, 0.5f) || Despejado(p)) continue;
                float luz = Mathf.Max(1f - Smooth(1f, 14f, dPath), 1f - Smooth(-2f, 6f, dClaro));
                if (keep > Mathf.Lerp(0.12f, 1f, luz)) continue;
                var lista = seco < Mathf.Lerp(0.45f, 0.97f, Muerte(p)) ? seca : viva;
                var go = Plantar(lista[which % lista.Count], parent, p, yaw, s, 0.04f, 0f);
                SinSombra(go); Lod(go, 0.025f);
                n++;
            }
            for (int tries = 0, k = 0; k < 160 && tries < 20000; tries++)
            {
                var p = Random.insideUnitCircle * (Radio - 30f);
                float s = Random.Range(0.6f, 1f), seco = Random.value, yaw = Random.Range(0f, 360f);
                if (PathDistance(p) < 4f || ClaroDistance(p) > 30f && PathDistance(p) > 25f || EnEdificio(p, 2f) || Despejado(p)) continue;
                var go = Plantar(manchas[seco < Mathf.Lerp(0.45f, 0.97f, Muerte(p)) ? 1 : 0], parent, p, yaw, s, 0.04f, 0f);
                SinSombra(go); Lod(go, 0.02f);
                n++; k++;
            }
            return n;
        }

        /// <summary>Muro invisible en el borde del bosque: Daniel no se sale del mapa.</summary>
        static void Limites(Transform parent)
        {
            const int N = 32; float r = Radio - 14f, lado = 2f * r * Mathf.Tan(Mathf.PI / N) + 1f;
            for (int i = 0; i < N; i++)
            {
                float ang = i * Mathf.PI * 2f / N;
                var go = new GameObject("Limite");
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(new Vector3(Mathf.Sin(ang) * r, 10f, Mathf.Cos(ang) * r), Quaternion.Euler(0f, ang * Mathf.Rad2Deg, 0f));
                go.AddComponent<BoxCollider>().size = new Vector3(lado, 60f, 1f);
            }
        }

        // ---------------------------------------------------------------- Utilidades

        static List<GameObject> Modelos(params string[] names)
        {
            var list = new List<GameObject>();
            foreach (var n in names)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + n + ".fbx");
                if (go != null) list.Add(go); else Debug.LogWarning("[Bosque] Falta el modelo " + n);
            }
            return list;
        }

        static Vector3 Suelo(Vector2 xz, float hundir)
        {
            var p = new Vector3(xz.x, 0f, xz.y);
            p.y = terrain.SampleHeight(p) + terrain.transform.position.y - hundir;
            return p;
        }

        static GameObject Plantar(GameObject prefab, Transform parent, Vector2 xz, float yaw, float scale, float hundir, float inclinar)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = prefab.name;
            var rot = Quaternion.Euler(Random.Range(-inclinar, inclinar), yaw, Random.Range(-inclinar, inclinar));
            go.transform.SetPositionAndRotation(Suelo(xz, hundir * scale), rot * go.transform.rotation);
            go.transform.localScale = go.transform.localScale * scale;
            return go;
        }

        static Bounds Limites(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>Cápsula en el tronco, medida en la malla: Daniel choca con el árbol pero pasa bajo las ramas.</summary>
        static void Tronco(GameObject go)
        {
            var mf = go.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;
            var mesh = mf.sharedMesh;
            if (!troncos.TryGetValue(mesh, out var t))
            {
                float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
                foreach (var v in mesh.vertices)
                {
                    if (v.y < 0.7f || v.y > 1.8f) continue;
                    minX = Mathf.Min(minX, v.x); maxX = Mathf.Max(maxX, v.x); minZ = Mathf.Min(minZ, v.z); maxZ = Mathf.Max(maxZ, v.z);
                }
                t = minX > maxX ? new Vector3(0f, 0f, 0.35f)
                    : new Vector3((minX + maxX) / 2f, (minZ + maxZ) / 2f, Mathf.Clamp(Mathf.Min(maxX - minX, maxZ - minZ) / 2f, 0.18f, 1.1f));
                troncos[mesh] = t;
            }
            var c = mf.gameObject.AddComponent<CapsuleCollider>();
            c.direction = 1; c.radius = t.z; c.height = 6f; c.center = new Vector3(t.x, 2.6f, t.y);
        }

        static void Malla(GameObject go)
        {
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                if (mf.sharedMesh != null) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        }

        static void SinSombra(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>Deja de dibujarse cuando ocupa menos de "cull" de la altura de pantalla.</summary>
        static void Lod(GameObject go, float cull)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var lg = go.AddComponent<LODGroup>();
            lg.SetLODs(new[] { new LOD(cull, rs) });
            lg.RecalculateBounds();
        }

        static bool Free(Vector2 p, float r)
        {
            if (EnEdificio(p, r)) return false;
            foreach (var t in taken)
            {
                float rr = (r + t.z) * 0.5f;
                if ((p.x - t.x) * (p.x - t.x) + (p.y - t.y) * (p.y - t.y) < rr * rr) return false;
            }
            return true;
        }

        static Transform Group(Transform root, string name)
        {
            var t = new GameObject(name).transform; t.SetParent(root, false); return t;
        }

        static float Smooth(float a, float b, float v) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, v));

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
