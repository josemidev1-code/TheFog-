# THE FOG — primera versión jugable (el prólogo: «la noche del incidente»)

Escrito el 20/11/2026. Escena: `Assets/Scenes/Bosque.unity`.

## 0. Lo que tienes que saber antes de nada

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
5. **Subido a GitHub el 20/11:** rama `main`, commit `82e0fb0` (todo lo de esta tanda, con la escena reconstruida). El audio
   (`.wav`, `.ogg`) va por Git LFS. La carpeta `.codex/` no se sigue.

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

**Nada de esto se ha visto funcionar.** Unity no se puede abrir en este equipo (sin licencia), así que todo se ha
escrito a ciegas: el código **compila** (comprobado con el compilador de Unity, sin abrir el editor) y las piezas se han
revisado con números y maquetas, pero nadie ha pulsado Play, nadie ha oído un solo sonido y nadie ha visto una pantalla
del juego. La primera vez que abras Unity es la primera prueba de verdad. Cuenta con que habrá cosas que corregir.

**La escena que hay guardada es la vieja.** `Bosque.unity` es anterior a todo esto: no tiene televisor ni luces en los
postes, el bidón está dentro de la cabaña y el generador al pie de la torre. La escena no se edita a mano: la hace el
constructor. Hasta que no ejecutes el **menú 2** se juega una versión recortada (apartado 8).

## 1. Qué tienes que hacer cuando Unity abra

1. **Abrir el proyecto** y esperar. La primera vez tarda: tiene que importar los 14 modelos tuyos de Tripo que aún no
   tienen `.meta` (`Assets/Models/Retro/Tripo/`), todo el sonido (`Assets/Resources/Audio/`, casi cien archivos), las
   imágenes de la interfaz (`Assets/Resources/UI/`, archivos `.bytes`) y las fuentes (`Assets/Resources/Fuentes/`).
   También activará tres módulos que se han añadido a `Packages/manifest.json` (`video`, `imgui`, `imageconversion`):
   son partes del propio Unity, no descarga nada.
2. **Mirar la consola.** No debe haber ningún fallo de compilación en rojo. Si lo hay, no sigas: copia el mensaje.
3. **Menú 1 solo si hace falta:** `THE FOG → Bosque → 1. Importar kit de modelos`. No hace falta si el kit ya estaba
   importado (lo estaba el 19/11). Ejecútalo solo si el menú 2 dice «Falta el kit» o si la cabaña y los árboles salen
   rosas o sin textura.
4. **Menú 2, construir:** `THE FOG → Bosque → 2. Construir escena del bosque`. Si la escena abierta tiene cambios sin
   guardar no hace nada (lo dice en la consola): guarda o reabre la escena y repite. Rehace la escena entera, la guarda
   y la pone la primera en Build Settings. Al acabar ejecuta solo el menú 3.
5. **Menú 3, comprobar:** `THE FOG → Bosque → 3. Comprobar primera versión` (se puede repetir cuando quieras). Escribe en
   la consola una lista «OK / FALTA» de lo que el juego necesita: objetos de la escena, las 10 luces del generador en su
   orden, sonidos, interfaz, fuentes, el vídeo y qué modelos opcionales existen ya. Las líneas con `--` son cosas
   opcionales que aún no están (el vídeo, tus modelos). Si sale alguna línea `FALTA`, apúntala.
6. **Play** con `Assets/Scenes/Bosque.unity` abierta (con esa escena abierta, Play arranca en ella; con cualquier otra
   arranca `TorreDaniel`).
7. **Mirar la consola mientras juegas.** Los avisos que empiezan por `[Bosque]` dicen qué ha faltado (un sonido, un
   objeto). Un mensaje rojo en mitad de la partida es un fallo de verdad: copia el texto.

Si algo se queda atascado, en el editor hay tres teclas de prueba: **F9** salta a la madrugada (desde que tienes la
misión del diario), **F10** (de madrugada) te deja junto al generador con la linterna y el bidón, **F11** te sube a la
cabina de la torre.

## 2. Teclas

| Tecla | Qué hace |
|---|---|
| W A S D, ratón | andar y mirar |
| Mayús | correr |
| **E** | usar lo que tienes delante; cerrar el diario, una nota, la tele o el walkie |
| **F** | linterna (cuando la hayas cogido) |
| **J** | diario |
| **M** | mapa (y cerrarlo) |
| **T** | walkie |
| A / D | pasar página del diario, cambiar de canal del walkie |
| INTRO | acostarse, solo si la escena no tiene cama usable (escena sin reconstruir) |
| F1 | ayuda de controles |
| ESC | soltar el ratón · Q sale (en la tarjeta final) |
| F9 / F10 / F11 | atajos de prueba, **solo dentro del editor** |

## 3. El guion, paso a paso

1. **Tarde del miércoles 11 de noviembre de 1987.** Tarjeta «VALDARA». Daniel está en la cabaña, con luz de la red del
   pueblo (bombilla, porche y, a lo lejos, la cabina de la torre). Misión: **«Abre el diario»** (tecla J, o el diario
   del escritorio cuando exista el modelo).
2. **El diario.** Cuatro entradas: ha vuelto a Valdara catorce años después, se ha sacado el título de guarda forestal y
   vigilante, le ha salido la plaza en la torre de control (Puesto Forestal 03) y el guarda que se jubila le ha dicho
   que, si se va la luz, lo primero es el generador. Si no lo abres, a los 3 minutos el juego sigue igualmente.
3. **El informativo.** Primero suena el walkie (el relevo, por el canal 3). Después la tele da la «última edición» de
   Cadena Regional (cadena inventada): feria de ganado, el coche de línea, **temblores en Valdara que la empresa de la
   mina atribuye a galerías viejas**, y el tiempo: nieblas densas. Se oye por toda la cabaña, con subtítulos. Si te
   acercas y pulsas E ves la pantalla (el vídeo si existe; si no, los rótulos de la cadena).
4. **Acostarse.** Desde que empieza «El tiempo» se puede usar la cama. Si no te acuestas, a los 4 minutos Daniel se
   duerme solo (si está dentro de la cabaña).
5. **Madrugada del jueves 12, 04:17.** En negro: una explosión lejana y se va la luz. Daniel despierta de pie junto a
   la cama, con más niebla y más oscuridad. Misión: **«Comprueba por qué no hay luz»**: probar la tele, la radio, mirar
   por la ventana (y el interruptor y el cuadro cuando existan los modelos). Con dos comprobaciones Daniel concluye que
   es un apagón general. Coger la linterna no lo da por comprobado. Si sales sin comprobar, o pasan 90 segundos, lo
   concluye igualmente.
6. **Linterna** (mesilla, tecla F) → **bidón** (fuera, junto a la furgoneta) → **generador** (en el costado de la
   cabaña, junto a la leñera): **«Llena el generador de gasolina»**, repostar y tres tirones. Nada tiene orden
   obligatorio: la misión que se enseña sale de lo que falte por hacer.
7. **Vuelve la luz**, por orden: la del generador, la bombilla, el porche, **los cinco postes del sendero uno a uno
   hacia la torre**, el pie de la torre y la cabina. Después se oye una voz lejana y el walkie suelta una llamada rota.
   Misión: **«Ve a la torre de control»**.
8. **Camino a la torre.** En el sendero hay una silueta bajo la luz del tercer poste; desaparece al alumbrarla o al
   acercarse. Desde el pie de la torre se oye llamar a la emisora. Si te pierdes, el walkie insiste cada 2 minutos y el
   mapa marca la torre.
9. **La llamada.** En la cabina, usar la emisora: el cabo Benigno Salcedo, de la Policía Municipal, cuenta que ha habido
   **una explosión en la mina** y que **todo Valdara está sin luz ni teléfono**. Mientras habla no se puede andar ni
   abrir nada; la niebla espesa y una voz que no es la suya se cuela en el canal.
10. **Final.** Nueva entrada en el diario, 20 segundos para leerla, se va la luz de la cabina, corte a negro y tarjeta
    «THE FOG · Fin de la primera versión · Continuará».

Todos los textos del juego están en un solo archivo: `Assets/Scripts/Entorno/BosqueGuion.cs`. Para cambiar una frase,
se cambia ahí.

## 4. El vídeo del informativo

- Va en `Assets/StreamingAssets/Video/noticia_valdara.mp4` (nombre y carpeta exactos). En esa carpeta hay un
  `LEEME.txt` con los tiempos.
- Si el archivo existe, el juego lo reproduce al mirar la tele y usa su sonido. Si no existe, se ven los rótulos de la
  cadena, suena una voz de locutor sintetizada (sin palabras) y salen los subtítulos. **Hoy no existe: es lo que verás.**
- Tiempos (desde que arranca el vídeo): 0–3,6 s sintonía; las diez frases empiezan en 3,6 / 8,95 / 15,8 / 22,15 / 29,0 /
  35,85 / 42,7 / 49,05 / 54,9 / 60,75 s; fin a los 65,1 s. Las frases exactas, en `LEEME.txt` y en `BosqueGuion.Noticia`.

## 5. Modelos que faltan (para hacer en Tripo)

Norma: todo lo colocado es un modelo 3D y **si falta un modelo tuyo no se sustituye por otro**. El constructor coloca
cada uno de estos **solo cuando exista** `Assets/Models/Retro/Tripo/<carpeta>/<carpeta>.gltf`; mientras no exista, no
pone nada en su sitio y el juego funciona sin él. Solo hay dos excepciones, que ya estaban decididas: la tele usa
`television_02` (Poly Haven) hasta que exista `televisor_80`, y la emisora de la torre sigue con
`vintage_radio_transceiver` (Poly Haven) hasta que exista `emisora`.

Cómo se meten: dejas el `.glb` en `Desktop/thefogjuego/ModelosTheFog`, se añade una línea a la tabla de
`tools/tripo_prepare_usuario.sh` (`carpeta|triángulos|altura en metros|modo|archivo`) y se ejecuta
`bash tools/tripo_prepare_usuario.sh <carpeta>`. Eso deja el modelo en su carpeta, a escala y con el pivote en el
centro de la base. Después, menú 2 otra vez. **El frente del modelo tiene que mirar hacia −X** (como los muebles que ya
hiciste): todas las colocaciones cuentan con eso.

| # | Qué es | Carpeta (nombre exacto) | Tamaño | Para qué y dónde aparece |
|---|---|---|---|---|
| 1 | **Farola de camino** (poste o brazo con lámpara, de pueblo, años 80, oxidada) | `farola_camino` | 4,5–5 m de alto | Las luces que guían a la torre. Una junto a cada poste del tendido (5), al borde del sendero; la luz sale de su cabeza. **Sin ella la luz sale del poste, sin lámpara.** |
| 2 | **Televisor** de tubo, portátil, años 80 | `televisor_80` | 40 cm de ancho × 40 de alto × 35 de fondo, como mucho (es el hueco que hay) | La tele de la cabaña, sobre la encimera. Sustituye a la provisional. |
| 3 | **Diario** (cuaderno de tapas duras, cerrado) | `diario` | 21 × 15 × 3 cm, tumbado | Sobre el escritorio. Se abre con E (hoy solo con J). |
| 4 | **Walkie** (transmisor de mano, con antena corta) | `walkie` | 7 × 4 cm de base, 20 cm de alto, **de pie** | Sobre el escritorio, al lado del detector. Se coge con E (hoy solo con T). |
| 5 | **Mapa** del monte de Valdara, clavado con chinchetas | `mapa` | 60 cm de ancho × 45 de alto, 1–2 cm de fondo | Pared de la puerta, por dentro, entre la puerta y la ventana. Se mira con E (hoy solo con M). |
| 6 | **Interruptor** de la luz, antiguo (porcelana o baquelita) | `interruptor` | 8 × 8 × 4 cm | Pared de la puerta, por dentro, al otro lado. Enciende y apaga la bombilla; de madrugada sirve para comprobar el apagón. |
| 7 | **Cuadro eléctrico** / caja de fusibles | `cuadro_electrico` | 40 cm de ancho × 50 de alto × 12 de fondo | Fachada lateral de la cabaña, por fuera, encima de donde entra la manguera del generador. Otra forma de comprobar el apagón. |
| 8 | **Bombilla** colgante (cable, casquillo y bombilla desnuda) | `bombilla` | 25 cm (mejor con el cable largo: 55 cm en total, ver apartado 9) | Centro del techo de la cabaña: la luz de dentro saldrá de ella. Hay foto de referencia en `Desktop/thefogjuego/fotosmodelosthefog/`. |
| 9 | **Aplique** de exterior (chapa oxidada con pantalla) | `aplique` | 35 cm | Fachada de la puerta, por fuera: la luz del porche saldrá de él. |
| 10 | **Emisora** de base, años 80, con micrófono de mano | `emisora` | 35 cm de ancho × 15 de alto × 30 de fondo | Banco de la cabina de la torre. Sustituye a la provisional. |
| 11 | **Retrete exterior** (caseta de tablas, tejadillo de chapa, puerta cerrada) | `retrete_exterior` | 1,1 × 1,1 × 2,2 m | El baño, detrás de la casa. **El constructor todavía no lo coloca** (ver abajo). |
| 12 | **Palanganero** (mueble lavabo antiguo con palangana, jarra y espejo) | `palanganero` | 1,3 m de alto | Para asearse, dentro de la casa. **Todavía no se coloca.** |
| 13 | **Palomilla de acometida** (soporte de pared con dos aisladores) | `palomilla` | 40 cm | Donde los cables llegan a la cumbrera de la casa y a la pata de la torre. **Todavía no se coloca.** |

Los diez primeros tienen su sitio escrito en el constructor y aparecen solos al reconstruir. Los tres últimos (11, 12
y 13) son de la lista anterior al prólogo y **no tienen colocación escrita**: cuando existan habrá que decidir el sitio
y añadirlo a `BosqueAsentamiento.cs` (los nombres de carpeta de esos tres son una propuesta).

Aviso sobre el televisor: para que el vídeo se vea en la propia tele (y no solo en la pantalla de «mirar la tele») el
modelo necesita una pieza suelta cuyo nombre empiece por `Pantalla`. Tripo entrega una sola malla y
`tools/tripo_prepare.py` une todas las piezas en una: habrá que separar el cristal a mano en Blender después de
prepararlo. Sin esa pieza la tele funciona igual que la provisional.

## 6. Sonido

- Todo el sonido está en `Assets/Resources/Audio/`. Parte sale de los packs descargados (pasos, viento, grillos,
  puertas, música) y parte está sintetizado con `tools/prepara_audio.py` (generador, estática, explosión, tele…).
- Las **voces** (locutor, cabo, relevo y la voz intrusa) son sintetizadas y **no dicen palabras**: es un murmullo con
  ritmo de habla; lo que dicen va en los subtítulos. Si no te convencen, `BosqueSonido.VolumenVoces = 0` las apaga
  (campo estático en `Assets/Scripts/Entorno/BosqueSonido.cs`); `VolumenMusica = 0` apaga la música.
- La música es de «Rest!» (CC BY-SA 4.0). Si se borra la carpeta `Assets/Resources/Audio/Musica/` el juego funciona
  igual, sin música.
- `Assets/Editor/Entorno/RecursosFogPostprocessor.cs` ajusta la importación del audio (música en streaming, bucles sin
  comprimir). Si el audio se importó antes de que ese archivo compilara, clic derecho en `Assets/Resources/Audio` →
  **Reimport**. No es imprescindible.

## 7. Créditos

- Interfaz: **PSX Horror UI Free**, de heyheythere — CC BY 4.0.
- Música: **Distant Screams: Dark Ambient Music Pack**, de Rest! — CC BY-SA 4.0.
- Efectos: Free Horror SFX (liminal-space-dev, CC0), grillos de OpenGameArt (CC0).
- Bosque, cabaña y torre: kit «Vegetation» de Elbolilloduro (CC0). Tele y emisora provisionales: Poly Haven (CC0).
- Fuentes: Barlow Condensed y VT323 (OFL), Special Elite (Apache 2.0).

La tarjeta final del juego nombra interfaz y música con autor y licencia, sin direcciones web. Las direcciones y el
detalle están en [ASSETS_EXTERNOS.md](ASSETS_EXTERNOS.md).

## 8. Limitaciones conocidas

- **Sin reconstruir la escena (menú 2) se juega una versión recortada:** no hay televisor (el informativo se salta con
  un aviso en la consola), la garrafa está dentro de la cabaña y el generador al pie de la torre. El juego cambia los
  textos de misión y de pensamiento para no mentir, pero la entrada del diario del apagón sigue diciendo «junto a la
  furgoneta». Las luces de los postes las crea el propio juego al arrancar.
- **Con `television_02` el vídeo del informativo solo se ve en la pantalla de «mirar la tele».** El modelo no tiene una
  malla «Pantalla…» ni emisión, así que de lejos parece apagada salvo por la luz azulada. Se arregla con `televisor_80`
  (y su pieza `Pantalla`, ver apartado 5).
- **Vídeo en un ejecutable:** `ProjectSettings` tiene `stripEngineCode: 1`. Si se compila con IL2CPP, el `VideoPlayer`
  (que el juego usa solo por reflexión) puede eliminarse del ejecutable. Cuando exista el vídeo y se vaya a compilar,
  hay que añadir `Assets/link.xml` conservando `UnityEngine.VideoModule`. En el editor no afecta.
- **Reconstruir cambia el bosque más de lo que parece.** Al mover el generador y el bidón cambia la lista de sitios
  ocupados, y las rocas, troncos, matas y hierba solo gastan números al azar cuando un punto queda libre: a partir de
  la primera diferencia **se recoloca todo lo que no son árboles** (rocas, troncos, tocones, matas, hierba). Los árboles
  se conservan, porque todo lo que se ha movido cae dentro de los claros, donde no se plantan.
- **La silueta del sendero queda bajo la luz del poste 2** (a 5,7 m). Su material no recibe luz, así que debería verse
  como un recorte negro contra el suelo alumbrado. No se ha movido porque moverla sí recolocaría los árboles.
- **Colocaciones de pared pendientes de ajuste.** Los modelos de pared se colocan con el centro de su base en un punto
  calculado con las medidas del muro: `mapa` e `interruptor` en x = 1,58 (4 cm por delante de la cara interior),
  `cuadro_electrico` en z = −2,80 (7 cm por fuera de la fachada) y `aplique` en x = 2,10 (4 cm por fuera). Según el
  fondo real de cada modelo pueden quedar uno o dos centímetros separados de la pared o metidos en ella.
- **Las luces del sendero salen del poste, sin lámpara,** hasta que exista `farola_camino`.
- **La emisión de los cristales** (ventanas que se ven encendidas desde fuera) puede no verse en un ejecutable. Solo
  afecta al brillo de las ventanas.
- **Retrete, palanganero y palomilla** no tienen colocación escrita (apartado 5).

## 9. Colocaciones hechas a ciegas (revisar en Unity)

Coordenadas locales de la cabaña (x hacia la puerta, z hacia la pared de la cama), salvo que se diga otra cosa.

| Qué | Dónde | Cómo se ha comprobado | Qué mirar |
|---|---|---|---|
| Televisor | `(-1.60, encimera, -1.95)`, mirando a la habitación | Maqueta de Blender (`tools/maqueta/`, vistas `v6_tele` y `v7_encimera`): apoya en el tramo liso, no en el fregadero; no corta a nada. Queda a 3,4 cm de la olla | Que se vea bien desde la cama y desde la puerta |
| Tres latas | delante de los platos (`-2.47 / -2.32 / -2.40`, z ≈ −1,6) | Misma maqueta: no se cortan con la taza (2,2 cm) ni con los platos | Que no cuelguen del borde |
| Bidón | fuera, a 1,5 m del costado de la furgoneta, a 7,7 m de la puerta | Cálculo con las medidas de la furgoneta (2,17 × 5,36 m) | Que se vea desde el porche de noche |
| Generador | costado de la leñera, a 1,7 m de la pared | Cálculo: a 2,95 m de la leña, a 0,9 m de la caja de herramientas | Que no pise la pala ni la leñera; el giro |
| Manguera | del generador a la pared, sube 35 cm por los troncos | Solo cálculo | Que no flote ni se entierre |
| Luces de poste | a 4,6 m de alto, 0,9 m hacia el sendero, sin sombras | Solo cálculo | Intensidad (5,5) y alcance (16 m): que alumbren el sendero sin quemar |
| Daniel al despertar | de pie junto a la cama, mirando a la tele | Solo cálculo (lo coloca el juego) | Que no aparezca dentro de un mueble |
| Diario y walkie | sobre el escritorio (`0.62, -1.68` y `1.08, -1.66`) | Medidas del escritorio, la radio y el detector | Solo cuando existan los modelos |
| Bombilla | base a 2,05 m del suelo, centro de la habitación | El techo empieza hacia 2,6 m: con un modelo de 25 cm quedará colgando del aire 30 cm o más | Subirla (o hacer el cable más largo) |

## 10. Decisiones tomadas que tienes que revisar

1. **Generador junto a la cabaña** (costado de la leñera), no al pie de la torre; se quitan sus bidones decorativos. La
   manguera va del generador a la pared de la casa.
2. **Bidón fuera, junto a la furgoneta** (el diario cuenta que Daniel le echó gasolina a la furgoneta el martes).
3. **La tele va en la encimera**, en el tramo liso (se mueven tres latas); es `television_02` hasta que exista `televisor_80`.
4. **Luces del sendero:** una luz por poste, sin modelo de lámpara hasta que exista `farola_camino`.
5. **Año 1987**, miércoles 11 → jueves 12 de noviembre; la primera versión es el prólogo (la noche del incidente).
6. **Policía Municipal:** cabo Benigno Salcedo. Cadena inventada «Cadena Regional». La torre es «torre de control» /
   Puesto Forestal 03.
7. **Walkie con T, mapa con M, diario con J** (en vez del «móvil», que en 1987 no cuadra); el walkie solo alcanza el
   pueblo a través del repetidor de la torre.
8. **Final con apagón** (`BosqueGuion.FinalConApagon`): tras la llamada se va la luz de la cabina y corta a negro. Con
   `false`, corte directo.
9. **Voces sintetizadas sin palabras** para locutor, cabo, relevo y voz intrusa (`BosqueSonido.VolumenVoces = 0` las apaga).
10. **Música CC BY-SA** (Rest!) incluida y acreditada; borrar `Assets/Resources/Audio/Musica/` la quita sin romper
    nada. La licencia obliga a «compartir igual»: decide tú si se queda si el juego se vende.
11. **`Bosque.unity` pasa a ser la primera escena de Build Settings** al reconstruir.
12. El monitor de cámaras sigue, pero ya no es obligatorio; la silueta del sendero aparece en el camino a la torre, sin
    moverla de sitio: queda bajo la farola del poste 2 y se ve como un recorte negro (moverla recolocaría los árboles).
13. La cama se puede usar desde que el informativo llega a «El tiempo»; el resto de la noticia se oye sobre negro.
14. Módulos añadidos al manifest: `video`, `imgui`, `imageconversion`.
15. **Dos circuitos de luz:** por la tarde solo lucen la bombilla, el porche y la cabina de la torre (red del pueblo).
    Las luces del sendero, la del pie de la torre y la del generador son el circuito de emergencia y se encienden por
    primera vez al arrancar el generador.
16. El informativo es la **«última edición»** y la cadena se despide con **«Despedida y cierre»**; el cabo aparece como
    **«MUNICIPAL»** en los subtítulos hasta que dice su nombre.
17. Los pensamientos nuevos de Daniel van en **primera persona** («Dejo la tele puesta», «Cojo el micrófono»). Las notas
    que ya estaban en la escena (aparato del abuelo, parte de incidencias) siguen en segunda persona: no se han tocado.
18. La tarjeta final acredita interfaz y música con autor y licencia, sin direcciones web (están en `ASSETS_EXTERNOS.md`).
19. Durante la llamada del cabo Daniel no puede andar ni abrir diario, mapa o walkie; después tiene 20 s (o lo que
    tarde en cerrar el diario) antes del final.

## 11. Cómo está hecho (para quien continúe)

Cuatro partes, escritas a la vez por cuatro implementadores que no podían hablar entre sí, contra un contrato cerrado:

| Parte | Archivos |
|---|---|
| Flujo de juego | `Assets/Scripts/Entorno/BosqueJuego*.cs` (director, va en Daniel), `BosqueGuion.cs` (textos), `MundoBosque.cs` (medidas), `Accionable.cs`, `VideoHueco.cs`, `GeneradorBosque.cs`, `EntornoWalker.cs` |
| Interfaz | `BosqueInterfaz*.cs`, `UiFog.cs`; `tools/prepara_ui.py`, `tools/genera_mapa.py`; `Assets/Resources/UI/`, `Assets/Resources/Fuentes/` |
| Sonido | `BosqueSonido.cs`, `SintesisBosque.cs`; `tools/prepara_audio.py`; `Assets/Resources/Audio/` |
| Constructor y editor | `Assets/Editor/Entorno/BosqueBuilder.cs`, `BosqueAsentamiento.cs`, `BosqueTendido.cs`, `BosqueComprobacion.cs` (menú 3), `RecursosFogPostprocessor.cs`; `Packages/manifest.json`; `Assets/StreamingAssets/Video/LEEME.txt` |

- La escena no gana ningún campo serializado nuevo: el juego encuentra lo que necesita por nombre o por componente
  (`Televisor`, `Cama`, `Ventana`, `Linterna`, `Garrafa de combustible`, `Radio de casa`, `Emisora de la torre`,
  `Luz poste 0`…`4`, el `GeneradorBosque`) y sigue adelante si algo no está.
- `MundoBosque.cs` repite en el juego medidas del constructor (dónde están el generador, el bidón, los postes, la tele).
  **Si se cambia una posición en el constructor hay que cambiarla también ahí.**
- En el constructor no se puede añadir ninguna llamada a `UnityEngine.Random` dentro de `Asentamiento` ni de `Tendido`:
  cambiaría todo el bosque (se planta con semilla 1987 después de ejecutarlas).
- Compilar sin abrir Unity: `bash tools/compilar_sin_unity.sh` desde la raíz del proyecto. Debe acabar en `COMPILA`.
  Solo comprueba que compila.

## 12. Estado de las comprobaciones

Estado real al escribir esto, el 20/11/2026:

- **Constructor y editor:** compilan aislados contra el contrato (`compila_parcial.sh D` → `COMPILA (rol D)`). No se ha
  ejecutado nunca: el menú 2 con estos cambios no ha corrido ni una vez.
- **Proyecto entero:** `bash tools/compilar_sin_unity.sh` → `COMPILA` (69 archivos de juego y 33 de editor, 0 fallos)
  en el momento de escribir esto, con las otras tres partes todavía pudiendo recibir retoques. **Repetirlo al
  integrar**: es la única comprobación que existe.
- **Maqueta de Blender:** la tele y lo que hay en la encimera (apartado 9). Nada más del prólogo tiene maqueta.
- **En Unity:** nada. Ni importación, ni construcción, ni Play.
- **Informes de los otros tres implementadores** (qué no han podido cumplir de la especificación): no estaban
  disponibles al escribir este documento; quien integre debe añadirlos aquí.

Lo que el constructor no cumple o deja abierto:

- Retrete exterior, palanganero y palomilla no tienen colocación (no estaban en el encargo del prólogo).
- El cuadro eléctrico, la bombilla y el aplique solo tienen sitio en la cabaña; al pie de la torre, en la cabina y
  junto al generador la luz sigue saliendo del aire.
- `televisor_80` necesitará separar a mano la pieza `Pantalla` para que el vídeo se vea en el propio aparato.
- `tools/maqueta/cabana_layout.json` se ha puesto al día (sin el bidón, con la tele y la encimera), pero las imágenes
  `docs/entorno/maqueta_casa_*.png` son las del 19/11: no enseñan la tele.
