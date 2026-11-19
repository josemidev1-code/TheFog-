using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>El generador al pie de la torre: sin gasolina no hay luz en la casa ni en la cabina.</summary>
    public class GeneradorBosque : Interactuable
    {
        public Light[] luces;
        [Tooltip("Cristales que se ven encendidos desde fuera cuando hay corriente.")]
        public Renderer[] cristales;
        public Color brillo = new Color(0.95f, 0.58f, 0.24f);
        public bool enMarcha;
        int tirones;
        bool repostado;

        public override string Rotulo => enMarcha ? "generador" : repostado ? "tirar del arranque" : "repostar";

        public override void Usar(BosqueJuego j)
        {
            if (enMarcha) { j.Decir("Ronronea. Mientras tenga gasolina, hay luz.", 4f); return; }
            if (!repostado)
            {
                if (!j.Tiene("garrafa")) { j.Decir("El depósito está seco. La garrafa se quedó en casa, junto a la puerta.", 5f); return; }
                repostado = true;
                j.Sonar(BosqueJuego.Sonido.Liquido, transform.position, 0.8f);
                j.Decir("Vacías la garrafa en el depósito. Huele a gasolina y a tierra mojada.", 4.5f);
                return;
            }
            tirones++;
            j.Sonar(BosqueJuego.Sonido.Tiron, transform.position, 0.9f);
            if (tirones == 1) j.Decir("Tiras del arranque. Nada.", 2.5f);
            else if (tirones == 2) j.Decir("Tose. Se cala.", 2.5f);
            else { enMarcha = true; j.GeneradorEnMarcha(this); }
        }
    }
}
