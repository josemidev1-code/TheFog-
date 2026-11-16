using UnityEngine;
using UnityEngine.UI;

namespace TheFog.Pixel
{
    /// <summary>
    /// Aspecto de THE FOG (en la línea de MOLDRISE): el mundo 3D se renderiza en tiempo real a muy baja
    /// resolución, se reduce a la paleta común de 32 colores con tramado y se escala a pantalla sin suavizar.
    /// Va en la cámara del jugador. Desactivar el componente = imagen normal.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class PixelArtCamera : MonoBehaviour
    {
        [Tooltip("Altura interna en píxeles. 180 = 320 × 180 en 16:9.")]
        [Range(120, 360)] public int altura = 180;
        [Tooltip("Paleta común (tira de colores, Read/Write activado). Assets/Art/Pixel/paleta_thefog.png")]
        public Texture2D paleta;
        public Shader shader;
        [Range(0f, 1f)] public float tramado = 0.35f;
        [Range(0.5f, 2f)] public float brillo = 1.05f;
        [Range(0.5f, 2f)] public float contraste = 1.05f;
        [Range(0f, 1f)] public float vineta = 0.35f;

        Camera cam;
        RenderTexture rt;
        Texture2D lut;
        Material mat;
        Canvas canvas;
        RawImage image;
        int lastW, lastH;

        public RenderTexture Imagen => rt;

        void OnEnable()
        {
            cam = GetComponent<Camera>();
            if (shader == null) shader = Shader.Find("TheFog/PixelPaleta");
            mat = new Material(shader) { hideFlags = HideFlags.DontSave };
            lut = BuildLut(paleta);
            mat.SetTexture("_Lut", lut);

            var go = new GameObject("PixelArt_Pantalla") { hideFlags = HideFlags.DontSave };
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100; // por debajo del resto de la interfaz
            image = new GameObject("Imagen").AddComponent<RawImage>();
            image.transform.SetParent(go.transform, false);
            var r = image.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            image.material = mat;
            image.raycastTarget = false;
            Resize();
        }

        void OnDisable()
        {
            if (cam != null) cam.targetTexture = null;
            if (rt != null) { rt.Release(); Destroy(rt); rt = null; }
            if (canvas != null) Destroy(canvas.gameObject);
            if (mat != null) Destroy(mat);
            if (lut != null) Destroy(lut);
            lastW = lastH = 0;
        }

        void Resize()
        {
            int h = altura;
            int w = Mathf.Max(16, Mathf.RoundToInt(h * (float)Screen.width / Mathf.Max(1, Screen.height)));
            if (w == lastW && h == lastH && rt != null) return;
            if (rt != null) { cam.targetTexture = null; rt.Release(); Destroy(rt); }
            rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point, name = "PixelArt_" + w + "x" + h };
            rt.Create();
            cam.targetTexture = rt;
            image.texture = rt;
            lastW = w; lastH = h;
        }

        void LateUpdate()
        {
            Resize();
            mat.SetFloat("_Tramado", tramado);
            mat.SetFloat("_Brillo", brillo);
            mat.SetFloat("_Contraste", contraste);
            mat.SetFloat("_Vineta", vineta);
        }

        /// <summary>Tabla 32×32×32 (aplanada a 1024×32): para cada color, el más cercano de la paleta.</summary>
        static Texture2D BuildLut(Texture2D paleta)
        {
            Color32[] pal;
            if (paleta != null && paleta.isReadable) pal = paleta.GetPixels32();
            else
            {
                Debug.LogWarning("[PixelArt] Paleta no legible; se usa escala de grises. Activa Read/Write en la textura de la paleta.");
                pal = new Color32[16];
                for (int i = 0; i < 16; i++) { byte v = (byte)(i * 17); pal[i] = new Color32(v, v, v, 255); }
            }
            var lut = new Texture2D(1024, 32, TextureFormat.RGB24, false, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var px = new Color32[1024 * 32];
            for (int b = 0; b < 32; b++)
            for (int g = 0; g < 32; g++)
            for (int r = 0; r < 32; r++)
            {
                float fr = r * 255f / 31f, fg = g * 255f / 31f, fb = b * 255f / 31f;
                float best = float.MaxValue; Color32 bc = pal[0];
                foreach (var p in pal)
                {
                    float dr = fr - p.r, dg = fg - p.g, db = fb - p.b;
                    float d = dr * dr * 0.3f + dg * dg * 0.59f + db * db * 0.11f;
                    if (d < best) { best = d; bc = p; }
                }
                px[g * 1024 + b * 32 + r] = bc;
            }
            lut.SetPixels32(px);
            lut.Apply(false, false);
            return lut;
        }
    }
}
