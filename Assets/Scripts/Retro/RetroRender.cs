using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TheFog.Retro
{
    /// <summary>
    /// Aspecto retro por resolución interna reducida y escalado sin suavizar (píxel visible).
    /// Cambia el asset de URP solo mientras está activo y lo restaura al desactivarse,
    /// para no dejar el proyecto modificado. Desactivar el componente = imagen original.
    /// </summary>
    [DisallowMultipleComponent]
    public class RetroRender : MonoBehaviour
    {
        [Tooltip("Fracción de la resolución de pantalla a la que se renderiza. 1 = original.")]
        [Range(0.2f, 1f)] public float renderScale = 0.5f;

        [Tooltip("Point = píxeles duros. Desactivado = escalado suave.")]
        public bool pointUpscale = true;

        UniversalRenderPipelineAsset asset;
        float previousScale;
        UpscalingFilterSelection previousFilter;
        bool captured;

        void OnEnable()
        {
            asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (asset == null) return;
            previousScale = asset.renderScale;
            previousFilter = asset.upscalingFilter;
            captured = true;
            Apply();
        }

        void OnValidate()
        {
            if (captured && Application.isPlaying) Apply();
        }

        public void Apply()
        {
            if (asset == null) return;
            asset.renderScale = renderScale;
            asset.upscalingFilter = pointUpscale ? UpscalingFilterSelection.Point : UpscalingFilterSelection.Linear;
        }

        void OnDisable()
        {
            if (!captured || asset == null) return;
            asset.renderScale = previousScale;
            asset.upscalingFilter = previousFilter;
            captured = false;
        }
    }
}
