#!/bin/bash
# Comprueba que los scripts del proyecto compilan SIN abrir Unity (no hace falta licencia).
# Reutiliza los argumentos con los que Unity compiló la última vez (Library/Bee/artifacts/*.dag/*.rsp) y el
# compilador Roslyn que trae el propio editor, con la lista de .cs actual de Assets/.
# Uso (desde la raíz del proyecto):  bash tools/compilar_sin_unity.sh
# Sale con 0 si no hay errores. Solo comprueba que compila: no construye la escena ni prueba nada en Play.
set -u
cd "$(dirname "$0")/.." || exit 2
DATA="${UNITY_DATA:-/c/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Data}"
CSC=$(find "$DATA/DotNetSdk/sdk" -name csc.dll -path "*bincore*" | head -1)
DAG=$(ls -d Library/Bee/artifacts/*.dag | head -1)
OUT="${TMPDIR:-/tmp}/thefog_compila"
mkdir -p "$OUT"
[ -f "$CSC" ] && [ -f "$DAG/Assembly-CSharp.rsp" ] || { echo "Falta el compilador o los .rsp de Unity"; exit 2; }

base() { # argumentos de Unity sin la lista de fuentes ni las salidas
  grep -v -e '\.cs"\?[[:space:]]*$' -e '^-out:' -e '^-refout:' -e '^-doc:' "$1" | tr -d '\r'
}

# 1) Runtime: todo .cs de Assets que no esté en una carpeta Editor.
base "$DAG/Assembly-CSharp.rsp" > "$OUT/runtime.rsp"
echo "-out:\"$(cygpath -m "$OUT")/Assembly-CSharp.dll\"" >> "$OUT/runtime.rsp"
find Assets -name "*.cs" -not -path "*/Editor/*" | sed 's/.*/"&"/' >> "$OUT/runtime.rsp"
echo "== Runtime ($(grep -c '\.cs"$' "$OUT/runtime.rsp") archivos)"
"$DATA/DotNetSdk/dotnet.exe" "$(cygpath -m "$CSC")" -nologo "@$(cygpath -m "$OUT/runtime.rsp")" 2>&1 | grep -E "error|warning CS(0618|0219|0414)" | grep -v "^$" | sort -u > "$OUT/runtime.log"
grep -c "error" "$OUT/runtime.log" | sed 's/^/errores: /'
grep "error" "$OUT/runtime.log" | head -60
R=$(grep -c "error" "$OUT/runtime.log")

# 2) Editor: los .cs de carpetas Editor, contra el runtime recién compilado.
base "$DAG/Assembly-CSharp-Editor.rsp" | grep -v 'Assembly-CSharp\.\(ref\.\)\?dll"' > "$OUT/editor.rsp"
echo "-r:\"$(cygpath -m "$OUT")/Assembly-CSharp.dll\"" >> "$OUT/editor.rsp"
echo "-out:\"$(cygpath -m "$OUT")/Assembly-CSharp-Editor.dll\"" >> "$OUT/editor.rsp"
find Assets -name "*.cs" -path "*/Editor/*" | sed 's/.*/"&"/' >> "$OUT/editor.rsp"
echo "== Editor ($(grep -c '\.cs"$' "$OUT/editor.rsp") archivos)"
E=0
if [ "$R" = "0" ]; then
  "$DATA/DotNetSdk/dotnet.exe" "$(cygpath -m "$CSC")" -nologo "@$(cygpath -m "$OUT/editor.rsp")" 2>&1 | grep -E "error" | sort -u > "$OUT/editor.log"
  E=$(grep -c "error" "$OUT/editor.log")
  echo "errores: $E"
  head -60 "$OUT/editor.log"
else
  echo "(no se compila el editor hasta que el runtime no tenga errores)"
fi
[ "$R" = "0" ] && [ "$E" = "0" ] && echo "COMPILA" && exit 0
exit 1
