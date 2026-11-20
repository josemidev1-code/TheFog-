using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>
    /// El generador de emergencia: sin gasolina no vuelve la luz a la casa, al sendero ni a la cabina.
    /// Por la tarde hay luz de la red y no hace nada. De madrugada: repostar, tres tirones y arranca.
    /// </summary>
    public class GeneradorBosque : Interactuable
    {
        public Light[] luces;
        [Tooltip("Cristales que se ven encendidos desde fuera cuando hay corriente.")]
        public Renderer[] cristales;
        public Color brillo = new Color(0.95f, 0.58f, 0.24f);
        public bool enMarcha;
        int tirones;
        bool repostado;

        public bool Repostado => repostado;

        public override string Rotulo => enMarcha ? BosqueGuion.RotGenerador : repostado ? BosqueGuion.RotGenArrancar : BosqueGuion.RotGenRepostar;

        public override void Usar(BosqueJuego j)
        {
            if (j == null) return;
            if (enMarcha) { j.Decir(BosqueGuion.GeneradorBien, 4f); return; }
            if (j.HayCorriente) { j.Decir(BosqueGuion.GeneradorRed, 4f); return; }      // por la tarde hay luz de la red
            if (!repostado)
            {
                if (!j.Tiene("garrafa") && j.HayRecogible("garrafa"))
                { j.Decir(j.BidonEnCasa ? BosqueGuion.GeneradorSecoDentro : BosqueGuion.GeneradorSeco, 5f); return; }
                j.Decir(j.Tiene("garrafa") ? BosqueGuion.Repostar : BosqueGuion.GeneradorSinBidon, 4.5f);
                repostado = true;
                j.Sonar(BosqueJuego.Sonido.Liquido, transform.position, 0.8f);
                j.GastarBidon();
                return;
            }
            tirones++;
            j.Sonar(BosqueJuego.Sonido.Tiron, transform.position, 0.9f);
            if (tirones == 1) j.Decir(BosqueGuion.Tiron1, 2.5f);
            else if (tirones == 2) j.Decir(BosqueGuion.Tiron2, 2.5f);
            else { enMarcha = true; j.GeneradorEnMarcha(this); }
        }
    }
}
