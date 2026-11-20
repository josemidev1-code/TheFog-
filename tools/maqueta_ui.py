# Maqueta de las pantallas de la interfaz del bosque (THE FOG): como no se puede abrir Unity, compone con PIL y las
# mismas TTF, piezas .bytes y constantes que BosqueInterfaz*.cs una imagen de cada pantalla, para revisarlas a ojo.
#   python -I tools/maqueta_ui.py [carpeta_de_salida] [ruta de BosqueGuion.cs]
# Las medidas de aqui son copia de BosqueInterfaz.cs / BosqueInterfazPantallas.cs: si se cambia una, cambiar la otra.
# El ajuste de linea y el alto de renglon son los de FreeType, muy parecidos (no identicos) a los de Unity.
import io, math, os, random, re, sys
from PIL import Image, ImageDraw, ImageFont

AQUI = os.path.dirname(os.path.abspath(__file__))
P = os.path.dirname(AQUI)
RES = os.path.join(P, "Assets", "Resources")
SALIDA_DEFECTO = os.path.join(P, "docs", "entorno", "ui_maquetas")
GUION_PROYECTO = os.path.join(P, "Assets", "Scripts", "Entorno", "BosqueGuion.cs")
GUION_CONTRATO = ("C:/Users/josem/AppData/Local/Temp/claude/185df850129c049a/ca85b398-328f-40fd-930e-1fbad95e0950/"
                  "scratchpad/diseno/contrato_final/BosqueGuion.cs")


def hexa(v, a=1.0):
    return ((v >> 16) & 255, (v >> 8) & 255, v & 255, int(round(a * 255)))


C_TEXTO, C_RESALTADO, C_PENSAMIENTO, C_APAGADO = hexa(0xD4CEC0), hexa(0xE3DFD6), hexa(0xDBD4B3), hexa(0x92929A)
C_ROJO, C_AMBAR, C_SOMBRA, C_TINTA = hexa(0xD63929), hexa(0xE8B565), hexa(0x0A0A09, 0.85), hexa(0x392921)
C_LAPIZ, C_FOSFORO, C_VELO = hexa(0x842921), hexa(0xB8F2B8), (0, 0, 0, int(0.72 * 255))
C_LOCUTOR, C_RELEVO, C_RESPALDO, C_FILETE, C_PAPEL = hexa(0xC6AD94), hexa(0xA5947B), hexa(0x181810, 0.9), hexa(0xC6AD94), hexa(0xDECEA5)
C_FILO_CLARO, C_FILO_OSCURO = hexa(0x7B6B52), hexa(0x080808)
SEPARADOR = "  \u00b7  "


# ------------------------------------------------------------------ Textos: se leen de BosqueGuion.cs
class Guion:
    def __init__(self, ruta):
        self.fuente = open(ruta, encoding="utf-8").read()
        self.consts = {}
        for m in re.finditer(r"public const string\s+(.*?);\s*$", self.fuente, re.S | re.M):
            self._declaraciones(m.group(1))

    @staticmethod
    def _literales(texto):
        """Lista de cadenas del trozo de codigo, uniendo las que van sumadas con +."""
        res, i, n, pegar = [], 0, len(texto), False
        while i < n:
            c = texto[i]
            if c == '"':
                j, s = i + 1, []
                while texto[j] != '"':
                    if texto[j] == "\\":
                        s.append({"n": "\n", "t": "\t"}.get(texto[j + 1], texto[j + 1])); j += 2
                    else:
                        s.append(texto[j]); j += 1
                s = "".join(s)
                if pegar and res:
                    res[-1] += s
                else:
                    res.append(s)
                pegar = False
                i = j + 1
            elif c == "+":
                pegar = True; i += 1
            elif c == "/" and texto[i:i + 2] == "//":
                while i < n and texto[i] != "\n":
                    i += 1
            else:
                if not c.isspace():
                    pegar = False
                i += 1
        return res

    def _declaraciones(self, cuerpo):
        # nombre = "..." (+ "...")*, nombre2 = ...
        for m in re.finditer(r"(\w+)\s*=\s*((?:\"(?:[^\"\\]|\\.)*\"\s*\+?\s*)+)", cuerpo):
            lits = self._literales(m.group(2))
            self.consts[m.group(1)] = "".join(lits) if len(lits) > 1 and "+" in m.group(2) else lits[0]

    def c(self, nombre):
        return self.consts[nombre]

    def lista(self, nombre):
        m = re.search(r"\b" + nombre + r"\s*=\s*\{(.*?)\};", self.fuente, re.S)
        cuerpo = m.group(1)
        # los identificadores (Locutor, Cabo...) se sustituyen por nada: solo interesan los literales
        return self._literales(cuerpo)


# ------------------------------------------------------------------ Lienzo con las primitivas de BosqueInterfaz
class Fuente:
    def __init__(self, archivo, tam):
        self.f = ImageFont.truetype(os.path.join(RES, "Fuentes", archivo), tam)
        asc, desc = self.f.getmetrics()
        self.renglon = asc + desc
        self.tam = tam

    def ancho(self, t):
        return int(math.ceil(self.f.getlength(t)))

    def parte(self, t, ancho):
        """Ajuste de linea por palabras, como wordWrap."""
        lineas = []
        for parrafo in t.split("\n"):
            palabras = parrafo.split(" ")
            actual = ""
            for p in palabras:
                prueba = p if actual == "" else actual + " " + p
                if self.f.getlength(prueba) <= ancho or actual == "":
                    actual = prueba
                else:
                    lineas.append(actual); actual = p
            lineas.append(actual)
        return lineas

    def alto(self, t, ancho):
        return 0 if not t else len(self.parte(t, ancho)) * self.renglon


_piezas = {}


def pieza(ruta):
    if ruta not in _piezas:
        f = os.path.join(RES, ruta + ".bytes")
        _piezas[ruta] = Image.open(io.BytesIO(open(f, "rb").read())).convert("RGBA") if os.path.isfile(f) else None
    return _piezas[ruta]


def R(x, y, w, h):
    return (int(round(x)), int(round(y)), int(round(w)), int(round(h)))


class Lienzo:
    def __init__(self, w, h, fondo):
        self.w, self.h = w, h
        self.e = max(1, h // 240)
        self.im = fondo.copy().convert("RGB")
        self.d = ImageDraw.Draw(self.im, "RGBA")
        t_sub = max(17, round(h / 32))
        vt_g = 16 * max(1, round(h / 24 / 16))
        vt_p = 16 * max(1, round(h / 45 / 16))
        while vt_p > 16 and vt_p > t_sub:
            vt_p -= 16
        self.tam_papel = max(15, round(h / 40))
        B, S, N, V = "BarlowCondensed-Medium.ttf", "BarlowCondensed-SemiBold.ttf", "SpecialElite-Regular.ttf", "VT323-Regular.ttf"
        self.sHud = Fuente(B, max(16, round(h / 36)))
        self.sMisTitulo = Fuente(S, max(16, round(h / 36)))
        self.sBarra = Fuente(S, max(14, round(h / 40)))
        self.sPeq = Fuente(B, max(13, round(h / 50)))
        self.sSub = Fuente(B, t_sub)
        self.sSubHabla = Fuente(S, max(14, round(h / 40)))
        self.sVtG = Fuente(V, vt_g)
        self.sVtP = Fuente(V, vt_p)
        self.sTarTitulo = Fuente(S, max(28, round(h / 13)))
        self.sTarCuerpo = Fuente(N, max(18, round(h / 30)))
        self.sTarPie = Fuente(B, max(12, round(h / 60)))
        self.sPapel = Fuente(N, self.tam_papel)
        self.sMapa = Fuente(N, max(11, round(h / 60)))
        self.avisos = []

    # ---- primitivas
    def caja(self, r, c):
        x, y, w, h = r
        if w <= 0 or h <= 0 or c[3] <= 0:
            return
        self.d.rectangle([x, y, x + w - 1, y + h - 1], fill=c)

    def texto(self, r, t, s, c, alfa, ancla, envolver, sombra):
        """ancla: dos letras, vertical (U, M, L) y horizontal (L, C, R)."""
        if not t or alfa <= 0.003:
            return
        x, y, w, h = r
        lineas = s.parte(t, w) if envolver else t.split("\n")
        total = len(lineas) * s.renglon
        y0 = y if ancla[0] == "U" else (y + (h - total) / 2.0 if ancla[0] == "M" else y + h - total)
        for i, linea in enumerate(lineas):
            lw = s.f.getlength(linea)
            x0 = x if ancla[1] == "L" else (x + (w - lw) / 2.0 if ancla[1] == "C" else x + w - lw)
            yy = y0 + i * s.renglon
            if x0 < -1 or x0 + lw > self.w + 1 or yy < -1 or yy + s.renglon > self.h + 1:
                self.avisos.append("texto fuera de la pantalla: " + linea[:40])
            if sombra:
                self.d.text((int(round(x0)) + self.e, int(round(yy)) + self.e), linea, font=s.f, fill=C_SOMBRA[:3] + (int(C_SOMBRA[3] * alfa),))
            self.d.text((int(round(x0)), int(round(yy))), linea, font=s.f, fill=c[:3] + (int(c[3] * alfa),))

    def pega(self, im, x, y, alfa=1.0):
        if alfa < 0.999:
            im = im.copy()
            im.putalpha(im.getchannel("A").point(lambda v: int(v * alfa)))
        self.im.paste(im, (int(round(x)), int(round(y))), im)

    def pieza(self, ruta, r, alfa=1.0):
        t = pieza(ruta)
        if t is None:
            return False
        x, y, w, h = r
        self.pega(t.resize((w, h), Image.NEAREST), x, y, alfa)
        return True

    def marco(self, ruta, l, t, r_, b, r, alfa=1.0):
        im = pieza(ruta)
        x, y, w, h = r
        if im is None:
            self.respaldo(r, C_RESPALDO, alfa)
            return
        e = self.e
        iw, ih = im.size
        out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        xs = [(0, l, 0, l * e), (l, iw - r_, l * e, w - r_ * e), (iw - r_, iw, w - r_ * e, w)]
        ys = [(0, t, 0, t * e), (t, ih - b, t * e, h - b * e), (ih - b, ih, h - b * e, h)]
        for (ox0, ox1, dx0, dx1) in xs:
            for (oy0, oy1, dy0, dy1) in ys:
                if ox1 <= ox0 or oy1 <= oy0 or dx1 <= dx0 or dy1 <= dy0:
                    continue
                out.paste(im.crop((ox0, oy0, ox1, oy1)).resize((dx1 - dx0, dy1 - dy0), Image.NEAREST), (dx0, dy0))
        self.pega(out, x, y, alfa)

    def respaldo(self, r, fondo, alfa=1.0):
        x, y, w, h = r
        e = self.e
        self.caja(r, fondo[:3] + (int(fondo[3] * alfa),))
        f = C_FILETE[:3] + (int(255 * alfa),)
        self.caja((x, y, w, e), f); self.caja((x, y + h - e, w, e), f); self.caja((x, y, e, h), f); self.caja((x + w - e, y, e, h), f)

    def divisor(self, r):
        x, y, w, h = r
        e = self.e
        t = pieza("UI/fog/divider")
        c = 8 * e
        if t is None or w < 3 * c:
            self.caja((x, y + 2 * e, w, e), C_FILO_CLARO)
            return
        medio = int(round(x + (w - c) * 0.5))
        for k, xx in ((0, x), (1, medio), (2, x + w - c)):
            self.pega(t.crop((8 * k, 0, 8 * k + 8, 5)).resize((c, 5 * e), Image.NEAREST), xx, y)
        for (a0, a1) in ((x + c, medio), (medio + c, x + w - c)):
            self.caja((a0, y + 2 * e, a1 - a0, e), C_FILO_CLARO)
            self.caja((a0, y + 3 * e, a1 - a0, e), C_FILO_OSCURO)

    # ---- HUD
    def mision(self, titulo, detalle, cabecera, color_cab, alfa=1.0):
        e, w = self.e, self.w
        ancho = round(min(130 * e, 0.36 * w))
        m = 6 * e
        ep = max(1, (e + 1) // 2)
        sangria = 6 * ep + 2 * e
        dentro = ancho - 2 * m
        h_cab = self.sPeq.renglon
        h_tit = self.sMisTitulo.alto(titulo, dentro - sangria)
        h_det = self.sPeq.alto(detalle, dentro)
        alto = m + h_cab + e + h_tit + ((e + h_det) if h_det > 0 else 0) + m
        r = R(6 * e, 6 * e, ancho, alto)
        self.marco("UI/fog/panel", 5, 5, 5, 5, r, alfa)
        x, y = r[0] + m, r[1] + m
        self.texto((x, y, dentro, h_cab), cabecera, self.sPeq, color_cab, alfa, "UL", False, True)
        y += h_cab + e
        hr = self.sMisTitulo.renglon
        self.pieza("UI/fog/pointer", R(x, y + (hr - 11 * ep) * 0.5, 6 * ep, 11 * ep), alfa)
        self.texto((x + sangria, y, dentro - sangria, h_tit), titulo, self.sMisTitulo, C_RESALTADO, alfa, "UL", True, True)
        y += h_tit + e
        if h_det > 0:
            self.texto((x, y, dentro, h_det), detalle, self.sPeq, C_APAGADO, alfa, "UL", True, True)
        return r

    def aviso(self, rotulo):
        e, w, h = self.e, self.w, self.h
        t = "[E]  " + rotulo
        tw, th = self.sHud.ancho(t), self.sHud.renglon
        r = R(w * 0.5 - tw * 0.5 - 5 * e, h * 0.60 - th * 0.5 - 3 * e, tw + 10 * e, th + 6 * e)
        self.marco("UI/fog/tooltip", 4, 4, 4, 4, r)
        self.texto(r, t, self.sHud, C_RESALTADO, 1.0, "MC", False, True)

    def pista(self, t, alfa=1.0):
        e, w = self.e, self.w
        self.texto(R(w * 0.3, 6 * e, w * 0.7 - 6 * e, self.sPeq.renglon), t, self.sPeq, C_AMBAR, alfa, "UR", False, True)

    def teclas(self, t):
        e, w, h = self.e, self.w, self.h
        th = self.sPeq.renglon
        self.texto(R(w * 0.3, h - 10 * e - th, w * 0.7 - 6 * e, th), t, self.sPeq, C_APAGADO, 0.5, "LR", False, True)

    def negro(self, a):
        self.caja((0, 0, self.w, self.h), (0, 0, 0, int(255 * a)))

    def tarjeta(self, titulo, cuerpo, pie, a=1.0):
        e, w, h = self.e, self.w, self.h
        h_tit = self.sTarTitulo.renglon if titulo else 0
        y_tit = h * 0.30 - h_tit * 0.5
        if titulo:
            self.texto(R(0, y_tit, w, h_tit), titulo, self.sTarTitulo, C_RESALTADO, a, "MC", False, True)
        if cuerpo:
            hc = self.sTarCuerpo.alto(cuerpo, w * 0.6)
            self.texto(R(w * 0.2, y_tit + h_tit + 4 * e, w * 0.6, hc), cuerpo, self.sTarCuerpo, C_TEXTO, a, "UC", True, True)
            if y_tit + h_tit + 4 * e + hc > h * 0.78 and pie:
                self.avisos.append("el cuerpo de la tarjeta pisa el pie")
        if pie:
            self.texto(R(w * 0.1, h * 0.78, w * 0.8, h * 0.19), pie, self.sTarPie, C_APAGADO, a, "MC", True, True)

    def zona(self, walkie):
        if not walkie:
            return 0, self.w
        return 0, max(self.w * 0.4, self.rect_walkie()[0] - 4 * self.e)

    def pensamientos(self, textos, techo_sub=None, walkie=False):
        """techo_sub: y mas alta que ocupa el subtitulo (lo que devuelve techo_subtitulo), o None si no hay."""
        e, w, h = self.e, self.w, self.h
        zx, za = self.zona(walkie)
        ancho = min(0.7 * w, za - 12 * e)
        x = zx + (za - ancho) * 0.5
        y = h * 0.80 + self.sHud.renglon
        if techo_sub is not None:
            y = min(y, techo_sub - 2 * e)
        for t in reversed(textos):
            alto = self.sHud.alto(t, ancho)
            y -= alto
            self.texto(R(x, y, ancho, alto), t, self.sHud, C_PENSAMIENTO, 1.0, "UC", True, True)
            y -= e

    def color_hablante(self, hb):
        return {"LOCUTOR": C_LOCUTOR, "MUNICIPAL": C_AMBAR, "CABO SALCEDO": C_AMBAR, "RELEVO": C_RELEVO, "DANIEL": C_TEXTO,
                "VOZ": C_ROJO}.get(hb, C_RESALTADO)

    def subtitulo(self, hablante, t, origen, compacto=False, walkie=False):
        e, w, h = self.e, self.w, self.h
        zx, za = self.zona(walkie)
        ancho = round(min(min(300 * e, 0.72 * w), za - 8 * e))
        mx = 12 * e
        pad = (3 if compacto else 4) * e
        ancho_texto = ancho - 2 * mx
        h_texto = self.sSub.alto(t, ancho_texto)
        dentro = compacto and bool(origen)
        h_habla = self.sSubHabla.renglon if hablante else 0
        if dentro:
            h_habla = max(h_habla, self.sVtP.renglon)
        alto = pad + h_habla + h_texto + pad
        y1 = h - (3 if compacto else 22) * e
        r = R(zx + (za - ancho) * 0.5, y1 - alto, ancho, alto)
        self.caja((r[0], r[1] + e, r[2], r[3] - 2 * e), (0, 0, 0, 127))
        self.marco("UI/fog/dialogue", 12, 4, 12, 4, r)
        y = r[1] + pad
        if dentro:
            wh = self.sSubHabla.ancho(hablante) if hablante else 0
            ws = self.sSubHabla.ancho(SEPARADOR) if hablante else 0
            wo = self.sVtP.ancho(origen)
            x = r[0] + (r[2] - (wh + ws + wo)) * 0.5
            if hablante:
                self.texto(R(x, y, wh, h_habla), hablante, self.sSubHabla, self.color_hablante(hablante), 1.0, "ML", False, True)
                self.texto(R(x + wh, y, ws, h_habla), SEPARADOR, self.sSubHabla, C_APAGADO, 1.0, "ML", False, True)
            self.texto(R(x + wh + ws, y, wo, h_habla), origen, self.sVtP, C_APAGADO, 1.0, "ML", False, True)
        elif hablante:
            self.texto(R(r[0] + mx, y, ancho_texto, h_habla), hablante, self.sSubHabla, self.color_hablante(hablante), 1.0, "MC", False, True)
        self.texto(R(r[0] + mx, y + h_habla, ancho_texto, h_texto), t, self.sSub, C_RESALTADO, 1.0, "UC", True, True)
        if origen and not compacto:
            ho = self.sVtP.renglon
            self.texto(R(r[0] + 2 * e, r[1] - ho, r[2], ho), origen, self.sVtP, C_APAGADO, 1.0, "LL", False, True)
        return r

    def techo_subtitulo(self, hablante, t, origen, walkie=False):
        """Como MideSubtitulo (modo normal): y mas alta de la caja, contando la etiqueta de origen."""
        e, w, h = self.e, self.w, self.h
        zx, za = self.zona(walkie)
        ancho = round(min(min(300 * e, 0.72 * w), za - 8 * e))
        alto = 8 * e + (self.sSubHabla.renglon if hablante else 0) + self.sSub.alto(t, ancho - 24 * e)
        return h - 22 * e - alto - (self.sVtP.renglon if origen else 0)

    # ---- Pantallas
    def rect_subido(self, ancho, alto):
        e, w, h = self.e, self.w, self.h
        y0 = max(2 * e, (h - alto * e) * 0.5 - 12 * e)
        return R((w - ancho * e) * 0.5, y0, ancho * e, alto * e)

    def hoja(self, p):
        if not self.pieza("UI/papel", p):
            self.respaldo(p, C_PAPEL)

    def cuerpo_papel(self, r, t):
        tam = self.tam_papel
        s = Fuente("SpecialElite-Regular.ttf", tam)
        while tam > 11 and s.alto(t, r[2]) > r[3]:
            tam -= 1
            s = Fuente("SpecialElite-Regular.ttf", tam)
        if s.alto(t, r[2]) > r[3]:
            self.avisos.append("el texto no cabe en el papel ni a 11 px")
        self.texto(r, t, s, C_TINTA, 1.0, "UL", True, False)
        return tam

    def pie_pantalla(self, y, t):
        self.texto(R(0, y, self.w, self.sPeq.renglon), t, self.sPeq, C_TEXTO, 0.85, "UC", False, True)

    def nota(self, titulo, t, pie):
        e = self.e
        self.caja((0, 0, self.w, self.h), C_VELO)
        p = self.rect_subido(230, 200)
        self.hoja(p)
        x, ancho = p[0] + 16 * e, 198 * e
        y = p[1] + 16 * e
        if titulo:
            ht = self.sPapel.alto(titulo.upper(), ancho)
            self.texto(R(x, y, ancho, ht), titulo.upper(), self.sPapel, C_TINTA, 1.0, "UL", True, False)
            y += ht + 2 * e
            self.divisor(R(x, y, ancho, 5 * e))
            y += 8 * e
        alto = p[1] + p[3] - 16 * e - y
        tam = self.cuerpo_papel(R(x, y, ancho, alto), t)
        self.pie_pantalla(p[1] + p[3] + e, pie)
        return p, tam

    def diario(self, g, entradas, i):
        e = self.e
        self.caja((0, 0, self.w, self.h), C_VELO)
        p = self.rect_subido(230, 200)
        self.hoja(p)
        x, ancho = p[0] + 16 * e, 198 * e
        hr = self.sPapel.renglon
        self.texto(R(x, p[1] + 13 * e, ancho, hr), g.c("DiarioTitulo"), self.sPapel, C_TINTA, 1.0, "LL", False, False)
        self.texto(R(x, p[1] + 13 * e, ancho, hr), g.c("DiarioSubtitulo"), self.sMapa, C_TINTA, 0.75, "LR", False, False)
        fecha, cuerpo = entradas[i]
        self.texto(R(x, p[1] + 22 * e, ancho, hr), fecha, self.sPapel, C_LAPIZ, 1.0, "LL", False, False)
        self.divisor(R(x, p[1] + 32 * e, ancho, 5 * e))
        tam = self.cuerpo_papel(R(x, p[1] + 40 * e, ancho, 136 * e), cuerpo)
        self.texto(R(p[0], p[1] + 179 * e, p[2], 8 * e), "%d/%d" % (i + 1, len(entradas)), self.sMapa, C_TINTA, 0.8, "MC", False, False)
        self.pie_pantalla(p[1] + p[3] + e, g.c("DiarioPie"))
        return p, tam

    def rotulo(self, m, u, v, t, c):
        e = self.e
        self.texto(R(m[0] + (5 + u) * e, m[1] + (18 + v) * e, 120 * e, self.sMapa.renglon), t, self.sMapa, c, 1.0, "UL", False, False)

    def en_mapa(self, m, x, z):
        e = self.e
        u = min(197.0, max(3.0, x + 125.0)); v = min(197.0, max(3.0, 100.0 - z))
        return round(m[0] + (5 + u) * e), round(m[1] + (18 + v) * e)

    def mapa(self, g, notas, objetivo, jugador, rumbo):
        e = self.e
        self.caja((0, 0, self.w, self.h), C_VELO)
        m = self.rect_subido(210, 223)
        if not self.pieza("UI/mapa", m):
            self.respaldo(m, C_RESPALDO)
            self.caja(R(m[0] + 5 * e, m[1] + 18 * e, 200 * e, 200 * e), C_PAPEL)
        barra = R(m[0] + 8 * e, m[1] + 3 * e, 194 * e, 12 * e)
        self.texto(barra, g.c("MapaTitulo"), self.sBarra, C_RESALTADO, 1.0, "ML", False, True)
        self.texto(barra, g.c("MapaPuesto"), self.sPeq, C_APAGADO, 1.0, "MR", False, True)
        for (u, v, n) in ((142, 112, "MapaCasa"), (6, 132, "MapaTorre"), (106, 88, "MapaCruce"), (48, 82, "MapaSendero"),
                          (150, 24, "MapaCarbonera"), (6, 74, "MapaPenas"), (132, 190, "MapaValdara"), (128, 5, "MapaPista")):
            self.rotulo(m, u, v, g.c(n), C_TINTA)
        self.rotulo(m, 150, 11, g.c("MapaCortada"), C_ROJO)
        if "SinLuz" in notas:
            self.rotulo(m, 16, 95, g.c("MapaNotaSinLuz"), C_LAPIZ)
        if "LucesFuncionan" in notas:
            self.rotulo(m, 58, 108, g.c("MapaNotaLuces"), C_LAPIZ)
        if "ValdaraSinLuz" in notas:
            self.texto(R(m[0] + (5 + 139) * e, m[1] + (18 + 171) * e, 56 * e, 16 * e), g.c("MapaNotaValdara"), self.sMapa, C_LAPIZ, 1.0, "UL", True, False)
        if "Explosion" in notas:
            self.rotulo(m, 140, 17, g.c("MapaNotaExplosion"), C_LAPIZ)
        if objetivo is not None:
            cx, cy = self.en_mapa(m, objetivo[0], objetivo[1])
            self.pieza("UI/fog/mark_item", R(cx - 2.5 * e, cy - 2.5 * e, 5 * e, 5 * e))
        if jugador is not None:
            cx, cy = self.en_mapa(m, jugador[0], jugador[1])
            f = Image.new("RGBA", (7, 7), (0, 0, 0, 0))
            for j in range(7):
                k = (j + 1) // 2
                for i in range(3 - k, 3 + k + 1):
                    f.putpixel((i, j), (8, 8, 8, 255) if (i in (3 - k, 3 + k) or j == 6) else C_ROJO)
            f = f.resize((7 * e, 7 * e), Image.NEAREST).rotate(-rumbo, resample=Image.NEAREST)
            self.pega(f, cx - 3.5 * e, cy - 3.5 * e)
            hr = self.sMapa.renglon
            self.texto(R(cx + 5 * e, cy - hr * 0.5, 40 * e, hr), g.c("MapaYo"), self.sMapa, C_LAPIZ, 1.0, "ML", False, False)
        self.pie_pantalla(m[1] + m[3] + e, g.c("MapaPie"))
        return m

    def rect_walkie(self):
        e, w, h = self.e, self.w, self.h
        return R(w - 8 * e - 160 * e, h - 8 * e - self.sPeq.renglon - e - 100 * e, 160 * e, 100 * e)

    def walkie(self, g, canales, canal, parpadeo=True):
        e = self.e
        r = self.rect_walkie()
        if not self.pieza("UI/walkie", r):
            self.respaldo(r, C_RESPALDO)
            self.caja(R(r[0] + 8 * e, r[1] + 24 * e, 144 * e, 52 * e), (0, 0, 0, 255))
        barra = R(r[0] + 8 * e, r[1] + 3 * e, 144 * e, 12 * e)
        self.texto(barra, g.c("WalkieTitulo"), self.sBarra, C_RESALTADO, 1.0, "ML", False, True)
        self.texto(barra, g.c("WalkieAparato"), self.sPeq, C_APAGADO, 1.0, "MR", False, True)
        nombre, estado, t, alerta = canales[canal]
        x, y, ancho = r[0] + 11 * e, r[1] + 27 * e, 138 * e
        hr = self.sVtP.renglon
        self.texto(R(x, y, ancho, hr), nombre, self.sVtP, C_FOSFORO, 1.0, "UL", False, False)
        if alerta:
            self.texto(R(x, y + hr, ancho, hr), estado, self.sVtP, C_ROJO, 1.0 if parpadeo else 0.25, "UL", False, False)
        else:
            self.texto(R(x, y + hr, ancho, hr), estado, self.sVtP, C_FOSFORO, 0.7, "UL", False, False)
        if len(self.sVtP.parte(t, ancho)) > 2 or 4 * hr > 46 * e:
            self.avisos.append("el texto del walkie no cabe en el recuadro: " + t[:30])
        self.texto(R(x, y + 2 * hr, ancho, 2 * hr), t, self.sVtP, C_FOSFORO, 1.0, "UL", True, False)
        n = len(canales)
        lado, hueco = 3 * e, 3 * e
        x0 = r[0] + r[2] * 0.5 - (n * lado + (n - 1) * hueco) * 0.5
        for i in range(n):
            self.caja(R(x0 + i * (lado + hueco), r[1] + 84 * e, lado, lado), C_FOSFORO if i == canal else C_FOSFORO[:3] + (51,))
        self.texto(R(r[0], r[1] + r[3] + e, r[2], self.sPeq.renglon), g.c("WalkiePie"), self.sPeq, C_TEXTO, 0.85, "UC", False, True)
        return r

    def barrido(self, r, fase=0.37):
        x, y, w, h = r
        texel = max(1, self.e // 2)
        for yy in range(y + 3 * texel, y + h, 4 * texel):    # la fila oscura es la de abajo de cada grupo de 4
            self.caja((x, yy, w, texel), (0, 0, 0, 71))
        alto = int(min(self.h * 0.03, h * (1 - fase)))
        self.caja((x, int(y + h * fase), w, alto), (255, 255, 255, 11))

    def nieve(self, r, alfa):
        x, y, w, h = r
        rnd = random.Random(4)
        base = Image.new("L", (64, 48))
        base.putdata([rnd.randrange(20, 236) for _ in range(64 * 48)])
        mosaico = Image.new("L", (256, 192))
        for j in range(4):
            for i in range(4):
                mosaico.paste(base, (i * 64, j * 48))
        g = mosaico.resize((w, h), Image.NEAREST)
        capa = Image.merge("RGBA", (g, g, g, Image.new("L", (w, h), int(255 * alfa))))
        self.im.paste(capa, (x, y), capa)

    def tele(self, g, cabecera, subcabecera, pie, imagen=None):
        e, w, h = self.e, self.w, self.h
        self.caja((0, 0, w, h), (0, 0, 0, 255))
        r = R(w * 0.06, h * 0.06, w * 0.88, h * 0.88)
        if imagen is not None:
            self.im.paste(imagen.resize((r[2], r[3])), (r[0], r[1]))
        elif cabecera is None:
            self.nieve(r, 0.8)
        else:
            self.nieve(r, 0.25)
            hg, hp = self.sVtG.renglon, self.sVtP.renglon
            self.texto(R(r[0], r[1] + r[3] * 0.10, r[2], hg), cabecera, self.sVtG, C_FOSFORO, 1.0, "UC", False, True)
            self.texto(R(r[0], r[1] + r[3] * 0.10 + hg + 2 * e, r[2], hg), subcabecera, self.sVtG, C_FOSFORO, 0.8, "UC", False, True)
            if pie:
                f = R(r[0], h * 0.62, r[2], hp + 4 * e)
                self.caja(f, (0, 0, 0, 153))
                self.caja((f[0], f[1], f[2], e), C_FOSFORO[:3] + (140,))
                self.texto(R(f[0] + 10 * e, f[1] + e, f[2] - 20 * e, f[3] - e), pie, self.sVtP, C_FOSFORO, 1.0, "ML", False, False)
        self.barrido(r)
        ht = self.sPeq.renglon
        self.texto(R(w * 0.3, h - 2 * e - ht, w * 0.7 - 6 * e, ht), g.c("TelePieTeclas"), self.sPeq, C_APAGADO, 1.0, "LR", False, True)

    def monitor(self, g, imagen, nombre):
        e, w, h = self.e, self.w, self.h
        self.caja((0, 0, w, h), (0, 0, 0, 255))
        r = R(w * 0.06, h * 0.06, w * 0.88, h * 0.88)
        if imagen is not None:
            v = imagen.resize((r[2], r[3])).convert("RGB")
            bandas = v.split()
            v = Image.merge("RGB", (bandas[0].point(lambda p: int(p * 0.62)), bandas[1].point(lambda p: int(p * 0.85)), bandas[2].point(lambda p: int(p * 0.62))))
            self.im.paste(v, (r[0], r[1]))
        self.barrido(r)
        hg = self.sVtG.renglon
        self.texto(R(w * 0.08, h * 0.08, w * 0.8, hg), nombre, self.sVtG, C_FOSFORO, 1.0, "UL", False, True)
        reloj = "03:1" + "4" + "   REC"
        y_pie = h * 0.92 - hg
        self.texto(R(w * 0.08, y_pie, w * 0.5, hg), reloj, self.sVtG, C_FOSFORO, 1.0, "UL", False, True)
        lado = round(hg * 0.34)
        self.caja(R(w * 0.08 + self.sVtG.ancho(reloj) + 3 * e, y_pie + (hg - lado) * 0.5, lado, lado), C_ROJO)
        self.texto(R(w * 0.3, y_pie, w * 0.62, hg), g.c("MonitorPie"), self.sVtG, C_FOSFORO, 1.0, "UR", False, True)


# ------------------------------------------------------------------ Fondos de prueba (no son el juego: solo dan contraste)
def fondo_cabana(w, h):
    """Interior de noche: penumbra calida, una ventana fria y un foco de lampara (el caso claro para el contraste)."""
    rnd = random.Random(7)
    im = Image.new("RGB", (w // 8, h // 8))
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            fx, fy = x / im.width, y / im.height
            luz = max(0.0, 1.0 - math.hypot(fx - 0.62, (fy - 0.45) * 1.3) * 2.2)
            base = 26 + 18 * fy + rnd.randrange(-3, 4)
            r_, g_, b_ = base + 120 * luz, base * 0.92 + 88 * luz, base * 0.8 + 40 * luz
            if 0.12 < fx < 0.27 and 0.2 < fy < 0.5:      # ventana
                r_, g_, b_ = 70 + rnd.randrange(8), 86 + rnd.randrange(8), 98 + rnd.randrange(8)
            px[x, y] = (int(min(255, r_)), int(min(255, g_)), int(min(255, b_)))
    return im.resize((w, h), Image.NEAREST)


def fondo_niebla(w, h):
    """Exterior de madrugada con niebla: gris azulado bastante claro (el peor caso para el texto claro)."""
    rnd = random.Random(9)
    im = Image.new("RGB", (w // 8, h // 8))
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            fy = y / im.height
            v = 92 - 50 * abs(fy - 0.45) * 2 + rnd.randrange(-4, 5)
            if rnd.random() < 0.02 or (x % 23 in (0, 1) and 0.2 < fy < 0.8):   # troncos
                v *= 0.45
            px[x, y] = (int(v * 0.86), int(v * 0.95), int(v))
    return im.resize((w, h), Image.NEAREST)


def main():
    salida = sys.argv[1] if len(sys.argv) > 1 else SALIDA_DEFECTO
    ruta_guion = sys.argv[2] if len(sys.argv) > 2 else (GUION_PROYECTO if os.path.isfile(GUION_PROYECTO) else GUION_CONTRATO)
    g = Guion(ruta_guion)
    os.makedirs(salida, exist_ok=True)
    avisos = []

    diario = g.lista("Diario")
    entradas = [(diario[i], diario[i + 1]) for i in range(0, len(diario), 2)]
    for extra in ("DiarioApagon", "DiarioGenerador", "DiarioLlamada"):
        l = g.lista(extra)
        entradas.append((l[0], l[1]))
    noticia = g.lista("Noticia")
    llamada = g.lista("Llamada")
    canales_tarde = [(g.lista("WalkieCanales")[0], g.c("WalkieSenal"), g.c("WalkieTardePuesto"), False),
                     (g.lista("WalkieCanales")[1], g.c("WalkieSenal"), g.c("WalkieTardeValdara"), False),
                     (g.lista("WalkieCanales")[2], g.c("WalkieSenal"), g.c("WalkieTardeEmergencias"), False)]
    canales_llamada = [(g.lista("WalkieCanales")[0], g.c("WalkieSinSenal"), g.c("WalkieNadie"), False),
                       (g.lista("WalkieCanales")[1], g.c("WalkieLlamada"), g.c("WalkieConteste"), True),
                       (g.lista("WalkieCanales")[2], g.c("WalkieSinRepetidor"), g.c("WalkieMudo"), True)]

    def guarda(l, nombre):
        ruta = os.path.join(salida, nombre + ".png")
        l.im.save(ruta)
        for a in l.avisos:
            avisos.append(nombre + ": " + a)
        print("  " + nombre + ".png")

    for (w, h) in ((1920, 1080), (1280, 720)):
        suf = "" if h == 1080 else "_720"
        cab, nie = fondo_cabana(w, h), fondo_niebla(w, h)
        print("%dx%d (e = %d)" % (w, h, max(1, h // 240)))

        # 01 HUD: mision recien llegada, aviso, pista, teclas y dos pensamientos.
        l = Lienzo(w, h, cab)
        m = g.lista("MInformativo")
        l.mision(m[0], m[1], g.c("MisionNueva"), C_ROJO)
        l.aviso(g.c("RotTeleMirar"))
        l.pista(g.c("DiarioNueva"))
        l.teclas(g.c("TeclasHud"))
        l.pensamientos([g.c("Inicio1"), g.c("Inicio2")])
        guarda(l, "01_hud_mision_nueva" + suf)

        # 02 HUD con subtitulo de la tele (tres renglones no hay: la linea mas larga) y pensamiento encima.
        l = Lienzo(w, h, cab)
        m = g.lista("MAcostarse")
        l.mision(m[0], m[1], g.c("MisionCabecera"), C_APAGADO)
        l.teclas(g.c("TeclasHud"))
        l.pensamientos([g.c("PiensaValdara")], l.techo_subtitulo("LOCUTOR", max(noticia, key=len), g.c("OrigenTele")))
        l.subtitulo("LOCUTOR", max(noticia, key=len), g.c("OrigenTele"))
        guarda(l, "02_hud_subtitulo_tele" + suf)

        # 03 HUD en el exterior con niebla: mision cumplida («HECHO»), tres pensamientos y subtitulo del walkie.
        l = Lienzo(w, h, nie)
        m = g.lista("MRepostar")
        l.mision(m[0], m[1], g.c("MisionHecha"), C_AMBAR)
        l.aviso(g.c("RotGenArrancar"))
        l.pista(g.c("WalkieAviso"))
        l.teclas(g.c("WalkieMapaTeclas"))
        l.pensamientos([g.c("Luces1"), g.c("Luces2"), g.c("WalkieLlamando")], l.techo_subtitulo("MUNICIPAL", g.lista("WalkieCabo")[1], g.c("OrigenWalkie5")))
        l.subtitulo("MUNICIPAL", g.lista("WalkieCabo")[1], g.c("OrigenWalkie5"))
        guarda(l, "03_hud_exterior_niebla" + suf)

        # 04 Nota.
        l = Lienzo(w, h, cab)
        l.nota("Parte de incidencias", "Puesto Forestal 03. Semana del 2 al 8 de noviembre.\n\n"
               "Lunes: sin novedad.\nMartes: sin novedad.\nMi\u00e9rcoles: corte de corriente de once minutos. Arranca el generador.\n"
               "Jueves: sin novedad.\nViernes: ruido en la c\u00e1mara 2. Se revisa el cable.\n\n"
               "(Texto de muestra de la maqueta: las notas reales salen de la escena.)", g.c("NotaPie"))
        guarda(l, "04_nota" + suf)

        # 05 Diario: todas las paginas (para ver que caben) y la mas larga con subtitulo compacto.
        tams = []
        for i in range(len(entradas)):
            l = Lienzo(w, h, cab)
            p, tam = l.diario(g, entradas, i)
            tams.append(tam)
            if i in (0, 3):
                guarda(l, "05_diario_pagina%d%s" % (i + 1, suf))
            else:
                for a in l.avisos:
                    avisos.append("diario pagina %d%s: %s" % (i + 1, suf, a))
        print("    letra del cuerpo del diario por pagina (base %d): %s" % (l.tam_papel, tams))
        l = Lienzo(w, h, cab)
        p, tam = l.diario(g, entradas, 3)
        r = l.subtitulo("LOCUTOR", max(noticia, key=len), g.c("OrigenTele"), compacto=True)
        print("    papel hasta y = %d; subtitulo compacto desde y = %d" % (p[1] + p[3], r[1]))
        guarda(l, "06_diario_con_subtitulo" + suf)

        # 07 Mapa con todas las anotaciones, objetivo y jugador; y con subtitulo compacto.
        l = Lienzo(w, h, nie)
        mrect = l.mapa(g, ("SinLuz", "LucesFuncionan", "ValdaraSinLuz", "Explosion"), (-100, -8), (-20, 4), 285)
        guarda(l, "07_mapa" + suf)
        l = Lienzo(w, h, nie)
        mrect = l.mapa(g, ("SinLuz",), (12.62, -3.74), (11, -8), 40)
        r = l.subtitulo("MUNICIPAL", g.lista("WalkieCabo")[0], g.c("OrigenWalkie5"), compacto=True)
        print("    mapa hasta y = %d; subtitulo compacto desde y = %d" % (mrect[1] + mrect[3], r[1]))
        guarda(l, "08_mapa_con_subtitulo" + suf)

        # 09 Walkie por la tarde (sin subtitulo) y 10 con llamada entrante, subtitulo y pensamiento.
        l = Lienzo(w, h, cab)
        m = g.lista("MInformativo")
        l.mision(m[0], m[1], g.c("MisionCabecera"), C_APAGADO)
        l.walkie(g, canales_tarde, 1)
        guarda(l, "09_walkie_tarde" + suf)
        l = Lienzo(w, h, nie)
        m = g.lista("MTorre")
        l.mision(m[0], m[1], g.c("MisionCabecera"), C_APAGADO)
        l.walkie(g, canales_llamada, 1)
        l.pensamientos([g.c("WalkieNoOye")], l.techo_subtitulo("MUNICIPAL", g.lista("WalkieCabo")[1], g.c("OrigenWalkie5"), walkie=True), walkie=True)
        l.subtitulo("MUNICIPAL", g.lista("WalkieCabo")[1], g.c("OrigenWalkie5"), walkie=True)
        guarda(l, "10_walkie_llamada" + suf)

        # 11 Tele con rotulos y subtitulo; 12 tele sin senal.
        l = Lienzo(w, h, cab)
        l.tele(g, g.c("TeleCadena"), g.c("TelePrograma"), g.lista("TeleRotulos")[2])
        l.subtitulo("LOCUTOR", noticia[4], g.c("OrigenTele"))
        guarda(l, "11_tele_informativo" + suf)
        l = Lienzo(w, h, cab)
        l.tele(g, None, None, None)
        guarda(l, "12_tele_sin_senal" + suf)

        # 13 Monitor de camaras.
        l = Lienzo(w, h, cab)
        l.monitor(g, nie, "CAM 2 \u00b7 SENDERO")
        guarda(l, "13_monitor" + suf)

        # 14-16 Tarjetas sobre negro; 17 la llamada final por la emisora.
        l = Lienzo(w, h, cab); l.negro(1.0)
        l.tarjeta(g.c("InicioTitulo"), g.c("InicioCuerpo"), None)
        guarda(l, "14_tarjeta_inicio" + suf)
        l = Lienzo(w, h, cab); l.negro(1.0)
        l.tarjeta(g.c("MadrugadaTitulo"), g.c("MadrugadaCuerpo"), None)
        l.pensamientos([g.c("Despertar1")])
        guarda(l, "15_tarjeta_madrugada" + suf)
        l = Lienzo(w, h, cab); l.negro(1.0)
        l.tarjeta(g.c("FinTitulo"), g.c("FinCuerpo"), g.c("FinPie"))
        guarda(l, "16_tarjeta_final" + suf)
        l = Lienzo(w, h, nie)
        m = g.lista("MEmisora")
        l.mision(m[0], m[1], g.c("MisionCabecera"), C_APAGADO)
        l.subtitulo("VOZ", llamada[13], g.c("OrigenEmisora"))
        guarda(l, "17_llamada_voz" + suf)

        # Comprobacion de medidas: todas las lineas habladas caben en tres renglones como mucho.
        l = Lienzo(w, h, cab)
        ancho_texto = round(min(300 * l.e, 0.72 * w)) - 24 * l.e
        peor = max(len(l.sSub.parte(t, ancho_texto)) for t in noticia + llamada + g.lista("WalkieRelevo") + g.lista("WalkieCabo"))
        print("    renglones del subtitulo mas largo: %d" % peor)

    if avisos:
        print("AVISOS:")
        for a in avisos:
            print("  " + a)
    else:
        print("Sin avisos de medidas.")
    print("OK -> " + salida)
    return 0


if __name__ == "__main__":
    sys.exit(main())
