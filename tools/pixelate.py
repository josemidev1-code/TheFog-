"""Convierte imágenes (renders, fotos de referencia) en pixel art con la paleta común de THE FOG.

- Reduce a la resolución del juego (por defecto 320 × 180) con filtro de área.
- Paleta fija y compartida por todo el juego (oscura, desaturada; ver docs/pixel/README.md).
  Si no existe, se crea la primera vez a partir de las imágenes de entrada (k-means) y se guarda.
- Tramado ordenado (Bayer 4 × 4) suave para los degradados de niebla y luz.
- Guarda el tamaño real (para Unity) y una vista ampliada ×4 (para revisar).

Uso: python -I tools/pixelate.py <salida_dir> <img1> [img2 ...] [--w 320 --h 180 --paleta tools/paleta_thefog.png --tramado 0.35]
"""
import argparse
import os

from PIL import Image

BAYER4 = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def build_palette(images, n, path):
    pool = Image.new("RGB", (len(images) * 160, 90))
    for i, im in enumerate(images):
        pool.paste(im.resize((160, 90), Image.BOX), (i * 160, 0))
    q = pool.quantize(colors=n, method=Image.Quantize.MEDIANCUT, kmeans=4)
    pal = q.getpalette()[: n * 3]
    cols = [tuple(pal[i:i + 3]) for i in range(0, len(pal), 3)]
    cols.sort(key=lambda c: 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2])
    strip = Image.new("RGB", (len(cols), 1))
    strip.putdata(cols)
    strip.save(path)
    return cols


def load_palette(path):
    im = Image.open(path).convert("RGB")
    return list(im.getdata())


def nearest(c, pal):
    best, bd = pal[0], 1e18
    for p in pal:
        d = (c[0] - p[0]) ** 2 * 0.3 + (c[1] - p[1]) ** 2 * 0.59 + (c[2] - p[2]) ** 2 * 0.11
        if d < bd:
            bd, best = d, p
    return best


def pixelate(im, w, h, pal, strength):
    small = im.convert("RGB").resize((w, h), Image.BOX)
    px = small.load()
    out = Image.new("RGB", (w, h))
    op = out.load()
    cache = {}
    amp = strength * 32.0
    for y in range(h):
        for x in range(w):
            t = (BAYER4[y % 4][x % 4] / 16.0 - 0.5) * amp
            r, g, b = px[x, y]
            c = (max(0, min(255, int(r + t))), max(0, min(255, int(g + t))), max(0, min(255, int(b + t))))
            key = (c[0] >> 2, c[1] >> 2, c[2] >> 2)
            if key not in cache:
                cache[key] = nearest(c, pal)
            op[x, y] = cache[key]
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("out")
    ap.add_argument("images", nargs="+")
    ap.add_argument("--w", type=int, default=320)
    ap.add_argument("--h", type=int, default=180)
    ap.add_argument("--paleta", default=os.path.join(os.path.dirname(__file__), "paleta_thefog.png"))
    ap.add_argument("--colores", type=int, default=32)
    ap.add_argument("--tramado", type=float, default=0.35)
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    imgs = [Image.open(p).convert("RGB") for p in a.images]
    pal = load_palette(a.paleta) if os.path.exists(a.paleta) else build_palette(imgs, a.colores, a.paleta)
    for p, im in zip(a.images, imgs):
        name = os.path.splitext(os.path.basename(p))[0]
        px = pixelate(im, a.w, a.h, pal, a.tramado)
        px.save(os.path.join(a.out, name + ".png"))
        px.resize((a.w * 4, a.h * 4), Image.NEAREST).save(os.path.join(a.out, name + "_x4.png"))
        print(name, "->", a.w, "x", a.h, "con", len(pal), "colores")


if __name__ == "__main__":
    main()
