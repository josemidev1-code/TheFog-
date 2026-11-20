# Bosque — estado y pendientes (20/11/2026)

Archivo de relevo: lo lee la tarea programada de las 12:55 si la sesión de la mañana se quedó sin límite.
Actualizar al terminar cada bloque.

## PRIMERA VERSIÓN JUGABLE (pedido de Josemi, 19/11 noche) — ESCRITA, SIN PROBAR EN UNITY

**Estado (20/11):** ya no está «sin empezar». Está escrita entera (flujo de juego, interfaz, sonido y constructor de la
escena) pero **solo se ha comprobado que compila**: Unity no abre en este equipo (licencia), así que no se ha
construido la escena, no se ha pulsado Play y nadie ha visto ni oído nada. Cómo probarla, el guion, las teclas, las
limitaciones y las decisiones que Josemi tiene que revisar: **[PRIMERA_VERSION.md](PRIMERA_VERSION.md)**.

**Pendiente, por orden:**

1. **Probarla en Unity.** Abrir el proyecto, esperar la importación, `THE FOG → Bosque → 2. Construir escena del
   bosque` (el menú 1 solo si dice que falta el kit o salen modelos sin textura), `3. Comprobar primera versión`, y
   Play con `Bosque.unity` abierta. Sin el menú 2 se juega una versión recortada sobre la escena vieja.
2. **Corregir lo que salga** en esa primera prueba (colocaciones hechas a ciegas, volúmenes, tamaños de letra, tiempos).
   Lista de qué mirar: PRIMERA_VERSION.md, apartados 8 y 9.
3. **Modelos de Josemi** para Tripo: lista de abajo («Modelos que faltan»). El constructor los coloca solos cuando existan.
4. **El vídeo del informativo** (`Assets/StreamingAssets/Video/noticia_valdara.mp4`): lo hace Josemi; tiempos en el
   `LEEME.txt` de esa carpeta. Mientras no exista se ven rótulos con subtítulos.
5. **Decisiones de Josemi:** las 19 de PRIMERA_VERSION.md, apartado 10 (generador junto a la cabaña, bidón fuera,
   walkie en vez de móvil, cabo Salcedo, música CC BY-SA…).

Dudas del 19/11 ya resueltas (a falta de que Josemi las apruebe): el «móvil» es un **walkie** (tecla T); quien habla en
la torre es el **cabo Benigno Salcedo**, de la Policía Municipal, por la emisora; la «mañana siguiente» es la
**madrugada** (04:17) del jueves 12 de noviembre de 1987.

Lo que se pidió, en orden (hasta aquí llega la primera versión):

1. Daniel aparece en la cabaña. El juego le dice que **abra el diario**. El diario cuenta que ha vuelto a Valdara, su
   pueblo de la infancia, después de sacarse el título de guardabosques-vigilante, porque le ha salido trabajo de
   vigilante en la torre de control.
2. En la **tele** sale una noticia española (estilo telediario de los 80, pueblo rural) que Daniel oye de fondo.
   El vídeo lo hará Josemi (quizá con Adobe Firefly); **aún no existe**.
3. A la mañana siguiente se levanta y **hay un apagón**; tiene que comprobarlo de alguna forma.
4. Coge la linterna, sale, encuentra el combustible y **rellena el generador**. Se encienden las luces de la casa y
   farolas o luces que le llevan hacia la torre.
5. Llega a la torre y **alguien le dice** que ha habido una explosión en la mina que ha afectado a todo el pueblo y que
   se ha ido la luz en todo Valdara. Fin de la primera versión.

Además: **misiones** en pantalla según el momento («Llena el generador de gasolina», «Ve a la torre de operaciones»),
un **mapa** y un **móvil**, interfaces con la estética del juego, **subtítulos** bien hechos, efectos de sonido y
ambientación de misterio y terror psicológico.

Todo eso está escrito (20/11): diario al empezar, tele con informativo, paso a la madrugada, comprobación del apagón,
luces que guían a la torre, mapa, walkie, la llamada con la explosión de la mina, interfaz y subtítulos nuevos y el
sonido (`Assets/Resources/Audio/`). Qué assets externos se han usado: `ASSETS_EXTERNOS.md`.

## AVISO (19/11 noche, sigue en pie)

Los muebles de Josemi (15 modelos en `Desktop/thefogjuego/ModelosTheFog`) están preparados y colocados en el código,
pero **falta construir la escena en Unity** (no pasó del aviso de licencia). El menú 2 construye a la vez esos muebles
y todo lo de la primera versión. Ninguno de esos 15 es de la lista de «Modelos que faltan» de abajo.
De la bombilla ya hay foto de referencia (`Desktop/thefogjuego/fotosmodelosthefog/`).

## Qué se pidió (Josemi, 19/11 por la mañana)

- Escena de bosque **aparte de Valdara** (`Assets/Scenes/Bosque.unity`), jugable, bien ambientada.
- Dentro: casa de Daniel con interior y puerta, torre de comunicaciones con interior, generador, entorno.
- **Todo modelos 3D**, nada de formas geométricas sueltas. Estética fusión MOLDRISE + No, I'm Not a Human,
  menos pixelada que antes (cámara altura 540, tramado 0,15). Misterio, inquietud.
- Interacción con el entorno y un lore que enganche (hablarlo con ChatGPT, app de escritorio, modo Chat).
- Modelos exclusivos: imagen en ChatGPT → Tripo AI (web) → descargar → integrar. Quedan ~100 créditos de Tripo.
- No hacer commit salvo que lo pida. Armoury Crate / RGB: descartado por él.

## Hecho

- Kit CC0 de Elbolilloduro («Vegetation»): `tools/bosque_split_pack.py` (vegetación) y
  `tools/bosque_split_edificios.py` (torre de vigía con cabina amueblada, cabaña con interior, coche, props).
  Modelos en `Assets/Models/Retro/Bosque/Modelos` y `.../Edificios`, texturas en `.../Texturas`.
- `Assets/Editor/Entorno/BosqueBuilder.cs` + `BosqueAsentamiento.cs`: menús `THE FOG/Bosque/1. Importar kit`
  y `2. Construir escena`. Terreno propio, senderos, 3 claros, casa en (11,-8), torre en (-100,-8),
  generador, postes, enseres, luces.
- Scripts de juego en `Assets/Scripts/Entorno/`: `BosqueJuego` (va en Daniel), `Interactuable`, `Examinable`,
  `Recogible`, `GeneradorBosque`, `RadioBosque`, `MonitorCamaras`, `PuertaAbatible`, `LuzParpadeo`.
- Secuencia acordada con ChatGPT (primeros 10 min): despertar en casa sin luz → linterna → garrafa →
  sendero (silueta que desaparece) → generador → cabina → cámaras (figura junto a la casa) → emisora.

## Pendiente anterior al prólogo (lista del 19/11; lo de la primera versión está arriba)

Ojo: lo que sigue describe la secuencia vieja (despertar sin luz → generador al pie de la torre). El prólogo la
sustituye; el generador pasa al costado de la cabaña al reconstruir.

Hecho ya (19/11, mañana): puntos 1 a 4 de la lista original (compila, cableado, Play probado de principio a fin,
silueta, filtro con `fidelidad`, capturas, biblia narrativa guardada en `docs/lore/`) y la entrada del diario.

1. Modelos exclusivos con Tripo. **Hecho el detector del abuelo** (imagen de ChatGPT en `ArtSource/Tripo/ref/detector_abuelo.png`
   -> Tripo v3.1 por CLI, 30 creditos -> `Assets/Models/Retro/Tripo/detector`, colocado sobre la comoda de la casa y examinable).
   Quedan 70 creditos. El fragmento del mineral queda **sin hacer hasta que Josemi lo pida** (aun no tiene sitio en la escena).
2. Mejoras de ambiente si sobra tiempo: señal oxidada de mantenimiento en el sendero (falta modelo), más enseres
   en la cabina (plano de Valdara, cuadro eléctrico), sonido de pasos.
3. Decisiones de Josemi: aprobar o corregir la biblia narrativa; conectar el bosque con Valdara y con el menú.
   **Decidido (19/11): la casa de Daniel se queda como cabaña de troncos.** No cambiarle las texturas.

Si no hay nada más que hacer de esta lista sin su decisión, no tocar nada.

## Modelos que faltan (pedírselos a Josemi; él los hace en Tripo)

Norma suya del 19/11 a mediodía: si falta un modelo no se inventa ni se sustituye por otro; se le pregunta.

Lista al 20/11: los seis que se le pidieron el 19/11 más los siete que pide la primera versión jugable. Deja el `.glb`
en `Desktop/thefogjuego/ModelosTheFog`; se preparan con `tools/tripo_prepare_usuario.sh` (añadiendo una línea a su
tabla) y quedan en `Assets/Models/Retro/Tripo/<carpeta>/<carpeta>.gltf`. **El frente del modelo, mirando a −X.**
El nombre de carpeta tiene que ser exactamente el de la tabla: el constructor busca ese archivo y, si existe, coloca
el modelo solo al reconstruir (menú 2). Mientras no exista no pone nada en su lugar.

| # | Modelo | Carpeta | Tamaño | Para qué | ¿Tiene sitio en el constructor? |
|---|---|---|---|---|---|
| 1 | **Farola de camino** (poste o brazo con lámpara, de pueblo, oxidada) | `farola_camino` | 4,5–5 m de alto | Las luces del sendero que guían a la torre: una por poste (5). Hoy la luz sale del poste, sin lámpara | Sí |
| 2 | **Televisor** de tubo, portátil, años 80 | `televisor_80` | máx. 40 × 40 × 35 cm (ancho × alto × fondo) | La tele de la cabaña (hoy, provisional de Poly Haven) | Sí |
| 3 | **Diario** (cuaderno de tapas duras, cerrado) | `diario` | 21 × 15 × 3 cm | Sobre el escritorio; abre el diario con E | Sí |
| 4 | **Walkie** (transmisor de mano, antena corta, de pie) | `walkie` | base 7 × 4 cm, 20 cm de alto | Sobre el escritorio; abre el walkie con E | Sí |
| 5 | **Mapa** del monte, clavado con chinchetas | `mapa` | 60 × 45 cm, 1–2 cm de fondo | Pared junto a la puerta, por dentro; abre el mapa con E | Sí |
| 6 | **Interruptor** antiguo de la luz | `interruptor` | 8 × 8 × 4 cm | Pared junto a la puerta; comprobar el apagón | Sí |
| 7 | **Cuadro eléctrico** / caja de fusibles | `cuadro_electrico` | 40 × 50 × 12 cm | Fachada del generador, por fuera; comprobar el apagón | Sí (solo en la casa; el del pie de la torre, no) |
| 8 | **Bombilla colgante** (cable, casquillo y bombilla desnuda) | `bombilla` | 25 cm (mejor 55 cm con el cable largo) | Techo de la cabaña: la luz de dentro sale del aire | Sí (solo en la casa; la de la cabina, no) |
| 9 | **Aplique de exterior** (chapa oxidada con pantalla) | `aplique` | 35 cm | Luz del porche | Sí (solo el porche; pie de torre y generador, no) |
| 10 | **Emisora** de base, años 80, con micrófono de mano | `emisora` | 35 × 15 × 30 cm | Banco de la cabina de la torre (hoy, provisional de Poly Haven) | Sí |
| 11 | **Retrete exterior** (caseta de tablas, tejadillo de chapa, puerta cerrada) | `retrete_exterior` | 1,1 × 1,1 × 2,2 m | El baño, detrás de la casa | **No:** falta decidir el sitio y escribirlo |
| 12 | **Palanganero** (mueble lavabo antiguo con palangana, jarra y espejo) | `palanganero` | 1,3 m de alto | Para asearse, dentro de la casa | **No** |
| 13 | **Palomilla de acometida** (soporte de pared con dos aisladores) | `palomilla` | 40 cm | Punta de la cumbrera del porche y pata de la torre, donde los cables acaban hoy en la madera y el metal | **No** |

(En el proyecto hay `industrial_wall_lamp` y `security_light` de Poly Haven; no se han colocado como aplique: sería
sustituir un modelo suyo por otro. Decide él si valen.)

Opcionales de antes: banco para el porche (está vacío), tajo de cortar leña con hacha junto a la leñera, señal oxidada
del sendero. El «plano de Valdara» de la lista anterior es ahora el `mapa` (n.º 5).

Decisiones suyas pendientes: baño fuera (propuesta) o dentro (haría falta inodoro, lavabo, ducha y una casa mayor); si la cama de la cabina de la torre se queda o se quita.

La cocina no necesita modelos: sale del propio pack (`tools/bosque_cocina.py`).

Cosas del pack que se dejan como vienen (avisado): la cabina tiene cinco archivadores que tapan media ventana, dos fregaderos y la estufa pegada a la cocina.
