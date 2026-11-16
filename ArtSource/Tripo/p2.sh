#!/usr/bin/env bash
# Modelo de calidad: imagen de referencia -> geometría P2 (120) -> textura v3.5 detailed alineada a la imagen (20).
# Uso: bash p2.sh <nombre> <imagen_referencia>
cd "$(dirname "$0")"
name="$1"; img="$2"
mkdir -p p2
tripo make "$img" --model P2 -p texture_quality=detailed --name "p2_$name" -o "p2/$name" --yes --no-open --json > "p2/$name.json" 2> "p2/$name.log"
task=$(grep -o '"task_id":"[^"]*"' "p2/$name.json" | head -1 | cut -d'"' -f4)
[ -z "$task" ] && { echo "$name: fallo en P2"; exit 1; }
tripo model texture "$task" --model v3.5 --texture-quality detailed --texture-alignment original_image \
  --name "p2_${name}_tex" -o "p2/${name}_tex" --yes --no-open --json > "p2/${name}_tex.json" 2> "p2/${name}_tex.log"
echo "$name: geometría $(grep -o '"credits_consumed":[0-9]*' "p2/$name.json"), textura $(grep -o '"credits_consumed":[0-9]*' "p2/${name}_tex.json")"
