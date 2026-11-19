using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>Objeto que Daniel se lleva: linterna, garrafa.</summary>
    public class Recogible : Interactuable
    {
        public string clave;
        public override void Usar(BosqueJuego j)
        {
            j.Recoger(clave);
            Destroy(gameObject);
        }
    }
}
