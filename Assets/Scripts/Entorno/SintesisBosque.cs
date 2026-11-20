using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>
    /// Sintetizador de respaldo del bosque: lo que suena cuando falta un archivo de Assets/Resources/Audio/.
    /// Es el sintetizador que llevaba BosqueJuego, copiado tal cual de la copia congelada del 19/11 y hecho público.
    /// Todo sale a 22 050 Hz mono. No guarda nada: quien lo llama se queda con el clip (BosqueSonido los cachea).
    /// </summary>
    public static class SintesisBosque
    {
        public delegate float Onda(float t, float fase, System.Random rnd, ref float estado);

        public static AudioClip Bucle(string nombre, float segundos, Onda onda)
        {
            const int sr = 22050;
            int n = (int)(sr * segundos);
            var d = new float[n];
            var rnd = new System.Random(1987);
            float estado = 0f;
            for (int i = 0; i < n; i++) d[i] = onda(i / (float)sr, i / (float)n, rnd, ref estado);
            // Cruce suave entre el final y el principio para que el bucle no dé un chasquido.
            int cruce = sr / 5;
            for (int i = 0; i < cruce; i++) { float a = i / (float)cruce; d[i] = d[i] * a + d[n - cruce + i] * (1f - a); }
            var clip = AudioClip.Create(nombre, n - cruce, 1, sr, false);
            var recorte = new float[n - cruce]; System.Array.Copy(d, recorte, n - cruce);
            clip.SetData(recorte, 0);
            return clip;
        }

        public static float Viento(float t, float fase, System.Random rnd, ref float e)
        {
            float ruido = (float)rnd.NextDouble() * 2f - 1f;
            e += (ruido - e) * 0.018f;                       // paso bajo: soplo grave
            float racha = 0.55f + 0.45f * Mathf.Sin(fase * Mathf.PI * 2f) * Mathf.Sin(fase * Mathf.PI * 6f + 1f);
            return e * 3.2f * racha;
        }

        public static float Motor(float t, float fase, System.Random rnd, ref float e)
        {
            float ruido = (float)rnd.NextDouble() * 2f - 1f;
            e += (ruido - e) * 0.12f;
            float golpes = Mathf.Pow(Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f * 23f)), 6f);   // explosiones del cilindro
            return 0.45f * Mathf.Sin(t * Mathf.PI * 2f * 46f) + 0.25f * Mathf.Sin(t * Mathf.PI * 2f * 92f + 0.7f) + e * 0.9f * (0.4f + golpes);
        }

        public static float Estatica(float t, float fase, System.Random rnd, ref float e)
        {
            float ruido = (float)rnd.NextDouble() * 2f - 1f;
            e += (ruido - e) * 0.5f;
            float chasquido = rnd.NextDouble() > 0.9992 ? 0.9f : 0f;
            float portadora = 0.06f * Mathf.Sin(t * Mathf.PI * 2f * 610f) * (0.5f + 0.5f * Mathf.Sin(t * 2.1f));
            float respira = 0.5f + 0.5f * Mathf.Sin(fase * Mathf.PI * 2f * 2f);   // sube y baja como una respiración
            return e * 0.28f * (0.55f + 0.45f * respira) + chasquido + portadora;
        }

        public static AudioClip Golpe(string nombre, float segundos, float tono, float aspereza)
        {
            const int sr = 22050;
            int n = (int)(sr * segundos);
            var d = new float[n];
            var rnd = new System.Random(nombre.GetHashCode());
            float e = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr, env = Mathf.Exp(-t * 6f / segundos);
                float ruido = (float)rnd.NextDouble() * 2f - 1f;
                e += (ruido - e) * Mathf.Clamp01(tono / 4000f);
                float grano = rnd.NextDouble() > 0.97 ? 1f : 0.55f;
                d[i] = env * (e * aspereza * 2.2f * grano + (1f - aspereza) * Mathf.Sin(t * Mathf.PI * 2f * tono));
            }
            var clip = AudioClip.Create(nombre, n, 1, sr, false);
            clip.SetData(d, 0);
            return clip;
        }

        /// <summary>Algo parecido a una voz: un tono que sube y cae con dos resonancias que se mueven, sin palabras.</summary>
        public static AudioClip Voz()
        {
            const int sr = 22050;
            int n = (int)(sr * 2.6f);
            var d = new float[n];
            float fase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr, u = i / (float)n;
                float f0 = Mathf.Lerp(150f, 92f, u) + 9f * Mathf.Sin(t * 31f);
                fase += f0 / sr;
                float sierra = 2f * Mathf.Repeat(fase, 1f) - 1f;
                float f1 = Mathf.Lerp(700f, 320f, Mathf.PingPong(u * 3f, 1f)), f2 = Mathf.Lerp(1100f, 2100f, Mathf.PingPong(u * 2f + 0.3f, 1f));
                float vocal = sierra * (0.6f + 0.4f * Mathf.Sin(t * Mathf.PI * 2f * f1)) * (0.7f + 0.3f * Mathf.Sin(t * Mathf.PI * 2f * f2));
                float silabas = Mathf.Pow(Mathf.Abs(Mathf.Sin(u * Mathf.PI * 5f)), 0.6f);
                float env = Mathf.Sin(u * Mathf.PI);
                d[i] = vocal * silabas * env * 0.5f;
            }
            var clip = AudioClip.Create("voz", n, 1, sr, false);
            clip.SetData(d, 0);
            return clip;
        }
    }
}
