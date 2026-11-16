using UnityEngine;
using UnityEngine.UI;

namespace TheFog.Entorno
{
    /// <summary>Muestra los controles al empezar y se desvanece. F1 la vuelve a mostrar.</summary>
    public class AyudaControles : MonoBehaviour
    {
        [TextArea] public string texto = "WASD  andar      RATÓN  mirar      MAYÚS  correr      ESC  soltar el ratón      F1  ayuda";
        public float segundos = 9f;

        Text label;
        float t;

        void Start()
        {
            var canvas = new GameObject("Ayuda").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            label = new GameObject("Texto").AddComponent<Text>();
            label.transform.SetParent(canvas.transform, false);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 18;
            label.alignment = TextAnchor.LowerCenter;
            label.color = new Color(0.86f, 0.82f, 0.72f, 1f);
            label.text = texto;
            var r = label.rectTransform;
            r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0); r.pivot = new Vector2(0.5f, 0);
            r.anchoredPosition = new Vector2(0, 24); r.sizeDelta = new Vector2(0, 40);
            t = segundos;
        }

        void Update()
        {
            var k = UnityEngine.InputSystem.Keyboard.current;
            if (k != null && k.f1Key.wasPressedThisFrame) t = segundos;
            t -= Time.unscaledDeltaTime;
            if (label != null) label.color = new Color(label.color.r, label.color.g, label.color.b, Mathf.Clamp01(t / 2f));
        }
    }
}
