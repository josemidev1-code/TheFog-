# Entorno de Daniel — diario de construcción

Escena: `Assets/Scenes/TorreDaniel.unity`. Objetivo: la torre de vigilancia, la casa de Daniel y el monte que las rodea, apartados del pueblo, **todo con modelos de Tripo** y la estética común ([../retro/README.md](../retro/README.md)): España rural de finales de los 80, realista, gastado, desaturado, nada de cartoon.

Las entradas van de la más reciente a la más antigua.

## Dónde lo dejamos (19/11/2026, mañana) — escena del bosque

- **Para jugar el bosque:** abrir `Assets/Scenes/Bosque.unity` y pulsar Play (con esa escena abierta, Play arranca en ella; con cualquier otra sigue arrancando `TorreDaniel`). WASD, ratón, Mayús correr, **E usar**, **F linterna**, Esc suelta el ratón.
- **Qué hay:** Daniel despierta en su casa sin luz → coge la linterna de la mesilla y la garrafa junto a la puerta → sendero entre la niebla (hay una silueta que desaparece al alumbrarla o acercarse) → generador al pie de la torre (repostar y tres tirones) → vuelven las luces → cabina → monitor con tres cámaras (en la de la casa hay alguien; al cambiar de cámara ya no está y la puerta queda abierta) → emisora → rótulo de «continuará».
- **Reconstruir la escena:** `THE FOG → Bosque → 1. Importar kit de modelos` (solo si cambian modelos o texturas) y `2. Construir escena del bosque`. Rehace todo con semilla fija.
- **Lore:** la propuesta de biblia narrativa escrita por ChatGPT está en [../lore/biblia_narrativa_propuesta_1.md](../lore/biblia_narrativa_propuesta_1.md). **Pendiente de que Josemi la apruebe**; los textos del juego (cuaderno, radio municipal, hoja de mantenimiento, transmisión) salen de ahí.
- **Subido a GitHub el 19/11 por la tarde.** Resumen para quien continúe: [RELEVO.md](../../RELEVO.md). Pendientes: [PENDIENTE_BOSQUE.md](PENDIENTE_BOSQUE.md).
- **Norma de Josemi (19/11, mediodía):** si falta un modelo, **no se inventa ni se sustituye: se le pide** y lo hace él en Tripo. La lista de lo que falta está en PENDIENTE_BOSQUE.md.

---

## 19/11/2026 (mediodía) — Bosque: interior recolocado y cables en los postes

**Por qué:** Josemi vio el interior y avisó de que las sillas, el tronco y la estufa («puesta hacia una pared») estaban mal colocados, y de que a los postes les faltaba el cable.

**Casa de Daniel** (`BosqueAsentamiento.cs`):
- La estufa va en la esquina, en diagonal, con la boca hacia la habitación (`Recolocar`, que gira y mueve una pieza que el pack trae ya colocada). La tetera apoya en la plancha (antes estaba medio hundida).
- Dos sillas: una sentada a la mesa de la radio y otra frente a la boca de la estufa (`Silla(sitio, mira)`). La silla suelta del pack se quita.
- Leña apilada contra la pared al lado de la estufa; mochila apoyada junto a la mesilla; cubo en el suelo entre la mesa y la cómoda.
- Estantería con contenido del mismo pack (libros, papeles, latas, botellas, cantimplora, cerillas, cazo, sartén, platos, taza, caja) y la mochila pequeña encima.
- Los enseres apoyan en el suelo de tablas (`SueloCasa`); antes flotaban 3 cm.

**Tendido** (`BosqueTendido.cs`, nuevo):
- Los postes giran 90°: el travesaño queda perpendicular a la línea. Cuatro hilos, uno por aislador (medidos en la malla del poste), con comba entre poste y poste.
- Acometida a la casa (los hilos acaban en la cumbrera del porche), bajada a una pata de la torre, cable por la pata hasta el suelo y manguera por el suelo hasta el generador.
- El cable es lo único que no es un modelo: cada vano mide distinto, así que es una malla de tubo fino calculada entre aisladores (`Assets/Models/Retro/Bosque/Tendido/Cables.asset`).
- Calle sin árboles de 7,5 m a cada lado de la línea (se quitan 10 árboles; el resto del bosque no cambia).

**Piezas del pack con las caras del revés** (`CarasDelDerecho`): la caja de herramientas (se veía como una chapa gris junto al generador), la linterna, el hacha y varios marcos de ventana vienen espejados. Se detectan por el volumen con signo de la malla y se sustituyen por una copia corregida en `Assets/Models/Retro/Bosque/Corregidos/` (18 mallas; la carpeta se rehace en cada construcción).

**Otros:** silla de la cabina a la mesita auxiliar (estaba de espaldas a la cómoda); emisora y monitor metidos hacia las ventanas; pala apoyada en la pared de la casa; fuera el tronco suelto de delante del porche; la hierba ya no crece atravesando bidones y cajas.

**Cómo se revisó:** capturas con luz de trabajo y sin niebla, revisadas por agentes independientes antes (búsqueda de fallos) y después (verificación de cada arreglo). En Play: Daniel llega andando a la mesilla, la mesa, la cómoda, la garrafa y la puerta; 15 objetos con E; sin errores en consola.

**Capturas:** [tendido de noche](../capturas/bosque_21_tendido_noche.png), [estufa y silla](../capturas/bosque_20_estufa_y_silla.png).

### Misma tarde — la cocina pasa de la torre a la casa

**Por qué:** Josemi: «la casa tiene más sentido que tenga cocina, ¿no?, y un baño… reconsidera eso y la torre». La cabina de la torre traía del pack una cocina entera y la casa ninguna.

- `tools/bosque_cocina.py` (Blender, sin interfaz) parte la encimera en L de la cabina por sus juntas (es una sola malla, pero modular) y saca tres piezas a `Assets/Models/Retro/Bosque/Edificios`: `Prop_Cocina.fbx` (fregadero + módulo estrecho + cajones, 2,46 m), `Prop_Banco_Torre.fbx` (dos módulos sin fregadero, 2,46 m) y `Prop_Cocina_Gas.fbx`. Los cantos al aire se tapan copiando las caras del canto original. Después hay que pasar `THE FOG → Bosque → 1. Importar kit`.
- **Casa:** encimera con fregadero y cocina de gas en la pared de la ventana, con los cacharros y la comida encima. La estantería se quita; la cómoda del detector pasa a la pared del fondo, entre la cocina y la cama, con el calendario encima; la mesa de la radio se corre hacia la puerta. El cuaderno del abuelo va tumbado sobre la mesa (el modelo viene de canto).
- **Torre:** fuera la encimera en L, la cocina de gas y los platos. Queda un banco de trabajo de dos módulos con la emisora, el monitor, los partes y la linterna. La cama, la estufa de leña y los archivadores siguen (la cama, a decidir).
- **Baño:** sin hacer. La cabaña es una habitación de 5 × 4,7 m; propuesta: retrete exterior detrás de la casa y palanganero dentro. Faltan los modelos (lista en PENDIENTE_BOSQUE.md).

**Arreglos de la segunda revisión independiente:** los cuatro hilos de la acometida se recogen en la punta de la cumbrera del porche (antes acababan en dos sitios); la bajada sigue una arista de la pata de la torre (se busca en la malla la arista larga casi vertical más cercana); el cable pasa a material con sombreado y normales radiales (antes se veía como una cinta plana); los marcos de ventana «mezclados» llevan caras por los dos lados; `heightmapPixelError` del terreno a 1.

**Ojo con las capturas en Play:** si Unity no tiene el foco, el juego no avanza y `manage_camera screenshot` deja en consola «PlayerLoop called recursively» y estados raros (rótulo de inicio atascado). Poner `Application.runInBackground = true` por `execute_code` nada más entrar en Play y esperar a que pase el fundido.

**Capturas:** [cocina en la casa](../capturas/bosque_32_cocina_casa.png), [banco de la torre](../capturas/bosque_33_banco_torre.png), [casa con luz de trabajo](../capturas/bosque_34_casa_con_cocina_luz_de_trabajo.png), [acometida](../capturas/bosque_36_acometida_cumbrera.png), [bajada a la torre](../capturas/bosque_37_bajada_torre.png).

---

## 19/11/2026 — Bosque: escena propia, casa, torre, generador e inicio jugable

**Por qué:** el bosque no va en el terreno de Valdara (ese es el pueblo). Va en una escena aparte, y todo lo que se coloca tiene que ser un modelo 3D con la estética MOLDRISE + No, I'm Not a Human, menos pixelada.

**Modelos** (un solo origen para que todo pegue): pack «Vegetation» de Elbolilloduro (itch.io, CC0, estilo PSX). Original en `ArtSource/Elbolilloduro/`.
- `tools/bosque_split_pack.py` → 65 piezas de vegetación (pinos, árboles de hoja, arbustos, hierba, rocas, troncos, tocones) en `Assets/Models/Retro/Bosque/Modelos`.
- `tools/bosque_split_edificios.py` → `Torre_Vigia.fbx` (torre de 24 m con cabina amueblada), `Cabana.fbx` (casa con interior, estufa, cama, puerta), `Coche.fbx` y 35 objetos sueltos (`Prop_*.fbx`) en `.../Edificios`.
- `tools/bosque_silueta.py` → `Figura/Silueta.fbx`, la figura humana (hecha en Blender; fuente en `ArtSource/Blender/Silueta.blend`).
- De lo que ya había: generador, pozo, leña, carro y postes de Tripo; radio, televisor y garrafa de Poly Haven.
- **Modelo exclusivo nuevo:** el detector del abuelo. Imagen pedida a ChatGPT (`ArtSource/Tripo/ref/detector_abuelo.png`) → Tripo v3.1 imagen-a-3D por CLI (30 créditos; quedan 70) → `tools/tripo_prepare.py` (4 998 triángulos, 25 cm) → `Assets/Models/Retro/Tripo/detector`. Está sobre la cómoda de la casa y se puede examinar. [Captura](../capturas/bosque_18_detector_abuelo.png).

**Escena** (`BosqueBuilder.cs` + `BosqueAsentamiento.cs`): terreno de 500 × 500 m con tres capas, sendero principal y ramal, tres claros, 2 161 árboles, 322 rocas, 290 troncos/tocones/ramas, 520 arbustos y 3 160 matas de hierba. Casa de Daniel en el claro central (11, −8), torre en el claro del oeste (−100, −8), generador con bidones al pie, postes siguiendo el ramal, explanadas llanas bajo los edificios. La estructura de la torre va con material de **metal oxidado** (la cabina sigue de madera); se quita con `TorreDeMetal = false`.

**Juego** (`Assets/Scripts/Entorno/`): `BosqueJuego` (en Daniel: E, linterna, textos, sonido generado por código, sucesos), `Interactuable`, `Examinable`, `Recogible`, `GeneradorBosque`, `RadioBosque`, `MonitorCamaras`, `PuertaAbatible`, `LuzParpadeo`.

**Filtro:** `PixelArtCamera` tiene un parámetro nuevo, `fidelidad` (0 = solo paleta). En el bosque va a 0,45 con tramado 0,25: la paleta tiñe, pero la niebla y el haz de la linterna ya no se rompen en manchas planas. Valdara no cambia (sigue a 0).

**Comprobado en Play:** se sale de la casa por la puerta, se sube la torre entera andando (el paso de escalón de Daniel sube a 0,45 m en esta escena), la E detecta objetos, las notas y el monitor se ven bien, sin errores en consola.

**Avisos de estética:**
- La casa es una cabaña de troncos. **Josemi la aprobó así el 19/11/2026 («me gusta así la casa»): no cambiarle las texturas a cal y teja.**
- El coche es una ranchera tipo pickup; encaja regular en la España de finales de los 80.
- Generador, radio y televisor son modelos realistas (Tripo / Poly Haven) dentro de un entorno PSX; el filtro los iguala, pero son de otra fuente.
- Licencia: los modelos del pack son CC0; confirmar las texturas antes de subirlas a un repositorio público.

**Capturas:** [casa sin luz](../capturas/bosque_10_casa_sin_luz.png), [silueta en el sendero](../capturas/bosque_11_sendero_silueta-1.png), [torre](../capturas/bosque_12_torre.png), [torre encendida](../capturas/bosque_14_torre_encendida.png), [generador](../capturas/bosque_15_generador.png), [nota](../capturas/bosque_16_nota.png), [monitor CAM 03](../capturas/bosque_17_monitor_cam03.png).

---

## Dónde lo dejamos (17/11/2026, noche)

- **Para jugar:** abrir el proyecto en Unity y pulsar Play. Arranca siempre `TorreDaniel` en la puerta de Daniel (menú THE FOG > Play arranca el juego). WASD, ratón, Mayús para correr, Esc suelta el ratón, F1 ayuda.
- **GitHub:** todo subido a `main`.
- **Capturas para el grupo** (en Play, con el filtro): [docs/capturas](../capturas/).
- **Blender:** el fuente del farol está en `ArtSource/Blender/FarolFernandino.blend`. Para trabajar con Blender desde Claude: abrir Blender 5.2 (el addon MCP arranca solo) y Unity; el servidor MCP de Unity arranca solo al abrir el editor (MCP for Unity > Advanced > Auto-start on load, activado el 17/11).
- **Siguiente, a elegir:** interiores jugables del pueblo (bar, casa de Fred, ayuntamiento); afueras (huertos, corrales, cementerio, boca de mina); detalle de calle (cables entre fachadas, bancos, macetas); amueblar la casa de Daniel; caseta de la torre jugable; primer trozo de juego (bucle de partida).

---

## 17/11/2026 — Primer modelo hecho en Blender: farol fernandino

- Blender 5.2 conectado: addon **MCP for Blender** activado (telemetría desactivada) y servidor `blender` registrado en Claude Code. Unity habla con Blender directamente con `blender_bridge` (MCP for Unity): se modela en Blender y se importa al proyecto sin pasos manuales.
- **Farol fernandino de forja** modelado en Blender por código (pletina, brazo curvo con voluta, farol troncopiramidal con cristal que brilla): 2 332 triángulos, 60 cm. En `Assets/Models/Blender/FarolFernandino.glb`.
- Sustituye a los faroles de caja en las casas del pueblo (al lado de la puerta, por debajo del balcón), el ayuntamiento, la iglesia y la puerta de Daniel (bajo la viga del emparrado). La luz se separa 0,9 m de la pared y baja a 0,8 de intensidad para no quemar la cal.
- Capturas: [en la casa de Daniel](farol_fernandino_casa.png), [en el pueblo](farol_fernandino_pueblo.png).

## 17/11/2026 — Pueblo de Valdara (primera versión) con estética MOLDRISE

**Hecho** (`THE FOG → Entorno → Construir pueblo de Valdara`, `Assets/Editor/Entorno/PuebloBuilder.cs`):
- Pueblo manchego a ~140 m al sur de la casa de Daniel, al final del camino. **Plaza mayor** de 24 × 22 m con **fuente**, cuatro farolas, **iglesia** de sillería con portada en arco, rosetón encendido y **campanario** con campana y cruz, **ayuntamiento** de dos plantas con balcón corrido, espadaña con reloj y mástil, y **bar** con escaparate encendido, toldo, letrero sin texto y sillas de plástico en la terraza.
- Cuatro **calles** en cruz con unas 45 **casas adosadas** variadas por semilla (1987): una o dos plantas, ancho de 5,5 a 9 m, zócalo azul, ocre, gris o de piedra, puertas verdes, marrones o azules, rejas, contraventanas abiertas o cerradas, balcones de forja, chimeneas, faroles en algunas puertas, ventanas encendidas al azar, alguna casa **abandonada** (ventanas tapiadas con tablas) y **tapias de corral** con portón entre casas. 22 luces en total (sin sombras, para rendimiento).
- **Terreno**: ahora de 600 × 600 m; el pueblo entero queda llano y las calles y la plaza van **empedradas** (cuarta capa, `PavingStones046`). Las lomas altas se alejan para cerrar el horizonte detrás del pueblo.
- **Trazado común** en `EntornoLayout.cs` (torre, casa, senderos, plaza, calles) para que suelo, monte y pueblo coincidan. El monte ya no pone árboles dentro del pueblo.
- Todo se ve con la cámara pixel art (320 × 180, paleta de 32 colores).

**Probado en Play:** paseo por la plaza y por la calle este. Capturas: [plaza con la iglesia](pueblo_plaza.png), [calle](pueblo_calle.png), [vista aérea sin niebla](pueblo_aereo.png).

**Falta / siguiente:**
- Las casas del pueblo no tienen interior (puertas cerradas). Elegir cuáles serán jugables (bar, casa de Fred, ayuntamiento…) y darles interior con el mismo kit.
- Afueras: huertos, corrales, eras, cementerio, entrada de la mina.
- El empedrado de la plaza se ve algo claro de noche; oscurecerlo.
- Detalle de calle: cables eléctricos entre fachadas, bancos, macetas, carteles sin marcas, coches viejos genéricos.

## 17/11/2026 — Casa de Daniel jugable (arquitectura)

**Hecho** (`THE FOG → Entorno → Construir casa de Daniel`, `Assets/Editor/Entorno/CasaDanielBuilder.cs`):
- Casa de labranza manchega de una planta, 11 × 7 m, en (-34, 0, -5), puerta al sur. Estilo de la referencia C.
- **Interior jugable**: cocina-comedor (6 × 6 m) con chimenea en el muro oeste (hogar, campana, repisa, brasas que alumbran); desde ella, dormitorio (noreste) y despensa (sureste) por pasos sin puerta. Muros de 50 cm y tabiques de 14 cm con colisión; techo encalado con vigas de madera; suelo hidráulico en la cocina y de barro en las habitaciones.
- **Exterior**: cal con zócalo azul añil, tejado a dos aguas de teja árabe con hastiales, cumbrera y alero; cinco ventanas con marco, cristal y reja; puerta verde abierta hacia dentro; chimenea con sombrerete; emparrado de madera y cañizo sobre la puerta; poyo de piedra; farol encendido en la puerta.
- **Luces**: bombilla de la cocina y farol encendidos, brasas; bombilla del dormitorio apagada (lista para usarla).
- Texturas CC0 de ambientCG con el filtro retro: `Plaster007/001`, `PaintedPlaster017`, `RoofingTiles006`, `Wood062`, `Planks023A`, `Concrete031`, `Metal053B` y dos nuevas para el suelo (`Tiles131` hidráulico, `Tiles047` barro).
- Piezas comunes en `ArquiKit.cs` (muros con huecos, cajas, hastiales, rejas, materiales) para reutilizarlas en la cabina de la torre. Mallas en `Assets/Art/Retro/Arquitectura/`.
- El entorno reserva el sitio de la casa; el sendero de la torre acaba en la puerta, bajo el emparrado.

**Corregido por el camino:** las cajas del kit tenían las caras al revés (se veía el interior de cada pieza; el tejado salía negro). Arreglado y regeneradas las mallas. Es probable que el constructor viejo de la torre (`TorreBuilder`) tenga el mismo fallo; no se usa en el entorno nuevo.

**Probado en Play:** Daniel entra por la puerta a la cocina, pasa al dormitorio y a la despensa, y los muros y tabiques le frenan donde no hay hueco. Capturas: [fuera](casa_jugable_fuera.png), [cocina y chimenea](casa_cocina_chimenea.png), [desde la despensa](casa_cocina.png).

**Falta:** muebles y atrezo (la casa está vacía), vegetación de la parra, y la cabina de la torre jugable.

## 17/11/2026 — Requisito nuevo: interiores jugables

**Decisión del equipo:** los interiores tienen que ser jugables (entrar en la casa de Daniel y en la cabina de la torre). Lo generado hasta ahora como edificio (casas P2/v3.1, referencias B y C) pasa a ser **material para el pueblo** (edificios de fondo).

**Por qué cambia el método:** Tripo siempre entrega un volumen cerrado, sin interior (comprobado con la "pared con ventana", que salió como caseta cerrada). Para poder entrar hace falta arquitectura modular: muros con grosor y huecos de puerta y ventana, suelo, techo y tejado por separado.

**Propuesta:**
1. **Casa de Daniel jugable**: constructor propio (como el de la torre) que levanta la casa por piezas con el estilo de la referencia C (cal con zócalo, teja árabe, emparrado), con habitaciones dentro (cocina-comedor con chimenea, dormitorio, aseo/despensa). Texturas: cal, teja y piedra CC0 ya descargadas, con el mismo filtro retro.
2. **Tripo para lo que va dentro y en los huecos**: puertas, ventanas con reja y contraventanas, chimenea, muebles y atrezo (método de calidad).
3. **Cabina de la torre jugable**: se corta en Blender la cabina del modelo de Tripo y se pone una cabina modular con interior; patas y escalera siguen siendo de Tripo, con colisiones para subir.

## 17/11/2026 — La casa "parece un rancho": se busca una casa vivida

**Problema:** la casa P2 (foto de referencia de cortijo en ruinas) se ve abandonada, tipo rancho ([casa_rancho_descartada.png](casa_rancho_descartada.png)). Daniel vive allí: tiene que ser una casa rural española **habitada**, gastada pero cuidada.

**Hecho:** dos fotos de referencia nuevas (15 créditos cada una) → [casa_opciones.png](casa_opciones.png)
- **B** — casa de pueblo de dos plantas: cal, zócalo de piedra, rejas, balcón de madera, portón en arco, poyo con macetas, parra en la fachada. Solo se ve la fachada: Tripo tendría que inventar los laterales.
- **C** — casa de labranza manchega de una planta: cal con zócalo azul, rejas y contraventanas verdes, emparrado con poyo, chimenea, corral pegado. Se ve entera: el 3D sale más fiel.

**Pendiente de decidir:** cuál, y con qué método (ver "Créditos").

## 17/11/2026 — Entorno colocado (primera versión)

- `THE FOG → Entorno → Construir suelo`: terreno de 400 × 400 m con tres capas CC0 (tierra con piedras, sendero de piedra, manchas de tierra con paja). Llano en torre y casa, lomas suaves fuera. **Se activó el módulo Terrain de Unity** en `Packages/manifest.json` (no estaba nunca; sin él el terreno desaparece en Play).
- `THE FOG → Entorno → Colocar entorno Tripo`: coloca todo sobre el terreno con semilla fija (1987). Torre + generador; casa con pozo, leña, carro y gallinero; muros de piedra seca cerrando la finca con hueco para el camino; postes de luz junto a los senderos; monte con encinas, árboles secos y matorral. Vista desde arriba: [planta.png](planta.png).
- Ajustes tras revisar: la puerta de la casa mira al sur (al pozo y al camino del pueblo); el sendero de la torre acaba en la puerta; el hueco del muro sur coincide con el camino; los postes no pueden caer dentro de la casa.
- **Escena limpia**: fuera el sistema de juego antiguo (intro, órdenes, radio y diálogos, HUD, reloj, anomalías, consola) y los restos de la torre y casa viejas. Daniel camina con `EntornoWalker` (Mayús corre, Esc suelta el ratón). `PuestoDaniel.unity` conserva el juego original.

## 17/11/2026 — Modelos

**Método de calidad** (`ArtSource/Tripo/p2.sh`): foto de referencia realista (`banana_pro`, 15) → geometría **P2** (120) → textura **v3.5 detailed** alineada a la foto (20) = **155 créditos por modelo**. La geometría P2 sale limpia (3–5 k triángulos) y sin textura; por eso el paso de textura.

**Método rápido** (`ArtSource/Tripo/tanda1.sh`): texto → v3.1 detailed (30). Va bien para estructuras y atrezo; en edificios y algunas plantas sale con aire de juguete.

Preparación de todos con Blender (gratis): `bash tools/tripo_prepare_tanda.sh [nombres]`.

| Modelo | Método | Resultado |
|---|---|---|
| torre | v3.1 | Bien. 40 k triángulos, 15,5 m |
| casa (v3.1) | v3.1 | **Descartada**: tejas naranjas y piedras redondas, de juguete |
| casa (P2) | P2 + textura | Buena calidad pero **parece un rancho abandonado** (ver arriba) |
| carro | P2 + textura | Bien |
| gallinero | P2 + textura | Bien (gris: la referencia salió en blanco y negro) |
| encina, matorral | v3.1 | Aceptables (hojas algo estilizadas, de lejos y con niebla funcionan) |
| muro_piedra, poste_luz, pozo, lena | v3.1 | Bien |
| pino, roca | v3.1 | **Fuera del entorno**: pino de dibujo, roca como una esponja. Rehacer con el método de calidad |
| coche | (solo referencia) | **Apartado**: sin logos pero se reconoce un modelo real (tipo SEAT 1430) |

Hojas: [tanda1.png](tanda1.png), [casa_p2_refs.png](casa_p2_refs.png).

## Créditos de Tripo
| Concepto | Créditos |
|---|---|
| Prueba de estilo (árbol, generador, pared) | 90 |
| Tanda 1 (10 modelos v3.1) | 300 |
| Fotos de referencia (casa, carro, coche, gallinero, casa B, casa C) | 90 |
| Casa P2 + textura | 140 |
| Carro y gallinero P2 + textura | 280 |
| **Total gastado** | **900** |
| **Saldo** | **100** |

Con 100 créditos **no llega** para una casa con el método de calidad (P2 + textura = 140). Opciones: recargar créditos, o hacer la casa con v3.1 desde la foto de referencia (40) + textura v3.5 (20) = 60, de menos calidad de geometría.
