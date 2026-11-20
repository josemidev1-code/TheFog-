using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TheFog.EditorTools.Entorno
{
    /// <summary>
    /// Comprobaciones del bosque sin abrir el editor (Unity en modo batch, con -executeMethod):
    ///   Informe  — lista las piezas de un objeto de la escena con su centro y tamaño en coordenadas locales de ese objeto
    ///              (las mismas que usan Dentro/Encima/Recolocar en BosqueAsentamiento).
    ///   Capturas — saca fotos con una luz de trabajo desde los puntos que se le pidan.
    /// Variables de entorno:
    ///   BOSQUE_REF     objeto de referencia (por defecto «CASA DE DANIEL»).
    ///   BOSQUE_SALIDA  archivo de texto del informe o carpeta de las capturas.
    ///   BOSQUE_VISTAS  «nombre;x,y,z;x,y,z[;fov]» separadas por «|»: cámara y punto al que mira, en local de la referencia.
    ///   BOSQUE_LUZ     «x,y,z;intensidad;alcance» de la luz de trabajo, en local de la referencia (opcional).
    /// </summary>
    public static class BosqueInforme
    {
        const string ScenePath = "Assets/Scenes/Bosque.unity";
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        static string Env(string k, string def) { var v = System.Environment.GetEnvironmentVariable(k); return string.IsNullOrEmpty(v) ? def : v; }

        static Transform Referencia()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            string nombre = Env("BOSQUE_REF", "CASA DE DANIEL");
            var go = Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t => t.name == nombre && t.gameObject.scene.IsValid());
            if (go == null) throw new System.Exception("[Bosque] No encuentro " + nombre);
            return go;
        }

        public static void Informe()
        {
            var r = Referencia();
            var sb = new StringBuilder();
            sb.AppendLine("# " + r.name + "  pos " + V(r.position) + "  yaw " + r.eulerAngles.y.ToString("0.0", Inv));
            sb.AppendLine("# ruta | centro local | tamano local");
            foreach (var rend in r.GetComponentsInChildren<Renderer>(true))
            {
                var b = rend.bounds; Vector3 min = Vector3.one * 1e9f, max = Vector3.one * -1e9f;
                for (int i = 0; i < 8; i++)
                {
                    var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    c = r.InverseTransformPoint(c); min = Vector3.Min(min, c); max = Vector3.Max(max, c);
                }
                string ruta = rend.name;
                for (var t = rend.transform.parent; t != null && t != r; t = t.parent) ruta = t.name + "/" + ruta;
                var mats = string.Join(",", rend.sharedMaterials.Where(m => m != null).Select(m => m.name));
                sb.AppendLine(ruta + " | " + V((min + max) / 2f) + " | " + V(max - min) + " | " + mats);
            }
            foreach (var l in r.GetComponentsInChildren<Light>(true))
                sb.AppendLine("LUZ " + l.name + " | " + V(r.InverseTransformPoint(l.transform.position)));
            File.WriteAllText(Env("BOSQUE_SALIDA", "Temp/bosque_informe.txt"), sb.ToString());
        }

        public static void Capturas()
        {
            var r = Referencia();
            string dir = Env("BOSQUE_SALIDA", "docs/capturas");
            Directory.CreateDirectory(dir);
            GameObject luz = null;
            string sl = Env("BOSQUE_LUZ", "");
            if (sl != "")
            {
                var p = sl.Split(';');
                luz = new GameObject("Luz de trabajo");
                luz.transform.position = r.TransformPoint(P(p[0]));
                var l = luz.AddComponent<Light>();
                l.type = LightType.Point; l.color = new Color(1f, 0.95f, 0.88f); l.shadows = LightShadows.None;
                l.intensity = p.Length > 1 ? float.Parse(p[1], Inv) : 6f;
                l.range = p.Length > 2 ? float.Parse(p[2], Inv) : 12f;
            }
            var camGo = new GameObject("Camara de comprobacion");
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = RenderSettings.fogColor;
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foreach (var vista in Env("BOSQUE_VISTAS", "").Split('|').Where(v => v.Trim() != ""))
            {
                var p = vista.Split(';');
                Vector3 pos = r.TransformPoint(P(p[1])), mira = r.TransformPoint(P(p[2]));
                camGo.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(mira - pos));
                cam.fieldOfView = p.Length > 3 ? float.Parse(p[3], Inv) : 70f;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(dir, p[0].Trim() + ".png"), tex.EncodeToPNG());
            }
            cam.targetTexture = null;
            Object.DestroyImmediate(camGo); Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
            if (luz != null) Object.DestroyImmediate(luz);
        }

        static Vector3 P(string s) { var c = s.Split(',').Select(x => float.Parse(x, Inv)).ToArray(); return new Vector3(c[0], c[1], c[2]); }
        static string V(Vector3 v) { return v.x.ToString("0.00", Inv) + ", " + v.y.ToString("0.00", Inv) + ", " + v.z.ToString("0.00", Inv); }
    }
}
