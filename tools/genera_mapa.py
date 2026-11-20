# Dibuja el mapa de guarda del monte de Valdara (THE FOG) -> Assets/Resources/UI/mapa.bytes (PNG crudo, 210x223).
#   python -I tools/genera_mapa.py [carpeta_del_kit] [carpeta_de_vistas_previas]
# Ventana «window» del kit horneada a 210x223 (cortes 5,18,5,5) con el plano de 200x200 pegado en (5, 18).
# Escala 1 px = 1 m. Ventana de mundo: x de -125 a 75, z de -100 a 100; norte (+z, la mina) arriba.
# Conversion mundo -> plano: u = x + 125, v = 100 - z. Los rotulos NO van en la imagen: los pinta BosqueInterfaz encima.
# Las medidas son copia de MundoBosque.cs (si cambian alli, cambiarlas aqui).
import math, os, random, sys
from PIL import Image, ImageDraw

AQUI = os.path.dirname(os.path.abspath(__file__))
P = os.path.dirname(AQUI)
sys.path.insert(0, AQUI)
from prepara_ui import nueve, KIT_DEFECTO, PREVIA_DEFECTO

PAPEL = (0xDE, 0xCE, 0xA5, 255)
MOTA = (0xC6, 0xB5, 0x8C, 255)
TINTA = (0x39, 0x29, 0x21, 255)
MONTE_CLARO = (0xA5, 0x94, 0x7B, 255)
MONTE_OSCURO = (0x7B, 0x6B, 0x52, 255)
CAMINO = (0x4A, 0x42, 0x31, 255)

SENDEROS = [
    [(0, -150), (-12, -110), (10, -70), (-5, -30), (0, 0), (25, 35), (62, 62), (70, 105), (40, 150)],
    [(0, 0), (-45, 10), (-90, -5)],
]
POSTES = [(-9.52, -1.37), (-30.02, 3.19), (-48.58, 5.22), (-68.50, -1.42), (-88.42, -8.06)]
CLAROS = [(3, -3, 17), (-96, -6, 17), (62, 62, 8)]          # x, z, radio
CASA_UV = (136, 108)
TORRE_UV = (25, 108)

# Huecos sin arbolado para que se lean los rotulos que el juego pinta encima (u0, v0, u1, v1), en px del plano.
# Tienen que corresponder con las posiciones de BosqueInterfaz (tabla de la seccion 6).
CALVAS = [
    (140, 110, 190, 118),   # CASA DEL GUARDA
    (4, 130, 52, 138),      # TORRE DE CONTROL
    (104, 86, 121, 93),     # CRUCE
    (40, 76, 96, 84),       # SENDERO DEL TENDIDO
    (148, 22, 186, 30),     # LA CARBONERA
    (4, 72, 34, 80),        # LAS PENAS
    (130, 188, 186, 196),   # VALDARA (sur) 4 km
    (126, 3, 190, 17),      # PISTA DE LA MINA (norte) / CORTADA
    (138, 15, 192, 22),     # explosion. no subir
    (14, 93, 34, 100),      # sin luz
    (56, 106, 100, 113),    # luces: funcionan
    (137, 169, 194, 186),   # Valdara: sin luz, sin telefono
]


def uv(x, z):
    return (x + 125.0, 100.0 - z)


def dist_segmento(px, py, ax, ay, bx, by):
    """Distancia de (px,py) al segmento y longitud recorrida sobre el hasta el punto mas cercano."""
    dx, dy = bx - ax, by - ay
    l2 = dx * dx + dy * dy
    t = 0.0 if l2 < 1e-9 else max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / l2))
    qx, qy = ax + dx * t, ay + dy * t
    return math.hypot(px - qx, py - qy), t * math.sqrt(l2)


def a_sendero(px, py):
    """(distancia al sendero mas cercano, distancia recorrida a lo largo de el)."""
    mejor = (1e9, 0.0)
    for s in SENDEROS:
        acum = 0.0
        for i in range(len(s) - 1):
            a, b = uv(*s[i]), uv(*s[i + 1])
            d, rec = dist_segmento(px, py, a[0], a[1], b[0], b[1])
            if d < mejor[0]:
                mejor = (d, acum + rec)
            acum += math.hypot(b[0] - a[0], b[1] - a[1])
    return mejor


def en_claro(px, py, margen=0.0):
    for (x, z, r) in CLAROS:
        c = uv(x, z)
        if math.hypot(px - c[0], py - c[1]) < r + margen:
            return True
    return False


def en_calva(px, py):
    for (u0, v0, u1, v1) in CALVAS:
        if u0 - 1 <= px <= u1 + 1 and v0 - 1 <= py <= v1 + 1:
            return True
    return False


# Cifras y letras de 3x5 para la barra de escala y la N de la rosa (no hay fuentes a 1 px por metro).
GLIFOS = {
    "2": ["###", "..#", "###", "#..", "###"],
    "0": ["###", "#.#", "#.#", "#.#", "###"],
    "m": [".....", ".....", "####.", "#.#.#", "#.#.#"],
    "N": ["#..#", "##.#", "#.##", "#..#", "#..#"],
    " ": ["..", "..", "..", "..", ".."],
}


def escribe(px, x, y, texto, color):
    for ch in texto:
        g = GLIFOS[ch]
        for j, fila in enumerate(g):
            for i, c in enumerate(fila):
                if c == "#":
                    px[x + i, y + j] = color
        x += len(g[0]) + 1
    return x


def plano():
    rnd = random.Random(1987)
    im = Image.new("RGBA", (200, 200), PAPEL)
    px = im.load()
    # Mota del papel.
    for _ in range(1500):
        px[rnd.randrange(200), rnd.randrange(200)] = MOTA

    # Distancias al sendero (una vez).
    ds = [[a_sendero(x + 0.5, y + 0.5) for x in range(200)] for y in range(200)]

    # Penas: manchas irregulares a 18-21 px de la torre, dejando libre el lado este (por donde llega el sendero).
    penas = []
    cx, cy = 29, 106
    for k in range(13):
        ang = math.radians(55 + k * 250.0 / 12.0)          # de 55 a 305 grados: todo menos el este
        r = 18 + rnd.random() * 3
        penas.append((cx + math.cos(ang) * r, cy - math.sin(ang) * r, 1.6 + rnd.random() * 1.4))

    def en_pena(x, y, margen=0.0):
        for (qx, qy, qr) in penas:
            if math.hypot(x - qx, y - qy) < qr + margen:
                return True
        return False

    # Monte: arbolitos de 3 px sobre rejilla con desplazamiento fijo, fuera de claros, senderos, penas y rotulos.
    paso = 7
    for gy in range(2, 200, paso):
        for gx in range(2, 200, paso):
            x = gx + rnd.randrange(-2, 3) + (3 if (gy // paso) % 2 else 0)
            y = gy + rnd.randrange(-2, 3)
            variante = rnd.randrange(3)
            if not (3 <= x < 197 and 5 <= y < 197):
                continue
            if en_claro(x, y, 2.0) or ds[y][x][0] < 5.0 or en_calva(x, y) or en_pena(x, y, 3.0):
                continue
            if 4 <= x <= 40 and 180 <= y <= 196:            # sitio de la barra de escala
                continue
            if math.hypot(x - 181, y - 142) < 15:            # sitio de la rosa de los vientos
                continue
            if variante == 0:                                # abeto: punta y falda
                px[x, y - 1] = MONTE_OSCURO
                px[x - 1, y] = MONTE_CLARO; px[x, y] = MONTE_CLARO; px[x + 1, y] = MONTE_CLARO
                px[x, y + 1] = MONTE_CLARO
            elif variante == 1:                              # copa redonda
                px[x - 1, y] = MONTE_CLARO; px[x, y - 1] = MONTE_CLARO; px[x + 1, y] = MONTE_CLARO
                px[x, y + 1] = MONTE_OSCURO
            else:                                            # rayado
                px[x - 1, y + 1] = MONTE_CLARO; px[x, y] = MONTE_CLARO; px[x + 1, y - 1] = MONTE_CLARO

    # Borde de los claros: puntos sueltos.
    for (x, z, r) in CLAROS:
        c = uv(x, z)
        n = int(2 * math.pi * r / 3)
        for k in range(n):
            a = 2 * math.pi * k / n
            qx, qy = int(round(c[0] + math.cos(a) * r)), int(round(c[1] + math.sin(a) * r))
            if 2 <= qx < 198 and 2 <= qy < 198 and ds[qy][qx][0] > 3.0 and not en_calva(qx, qy):
                px[qx, qy] = MONTE_OSCURO

    # Penas.
    for y in range(200):
        for x in range(200):
            if en_pena(x + 0.5, y + 0.5):
                borde = not en_pena(x + 0.5, y + 0.5, -0.9)
                px[x, y] = TINTA if borde else MONTE_CLARO

    # Senderos: doble trazo discontinuo (dos orillas con el centro limpio).
    for y in range(200):
        for x in range(200):
            d, rec = ds[y][x]
            if d < 1.0:
                px[x, y] = PAPEL
            elif d < 2.0 and (rec % 7.0) < 4.5:
                px[x, y] = CAMINO

    d = ImageDraw.Draw(im)

    # Tendido: linea fina de puntos casa -> postes -> torre, con una crucecita en cada poste.
    puntos = [CASA_UV] + [uv(*p) for p in POSTES] + [TORRE_UV]
    for i in range(len(puntos) - 1):
        (ax, ay), (bx, by) = puntos[i], puntos[i + 1]
        n = int(math.hypot(bx - ax, by - ay))
        for k in range(0, n + 1, 2):
            t = k / float(max(1, n))
            px[int(round(ax + (bx - ax) * t)), int(round(ay + (by - ay) * t))] = TINTA
    for p in POSTES:
        qx, qy = [int(round(c)) for c in uv(*p)]
        for k in (-1, 0, 1):
            px[qx + k, qy] = TINTA
            px[qx, qy + k] = TINTA

    # Casa: rectangulo relleno de 6x5 centrado en (136, 108).
    d.rectangle([133, 106, 138, 110], fill=TINTA)
    # Generador: punto de 2 px. Furgoneta: rectangulo 2x4. Pozo: circulito.
    d.rectangle([138, 104, 139, 105], fill=TINTA)
    d.rectangle([132, 97, 133, 100], outline=TINTA, fill=PAPEL)
    for (i, j) in [(0, -1), (-1, 0), (1, 0), (0, 1)]:
        px[124 + i, 110 + j] = TINTA
    # Torre: cuadrado de 5 px con aspa.
    d.rectangle([23, 106, 27, 110], outline=TINTA, fill=PAPEL)
    for k in range(1, 4):
        px[23 + k, 106 + k] = TINTA
        px[27 - k, 106 + k] = TINTA

    # Rosa de los vientos (lineas), con la N arriba; en el margen derecho.
    rx, ry = 181, 144
    for k in range(1, 9):
        px[rx, ry - k] = TINTA
    for k in range(1, 6):
        px[rx, ry + k] = TINTA; px[rx - k, ry] = TINTA; px[rx + k, ry] = TINTA
    px[rx, ry] = TINTA
    px[rx - 1, ry - 7] = TINTA; px[rx + 1, ry - 7] = TINTA          # punta de la aguja norte
    px[rx - 2, ry - 6] = TINTA; px[rx + 2, ry - 6] = TINTA
    for k in (1, 2):                                                # aspas cortas en diagonal
        px[rx - k, ry - k] = CAMINO; px[rx + k, ry - k] = CAMINO; px[rx - k, ry + k] = CAMINO; px[rx + k, ry + k] = CAMINO
    escribe(px, rx - 2, ry - 15, "N", TINTA)

    # Barra de escala «20 m» (20 px), abajo a la izquierda.
    bx, by = 8, 191
    d.line([bx, by, bx + 20, by], fill=TINTA)
    for k in (0, 10, 20):
        d.line([bx + k, by - 2, bx + k, by], fill=TINTA)
    d.line([bx, by + 1, bx + 10, by + 1], fill=TINTA)              # mitad rellena
    escribe(px, bx + 2, by - 9, "20 m", TINTA)

    # Doble filete de marco.
    d.rectangle([0, 0, 199, 199], outline=TINTA)
    d.rectangle([2, 2, 197, 197], outline=CAMINO)
    return im


def main():
    kit = sys.argv[1] if len(sys.argv) > 1 else KIT_DEFECTO
    previa = sys.argv[2] if len(sys.argv) > 2 else PREVIA_DEFECTO
    ventana = os.path.join(kit, "themes", "fog", "window.png")
    if not os.path.isfile(ventana):
        print("No encuentro " + ventana)
        return 2
    mapa = nueve(Image.open(ventana).convert("RGBA"), 5, 18, 5, 5, 210, 223)
    mapa.paste(plano(), (5, 18))
    rojos = sum(1 for y in range(mapa.height) for x in range(mapa.width)
                if mapa.getpixel((x, y))[0] > 150 and mapa.getpixel((x, y))[1] < 90 and mapa.getpixel((x, y))[2] < 90)
    destino = os.path.join(P, "Assets", "Resources", "UI", "mapa.bytes")
    os.makedirs(os.path.dirname(destino), exist_ok=True)
    mapa.save(destino, format="PNG", optimize=True)
    os.makedirs(previa, exist_ok=True)
    mapa.resize((840, 892), Image.NEAREST).convert("RGB").save(os.path.join(previa, "mapa.png"))
    print("Assets/Resources/UI/mapa.bytes  %dx%d  %d bytes  (pixeles rojos: %d)" % (mapa.width, mapa.height, os.path.getsize(destino), rojos))
    print("OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
