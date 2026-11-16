using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TheFog.EditorTools.Entorno
{
    /// <summary>
    /// Construye el suelo de Valdara (torre, casa de Daniel, monte y pueblo) como Terrain de Unity con cuatro capas CC0:
    /// tierra seca con piedras, sendero de piedra, manchas de tierra con paja y empedrado de calles y plaza.
    /// Llano en la torre, la casa y todo el pueblo (a y = 0); lomas suaves fuera y más altas hacia el borde.
    /// Trazado en EntornoLayout. Se puede repetir: rehace el objeto "TERRENO" y sus assets.
    /// </summary>
    public static class SueloBuilder
    {
        const string Dir = "Assets/Art/Retro/Suelo/Terreno";
        const float Size = 600f;     // metros de lado
        const float MaxH = 30f;      // altura máxima del heightmap
        const float Below = 6f;      // cuánto puede bajar el terreno por debajo de y = 0
        const int HeightRes = 513;
        const int AlphaRes = 1024;

        static Vector2 Torre => EntornoLayout.Torre;
        static Vector2 Casa => EntornoLayout.Casa;

        [MenuItem("THE FOG/Entorno/Construir suelo (escena abierta)")]
        public static void Build()
        {
            EnsureFolder(Dir);
            var layers = new[]
            {
                Layer("Base_TierraPiedras", "Assets/Art/Retro/Suelo/dry_ground_rocks/dry_ground_rocks", 3.0f),
                Layer("Sendero_Piedra", "Assets/Art/Retro/Suelo/rocky_trail/rocky_trail", 2.5f),
                Layer("Manchas_TierraPaja", "Assets/Art/Retro/Suelo/Ground072/Ground072_1K-JPG", 4.0f),
                Layer("Calle_Empedrado", "Assets/Art/Retro/Materiales/PavingStones046/PavingStones046_1K-JPG", 2.5f),
            };

            var tdPath = Dir + "/SueloDaniel.asset";
            var td = AssetDatabase.LoadAssetAtPath<TerrainData>(tdPath);
            if (td == null) { td = new TerrainData(); AssetDatabase.CreateAsset(td, tdPath); }
            td.heightmapResolution = HeightRes;
            td.alphamapResolution = AlphaRes;
            td.baseMapResolution = 1024;
            td.size = new Vector3(Size, MaxH, Size);
            td.terrainLayers = layers;
            td.SetHeights(0, 0, Heights());
            td.SetAlphamaps(0, 0, Alphas());
            EditorUtility.SetDirty(td);

            var old = GameObject.Find("TERRENO");
            if (old != null) Object.DestroyImmediate(old);
            var go = Terrain.CreateTerrainGameObject(td);
            go.name = "TERRENO";
            go.transform.position = new Vector3(-Size / 2f, -Below, -Size / 2f);
            var t = go.GetComponent<Terrain>();
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/TerrainLit.mat");
            if (mat != null) t.materialTemplate = mat;
            t.heightmapPixelError = 3f;
            t.basemapDistance = 250f;   // a menos distancia, la base pierde el detalle de la textura
            t.drawInstanced = true;
            t.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(go.scene);
            Debug.Log("[Suelo] Terreno de " + Size + " m construido con " + layers.Length + " capas.");
        }

        static float[,] Heights()
        {
            var h = new float[HeightRes, HeightRes];
            for (int zi = 0; zi < HeightRes; zi++)
            for (int xi = 0; xi < HeightRes; xi++)
            {
                float x = -Size / 2f + Size * xi / (HeightRes - 1);
                float z = -Size / 2f + Size * zi / (HeightRes - 1);
                float y = Elevation(x, z);
                h[zi, xi] = Mathf.Clamp01((y + Below) / MaxH);
            }
            return h;
        }

        /// <summary>Altura en metros sobre y = 0.</summary>
        static float Elevation(float x, float z)
        {
            var p = new Vector2(x, z);
            // Ondulación suave del monte.
            float hills = (Mathf.PerlinNoise(x * 0.011f + 13f, z * 0.011f + 7f) - 0.45f) * 7f
                        + (Mathf.PerlinNoise(x * 0.045f + 3f, z * 0.045f + 91f) - 0.5f) * 1.4f;
            // Lomas más altas hacia el borde, para cerrar el horizonte en la niebla.
            // (medido desde el punto medio entre la torre y el pueblo)
            float r = (p - new Vector2(0f, -75f)).magnitude;
            hills += Smooth(150f, 280f, r) * Mathf.PerlinNoise(x * 0.02f + 50f, z * 0.02f) * 18f;
            // Llano donde están la torre, la casa y el pueblo.
            float dKey = Mathf.Min(Vector2.Distance(p, Torre) - 4f, Vector2.Distance(p, Casa) - 6f);
            float flat = Smooth(10f, 38f, dKey);
            flat *= Smooth(0f, 28f, EntornoLayout.PuebloDistance(p));
            // El camino al pueblo va casi a nivel.
            float dPath = PathDistance(p);
            flat *= Mathf.Lerp(0.35f, 1f, Smooth(2f, 12f, dPath));
            return hills * flat;
        }

        static float[,,] Alphas()
        {
            var a = new float[AlphaRes, AlphaRes, 4];
            for (int zi = 0; zi < AlphaRes; zi++)
            for (int xi = 0; xi < AlphaRes; xi++)
            {
                float x = -Size / 2f + Size * (xi + 0.5f) / AlphaRes;
                float z = -Size / 2f + Size * (zi + 0.5f) / AlphaRes;
                var p = new Vector2(x, z);
                // Borde del sendero irregular.
                float jitter = (Mathf.PerlinNoise(x * 0.6f, z * 0.6f) - 0.5f) * 0.7f;
                float path = 1f - Smooth(0.55f, 1.25f, PathDistance(p) + jitter);
                float patchN = Mathf.PerlinNoise(x * 0.06f + 31f, z * 0.06f + 17f) * 0.8f + Mathf.PerlinNoise(x * 0.25f, z * 0.25f + 5f) * 0.2f;
                float patch = Smooth(0.52f, 0.66f, patchN) * (1f - path);
                // Tierra pisada con paja alrededor de la casa.
                patch = Mathf.Max(patch, (1f - Smooth(5f, 11f, Vector2.Distance(p, Casa))) * 0.7f * (1f - path));
                // Empedrado: calles del pueblo y plaza, con borde algo irregular.
                float calle = 1f - Smooth(EntornoLayout.CalleMedia - 0.2f, EntornoLayout.CalleMedia + 0.5f, Mathf.Min(EntornoLayout.CalleDistance(p), EntornoLayout.PlazaDistance(p) + EntornoLayout.CalleMedia - 0.4f) + jitter * 0.6f);
                path *= 1f - calle; patch *= 1f - calle;
                float baseW = Mathf.Max(0f, 1f - path - patch - calle);
                a[zi, xi, 0] = baseW;
                a[zi, xi, 1] = path;
                a[zi, xi, 2] = patch;
                a[zi, xi, 3] = calle;
            }
            return a;
        }

        static float PathDistance(Vector2 p) => EntornoLayout.SenderoDistance(p);

        static float Smooth(float a, float b, float v) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, v));

        static TerrainLayer Layer(string name, string texBase, float tile)
        {
            var path = Dir + "/" + name + ".terrainlayer";
            var l = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (l == null) { l = new TerrainLayer(); AssetDatabase.CreateAsset(l, path); }
            l.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texBase + "_Color.jpg");
            l.normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texBase + "_NormalGL.jpg");
            l.normalScale = 1f;
            l.tileSize = new Vector2(tile, tile);
            l.smoothness = 0.05f;
            l.metallic = 0f;
            if (l.diffuseTexture == null) Debug.LogWarning("[Suelo] Falta la textura " + texBase + "_Color.jpg");
            EditorUtility.SetDirty(l);
            return l;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
