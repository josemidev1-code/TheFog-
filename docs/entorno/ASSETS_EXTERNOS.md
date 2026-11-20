# Assets externos (descargados el 19/11/2026; usados en la primera versión el 20/11/2026)

Los originales están en `Desktop/thefogjuego/AssetsDescargados/` (fuera del proyecto y de git). Lo que la primera
versión jugable usa de ellos se ha copiado o convertido a `Assets/Resources/` con los guiones de `tools/`
(`prepara_audio.py`, `prepara_ui.py`). **Nada se ha oído ni visto dentro de Unity todavía** (ver
[PRIMERA_VERSION.md](PRIMERA_VERSION.md)).

## Qué hay descargado

| Carpeta | Qué es | Origen | Licencia | Crédito obligatorio |
|---|---|---|---|---|
| `psx_derelict_furniture/` | 6 muebles PSX (silla, sofá, nevera, colchón, estante, jarrón) en `.glb`/`.fbx`/`.blend` con textura | https://daniel-jurys.itch.io/psx-derelict-furniture (versión Standard, gratuita) | CC0 | No |
| `horror_sfx/` | **85** efectos `.mp3` (no 86, como se apuntó al bajarlos), y **5 de ellos son duplicados** de otros (mismo contenido con otro nombre): viento, pasos sobre madera, grava, hojas, barro, metal, escalera, moqueta y hormigón, puertas que chirrían, gruñidos | https://liminal-space-dev.itch.io/free-horror-sfx-sounds (vol. 1 y 2) | CC0 | No. **Generados con IA** según su página |
| `oga_crickets_cc0/` | Grillos de noche en bucle (2 `.mp3`) | https://opengameart.org/content/crickets-ambient-noise-loopable | CC0 | No |
| `oga_distant_screams_ccbysa/` | Música ambiental de terror: menú, 2 de calma, tensión, amenaza y 2 golpes de susto (`.ogg`/`.mp3`) | https://opengameart.org/content/distant-screams-dark-ambient-horror-music-pack | **CC BY-SA 4.0** | **Sí** (ver abajo) |
| `psx_horror_ui_free_ccby/` | Interfaz estilo PS1, tema «fog»: paneles, botones, cuadro de diálogo, documento, mapa por salas, casillas de inventario, cursor, fuente de mapa de bits e iconos en PNG de 9 cortes | https://github.com/Spyridon-Pikoulas/godot-psx-horror-ui-free (v1.0.1) | **CC BY 4.0** | **Sí** (ver abajo) |

Las carpetas `horror_sfx_vol1/`, `nocturnis_vol1/`, `psx_rural_environment/` y `retro_furniture_psx/` están **vacías**
(descargas que no se llegaron a hacer; ver «No descargados»).

## Qué se ha usado en la primera versión

| Destino en el proyecto | De dónde sale |
|---|---|
| `Assets/Resources/Audio/Pasos/` (madera, metal, hormigón, grava, hojas) | `horror_sfx`: pisadas troceadas de «Wind Footsteps», «Metal Footsteps», «Concrete Footsteps», «Gravel Footsteps» y «Leaves Footsteps» |
| `Assets/Resources/Audio/Ambiente/` (viento en bucle, viento de interior, rachas, aullido) | `horror_sfx`: «Ambient Wind» (1, 5, 6, 7 y 9) |
| `Assets/Resources/Audio/Ambiente/grillos_bucle` | `oga_crickets_cc0/crickets_1.mp3` |
| `Assets/Resources/Audio/Puertas/` | `horror_sfx`: «Creaking Door» (4, 6, 9 y 12) |
| `Assets/Resources/Audio/Musica/` (`calma_pantano`, `tension`, `calma_torre`) | `oga_distant_screams_ccbysa`: «Quiet 1», «Tension 1» y «Quiet 2», sin recodificar |
| `Assets/Resources/Audio/Sustos/` (`golpe_1`, `golpe_2`) | `oga_distant_screams_ccbysa`: «Stinger Encounter 1» y «2» |
| `Assets/Resources/Audio/Sint/` | **No es de ningún pack:** sintetizado con `tools/prepara_audio.py` (generador, estática, nieve y zumbido de tele, explosión, voces sin palabras, sintonía, despertador, brasas, clics, walkie…) |
| `Assets/Resources/UI/fog/*.bytes`, `UI/papel.bytes`, `UI/walkie.bytes` | `psx_horror_ui_free_ccby`, tema «fog»: panel, diálogo, tooltip, divisor, puntero, marca; la hoja de papel y el aparato del walkie se han compuesto a partir de `document.png`, `window.png` y `monitor.png` |
| `Assets/Resources/UI/mapa.bytes` | **Dibujado para el juego** con `tools/genera_mapa.py` (no es del kit) |
| `Assets/Resources/Fuentes/*.ttf` | Copias de las fuentes que ya tenía el proyecto en `Assets/Art/Puesto/Fuentes/` (con sus licencias) |

No se ha usado: `psx_derelict_furniture` (ningún mueble), los gruñidos y las pisadas de barro, escalera y moqueta, la
pista de menú y la de amenaza, ni la fuente de mapa de bits del kit de interfaz.

Las imágenes de la interfaz van como PNG con extensión `.bytes` a propósito: así Unity no las reimporta ni las
comprime, y el juego las carga tal cual con el píxel nítido.

## Fuentes

Las cuatro fuentes del proyecto (Barlow Condensed Medium y SemiBold, Special Elite y VT323) **tienen todos los
caracteres del castellano**: á é í ó ú ü ñ ¿ ¡ « » … · º ª. No tienen los símbolos ● ■ ► ← → ↑ ↓, así que los textos del
juego no los usan. Licencias: Barlow Condensed y VT323, SIL Open Font License; Special Elite, Apache 2.0 (los archivos
de licencia están junto a las fuentes).

## Créditos obligatorios (literales, con su dirección)

- `PSX Horror UI Free by heyheythere - https://heyheythere.itch.io/psx-horror-ui-free - CC BY 4.0`
- `Distant Screams: Dark Ambient Music Pack - Rest! (https://rest--vgmusic.weebly.com/) - CC BY-SA 4.0`

La tarjeta final del juego nombra los dos con autor y licencia, **sin la dirección web** (para no romper el tono): la
dirección completa vive en este documento. Si se publica el juego, estos dos créditos tienen que ir también en la
página o en un archivo de créditos.

**La música obliga a «compartir igual» (CC BY-SA).** Si el juego se vende, decide Josemi si se queda. Quitarla no rompe
nada: se borra la carpeta `Assets/Resources/Audio/Musica/` y el juego sigue sin música (los golpes de susto de
`Audio/Sustos/` son del mismo pack y tienen la misma licencia).

## No descargados

- **Nocturnis Vol. 1** (música, juanjosound en itch.io): 920 MB y sin licencia formal en la página (pide crédito). Pendiente de que Josemi diga si lo quiere.
- **PSX Rural Environment Pack** (caliberuk): es de pago.
- **Retro Furniture PSX** (swickster): la página no existe (404).

Lo que el 19/11 se apuntó como «falta todavía» (viento largo en bucle, zumbido de generador, estática de radio y
televisor, fuente con tildes y «ñ») ya está resuelto: el viento en bucle sale de «Ambient Wind», lo demás está
sintetizado y las fuentes del proyecto ya tenían los caracteres.
