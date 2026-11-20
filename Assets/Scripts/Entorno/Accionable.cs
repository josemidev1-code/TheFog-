using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>
    /// Objeto que solo avisa al director de que se ha usado: la tele, la cama, la ventana, el interruptor, el diario…
    /// Qué pasa lo decide BosqueJuego.Accion según el momento de la partida. El rótulo se puede cambiar en ejecución.
    /// </summary>
    public class Accionable : Interactuable
    {
        [Tooltip("tele, cama, ventana, interruptor, cuadro, diario, mapa, walkie")]
        public string clave;
        public override void Usar(BosqueJuego j) { if (j != null) j.Accion(clave, this); }
    }
}
