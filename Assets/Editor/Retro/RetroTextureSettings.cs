using UnityEngine;

namespace TheFog.EditorTools.Retro
{
    /// <summary>
    /// Ajustes del downscaling de texturas retro. Solo afecta a las carpetas listadas.
    /// Los archivos originales no se tocan: cambia la importación. Para volver a la calidad
    /// original, desactiva "enabled" y usa THE FOG → Retro → Reimportar texturas retro.
    /// </summary>
    [CreateAssetMenu(menuName = "THE FOG/Retro/Ajustes de texturas retro", fileName = "RetroTextureSettings")]
    public class RetroTextureSettings : ScriptableObject
    {
        public const string AssetPath = "Assets/Editor/Retro/RetroTextureSettings.asset";

        [Tooltip("Desactivar devuelve las texturas a su tamaño y filtro originales al reimportar.")]
        public bool enabled = true;

        [Tooltip("Carpetas (y subcarpetas) a las que se aplica.")]
        public string[] folders = { "Assets/Models/Retro/" };

        [Header("Escenario y atrezo")]
        [Tooltip("Tamaño máximo de color (albedo). 256 da el aspecto retro sin perder la forma.")]
        public int maxColorSize = 256;
        [Tooltip("Normales y rugosidad pueden ir más bajas: casi no se notan.")]
        public int maxDataSize = 128;

        [Header("Detalle que debe leerse (caras, interactivos, pistas)")]
        [Tooltip("Si la ruta contiene alguna de estas palabras, usa maxDetailSize.")]
        public string[] detailKeywords = { "_Detalle", "/Personajes/", "/Interactivos/", "/Pistas/" };
        public int maxDetailSize = 512;

        [Header("Atlas de modelos generados (Tripo)")]
        [Tooltip("Tripo mete todo el modelo en una sola textura; a 256 px se pierde 16 veces más detalle que en un prop normal.")]
        public string[] atlasKeywords = { "/Tripo/" };
        public int maxAtlasSize = 512;
        [Tooltip("Modelos grandes (torre, casa): si el archivo contiene alguna de estas palabras, usa maxBigAtlasSize.")]
        public string[] bigAtlasKeywords = { "_Grande" };
        public int maxBigAtlasSize = 1024;

        [Header("Filtro")]
        [Tooltip("Point = píxeles duros (estilo PS1). Bilinear = más suave.")]
        public FilterMode filter = FilterMode.Point;
        public bool mipmaps = true;

        public bool Applies(string path)
        {
            if (!enabled || string.IsNullOrEmpty(path)) return false;
            foreach (var f in folders)
                if (!string.IsNullOrEmpty(f) && path.StartsWith(f, System.StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public bool IsDetail(string path) => Contains(path, detailKeywords);
        public bool IsAtlas(string path) => Contains(path, atlasKeywords);

        /// <summary>Tamaño máximo para una textura según su ruta y si es de datos (normales, rugosidad).</summary>
        public int MaxSizeFor(string path, bool isData)
        {
            if (Contains(path, atlasKeywords))
                return Contains(path, bigAtlasKeywords) ? maxBigAtlasSize : maxAtlasSize;
            if (IsDetail(path)) return maxDetailSize;
            return isData ? maxDataSize : maxColorSize;
        }

        static bool Contains(string path, string[] keywords)
        {
            if (keywords == null) return false;
            foreach (var k in keywords)
                if (!string.IsNullOrEmpty(k) && path.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}
