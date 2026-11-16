# Estética común y assets retro de THE FOG

**Regla general:** todo lo que entra en Valdara —descargado, generado con Tripo o hecho a mano— tiene que parecer del mismo juego. Para no depender del ojo de cada uno, la estética se impone con tres filtros que pasan **todos** los assets.

## 1. Dirección artística (qué buscamos)
- Pueblo rural **español** de finales de los 80 / principios de los 90. Realista, nada de cartoon ni low-poly exagerado.
- Todo **usado y envejecido**: cal desconchada, óxido, madera gastada, polvo. Nada nuevo ni brillante.
- Paleta **desaturada y fría** (verdes grisáceos, ocres apagados, terracota sucia) con luz cálida puntual (bombillas, farolas).
- Proporciones creíbles: puertas de 2,0–2,1 m, plantas de 2,8–3 m, techos bajos.
- **Sin marcas reales** (Telefónica, Correos, Butano, CAMPSA, SEAT…): versiones genéricas.

## 2. Pipeline técnico (lo que lo hace igual)
| Paso | Dónde | Qué hace |
|---|---|---|
| Carpeta | `Assets/Models/Retro/` (modelos) y `Assets/Art/Retro/` (materiales) | Todo asset nuevo va aquí. Generados con Tripo: `Assets/Models/Retro/Tripo/<nombre>/` |
| Texturas | `Assets/Editor/Retro/RetroTextureSettings.asset` | Color 256 px, normales/rugosidad 128 px, filtro *point*. Caras, interactivos y pistas: 512 px (rutas con `/Personajes/`, `/Interactivos/`, `/Pistas/` o `_Detalle`). No toca los archivos: solo la importación. Revertir: `enabled = false` y **THE FOG → Retro → Reimportar texturas retro** |
| Render | Componente `RetroRender` (`SISTEMAS/Retro` en `TorreDaniel`) | Resolución interna 50 % con escalado *point* mientras se juega; restaura URP al salir |
| Color | Volumen de post-proceso existente | Ajuste de color, grano y viñeta comunes |

Comparativas: [comparativa.png](comparativa.png) (original / texturas / render 50 % / 35 %) y [catalogo_cc0.png](catalogo_cc0.png) (todos los modelos CC0 con el ajuste retro).

## 3. Plantilla para generar con Tripo
Usar **siempre** el mismo bloque de estilo al final del prompt, cambiando solo el objeto:

```
<OBJETO>, rural Spain late 1980s, worn and weathered, aged materials, dust and grime,
realistic proportions, muted desaturated colors, no logos, no text, single object,
neutral pose, game asset
```

Ejemplo: `generic yellow street mailbox on a short metal post, rural Spain late 1980s, worn and weathered, …`

Comprobación antes de aceptar un modelo generado:
1. Escala real en metros y pivote en la base; frente hacia el mismo eje que el resto (ver `docs/puesto-daniel.md`).
2. Sin logotipos ni texto inventado.
3. Polígonos razonables para su tamaño (atrezo pequeño < 20k triángulos; si no, simplificar).
4. Captura junto a dos assets CC0 del catálogo con la misma luz: si "salta", se repite o se descarta.
5. Registrar aquí prompt, créditos gastados y resultado.

### Preparar lo generado (gratis, con Blender)
Tripo entrega mallas de >1 M de triángulos a escala 1 y con texturas incrustadas. Se preparan con Blender 5.2 en local:

```
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup -P tools/tripo_prepare.py -- <model.glb> Assets/Models/Retro/Tripo/<nombre> <nombre> <triangulos> <altura_m>
```

Suelda la malla, reduce polígonos, **hornea el relieve** de la malla original en `<nombre>_Normal.png`, escala a metros, pivote en la base y exporta `.gltf` (con tangentes) + `<nombre>_Color.jpg`. Para torre y casa se añade `grande` al final: las texturas se llaman `_Grande` y suben a 1024 px. Los originales de Tripo quedan en `ArtSource/Tripo/` (fuera de `Assets`).

Ajuste retro para Tripo (`RetroTextureSettings`, rutas con `/Tripo/`): color y relieve a **512 px** (1024 con `_Grande`), filtro *point*. Tripo mete todo el modelo en una sola textura, así que a 256 px se perdía la pintura y el óxido. El relieve se importa como textura **lineal normal** (no tipo *Normal Map* de Unity): el shader glTF lo lee así; con el tipo *Normal Map* salían manchas negras. Comparativa: [tripo_relieve_512.png](tripo_relieve_512.png).

### Registro de generaciones
| Fecha | Modelo | Prompt (objeto) | Créditos | Resultado |
|---|---|---|---|---|
| 16/11/2026 | arbol_seco (v3.1, detailed) | dead leafless tree with a twisted trunk and gnarled bare branches, dry cracked grey bark, Mediterranean countryside | 30 | 1,41 M → 15 000 triángulos, 7 m. Encaja con los árboles del monte ([captura](tripo_arbol_corregido.png)) |
| 16/11/2026 | generador (v3.1, detailed) | old portable petrol electric generator in a metal tube frame, chipped green paint, rust and oil stains | 30 | 1,41 M → 8 000 triángulos, 0,60 m, pie de la torre ([captura](tripo_generador_corregido.png)) |
| 16/11/2026 | pared_ventana (v3.1, detailed) | flat rectangular exterior wall panel of a wooden fire lookout cabin with one wide window… | 30 | **No sirve como módulo**: Tripo hizo una caseta cerrada de 3,1 × 1,9 × 2,6 m con tejado y cristal opaco. Lección: Tripo siempre da volúmenes cerrados; paredes sueltas no ([captura](tripo_entorno_prueba.png)) |

Saldo tras la prueba: **910** créditos (90 gastados).

**Grietas blancas (corregido):** la primera versión del árbol y el generador tenía rendijas por las que se veía el cielo. Causa: Tripo entrega la malla partida en islas en cada costura de UV y al reducir polígonos cada isla encogía por su lado. `tripo_prepare.py` ahora suelda vértices antes de reducir y exporta a doble cara.

Edificios en los que se entra: mejor con el generador de pueblo (rama de respaldo `backup/cambios-locales-2026-11-15`, `PuestoDanielTown.cs`) y materiales de ambientCG. Tripo da una sola malla sin interior: sirve para **edificios de fondo** y atrezo.

## 4. Procedencia y licencias
Todos los assets de esta carpeta son **CC0** (dominio público, uso comercial sin atribución). Se descargaron el 16/11/2026.

**Poly Haven** (https://polyhaven.com, glTF 1K) en `Assets/Models/Retro/PolyHaven/`:
boulder_01, dead_tree_trunk, dead_tree_trunk_02, grass_medium_01, grass_medium_02, hatchet, industrial_wall_lamp, metal_jug, modular_electric_cables, modular_electricity_poles, moss_01, old_bed_frame, old_gas_mask, painted_wooden_cabinet, painted_wooden_chair_01, painted_wooden_shelves, painted_wooden_table, pine_sapling_small, plastic_jerrycan, portable_generator, propane_tank, rock_07, rock_09, rock_moss_set_02, rollershutter_door, rollershutter_window_01, root_cluster_02, rusted_wheel_rim_02, security_light, television_02, tree_stump_02, vintage_radio_transceiver, watering_can_metal_01, wicker_basket_01, wooden_axe, wooden_axe_02, wooden_barrels_01, wooden_broom, wooden_ladder_02, wooden_military_crate, wooden_picnic_table, wooden_stool_01, wooden_table_02.

**ambientCG** (https://ambientcg.com, 1K-JPG) en `Assets/Art/Retro/Materiales/`:
Plaster001, Plaster007, PaintedPlaster017, Facade006, RoofingTiles013A, RoofingTiles006, PavingStones046, PavingStones119, Rock030, Bricks085, PaintedWood007A, Asphalt031, Gravel023, Metal053B.

Descartados tras revisarlos: `flood_lights` (fila de focos de exposición), `WoodenChair_01` (silla gótica tallada), `pine_sapling_medium` (268 MB de geometría).

## 5. En el juego (16/11/2026)
- `Assets/Scenes/TorreDaniel.unity` es la **primera escena de Build Settings** (antes `PuestoDaniel`, que sigue intacta y en la lista). Solo torre, casa de Daniel, monte y niebla; el pueblo irá más lejos. Consola con 1 cámara (casa) y 1 lugar de anomalías.
- Ajuste retro de texturas activo en `Assets/Models/Retro/`, `Assets/Art/Retro/`, `Assets/Models/Optimized/` (texturas externas de Poly Haven) y `Assets/Art/Puesto/Suelo/`. Los GLB con texturas incrustadas (modelos del equipo, Higgsfield/Tripo) no se pueden reducir por importación: reciben el pixelado del render al 50 %.
- Atrezo colocado en `Atrezo Retro`: pie de la torre (generador, bidones, bombona, caja, rueda, escalera, barriles, aparador), patio de la casa (merendero con cesta, silla, cántara, regadera, escoba, taburete, leñera con hacha) y `Monte` (40 rocas, raíces, troncos, pinos jóvenes, hierba y musgo con semilla 1987, fuera de la vista de la cámara de vigilancia).
- Probado en Play: intro, HUD, radio, consola con la cámara de la casa y una anomalía forzada, sin errores del juego. `RetroRender` deja URP como estaba al salir. No se ha jugado un turno completo hasta las 06:00.

## 6. Pendiente
- Simplificar mallas pesadas: dead_tree_trunk_02 (~83k triángulos), tree_stump_02 (~62k), boulder_01 (~67k vértices).
- Crear materiales de Unity a partir de las texturas de ambientCG cuando se use el generador de pueblo.
- Decidir si el resto del proyecto (`Assets/Models/Optimized`, `Assets/Art/Puesto`) pasa también por el ajuste retro para que la cabina y la casa no desentonen.
