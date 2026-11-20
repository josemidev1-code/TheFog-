# RELEVO — dónde está THE FOG ahora mismo

**Última actualización:** 20/11/2026. Escrito por Claude (sesión con Josemi).

Este archivo es lo primero que tiene que leer quien continúe el proyecto, sea una persona o una IA.
Se actualiza **al final de cada tanda de trabajo** y se sube a GitHub con el resto de cambios.
El detalle de cada día está en el diario: [docs/entorno/README.md](docs/entorno/README.md).

## 0. LO PRIMERO (estado a 20/11)

**ACTUALIZACIÓN 20/11, madrugada — YA SE HA PROBADO EN UNITY.** Josemi instaló Unity Hub y la licencia funciona.
Con eso se hizo, de verdad:

1. Unity importó todo sin errores de compilación.
2. Se construyó la escena (`THE FOG → Bosque → 2`, en modo batch). La comprobación del menú 3 dio todo OK (tele, cama,
   ventana, linterna, bidón, radios, 5 luces de poste, generador con 10 luces junto a la cabaña, recursos de audio,
   interfaz y fuentes). Lo único ausente es lo opcional: el vídeo del informativo y los modelos de Tripo por hacer.
3. Se jugó la partida entera con una **prueba automática en Play** (`THE FOG → Bosque → 4. Prueba automática`,
   `Assets/Tests/BosquePruebaDriver.cs`): teclado simulado y los atajos F9/F10/F11. Resultado en
   `docs/entorno/primera_version_prueba.json`: **superada, 0 fallos, 0 avisos**, 285 s. Pasa por diario, mapa, walkie,
   tele, informativo, dormir, madrugada, generador (10 de 10 luces), cabina, llamada del cabo Salcedo y tarjeta final.
4. Capturas reales en `docs/capturas/primera_version/` (`juego_*.png` en Play; `pv_*.png` de la escena con luz de trabajo).

**Lo que la prueba automática NO cubre:** usa atajos (no anda de la cama al generador ni sube a la torre a pie, no coge
la linterna ni el bidón a mano, no prueba el orden libre) y nadie ha **oído** el sonido. Falta una partida a mano.

**Corregido el 20/11 y comprobado en capturas de una segunda y tercera pasada de la prueba (0 fallos):** los cristales
ya no brillan vistos desde dentro del edificio en el que está Daniel (`BosqueJuegoMundo.PintarCristal`), y los rótulos
y pensamientos ya no salen dobles (la sombra del texto se separa según el tamaño de la letra, `BosqueInterfaz.Texto`).

**Mando:** Josemi confirmó que hay un mando conectado al PC; Daniel se movía y giraba solo entre capturas de la prueba.
Se añadió zona muerta de 0,3 a las palancas (`PInput.Palanca`, en `Assets/Scripts/Puesto/PuestoCore.cs`). Josemi cortó
la comprobación («está todo bien»), pero la prueba que ya estaba en marcha terminó: con la zona muerta Daniel no se
movió ni giró en las cinco primeras capturas (misma posición y mirada), 0 fallos. El mando («Xbox Controller») daba 0 en
reposo en el instante medido, así que la deriva es intermitente y no quedó medida directamente.

**Opinión de Josemi tras oírlo (20/11):** no le gustan las voces de fondo (tele, radio, voz intrusa) y quiere una banda
sonora buena. Hecho: voces apagadas (`BosqueSonido.VolumenVoces = 0`) y música subida al doble o más. **Pendiente:**
buscar o encargar una banda sonora mejor que la de Rest! (y decidir entonces qué pasa con la licencia CC BY-SA).

**Discord:** publicado el 20/11 en `#avances` del servidor THE FOG (texto + diez capturas), desde la aplicación de
Discord del PC con el control del escritorio.

Lo que sigue más abajo se escribió ANTES de poder abrir Unity; donde diga «no se ha probado» o «la escena guardada es
la vieja», manda esta actualización.

**La primera versión jugable (el prólogo, «la noche del incidente») está escrita entera y NO se ha probado.** Unity no
abre en este equipo («No valid Unity Editor license found»; Unity Hub no está instalado). Lo único comprobado es que el
código **compila** con el compilador de Unity sin abrir el editor (`bash tools/compilar_sin_unity.sh`). Nadie ha
construido la escena con los cambios, nadie ha pulsado Play, nadie ha oído un sonido ni ha visto una pantalla.
`Bosque.unity` sigue siendo la del commit e28e22e: no tiene ni los muebles de Josemi del 19/11 ni nada del prólogo.
Ninguno de estos cambios está en un commit.

Pasos cuando Unity abra (detalle en [docs/entorno/PRIMERA_VERSION.md](docs/entorno/PRIMERA_VERSION.md)):

1. Abrir el proyecto y esperar a que importe: los 14 modelos de Tripo sin `.meta`, el sonido
   (`Assets/Resources/Audio/`), la interfaz (`Assets/Resources/UI/`, `.bytes`) y las fuentes. Que la consola no tenga
   fallos de compilación (los archivos de `Assets/Editor/Entorno/` y de `Assets/Scripts/Entorno/` no han pasado nunca
   por el editor).
2. `THE FOG → Bosque → 1. Importar kit de modelos` **solo si hace falta** (si el menú 2 dice «Falta el kit» o los
   modelos del pack salen sin textura).
3. `THE FOG → Bosque → 2. Construir escena del bosque`. Rehace la escena, la guarda y la pone primera en Build Settings.
4. `THE FOG → Bosque → 3. Comprobar primera versión`: lista «OK / FALTA» en la consola.
5. Play con `Assets/Scenes/Bosque.unity` abierta. Jugar entera y apuntar lo que falle.

Sin el paso 3 (construir) se juega una versión recortada sobre la escena vieja: sin tele, con la garrafa dentro y el
generador al pie de la torre.

El proyecto está en `Desktop/thefogjuego/THE FOG/ProyectoUnity`. Los `.glb` originales de Josemi, en
`Desktop/thefogjuego/ModelosTheFog`; los assets descargados, en `Desktop/thefogjuego/AssetsDescargados` (los dos fuera
del proyecto y de git).

## 1. En qué se está trabajando

La escena del **bosque**: `Assets/Scenes/Bosque.unity`, aparte de Valdara. Sobre ella va la **primera versión jugable**
que pidió Josemi el 19/11 por la noche, el prólogo:

Tarde del miércoles 11/11/1987: Daniel en la cabaña → **abre el diario** (ha vuelto a Valdara, tiene el título de
guarda forestal y vigilante y plaza en la torre de control) → **informativo en la tele** (temblores en Valdara, nieblas)
→ se acuesta → madrugada: **explosión y apagón** → comprueba que no hay luz (tele, radio, ventana) → linterna → bidón
junto a la furgoneta → **generador** en el costado de la cabaña → vuelven las luces de la casa y **las del sendero, poste
a poste, hacia la torre** → cabina → **llamada del cabo Salcedo por la emisora**: explosión en la mina, todo Valdara sin
luz ni teléfono → fin. Con misiones en pantalla, diario (J), mapa (M), walkie (T), subtítulos y sonido.

Para jugarla: construir la escena (sección 0), abrir `Assets/Scenes/Bosque.unity` y pulsar Play. WASD, ratón, Mayús
correr, **E** usar, **F** linterna, **J** diario, **M** mapa, **T** walkie, F1 ayuda, Esc suelta el ratón. En el editor,
F9/F10/F11 saltan a la madrugada, al generador y a la cabina. Con cualquier otra escena abierta, Play arranca
`TorreDaniel` (lo decide `Assets/Editor/Entorno/PlayArrancaJuego.cs`). Al reconstruir, el bosque pasa a ser la primera
escena de Build Settings; sigue sin conectar con Valdara ni con el menú.

La versión anterior de la escena (despertar sin luz → garrafa junto a la puerta → generador al pie de la torre → cámaras
→ emisora) queda sustituida por el prólogo; el monitor de cámaras sigue en la cabina, pero ya no es obligatorio.

## 2. Qué hay hecho en el bosque

- **Terreno, senderos, tres claros, árboles y hierba**, con el kit CC0 «Vegetation» de Elbolilloduro.
- **Casa de Daniel** (cabaña de troncos, se queda así): estufa de leña en la esquina y **cocina** (encimera con fregadero
  y cocina de gas) en la pared de la ventana. Puerta que se abre. **Escrito, pendiente de construir en Unity:** muebles
  de Josemi — cama de hierro, mesita con despertador y la linterna, librería con la maceta encima, escritorio con la
  radio antigua, el cuaderno y el detector del abuelo, corcho, radiador, ventana con cortinas, alfombra y bidón.
  (En la escena guardada aún están la cama, la mesilla, la cómoda y la mesa del pack.)
- **Torre de comunicaciones** con cabina amueblada: banco de trabajo con emisora, monitor de cámaras y partes, cama,
  estufa y archivadores. La cocina que traía el pack se pasó a la casa.
- **Generador**: en la escena guardada, al pie de la torre con manguera hasta la pata. **Escrito, pendiente de
  construir:** el modelo de Josemi, en el costado de la leñera de la cabaña, con la manguera hasta la pared de la casa.
- **Tendido eléctrico**: postes a lo largo del sendero con cuatro cables, acometida a la cumbrera del porche de la casa
  y bajada por la pata de la torre. **Escrito, pendiente de construir:** una luz por poste («Luz poste 0…4»), apagadas
  hasta que arranca el generador.
- **Interacción**: 15 objetos (examinar, recoger, generador, radio, monitor, puerta). **Escrito, pendiente de
  construir:** televisor sobre la encimera, cama y ventana usables, bidón fuera junto a la furgoneta.
- **Estética**: cámara pixelada a 540 de altura, tramado 0,15, paleta con «fidelidad» para que la niebla no se aplaste.
- **Primera versión jugable (20/11), escrita y sin probar:** director de la partida con fases y misiones, interfaz
  nueva (misión, subtítulos con hablante, diario, mapa, walkie, tele, tarjetas), sonido (pasos por superficie, ambiente,
  música, voces sintetizadas sin palabras) y el hueco para el vídeo del informativo. Todo en
  [docs/entorno/PRIMERA_VERSION.md](docs/entorno/PRIMERA_VERSION.md).

## 3. Cómo está montado (importante antes de tocar nada)

**La escena no se edita a mano: se genera con código.** Si mueves algo en el editor y luego reconstruyes, se pierde.

- Constructores, en `Assets/Editor/Entorno/`:
  - `BosqueBuilder.cs` — terreno, senderos, árboles, hierba.
  - `BosqueAsentamiento.cs` — casa, torre, generador, muebles y enseres (aquí se recoloca cada objeto).
  - `BosqueTendido.cs` — postes, cables, luces del sendero y manguera del generador.
  - `BosqueComprobacion.cs` — el menú 3 (lista «OK / FALTA»; no cambia nada).
  - `RecursosFogPostprocessor.cs` — ajustes de importación del sonido.
- Menús de Unity:
  - `THE FOG → Bosque → 1. Importar kit de modelos` — solo si cambian modelos o texturas.
  - `THE FOG → Bosque → 2. Construir escena del bosque` — rehace y guarda la escena (semilla fija).
    Si la escena abierta tiene cambios sin guardar, no hace nada: reabrirla antes.
  - `THE FOG → Bosque → 3. Comprobar primera versión` — comprueba escena, recursos, vídeo y modelos opcionales.
- Scripts del juego, en `Assets/Scripts/Entorno/`: `BosqueJuego` (va en Daniel: fases, misiones, mundo),
  `BosqueInterfaz` (toda la pantalla), `BosqueSonido` (todo el sonido), `BosqueGuion` (**todos los textos**),
  `MundoBosque` (medidas del constructor repetidas para el juego: **si se mueve algo en el constructor, cambiarlo
  también ahí**), `Accionable`, `VideoHueco`, `Interactuable`, `Examinable`, `Recogible`, `GeneradorBosque`,
  `RadioBosque`, `MonitorCamaras`, `PuertaAbatible`, `LuzParpadeo`.
- El juego no depende de campos cableados nuevos: encuentra los objetos por nombre o por componente («Televisor»,
  «Cama», «Ventana», «Linterna», «Garrafa de combustible», «Luz poste 0…4»…). No renombrarlos en el constructor.
- En `Asentamiento` y `Tendido` no se puede añadir ninguna llamada a `UnityEngine.Random`: recolocaría todo el bosque.
- Compilar sin abrir Unity: `bash tools/compilar_sin_unity.sh` (debe acabar en `COMPILA`).
- Herramientas de Blender (se ejecutan sin abrir Blender), en `tools/`:
  `bosque_split_pack.py` (vegetación), `bosque_split_edificios.py` (torre, cabaña, objetos),
  `bosque_cocina.py` (corta la encimera del pack para la cocina de la casa y el banco de la torre),
  `bosque_silueta.py`, `tripo_prepare.py` (prepara un `.glb` de Tripo para Unity), `tripo_prepare_usuario.sh` (prepara
  los modelos de Josemi; la tabla dice qué archivo es cada uno) y `maqueta/` (maqueta de la cabaña en Blender para
  comprobar colocaciones sin Unity, con las mismas coordenadas que el constructor; ya incluye la tele y la encimera).
  De la primera versión: `prepara_audio.py` (genera `Assets/Resources/Audio/`), `prepara_ui.py` y `genera_mapa.py`
  (generan `Assets/Resources/UI/`).
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

**Lo primero: probar la primera versión en Unity** (sección 0) y corregir lo que salga. Qué mirar con más cuidado:
[docs/entorno/PRIMERA_VERSION.md](docs/entorno/PRIMERA_VERSION.md), apartados 8 (limitaciones) y 9 (colocaciones a ciegas).

**Esperando modelos de Josemi** (tabla completa, con para qué sirve cada uno, en
[docs/entorno/PENDIENTE_BOSQUE.md](docs/entorno/PENDIENTE_BOSQUE.md)). El nombre de carpeta es
`Assets/Models/Retro/Tripo/<carpeta>/<carpeta>.gltf`; el frente del modelo, mirando a −X. Los diez primeros los coloca
el constructor solo en cuanto existan; sin ellos no pone nada en su lugar (salvo tele y emisora, que usan un modelo
provisional de Poly Haven, ya decidido).

| # | Modelo | Carpeta | Tamaño | Para qué |
|---|---|---|---|---|
| 1 | Farola de camino | `farola_camino` | 4,5–5 m de alto | Las luces del sendero hacia la torre (hoy salen del poste, sin lámpara) |
| 2 | Televisor de tubo, años 80 | `televisor_80` | máx. 40 × 40 × 35 cm | La tele de la cabaña, en la encimera |
| 3 | Diario (cuaderno cerrado) | `diario` | 21 × 15 × 3 cm | Sobre el escritorio; se abre con E |
| 4 | Walkie (de pie, antena corta) | `walkie` | base 7 × 4 cm, 20 cm de alto | Sobre el escritorio |
| 5 | Mapa del monte, clavado | `mapa` | 60 × 45 cm | Pared junto a la puerta, por dentro |
| 6 | Interruptor antiguo | `interruptor` | 8 × 8 × 4 cm | Pared junto a la puerta; comprobar el apagón |
| 7 | Cuadro eléctrico | `cuadro_electrico` | 40 × 50 × 12 cm | Fachada del generador, por fuera |
| 8 | Bombilla colgante | `bombilla` | 25 cm (mejor 55 con el cable) | Techo de la cabaña |
| 9 | Aplique de exterior | `aplique` | 35 cm | Luz del porche |
| 10 | Emisora de base con micrófono | `emisora` | 35 × 15 × 30 cm | Banco de la cabina de la torre |
| 11 | Retrete exterior (caseta de tablas) | `retrete_exterior` | 1,1 × 1,1 × 2,2 m | El baño, detrás de la casa. **Sin sitio escrito todavía** |
| 12 | Palanganero | `palanganero` | 1,3 m de alto | Asearse dentro de la casa. **Sin sitio escrito** |
| 13 | Palomilla de acometida (dos aisladores) | `palomilla` | 40 cm | Donde los cables llegan a la casa y a la torre. **Sin sitio escrito** |

Opcionales: banco para el porche, tajo de cortar leña con hacha, señal oxidada del sendero.

**El vídeo del informativo:** `Assets/StreamingAssets/Video/noticia_valdara.mp4`, 65 segundos; tiempos y frases en el
`LEEME.txt` de esa carpeta. Sin él se ven rótulos con subtítulos.

**Decisiones de la primera versión que Josemi tiene que revisar** (las mismas 19 de PRIMERA_VERSION.md, apartado 10):

1. Generador junto a la cabaña (costado de la leñera), no al pie de la torre; se quitan sus bidones decorativos; la
   manguera va a la pared de la casa.
2. Bidón fuera, junto a la furgoneta.
3. La tele va en la encimera (se mueven tres latas); es `television_02` hasta que exista `televisor_80`.
4. Luces del sendero: una por poste, sin modelo de lámpara hasta que exista `farola_camino`.
5. Año 1987, miércoles 11 → jueves 12 de noviembre; la primera versión es el prólogo.
6. Policía Municipal: cabo Benigno Salcedo. Cadena inventada «Cadena Regional». La torre es «torre de control» /
   Puesto Forestal 03.
7. Walkie con T, mapa con M, diario con J; el walkie solo alcanza el pueblo a través del repetidor de la torre.
8. Final con apagón (`BosqueGuion.FinalConApagon`); con `false`, corte directo a negro.
9. Voces sintetizadas sin palabras (`BosqueSonido.VolumenVoces = 0` las apaga).
10. Música CC BY-SA (Rest!) incluida y acreditada; borrar `Assets/Resources/Audio/Musica/` la quita sin romper nada.
11. `Bosque.unity` pasa a ser la primera escena de Build Settings al reconstruir.
12. El monitor de cámaras ya no es obligatorio; la silueta del sendero no se mueve: queda bajo la luz del poste 2.
13. La cama se puede usar desde que el informativo llega a «El tiempo»; el resto se oye sobre negro.
14. Módulos añadidos al manifest: `video`, `imgui`, `imageconversion`.
15. Dos circuitos de luz: por la tarde solo lucen bombilla, porche y cabina (red del pueblo); sendero, pie de torre y
    generador son el circuito de emergencia y se encienden al arrancar el generador.
16. El informativo es la «última edición» y acaba en «Despedida y cierre»; el cabo es «MUNICIPAL» en los subtítulos
    hasta que dice su nombre.
17. Los pensamientos nuevos de Daniel van en primera persona; las notas que ya estaban siguen en segunda.
18. La tarjeta final acredita interfaz y música sin direcciones web (están en `docs/entorno/ASSETS_EXTERNOS.md`).
19. Durante la llamada Daniel no puede andar ni abrir diario, mapa o walkie; después tiene 20 s antes del final.

**Otras decisiones que tiene que tomar Josemi:**

- Baño fuera (retrete detrás de la casa y palanganero dentro; es la propuesta) o dentro (harían falta inodoro, lavabo,
  ducha y una casa más grande).
- La cama de la cabina de la torre: se queda o se quita.
- Aprobar o corregir la biblia narrativa: [docs/lore/biblia_narrativa_propuesta_1.md](docs/lore/biblia_narrativa_propuesta_1.md).
  Los textos del juego (cuaderno, radio, hoja de mantenimiento, transmisión) salen de ahí.
- Conectar el bosque con Valdara y con el menú (en Build Settings entra solo al reconstruir).

- De los modelos de Josemi del 19/11: qué hacer con el **radiador** (no cuadra con una cabaña de estufa de leña; está
  colocado bajo la ventana), la **pila de litio** (rótulo moderno; sin colocar), el modelo **«libros»** (trae piezas
  flotando; sin colocar: rehacerlo o separarlo en piezas con Blender) y la **planta** (sin colgador; va sobre la librería).

**Sin comprobar:** todo lo de la sección 0. Nada de la tanda del 19/11 noche (muebles) ni de la del 20/11 (primera
versión) se ha visto en Unity: los muebles y la tele, solo en maqueta de Blender; el resto, solo compilado. Tampoco se
repitió la prueba completa en Play tras el retoque anterior de la mesa.

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
