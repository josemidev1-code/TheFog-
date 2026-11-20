using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>
    /// Copia en ejecución de las medidas del constructor del bosque (el juego no ve el código del editor).
    /// Coordenadas de mundo en planta: Vector2(x, z). Coordenadas «locales» = del objeto CASA DE DANIEL (x hacia la puerta)
    /// o TORRE DE DANIEL. Si el constructor cambia una posición, hay que cambiarla también aquí.
    /// </summary>
    public static class MundoBosque
    {
        public static readonly Vector2 Casa = new Vector2(11f, -8f);
        public static readonly Vector2 Torre = new Vector2(-100f, -8f);
        public static readonly Vector2 Generador = new Vector2(12.62f, -3.74f);   // costado de la leñera
        public static readonly Vector2 Bidon = new Vector2(6.34f, 0.21f);         // junto a la furgoneta
        public static readonly Vector2 Furgoneta = new Vector2(7.14f, 2.84f);

        public static readonly Vector2[][] Senderos =
        {
            new[] { new Vector2(0, -150), new Vector2(-12, -110), new Vector2(10, -70), new Vector2(-5, -30), new Vector2(0, 0),
                    new Vector2(25, 35), new Vector2(62, 62), new Vector2(70, 105), new Vector2(40, 150) },
            new[] { new Vector2(0, 0), new Vector2(-45, 10), new Vector2(-90, -5) },
        };

        /// <summary>Postes del tendido, de la casa a la torre.</summary>
        public static readonly Vector2[] Postes =
        {
            new Vector2(-9.52f, -1.37f), new Vector2(-30.02f, 3.19f), new Vector2(-48.58f, 5.22f),
            new Vector2(-68.50f, -1.42f), new Vector2(-88.42f, -8.06f),
        };

        // Puntos en coordenadas locales de la casa.
        public static readonly Vector3 LocalDespertar = new Vector3(-1.7f, 0.15f, 1.1f);    // de pie junto a la cama
        public static readonly Vector3 LocalTele = new Vector3(-1.60f, 1.10f, -1.95f);      // sobre la encimera (no en el fregadero)
        public static readonly Vector3 LocalMesilla = new Vector3(-1.93f, 0.70f, 2.11f);    // el despertador
        public static readonly Vector3 LocalBrasas = new Vector3(0.48f, 0.5f, 1.2f);
        public static readonly Vector3 LocalPuerta = new Vector3(1.96f, 1.1f, 0.06f);
        public static readonly Vector3 LocalBombilla = new Vector3(0f, 2.3f, 0f);
        // Punto en coordenadas locales de la torre.
        public static readonly Vector3 LocalEmisora = new Vector3(0.58f, 18.65f, -2.85f);

        /// <summary>¿Está el punto dentro de la cabaña (de paredes adentro)? Sin casa: a menos de 3,5 m de su centro.</summary>
        public static bool DentroDeCasa(Transform casa, Vector3 p)
        {
            if (casa == null) return Vector2.Distance(new Vector2(p.x, p.z), Casa) < 3.5f;
            Vector3 l = casa.InverseTransformPoint(p);
            return l.x > -3.7f && l.x < 2.05f && l.z > -2.5f && l.z < 2.5f && l.y > -0.5f && l.y < 3.6f;
        }

        /// <summary>¿Está el punto dentro de la cabina de la torre (arriba)? Sin torre: nunca.</summary>
        public static bool EnCabina(Transform torre, Vector3 p)
        {
            if (torre == null) return false;
            Vector3 l = torre.InverseTransformPoint(p);
            return l.x > -3.8f && l.x < 4.3f && l.z > -3.4f && l.z < 3.9f && l.y > 17.3f && l.y < 21.6f;
        }

        /// <summary>Distancia en planta al sendero más cercano.</summary>
        public static float DistanciaASendero(Vector2 p)
        {
            float d = float.MaxValue;
            foreach (var s in Senderos)
                for (int i = 0; i < s.Length - 1; i++) d = Mathf.Min(d, Vector2.Distance(p, MasCercano(p, s[i], s[i + 1])));
            return d;
        }

        /// <summary>Punto del ramal casa-torre más cercano a p (para orientar hacia el sendero la luz de un poste).</summary>
        public static Vector2 PuntoDelRamal(Vector2 p)
        {
            var r = Senderos[1];
            Vector2 mejor = r[0]; float d = float.MaxValue;
            for (int i = 0; i < r.Length - 1; i++)
            {
                Vector2 q = MasCercano(p, r[i], r[i + 1]);
                float dd = (q - p).sqrMagnitude;
                if (dd < d) { d = dd; mejor = q; }
            }
            return mejor;
        }

        /// <summary>Altura del terreno en un punto de la planta (0 si no hay terreno ni nada debajo).</summary>
        public static float Altura(Vector2 xz)
        {
            var t = Terrain.activeTerrain;
            if (t != null) return t.SampleHeight(new Vector3(xz.x, 0f, xz.y)) + t.transform.position.y;
            RaycastHit h;
            return Physics.Raycast(new Vector3(xz.x, 80f, xz.y), Vector3.down, out h, 200f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? h.point.y : 0f;
        }

        static Vector2 MasCercano(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return a + ab * t;
        }
    }
}
