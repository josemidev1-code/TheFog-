# Entorno de Daniel — diario de construcción

Escena: `Assets/Scenes/TorreDaniel.unity`. Objetivo: la torre de vigilancia, la casa de Daniel y el monte que las rodea, apartados del pueblo, **todo con modelos de Tripo** y la estética común ([../retro/README.md](../retro/README.md)): España rural de finales de los 80, realista, gastado, desaturado, nada de cartoon.

Las entradas van de la más reciente a la más antigua.

---

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
