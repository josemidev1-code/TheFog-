using System.Collections.Generic;
using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>
    /// Recursos de la interfaz del bosque (uso interno de BosqueInterfaz). Las imágenes están en Resources/UI/ como PNG crudo
    /// con extensión .bytes (TextAsset) y se decodifican aquí con filtro Point, para no depender del importador de texturas.
    /// Nada de aquí lanza una excepción: si falta una pieza se devuelve null y quien dibuja usa un respaldo liso.
    /// </summary>
    public static class UiFog
    {
        static readonly Dictionary<string, Texture2D> texturas = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, GUIStyle> marcos = new Dictionary<string, GUIStyle>();
        static readonly List<Texture2D> creadas = new List<Texture2D>();
        static readonly HashSet<string> avisadas = new HashSet<string>();

        /// <summary>"UI/fog/panel": textura Point/Clamp en caché; null si falta.</summary>
        public static Texture2D Textura(string ruta)
        {
            if (string.IsNullOrEmpty(ruta)) return null;
            Texture2D t;
            if (texturas.TryGetValue(ruta, out t))
            {
                if (t != null || avisadas.Contains(ruta)) return t;
                texturas.Remove(ruta);   // la destruyeron por fuera: se vuelve a cargar
            }
            t = null;
            try
            {
                var datos = Resources.Load<TextAsset>(ruta);
                if (datos != null)
                {
                    t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    t.name = ruta;
                    t.hideFlags = HideFlags.HideAndDontSave;
                    if (t.LoadImage(datos.bytes))
                    {
                        t.filterMode = FilterMode.Point;
                        t.wrapMode = TextureWrapMode.Clamp;
                        creadas.Add(t);
                    }
                    else { Destruir(t); t = null; }
                }
            }
            catch (System.Exception ex)
            {
                if (t != null) Destruir(t);
                t = null;
                Debug.LogWarning("[Bosque] No se pudo leer la imagen de interfaz «" + ruta + "»: " + ex.Message);
            }
            if (t == null && avisadas.Add(ruta))
                Debug.LogWarning("[Bosque] Falta la imagen de interfaz Resources/" + ruta + ".bytes: se dibuja un respaldo liso.");
            texturas[ruta] = t;
            return t;
        }

        /// <summary>
        /// Marco de 9 cortes para piezas de centro liso: amplía la textura ×escala por vecino más próximo y devuelve un GUIStyle
        /// con normal.background = esa textura y los bordes ya multiplicados. Caché por (ruta, escala). null si falta la textura.
        /// </summary>
        public static GUIStyle Marco(string ruta, int izq, int arr, int der, int aba, int escala)
        {
            if (escala < 1) escala = 1;
            string clave = ruta + "@" + escala;
            GUIStyle estilo;
            if (marcos.TryGetValue(clave, out estilo))
            {
                if (estilo == null || estilo.normal.background != null) return estilo;
                marcos.Remove(clave);
            }
            estilo = null;
            var grande = Ampliada(ruta, escala);
            if (grande != null)
            {
                estilo = new GUIStyle();
                estilo.normal.background = grande;
                // OJO: RectOffset es (izquierda, derecha, arriba, abajo).
                estilo.border = new RectOffset(izq * escala, der * escala, arr * escala, aba * escala);
                estilo.padding = new RectOffset(0, 0, 0, 0);
                estilo.margin = new RectOffset(0, 0, 0, 0);
            }
            marcos[clave] = estilo;
            return estilo;
        }

        /// <summary>Copia de la textura ampliada ×escala por vecino más próximo (Point). null si falta.</summary>
        static Texture2D Ampliada(string ruta, int escala)
        {
            var t = Textura(ruta);
            if (t == null) return null;
            if (escala == 1) return t;
            try
            {
                int w = t.width, h = t.height;
                Color32[] o = t.GetPixels32();
                var d = new Color32[w * escala * h * escala];
                int W = w * escala;
                for (int y = 0; y < h * escala; y++)
                {
                    int fila = (y / escala) * w;
                    for (int x = 0; x < W; x++) d[y * W + x] = o[fila + x / escala];
                }
                return Nueva(W, h * escala, d, TextureWrapMode.Clamp);
            }
            catch (System.Exception ex)
            {
                if (avisadas.Add(ruta + "@")) Debug.LogWarning("[Bosque] No se pudo ampliar «" + ruta + "»: " + ex.Message);
                return null;
            }
        }

        /// <summary>Textura creada por código (sRGB, Point), que Vaciar() también destruye.</summary>
        public static Texture2D Nueva(int ancho, int alto, Color32[] pixeles, TextureWrapMode envoltura)
        {
            var t = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
            t.hideFlags = HideFlags.HideAndDontSave;
            t.filterMode = FilterMode.Point;
            t.wrapMode = envoltura;
            t.SetPixels32(pixeles);
            t.Apply(false, false);
            creadas.Add(t);
            return t;
        }

        /// <summary>preferida, o Resources/Fuentes/nombre; puede ser null (entonces se usa la de GUI.skin).</summary>
        public static Font Fuente(Font preferida, string nombre)
        {
            if (preferida != null) return preferida;
            Font f = null;
            try { f = Resources.Load<Font>("Fuentes/" + nombre); }
            catch (System.Exception) { f = null; }
            if (f == null && avisadas.Add("Fuentes/" + nombre))
                Debug.LogWarning("[Bosque] Falta la fuente Resources/Fuentes/" + nombre + ": se usa la de serie.");
            return f;
        }

        /// <summary>Destruye lo creado (OnDestroy y al cambiar la resolución). Lo que haga falta se vuelve a cargar solo.</summary>
        public static void Vaciar()
        {
            for (int i = 0; i < creadas.Count; i++) if (creadas[i] != null) Destruir(creadas[i]);
            creadas.Clear();
            texturas.Clear();
            marcos.Clear();
        }

        static void Destruir(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }
    }
}
