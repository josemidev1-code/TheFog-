#!/usr/bin/env bash
# Prepara con Blender todos los modelos de una tanda de Tripo y los copia a Assets/Models/Retro/Tripo/<nombre>/.
# Uso: bash tools/tripo_prepare_tanda.sh            (todos los de la tabla que tengan model.glb)
#      bash tools/tripo_prepare_tanda.sh casa pozo  (solo esos)
# Tabla: nombre  triangulos  altura_m  [grande]
set -u
cd "$(dirname "$0")/.."
BLENDER="/c/Program Files/Blender Foundation/Blender 5.2/blender.exe"
# Fuera del entorno por calidad insuficiente (17/11/2026): pino y roca. Siguen en la tabla por si se rehacen.
TABLE="
torre       40000 15.5 grande
casa        30000 5.2  grande
encina      20000 7.0
pino        15000 6.5
muro_piedra 5000  1.0
poste_luz   3000  8.0
lena        6000  1.2
roca        4000  1.8
matorral    8000  1.0
pozo        8000  2.3
carro       20000 1.5
gallinero   20000 2.2
"
only=" $* "
echo "$TABLE" | while read -r name tris height big; do
  [ -z "$name" ] && continue
  [ $# -gt 0 ] && [[ "$only" != *" $name "* ]] && continue
  # Si hay versión de calidad (P2 + textura v3.5), manda esa.
  glb=$(find "ArtSource/Tripo/p2/${name}_tex" -name model.glb 2>/dev/null | head -1)
  [ -z "$glb" ] && glb=$(find "ArtSource/Tripo/$name" -name model.glb 2>/dev/null | head -1)
  echo "$name <- $glb"
  if [ -z "$glb" ]; then echo "$name: sin model.glb, se salta"; continue; fi
  prep="ArtSource/Tripo/_prep/$name"
  rm -rf "$prep"
  "$BLENDER" -b --factory-startup -P tools/tripo_prepare.py -- "$glb" "$prep" "$name" "$tris" "$height" $big 2>&1 | grep -E "TRIPO_PREPARE|Error" || echo "$name: FALLO en Blender"
  dest="Assets/Models/Retro/Tripo/$name"
  mkdir -p "$dest"
  cp "$prep"/* "$dest"/
done
