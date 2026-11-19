# Bosque — estado y pendientes (19/11/2026)

Archivo de relevo: lo lee la tarea programada de las 12:55 si la sesión de la mañana se quedó sin límite.
Actualizar al terminar cada bloque.

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

## Pendiente (por orden)

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

Lista que se le dio el 19/11 (dijo «ahora te los hago»). Los deja como `.glb` en `ArtSource/Tripo/` con el nombre del modelo.

1. **Bombilla colgante** (casquillo, cable y bombilla desnuda, 25 cm): las luces del interior de la casa y de la cabina salen del aire, no hay aparato.
2. **Aplique de exterior** (chapa oxidada con pantalla, 35 cm): luz del porche, del pie de la torre y del generador.
   (En el proyecto ya hay `industrial_wall_lamp` y `security_light` de Poly Haven; no se han colocado: decide él si valen.)
3. **Retrete exterior** (caseta de tablas con tejadillo de chapa y puerta cerrada, 1,1 × 1,1 × 2,2 m): el baño, detrás de la casa.
4. **Palanganero** (mueble lavabo antiguo con palangana, jarra y espejo, 1,3 m): para asearse dentro de la casa.
5. **Palomilla de acometida** (soporte de pared con dos aisladores, 40 cm): en la punta de la cumbrera del porche y en la pata de la torre, donde ahora los cables acaban directamente en la madera y el metal.
6. **Cuadro eléctrico / caja de fusibles** (40 × 50 cm): junto a la puerta de la casa y al pie de la torre.
7. Opcionales: banco para el porche (está vacío), tajo de cortar leña con hacha junto a la leñera, señal oxidada del sendero, plano de Valdara para la cabina.

Decisiones suyas pendientes: baño fuera (propuesta) o dentro (haría falta inodoro, lavabo, ducha y una casa mayor); si la cama de la cabina de la torre se queda o se quita.

La cocina no necesita modelos: sale del propio pack (`tools/bosque_cocina.py`).

Cosas del pack que se dejan como vienen (avisado): la cabina tiene cinco archivadores que tapan media ventana, dos fregaderos y la estufa pegada a la cocina.
