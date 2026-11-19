using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>Algo que Daniel puede usar mirándolo de cerca y pulsando E. El collider puede estar en un hijo.</summary>
    public abstract class Interactuable : MonoBehaviour
    {
        public string rotulo = "usar";
        public virtual string Rotulo => rotulo;
        public abstract void Usar(BosqueJuego j);
    }
}
