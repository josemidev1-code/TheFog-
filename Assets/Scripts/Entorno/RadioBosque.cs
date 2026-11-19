using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>La radio de casa (solo estática) y la emisora de la cabina (la transmisión de Valdara).</summary>
    public class RadioBosque : Interactuable
    {
        public bool deTorre;
        public override void Usar(BosqueJuego j) => j.UsarRadio(this);
    }
}
