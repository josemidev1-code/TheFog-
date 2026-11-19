using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>Luz que tiembla un poco: brasas, bombillas con mala corriente.</summary>
    public class LuzParpadeo : MonoBehaviour
    {
        public float cantidad = 0.25f, velocidad = 7f;
        Light luz; float baseI, semilla;
        void Awake() { luz = GetComponent<Light>(); baseI = luz.intensity; semilla = Random.value * 100f; }
        void Update()
        {
            float n = Mathf.PerlinNoise(semilla, Time.time * velocidad);
            luz.intensity = baseI * (1f - cantidad + cantidad * 2f * n);
        }
    }
}
