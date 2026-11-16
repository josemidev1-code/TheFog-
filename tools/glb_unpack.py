"""Separa un .glb en .gltf + .bin + texturas sueltas (PNG/JPG) sin modificar el original.

Sirve para que las texturas de los modelos de Tripo pasen por el ajuste retro de Unity
(RetroTexturePostprocessor solo actúa sobre texturas que son archivos propios).

Uso:  python -I tools/glb_unpack.py <entrada.glb> <carpeta_salida> [nombre]
"""
import json
import os
import struct
import sys

MIME_EXT = {"image/png": ".png", "image/jpeg": ".jpg", "image/webp": ".webp"}


def unpack(src, out_dir, name=None):
    name = name or os.path.splitext(os.path.basename(src))[0]
    data = open(src, "rb").read()
    magic, version, length = struct.unpack_from("<4sII", data, 0)
    if magic != b"glTF" or version != 2:
        raise SystemExit(f"{src}: no es un GLB 2.0")
    off, gltf, bin_chunk = 12, None, b""
    while off < length:
        clen, ctype = struct.unpack_from("<I4s", data, off)
        chunk = data[off + 8: off + 8 + clen]
        if ctype == b"JSON":
            gltf = json.loads(chunk.decode("utf-8"))
        elif ctype == b"BIN\x00":
            bin_chunk = chunk
        off += 8 + clen
    if gltf is None:
        raise SystemExit(f"{src}: sin bloque JSON")

    os.makedirs(out_dir, exist_ok=True)
    views = gltf.get("bufferViews", [])
    image_views = set()
    for i, img in enumerate(gltf.get("images", [])):
        if "bufferView" not in img:
            continue
        bv = views[img["bufferView"]]
        raw = bin_chunk[bv.get("byteOffset", 0): bv.get("byteOffset", 0) + bv["byteLength"]]
        ext = MIME_EXT.get(img.get("mimeType", ""), ".png")
        role = img.get("name") or f"tex{i}"
        safe = "".join(c if c.isalnum() or c in "-_" else "_" for c in role)
        fname = f"{name}_{safe}{ext}"
        open(os.path.join(out_dir, fname), "wb").write(raw)
        image_views.add(img["bufferView"])
        img.pop("bufferView", None)
        img.pop("mimeType", None)
        img["uri"] = fname

    # Reconstruye el binario sin las imágenes y recoloca los bufferViews restantes.
    new_bin, remap = bytearray(), {}
    new_views = []
    for i, bv in enumerate(views):
        if i in image_views:
            continue
        while len(new_bin) % 4:
            new_bin.append(0)
        start = bv.get("byteOffset", 0)
        chunk = bin_chunk[start: start + bv["byteLength"]]
        nbv = dict(bv)
        nbv["byteOffset"] = len(new_bin)
        nbv["buffer"] = 0
        new_bin.extend(chunk)
        remap[i] = len(new_views)
        new_views.append(nbv)
    gltf["bufferViews"] = new_views
    for acc in gltf.get("accessors", []):
        if "bufferView" in acc:
            acc["bufferView"] = remap[acc["bufferView"]]
    bin_name = f"{name}.bin"
    gltf["buffers"] = [{"byteLength": len(new_bin), "uri": bin_name}]
    open(os.path.join(out_dir, bin_name), "wb").write(new_bin)
    with open(os.path.join(out_dir, f"{name}.gltf"), "w", encoding="utf-8") as f:
        json.dump(gltf, f, indent=1)
    return len(gltf.get("images", [])), len(new_bin)


if __name__ == "__main__":
    if len(sys.argv) < 3:
        raise SystemExit(__doc__)
    n, size = unpack(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else None)
    print(f"{sys.argv[1]} -> {sys.argv[2]}: {n} texturas, binario {size/1e6:.2f} MB")
