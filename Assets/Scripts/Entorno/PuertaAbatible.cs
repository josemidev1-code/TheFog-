using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>
    /// Puerta que gira sobre su bisagra. Va en el objeto bisagra (padre de la hoja, que lleva el collider).
    /// Mirándola de cerca, E la abre o la cierra.
    /// </summary>
    public class PuertaAbatible : Interactuable
    {
        public float anguloAbierta = 100f;
        public float segundos = 0.9f;
        public bool abierta;

        Quaternion cerrada;
        float t;

        public override string Rotulo => abierta ? "cerrar" : "abrir";

        void Awake()
        {
            cerrada = transform.localRotation;
            t = abierta ? 1f : 0f;
        }

        public override void Usar(BosqueJuego j)
        {
            abierta = !abierta;
            if (j != null) j.Sonar(BosqueJuego.Sonido.Puerta, transform.position, 0.6f);
        }

        void Update()
        {
            t = Mathf.MoveTowards(t, abierta ? 1f : 0f, Time.deltaTime / Mathf.Max(0.05f, segundos));
            transform.localRotation = cerrada * Quaternion.Euler(0f, anguloAbierta * Mathf.SmoothStep(0f, 1f, t), 0f);
        }
    }
}
