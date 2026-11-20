using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>
    /// El hueco del telediario: si existe Assets/StreamingAssets/Video/noticia_valdara.mp4 se reproduce en la tele.
    /// Todo va por reflexión (el módulo de vídeo puede no estar en el proyecto) y dentro de try/catch:
    /// sin archivo, sin módulo o con cualquier fallo, la tele sigue con rótulos, sonido y subtítulos.
    /// </summary>
    public static class VideoHueco
    {
        public const string Archivo = "Video/noticia_valdara.mp4";      // dentro de Assets/StreamingAssets/

        static bool avisado;

        public static string Ruta
        {
            get
            {
                try { return System.IO.Path.Combine(Application.streamingAssetsPath, Archivo); }
                catch (System.Exception) { return Archivo; }
            }
        }

        public static bool Existe
        {
            get
            {
                try { return System.IO.File.Exists(Ruta); }
                catch (System.Exception) { return false; }
            }
        }

        static System.Type Tipo()
        {
            try { return System.Type.GetType("UnityEngine.Video.VideoPlayer, UnityEngine.VideoModule"); }
            catch (System.Exception) { return null; }
        }

        static void Aviso(string texto)
        {
            if (avisado) return;
            avisado = true;
            Debug.LogWarning("[Bosque] Vídeo de la tele: " + texto);
        }

        static bool Poner(System.Type t, Component vp, string nombre, object valor)
        {
            try
            {
                var p = t.GetProperty(nombre);
                if (p == null || !p.CanWrite) return false;
                p.SetValue(vp, valor, null);
                return true;
            }
            catch (System.Exception) { return false; }
        }

        static bool PonerEnum(System.Type t, Component vp, string nombre, string valor)
        {
            try
            {
                var p = t.GetProperty(nombre);
                if (p == null || !p.CanWrite) return false;
                p.SetValue(vp, System.Enum.Parse(p.PropertyType, valor), null);
                return true;
            }
            catch (System.Exception) { return false; }
        }

        /// <summary>Devuelve el VideoPlayer configurado (sin preparar ni reproducir) o null si no hay archivo, módulo de vídeo o algo falla.</summary>
        public static Component Crear(GameObject anfitrion, RenderTexture destino, AudioSource altavoz)
        {
            try
            {
                if (anfitrion == null || !Existe) return null;
                var t = Tipo();
                if (t == null) { Aviso("el módulo de vídeo no está en el proyecto; la tele sigue con rótulos."); return null; }
                var vp = anfitrion.AddComponent(t);
                if (vp == null) return null;
                Poner(t, vp, "playOnAwake", false);
                Poner(t, vp, "isLooping", false);
                bool bien = PonerEnum(t, vp, "source", "Url") & Poner(t, vp, "url", Ruta)
                          & PonerEnum(t, vp, "renderMode", "RenderTexture") & Poner(t, vp, "targetTexture", destino);
                if (!bien)
                {
                    Aviso("no se ha podido configurar el reproductor; la tele sigue con rótulos.");
                    Object.Destroy(vp);
                    return null;
                }
                // Sonido con posición: sale del altavoz 3D de la tele. Si falla, queda el audio directo (2D).
                bool conAltavoz = false;
                if (altavoz != null)
                {
                    try
                    {
                        if (PonerEnum(t, vp, "audioOutputMode", "AudioSource"))
                        {
                            var m = t.GetMethod("SetTargetAudioSource");
                            if (m != null) { m.Invoke(vp, new object[] { (ushort)0, altavoz }); conAltavoz = true; }
                        }
                    }
                    catch (System.Exception) { conAltavoz = false; }
                }
                if (!conAltavoz)
                {
                    PonerEnum(t, vp, "audioOutputMode", "Direct");
                    Aviso("el sonido no se ha podido llevar al altavoz de la tele; sonará sin posición.");
                }
                return vp;
            }
            catch (System.Exception ex)
            {
                Aviso(ex.Message);
                return null;
            }
        }

        static void Llamar(Component video, string metodo)
        {
            try
            {
                if (video == null) return;
                var m = video.GetType().GetMethod(metodo, System.Type.EmptyTypes);
                if (m != null) m.Invoke(video, null);
            }
            catch (System.Exception ex) { Aviso(ex.Message); }
        }

        static bool Leer(Component video, string propiedad)
        {
            try
            {
                if (video == null) return false;
                var p = video.GetType().GetProperty(propiedad);
                return p != null && (bool)p.GetValue(video, null);
            }
            catch (System.Exception) { return false; }
        }

        /// <summary>VideoPlayer.Prepare(); admite null.</summary>
        public static void Preparar(Component video) { Llamar(video, "Prepare"); }

        /// <summary>isPrepared; false con null o ante cualquier fallo.</summary>
        public static bool Preparado(Component video) { return Leer(video, "isPrepared"); }

        /// <summary>Admite null.</summary>
        public static void Reproducir(Component video) { Llamar(video, "Play"); }

        /// <summary>isPlaying; false con null o ante cualquier fallo.</summary>
        public static bool Reproduciendo(Component video) { return Leer(video, "isPlaying"); }

        /// <summary>Admite null.</summary>
        public static void Parar(Component video) { Llamar(video, "Stop"); }
    }
}
