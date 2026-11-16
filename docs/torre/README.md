# Torre de Daniel — reconstrucción por pasos

Escena: `Assets/Scenes/TorreDaniel.unity`. Regla de estética: [../retro/README.md](../retro/README.md) (todo realista y envejecido, CC0 + ajuste retro; Tripo solo para lo que no exista).

| Paso | Estado |
|---|---|
| 1. Estructura exterior | **Hecho (16/11/2026)** |
| 2. Envolvente de la cabina (paredes, ventanas, tejado, puerta) | Pendiente |
| 3. Interior coherente (muebles CC0, armario y nevera reales) | Pendiente |
| 4. Iluminación (lámparas, emergencia apagada, flexo) | Pendiente |
| 5. Pie de la torre con sentido (generador, cable, sendero) | Pendiente |

## Reinicio con Tripo (16/11/2026)
Se decide hacer **todos** los modelos con Tripo AI. La escena se vacía y se reconstruye:
- **Escena vacía de modelos**: borrados `Atrezo Retro`, `VALDARA/Vegetacion`, el terreno, la estructura de la torre (con sus colisiones de escalera y pasarela), la cabina, la casa, la farola y las vallas. Los archivos de los assets siguen en el proyecto; `PuestoDaniel.unity` no se ha tocado.
- Se queda solo la **lógica del juego sin modelo**: puerta, consola, radio, linterna, notas, muebles registrables, panel eléctrico, luces y anomalías de la casa, como objetos vacíos con su colisión de interacción en el mismo sitio. Al meter cada modelo de Tripo se cuelgan de él.
- `SueloInvisible`: caja de colisión de 600 × 600 m a y = 0 para que Daniel no caiga. Daniel empieza en el suelo (6,5; 0; -5) hasta que haya torre.
- `Consola/Pantalla`: renderer sin malla (invisible) para que `SecurityConsole` no dé errores.
- Lo nuevo va en el grupo raíz `ENTORNO TRIPO`. Preparación: `tools/tripo_prepare.py` (ver `docs/retro/README.md`).
- Probado en Play: arranca, Daniel en el suelo y en estado `Playing`. El error de audio (`ProcAudio` en `PuestoCore.cs`, clips destruidos al repetir Play en el editor) ya existía antes y no viene de este cambio.

Los pasos 2–5 de abajo quedan sustituidos por la reconstrucción con Tripo.

Capturas: `antes_hoja.png` (estado inicial) y `paso1_hoja.png` (tras el paso 1).

## Paso 1 — Estructura exterior
- Constructor: `Assets/Editor/Torre/TorreBuilder.cs`, menú **THE FOG → Torre → Construir estructura (escena abierta)**. Se puede repetir: rehace el grupo `TORRE DE VIGILANCIA/Estructura`. Medidas al principio del archivo.
- Pasarela a **12 m** (antes ~7 m). La cabina, su tejado provisional y Daniel se suben 5 m; el tejado viejo queda dentro de `CABINA` hasta el paso 2.
- 4 patas de 32 cm inclinadas (base ±3,6 m, arriba ±3,0 m) sobre zapatas de hormigón; largueros a 4 y 8 m; cruces de San Andrés en cada cara; viguetas, vigas de borde y jabalcones bajo la pasarela de 8,6 × 8,6 m; barandilla de 1,05 m con hueco donde llega la escalera.
- Torre de escalera adosada al sur: 4 tramos de 3 m (15 peldaños de 20 × 26 cm, 1 m de ancho) con rellanos a 3, 6 y 9 m y llegada a 12 m junto a la puerta. Cada tramo tiene una rampa de colisión invisible para que el `CharacterController` suba sin tropezar; las barandillas llevan paredes de colisión invisibles.
- Materiales (`Assets/Art/Torre/Materiales`): vigas `Wood062`, tablones `Planks023A`, hormigón `Concrete031` (ambientCG, CC0), con UV en metros y el ajuste retro de texturas. Mallas guardadas en `Assets/Art/Torre/Mallas`.
- `PuestoAutoTest`: la comprobación "Daniel está de pie en la cabina" ya no usa alturas fijas (6,5–9 m) sino la altura de la consola, para que valga en las dos escenas.

**Probado en Play:** Daniel sube del suelo a la pasarela tramo a tramo con su CharacterController (llega a y = 12,03 frente a la puerta) y las barandillas lo frenan en pasarela, tramos y rellanos. Sin errores en consola. No se ha subido con teclado y ratón a mano.

**Pendiente de este paso:** la torre se ve muy oscura de noche; se valorará en el paso 4 (iluminación). La escalera de tijera y el aparador del pie de la torre se quitan en el paso 5.

### Luz de noche legible (16/11/2026)
Solo en `TorreDaniel` (componente `SkyCycle` y luz `Luna`): luna 0,35 → **0,75** y ambiente nocturno (0,10; 0,12; 0,12) → **(0,085; 0,10; 0,11)**. Más luz direccional y menos ambiente: los modelos tienen cara iluminada y cara en sombra en vez de ser siluetas grises, y la noche sigue oscura. El código y `PuestoDaniel` no cambian. Captura en Play: [noche_luna_075.png](noche_luna_075.png).
