#!/usr/bin/env bash
# Tanda 1 del entorno de Daniel (17/11/2026). 10 modelos v3.1 detailed, 30 créditos cada uno.
cd "$(dirname "$0")"
STYLE="rural Spain late 1980s, worn and weathered, aged materials, dust and grime, realistic proportions, muted desaturated colors, no logos, no text, single object, neutral pose, game asset"

gen() {
  local name="$1"; shift
  tripo make "$*, $STYLE" --model tripo-v3.1 -p texture_quality=detailed --name "t1_$name" \
    -o "$name" --yes --no-open --json > "$name.json" 2> "$name.log"
  echo "$name: $(grep -o '"status":"[a-z]*"' "$name.json" | head -1) $(grep -o '"credits_consumed":[0-9]*' "$name.json")"
}

gen torre "tall wooden forest fire lookout tower standing on four splayed square timber legs with diagonal cross bracing, zig-zag wooden staircase with landings attached to one side, small square wooden cabin on top with windows on all four sides and a low pitched corrugated metal roof, narrow wooden deck with railing around the cabin" &
gen casa "small one-story rural Spanish farmhouse, thick stone walls covered in peeling whitewash lime plaster, old curved terracotta tile roof, faded green wooden door, small windows with wooden shutters, simple chimney, attached lean-to woodshed" &
gen encina "old holm oak tree with a short thick gnarled trunk and a dense rounded dark green crown, Spanish dehesa" &
wait
gen pino "young Aleppo pine tree with a thin leaning trunk, reddish grey bark and sparse irregular clumps of dark green needles" &
gen muro_piedra "straight section of dry stone wall made of stacked rough grey fieldstones without mortar, about 3 meters long and 1 meter tall" &
gen poste_luz "old wooden electricity utility pole with one wooden crossarm and white ceramic insulators" &
wait
gen lena "neat pile of stacked chopped firewood logs held by wooden stakes" &
gen roca "large weathered grey granite boulder with patches of pale lichen" &
gen matorral "dry Mediterranean scrub bush of rockrose and broom, dense twiggy shrub with small dull olive green leaves" &
gen pozo "old round stone water well with a whitewashed rim, wrought iron arch with a pulley and an old metal bucket" &
wait
tripo balance --json
