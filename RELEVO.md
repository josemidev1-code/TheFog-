# RELEVO — dónde está THE FOG ahora mismo

**Última actualización:** 19/11/2026, tarde. Escrito por Claude (sesión con Josemi).

Este archivo es lo primero que tiene que leer quien continúe el proyecto, sea una persona o una IA.
Se actualiza **al final de cada tanda de trabajo** y se sube a GitHub con el resto de cambios.
El detalle de cada día está en el diario: [docs/entorno/README.md](docs/entorno/README.md).

## 1. En qué se está trabajando

La escena del **bosque**: `Assets/Scenes/Bosque.unity`. Es una escena aparte de Valdara, jugable de principio a fin
(unos 10 minutos): Daniel despierta en su casa sin luz → coge la linterna de la mesilla → coge la garrafa junto a la
puerta → sendero entre la niebla (una silueta que desaparece al alumbrarla) → generador al pie de la torre (repostar y
tres tirones) → vuelven las luces → cabina de la torre → monitor con tres cámaras (en la de la casa hay alguien) →
emisora → rótulo de «continuará».

Para jugarla: abrir `Assets/Scenes/Bosque.unity` y pulsar Play. WASD, ratón, Mayús correr, **E** usar, **F** linterna,
Esc suelta el ratón. Con cualquier otra escena abierta, Play arranca `TorreDaniel` (lo decide
`Assets/Editor/Entorno/PlayArrancaJuego.cs`). **El bosque todavía no está en Build Settings** ni conectado al menú.

## 2. Qué hay hecho en el bosque

- **Terreno, senderos, tres claros, árboles y hierba**, con el kit CC0 «Vegetation» de Elbolilloduro.
- **Casa de Daniel** (cabaña de troncos, se queda así): cama, mesilla con la linterna, cómoda con el detector del
  abuelo, estufa de leña en la esquina, mesa con la radio y el cuaderno, y **cocina** (encimera con fregadero y cocina
  de gas) en la pared de la ventana. Puerta que se abre.
- **Torre de comunicaciones** con cabina amueblada: banco de trabajo con emisora, monitor de cámaras y partes, cama,
  estufa y archivadores. La cocina que traía el pack se pasó a la casa.
- **Generador** al pie de la torre, con manguera hasta la pata.
- **Tendido eléctrico**: postes a lo largo del sendero con cuatro cables, acometida a la cumbrera del porche de la casa
  y bajada por la pata de la torre.
- **Interacción**: 15 objetos (examinar, recoger, generador, radio, monitor, puerta).
- **Estética**: cámara pixelada a 540 de altura, tramado 0,15, paleta con «fidelidad» para que la niebla no se aplaste.

## 3. Cómo está montado (importante antes de tocar nada)

**La escena no se edita a mano: se genera con código.** Si mueves algo en el editor y luego reconstruyes, se pierde.

- Constructores, en `Assets/Editor/Entorno/`:
  - `BosqueBuilder.cs` — terreno, senderos, árboles, hierba.
  - `BosqueAsentamiento.cs` — casa, torre, generador, muebles y enseres (aquí se recoloca cada objeto).
  - `BosqueTendido.cs` — postes y cables.
- Menús de Unity:
  - `THE FOG → Bosque → 1. Importar kit de modelos` — solo si cambian modelos o texturas.
  - `THE FOG → Bosque → 2. Construir escena del bosque` — rehace y guarda la escena (semilla fija).
    Si la escena abierta tiene cambios sin guardar, no hace nada: reabrirla antes.
- Scripts del juego, en `Assets/Scripts/Entorno/`: `BosqueJuego` (va en Daniel, lleva la secuencia y los objetivos),
  `Interactuable`, `Examinable`, `Recogible`, `GeneradorBosque`, `RadioBosque`, `MonitorCamaras`, `PuertaAbatible`,
  `LuzParpadeo`.
- Herramientas de Blender (se ejecutan sin abrir Blender), en `tools/`:
  `bosque_split_pack.py` (vegetación), `bosque_split_edificios.py` (torre, cabaña, objetos),
  `bosque_cocina.py` (corta la encimera del pack para la cocina de la casa y el banco de la torre),
  `bosque_silueta.py`, `tripo_prepare.py` (prepara un `.glb` de Tripo para Unity).
- Modelos: `Assets/Models/Retro/Bosque/` (kit) y `Assets/Models/Retro/Tripo/` (hechos con Tripo).
  Las carpetas `Bosque/Corregidos` y `Bosque/Tendido` las genera el constructor; no editarlas a mano.
- El pack original está en `ArtSource/Elbolilloduro/Vegetation/` (el `.zip` no se sube: es lo mismo comprimido).

## 4. Normas de Josemi

1. **Todo lo que se coloca es un modelo 3D.** Nada de cubos o formas sueltas haciendo de objeto.
2. **Si falta un modelo, no se inventa ni se sustituye por otro: se le pide a Josemi** con una lista concreta
   (qué es, tamaño, para qué) y él lo hace en Tripo AI. Los deja como `.glb` en `ArtSource/Tripo/`.
3. **Estética**: mezcla de MOLDRISE y No, I'm Not a Human. España rural de finales de los 80, gastado, desaturado,
   inquietante. No mezclar estilos. Más detalle en [docs/retro/README.md](docs/retro/README.md).
4. **La casa de Daniel se queda como cabaña de troncos.** No cambiarle las texturas.
5. **Commit solo cuando él lo pida.** Publicar en Discord (servidor THE FOG, canal `#avances`) solo cuando él lo pida.
6. Los créditos de Tripo son pocos (quedaban 70 el 19/11) y no puede comprar más.

## 5. Pendiente

**Esperando modelos de Josemi** (lista completa en [docs/entorno/PENDIENTE_BOSQUE.md](docs/entorno/PENDIENTE_BOSQUE.md)):

1. Bombilla colgante (25 cm) — las luces de dentro salen del aire, no hay aparato.
2. Aplique de exterior (35 cm) — porche, pie de la torre y generador.
3. Retrete exterior (caseta de tablas, 1,1 × 1,1 × 2,2 m) — el baño, detrás de la casa.
4. Palanganero (1,3 m) — para asearse dentro de la casa.
5. Palomilla de acometida con dos aisladores (40 cm) — donde los cables llegan a la casa y a la torre.
6. Cuadro eléctrico (40 × 50 cm) — junto a la puerta de la casa y al pie de la torre.
7. Opcionales: banco para el porche, tajo de cortar leña con hacha, señal oxidada del sendero, plano de Valdara.

**Decisiones que tiene que tomar Josemi:**

- Baño fuera (retrete detrás de la casa y palanganero dentro; es la propuesta) o dentro (harían falta inodoro, lavabo,
  ducha y una casa más grande).
- La cama de la cabina de la torre: se queda o se quita.
- Aprobar o corregir la biblia narrativa: [docs/lore/biblia_narrativa_propuesta_1.md](docs/lore/biblia_narrativa_propuesta_1.md).
  Los textos del juego (cuaderno, radio, hoja de mantenimiento, transmisión) salen de ahí.
- Conectar el bosque con Valdara y con el menú, y meterlo en Build Settings.

**Sin comprobar:** después del último retoque (taza y cuaderno sobre la mesa) no se repitió la prueba completa en Play.
La última prueba completa dio bien: Daniel llega a todos los objetos y la secuencia termina.

**Se deja como viene en el pack:** en la cabina de la torre hay cinco archivadores que tapan media ventana.

## 6. Avisos para quien use una IA con Unity (MCP)

- Al entrar en Play desde la IA, poner `Application.runInBackground = true`; si no, con Unity sin foco los fotogramas no
  avanzan y las capturas salen con el rótulo de inicio atascado.
- Las capturas de comprobación van a `docs/capturas/` con el nombre `bosque_NN_descripcion.png`.
- En el código que se ejecuta desde la IA hay que escribir `UnityEngine.Object`, no `Object`.

## 7. Otras escenas del proyecto

- `TorreDaniel.unity` — Valdara: casa de Daniel, monte y pueblo (estética MOLDRISE). Es la primera de Build Settings.
- `PuestoDaniel.unity` — turno de noche en la torre, con cámaras y anomalías. Ver [docs/puesto-daniel.md](docs/puesto-daniel.md).
- `DanielRoom.unity`, `FirstPlayable.unity` — prototipos anteriores.

Cómo abrir el proyecto en otro ordenador: [docs/ABRIR_EN_CLASE.md](docs/ABRIR_EN_CLASE.md) (hace falta Git LFS).

## 8. Cómo mantener este archivo

Al terminar cada tanda: cambiar la fecha de arriba, actualizar las secciones 2 y 5, añadir la entrada del día al diario
(`docs/entorno/README.md`) y subirlo todo a GitHub en el mismo commit que el trabajo.
