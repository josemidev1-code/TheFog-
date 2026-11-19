using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>Objeto que se mira o se lee: enseña un texto (nota, foto, calendario).</summary>
    public class Examinable : Interactuable
    {
        public string titulo;
        [TextArea(3, 10)] public string texto;
        [Tooltip("Si es una frase corta se dice como pensamiento en vez de abrir la nota.")]
        public bool pensamiento;

        public override void Usar(BosqueJuego j)
        {
            if (pensamiento) j.Decir(texto, 5f); else j.Leer(titulo, texto);
        }
    }
}
