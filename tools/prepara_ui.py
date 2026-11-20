# Prepara los recursos de la interfaz del bosque (THE FOG) a partir del kit «PSX Horror UI Free» (tema fog, CC BY 4.0).
#   python -I tools/prepara_ui.py [carpeta_del_kit] [carpeta_de_vistas_previas]
# Escribe en Assets/Resources/UI/** las piezas como PNG crudo con extension .bytes (Unity las importa como TextAsset y el
# juego las decodifica con Texture2D.LoadImage, asi nada depende del importador de texturas), hornea la hoja de papel y el
# aparato del walkie, y copia las fuentes a Assets/Resources/Fuentes/. Deja ademas una vista previa x4 de cada pieza.
import os, shutil, sys
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
P = os.path.dirname(AQUI)
KIT_DEFECTO = ("C:/Users/josem/Desktop/thefogjuego/AssetsDescargados/psx_horror_ui_free_ccby/psx_horror_ui_free/"
               "godot-psx-horror-ui-free-f11f099a7db65bcb6893cd6223ac557ed30691a6/addons/psx_horror_ui")
PREVIA_DEFECTO = ("C:/Users/josem/AppData/Local/Temp/claude/185df850129c049a/ca85b398-328f-40fd-930e-1fbad95e0950/"
                  "scratchpad/diseno/ui_previa")

# pieza -> (ancho, alto) esperados; los cortes de cada una estan en la tabla 5.2 y en UiFog/BosqueInterfaz.
PIEZAS = {"panel": (18, 18), "dialogue": (40, 20), "tooltip": (12, 12), "divider": (24, 5), "pointer": (6, 11), "mark_item": (5, 5)}
FUENTES = ["BarlowCondensed-Medium.ttf", "BarlowCondensed-SemiBold.ttf", "SpecialElite-Regular.ttf", "VT323-Regular.ttf",
           "BarlowCondensed-OFL.txt", "SpecialElite-LICENSE.txt", "VT323-OFL.txt"]


def nueve(im, l, t, r, b, W, H):
    """im: imagen RGBA 1x. l,t,r,b: bordes (izquierda, arriba, derecha, abajo). Devuelve W x H px.
    Esquinas copiadas; bordes y centro en mosaico, recortando la ultima baldosa."""
    w, h = im.size
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    xs = [(0, l, 0, l), (l, w - r, l, W - r), (w - r, w, W - r, W)]      # (ox0, ox1, dx0, dx1)
    ys = [(0, t, 0, t), (t, h - b, t, H - b), (h - b, h, H - b, H)]
    for (ox0, ox1, dx0, dx1) in xs:
        for (oy0, oy1, dy0, dy1) in ys:
            if ox1 <= ox0 or oy1 <= oy0 or dx1 <= dx0 or dy1 <= dy0:
                continue
            trozo = im.crop((ox0, oy0, ox1, oy1))
            tw, th = trozo.size
            y = dy0
            while y < dy1:
                x = dx0
                while x < dx1:
                    rec = trozo.crop((0, 0, min(tw, dx1 - x), min(th, dy1 - y)))
                    out.paste(rec, (x, y))
                    x += tw
                y += th
    return out


def guarda(im, destino, previa, nombre):
    os.makedirs(os.path.dirname(destino), exist_ok=True)
    im.save(destino, format="PNG", optimize=True)
    if previa:
        os.makedirs(previa, exist_ok=True)
        fondo = Image.new("RGBA", (im.width * 4 + 16, im.height * 4 + 16), (60, 70, 70, 255))
        fondo.alpha_composite(im.resize((im.width * 4, im.height * 4), Image.NEAREST), (8, 8))
        fondo.convert("RGB").save(os.path.join(previa, "pieza_" + nombre + ".png"))
    print("  %-26s %3dx%-3d %5d bytes" % (os.path.relpath(destino, P).replace("\\", "/"), im.width, im.height, os.path.getsize(destino)))


def papel(fog):
    return nueve(Image.open(os.path.join(fog, "document.png")).convert("RGBA"), 6, 12, 6, 12, 230, 200)


def walkie(fog):
    v = nueve(Image.open(os.path.join(fog, "window.png")).convert("RGBA"), 5, 18, 5, 5, 160, 100)
    m = nueve(Image.open(os.path.join(fog, "monitor.png")).convert("RGBA"), 4, 4, 4, 4, 144, 52)
    v.alpha_composite(m, (8, 24))
    return v


def main():
    kit = sys.argv[1] if len(sys.argv) > 1 else KIT_DEFECTO
    previa = sys.argv[2] if len(sys.argv) > 2 else PREVIA_DEFECTO
    fog = os.path.join(kit, "themes", "fog")
    if not os.path.isdir(fog):
        print("No encuentro el tema fog del kit en: " + fog)
        return 2
    ui = os.path.join(P, "Assets", "Resources", "UI")
    print("Piezas del kit:")
    for nombre, tam in PIEZAS.items():
        im = Image.open(os.path.join(fog, nombre + ".png")).convert("RGBA")
        if im.size != tam:
            print("  AVISO: %s mide %s y se esperaba %s (revisar los cortes)" % (nombre, im.size, tam))
        guarda(im, os.path.join(ui, "fog", nombre + ".bytes"), previa, nombre)
    print("Piezas horneadas:")
    guarda(papel(fog), os.path.join(ui, "papel.bytes"), previa, "papel")
    guarda(walkie(fog), os.path.join(ui, "walkie.bytes"), previa, "walkie")
    print("Fuentes:")
    origen = os.path.join(P, "Assets", "Art", "Puesto", "Fuentes")
    destino = os.path.join(P, "Assets", "Resources", "Fuentes")
    os.makedirs(destino, exist_ok=True)
    for f in FUENTES:
        o = os.path.join(origen, f)
        if not os.path.isfile(o):
            print("  AVISO: falta " + o)
            continue
        shutil.copyfile(o, os.path.join(destino, f))
        print("  Assets/Resources/Fuentes/" + f)
    print("OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
