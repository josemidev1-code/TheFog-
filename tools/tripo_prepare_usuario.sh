#!/usr/bin/env bash
# Prepara con Blender los modelos que Josemi hizo en Tripo (19/11/2026) y los copia a Assets/Models/Retro/Tripo/<nombre>/.
# Los .glb originales están fuera del proyecto, en Desktop/thefogjuego/ModelosTheFog; aquí se copian a
# ArtSource/Tripo/<nombre>/tripo-out/usuario/model.glb (carpeta que git no sube) antes de prepararlos.
# Uso: bash tools/tripo_prepare_usuario.sh                 (todos)
#      bash tools/tripo_prepare_usuario.sh cama alfombra   (solo esos)
# Tabla: nombre | triangulos | altura_m | modo | archivo original
#   modo "relieve": malla de ~2 M de triángulos; se reduce y se hornea el relieve.
#   modo "ligero":  ya viene con ~10 k triángulos y sus texturas; solo se escala y se centra.
set -u
cd "$(dirname "$0")/.."
BLENDER="/c/Program Files/Blender Foundation/Blender 5.2/blender.exe"
ORIGEN="${ORIGEN:-../../ModelosTheFog}"
LOG=ArtSource/Tripo/_prep/usuario.log
mkdir -p ArtSource/Tripo/_prep
TABLE="
alfombra|2000|0.065|relieve|tripo_model_06e702b9-f03e-4628-9d07-8cbf86012fcf_meshopt.glb
radiador|10000|0.75|relieve|tripo_model_34beb11b-7c70-464c-b326-140a14bcf7c3_meshopt.glb
escritorio|10000|0.77|relieve|tripo_model_4b58552e-411c-4b60-b186-34e3f496eb39_meshopt.glb
planta_colgante|10000|0.80|relieve|tripo_model_5ce7c70d-f462-48db-9365-3641af9bf14e_meshopt.glb
mesita_noche|6000|0.75|relieve|tripo_model_81dc45c1-7d3b-417b-9dab-03fc6bbc6d1a_meshopt.glb
cama|12000|1.05|relieve|tripo_model_935ba97a-69d6-4c0b-969b-a25bca0c2194_meshopt.glb
libreria|12000|1.90|relieve|tripo_model_9bd775c3-858c-41d2-ade2-306a333736aa_meshopt.glb
corcho|3000|0.90|relieve|tripo_model_d7ecb575-182d-438e-ab5e-10c161748767_meshopt.glb
ventana_cortinas|10000|1.20|relieve|tripo_model_dfa81d96-cc28-4a05-80e9-b1d3eb84ac48_meshopt.glb
libros|8000|0.46|relieve|tripo_model_fd0a5345-5eb7-41a3-99b2-1033f75972d9_meshopt.glb
bidon|10100|0.47|ligero|Modelo Bidond e gasolina.glb
linterna|10100|0.09|ligero|old flashlight 3d model.glb
generador_daniel|10100|0.65|ligero|generador luz casa daniel.glb
radio_antigua|10100|0.215|ligero|vintage radio 3d model.glb
"
only=" $* "
echo "INICIO $(date)" >> "$LOG"
echo "$TABLE" | while IFS='|' read -r name tris height mode file; do
  [ -z "$name" ] && continue
  [ $# -gt 0 ] && [[ "$only" != *" $name "* ]] && continue
  src="ArtSource/Tripo/$name/tripo-out/usuario/model.glb"
  if [ ! -f "$src" ]; then
    if [ ! -f "$ORIGEN/$file" ]; then echo "$name: no encuentro $ORIGEN/$file" | tee -a "$LOG"; continue; fi
    mkdir -p "$(dirname "$src")" && cp "$ORIGEN/$file" "$src"
  fi
  prep="ArtSource/Tripo/_prep/$name"
  rm -rf "$prep"
  extra=""; [ "$mode" = "ligero" ] && extra="ligero"
  "$BLENDER" -b --factory-startup -P tools/tripo_prepare.py -- "$src" "$prep" "$name" "$tris" "$height" $extra 2>&1 \
    | grep -E "TRIPO_PREPARE|Error|Traceback" | tee -a "$LOG"
  if [ ! -f "$prep/$name.gltf" ]; then echo "$name: FALLO en Blender" | tee -a "$LOG"; continue; fi
  dest="Assets/Models/Retro/Tripo/$name"
  mkdir -p "$dest"
  cp "$prep"/* "$dest"/
done
echo "FIN $(date)" >> "$LOG"
