using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>El monitor de la cabina: enseña las cámaras del sendero, el bosque y la casa.</summary>
    public class MonitorCamaras : Interactuable
    {
        public Camera[] camaras;
        public string[] nombres;
        public GameObject figura;          // lo que se ve una sola vez en la cámara de la casa
        public PuertaAbatible puertaCasa;  // al volver a mirar, la puerta está abierta
        public override void Usar(BosqueJuego j) => j.AbrirMonitor(this);
    }
}
