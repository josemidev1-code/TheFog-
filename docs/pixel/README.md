# THE FOG — estética MOLDRISE (pixel art en tiempo real)

## Decisión (17/11/2026)
Todo el juego con la **estética de MOLDRISE**: primera persona en **3D en tiempo real**, pero el mundo se ve como **pixel art de la era de 32 bits** (baja resolución, paleta corta, tramado). Encaja con el documento maestro (primera persona, terror opresivo) y aprovecha lo ya construido: casa de Daniel jugable, torre, monte.

Referencia: MOLDRISE (Raffaele Picca, 2026) — terror en primera persona en 3D con gráficos de estilo pixel art 32 bits. [Steam](https://store.steampowered.com/app/4519130/MOLDRISE/).

> Pendiente de reflejar en el GDD: el documento maestro dice "estética realista". Ahora es "realista en contenido, pixel art en imagen".

## Cómo funciona
- `Assets/Scripts/Pixel/PixelArtCamera.cs` en la cámara de Daniel: renderiza el 3D a **320 × 180** (ajustable: `altura`), lo reduce a la **paleta común de 32 colores** con **tramado ordenado** y lo muestra a pantalla completa sin suavizar. Ajustes: tramado, brillo, contraste, viñeta. Desactivar el componente = imagen normal.
- `Assets/Shaders/Pixel/PixelPaleta.shader`: el filtro (tabla de colores 32×32×32 precalculada a partir de la paleta).
- Paleta: `Assets/Art/Pixel/paleta_thefog.png` ([vista](paleta.png)), sacada de las escenas del juego: oscura, fría y desaturada, con los cálidos de bombillas y brasas. Es la misma que usa `tools/pixelate.py` para imágenes sueltas (carteles, documentos, menús).
- Sustituye a `RetroRender` (resolución al 50 %), que queda desactivado en `TorreDaniel`.

## Capturas en Play
- [Cocina de la casa de Daniel](pixel_cocina.png)
- [Casa de noche desde el camino](pixel_casa_noche.png)
- [Prueba inicial sobre renders](prueba_look.png)

## Pendiente de ajustar
- Los bancos de niebla (partículas) se iluminan con el farol y se ven como una mancha naranja grande.
- Comprobar legibilidad de objetos pequeños e interactivos a 320 × 180 (quizá 384 × 216).
- Interfaz (textos, reloj, avisos) en pixel art con la misma paleta.

## Siguiente: el juego
Vertical slice de la Fase B del documento maestro, toda con este look: noche en la torre y la casa, generador con combustible, linterna con pilas, radio, un visitante (Fred, sano o infectado según la semilla), niebla letal, una criatura con sentidos claros y el informe de la run.
