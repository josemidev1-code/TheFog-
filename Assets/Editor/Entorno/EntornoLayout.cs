using UnityEngine;

namespace TheFog.EditorTools.Entorno
{
    /// <summary>
    /// Trazado común del entorno (coordenadas XZ del mundo): torre, casa de Daniel, senderos y pueblo de Valdara.
    /// Lo usan SueloBuilder (allanar y pintar), EntornoBuilder (monte, postes) y PuebloBuilder (casas y plaza).
    /// </summary>
    public static class EntornoLayout
    {
        public static readonly Vector2 Torre = new Vector2(0f, 0f);
        public static readonly Vector2 Casa = new Vector2(-34f, -5f);

        // Pueblo: plaza mayor y calles en cruz.
        public static readonly Vector2 Plaza = new Vector2(0f, -150f);
        public const float PlazaMedioX = 12f, PlazaMedioZ = 11f;   // plaza de 24 × 22 m
        public const float CalleMedia = 3.2f;                       // calles de ~6,4 m entre fachadas
        public static readonly Rect Pueblo = new Rect(-62f, -200f, 124f, 92f); // x, z, ancho, fondo (zona allanada)

        // Senderos de tierra y piedra fuera del pueblo.
        public static readonly Vector2[][] Senderos =
        {
            // pie de la torre → puerta de la casa
            new[] { new Vector2(1.5f, -7f), new Vector2(-6f, -9.5f), new Vector2(-16f, -8.5f), new Vector2(-24f, -8f), new Vector2(-30f, -10.5f), new Vector2(-35.8f, -9.3f) },
            // casa → camino del pueblo (entra por la calle norte)
            new[] { new Vector2(-34f, -11f), new Vector2(-31f, -35f), new Vector2(-19f, -64f), new Vector2(-6f, -95f), new Vector2(0f, -110f) },
        };

        // Calles del pueblo (ejes).
        public static readonly Vector2[][] Calles =
        {
            new[] { new Vector2(0f, -110f), new Vector2(0f, -139f) },          // calle norte (Real)
            new[] { new Vector2(0f, -161f), new Vector2(0f, -198f) },          // calle sur
            new[] { new Vector2(-60f, -150f), new Vector2(-12f, -150f) },      // calle oeste
            new[] { new Vector2(12f, -150f), new Vector2(60f, -150f) },        // calle este
        };

        public static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        static float LinesDistance(Vector2 p, Vector2[][] lines)
        {
            float best = float.MaxValue;
            foreach (var line in lines)
                for (int i = 0; i < line.Length - 1; i++)
                    best = Mathf.Min(best, SegmentDistance(p, line[i], line[i + 1]));
            return best;
        }

        public static float SenderoDistance(Vector2 p) => LinesDistance(p, Senderos);
        public static float CalleDistance(Vector2 p) => LinesDistance(p, Calles);

        /// <summary>Distancia a la plaza (0 dentro).</summary>
        public static float PlazaDistance(Vector2 p)
        {
            float dx = Mathf.Max(0f, Mathf.Abs(p.x - Plaza.x) - PlazaMedioX);
            float dz = Mathf.Max(0f, Mathf.Abs(p.y - Plaza.y) - PlazaMedioZ);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Distancia a la zona del pueblo (0 dentro).</summary>
        public static float PuebloDistance(Vector2 p)
        {
            float dx = Mathf.Max(0f, Mathf.Max(Pueblo.xMin - p.x, p.x - Pueblo.xMax));
            float dz = Mathf.Max(0f, Mathf.Max(Pueblo.yMin - p.y, p.y - Pueblo.yMax));
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
