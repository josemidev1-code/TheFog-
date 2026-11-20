using UnityEditor;
using UnityEngine;

namespace TheFog.EditorTools.Entorno
{
    /// <summary>
    /// Ajustes de importación del sonido del bosque (Assets/Resources/Audio/). Solo afina memoria y costuras:
    /// la música va en streaming, los bucles en PCM (para que la costura sea exacta) y el resto en ADPCM.
    /// Si faltara, el juego suena igual con los ajustes por defecto de Unity.
    /// La interfaz no necesita nada parecido: sus imágenes van como .bytes y se decodifican en ejecución.
    /// </summary>
    class RecursosFogPostprocessor : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Resources/Audio/")) return;
            var ai = (AudioImporter)assetImporter;
            ai.forceToMono = false;
            var s = ai.defaultSampleSettings;
            if (assetPath.Contains("/Musica/"))
            {
                s.loadType = AudioClipLoadType.Streaming;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.6f;
            }
            else if (System.IO.Path.GetFileNameWithoutExtension(assetPath).Contains("_bucle"))
            {
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.PCM;
            }
            else
            {
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.ADPCM;
            }
            ai.defaultSampleSettings = s;
        }
    }
}
