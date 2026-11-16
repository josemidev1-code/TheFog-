using UnityEditor;
using UnityEngine;

namespace TheFog.EditorTools.Retro
{
    /// <summary>
    /// Reduce el tamaño y endurece el filtro de las texturas de las carpetas retro al importarlas.
    /// No modifica los archivos de imagen, solo sus ajustes de importación.
    /// </summary>
    public class RetroTexturePostprocessor : AssetPostprocessor
    {
        static RetroTextureSettings cached;

        static RetroTextureSettings Settings
        {
            get
            {
                if (cached == null) cached = AssetDatabase.LoadAssetAtPath<RetroTextureSettings>(RetroTextureSettings.AssetPath);
                return cached;
            }
        }

        void OnPreprocessTexture()
        {
            var s = Settings;
            if (s == null || !s.Applies(assetPath)) return;

            var ti = (TextureImporter)assetImporter;
            // Los relieves horneados de Tripo los lee el shader glTF: tienen que ser textura lineal normal,
            // no del tipo "Normal Map" de Unity (que los comprime en otro formato y salen manchas negras).
            if (s.IsAtlas(assetPath) && assetPath.IndexOf("_Normal", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = false;
            }
            bool isData = ti.textureType == TextureImporterType.NormalMap || !ti.sRGBTexture;
            int size = s.MaxSizeFor(assetPath, isData);

            ti.maxTextureSize = size;
            ti.filterMode = s.filter;
            ti.mipmapEnabled = s.mipmaps;
            ti.anisoLevel = 0;
        }

        [MenuItem("THE FOG/Retro/Reimportar texturas retro")]
        public static void Reimport()
        {
            cached = null;
            var s = Settings;
            var folders = s != null ? s.folders : new[] { "Assets/Models/Retro/" };
            var valid = System.Array.FindAll(folders, f => AssetDatabase.IsValidFolder(f.TrimEnd('/')));
            for (int i = 0; i < valid.Length; i++) valid[i] = valid[i].TrimEnd('/');
            if (valid.Length == 0) return;
            int n = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", valid))
                {
                    var p = AssetDatabase.GUIDToAssetPath(guid);
                    var ti = AssetImporter.GetAtPath(p) as TextureImporter;
                    if (ti == null) continue;
                    if (s == null || !s.enabled)
                    {
                        // Vuelve a los valores por defecto de Unity.
                        ti.maxTextureSize = 2048; ti.filterMode = FilterMode.Bilinear; ti.anisoLevel = 1;
                    }
                    ti.SaveAndReimport(); n++;
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            Debug.Log($"[Retro] Reimportadas {n} texturas ({(s != null && s.enabled ? "retro" : "originales")}).");
        }

        [MenuItem("THE FOG/Retro/Seleccionar ajustes")]
        static void Select()
        {
            var s = Settings;
            if (s == null)
            {
                s = ScriptableObject.CreateInstance<RetroTextureSettings>();
                AssetDatabase.CreateAsset(s, RetroTextureSettings.AssetPath);
                cached = s;
            }
            Selection.activeObject = s;
        }
    }
}
