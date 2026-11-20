# THE FOG - prepara el audio del juego a partir de los assets descargados y sintetiza lo que falta.
# Uso (Blender trae numpy y el modulo aud, que decodifica mp3/ogg), desde la carpeta del proyecto:
#   "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup -P tools/prepara_audio.py -- <P>/Assets/Resources/Audio [manifiesto.json]
# Con el Python del sistema (python prepara_audio.py <carpeta_salida>) solo hace la parte sintetizada (Sint/), porque no hay aud.
# Todo sale en WAV PCM 16 bit, 22050 Hz, mono (los grillos en estereo). La musica y los golpes de susto (.ogg) se copian tal cual.
# No toca nada fuera de <carpeta_salida> (y del manifiesto, si se pide). No escribe .meta.
# Al final imprime duracion, pico, RMS y salto de bucle de cada archivo y una COMPROBACION con lo que se salga de norma:
# nadie puede oirlo, asi que esos numeros son la unica revision posible.
import os, sys, wave, shutil, json
import numpy as np

try:
    import aud
except Exception:
    aud = None

ORIGEN = r"C:/Users/josem/Desktop/thefogjuego/AssetsDescargados"
V1 = ORIGEN + "/horror_sfx/horror_sfx_vol_1/horror_sfx_vol_1"
V2 = ORIGEN + "/horror_sfx/horror_sfx_vol_2"
MUS = ORIGEN + "/oga_distant_screams_ccbysa/distant_screams_tiny_pack/Distant Screams Music Pack - Tiny Pack"
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
SALIDA = args[0]
SR = 22050
manifiesto = {}

def db(x):
    return 10.0 ** (x / 20.0)

P3 = db(-3.0)     # pico de pasos y efectos
P6 = db(-6.0)
P9 = db(-9.0)

# ---------------------------------------------------------------- utilidades

def filtro(x, sr, alto=None, bajo=None, orden=4):
    """Paso alto / paso bajo tipo Butterworth aplicado en frecuencia (circular: no rompe los bucles)."""
    n = len(x)
    X = np.fft.rfft(x)
    f = np.fft.rfftfreq(n, 1.0 / sr)
    g = np.ones_like(f)
    if alto:
        with np.errstate(divide="ignore"):
            g *= 1.0 / np.sqrt(1.0 + (alto / np.maximum(f, 1e-6)) ** (2 * orden))
    if bajo:
        g *= 1.0 / np.sqrt(1.0 + (f / bajo) ** (2 * orden))
    return np.fft.irfft(X * g, n).astype(np.float32)

def pico(x, nivel=0.7):
    m = float(np.abs(x).max())
    return x if m < 1e-9 else (x * (nivel / m)).astype(np.float32)

def rms_de(x):
    return float(np.sqrt((np.asarray(x, dtype=np.float64) ** 2).mean()))

def a_rms(x, nivel_db, techo_db=-1.0):
    """Normaliza a un RMS dado. Si algun pico pasa del techo lo redondea con tanh y vuelve a ajustar (nunca recorta en seco)."""
    x = np.asarray(x, dtype=np.float32)
    techo = db(techo_db)
    for _ in range(4):
        r = rms_de(x)
        if r < 1e-9:
            return x
        x = (x * (db(nivel_db) / r)).astype(np.float32)
        if float(np.abs(x).max()) <= techo:
            break
        x = (techo * np.tanh(x / techo)).astype(np.float32)
    return x

def fundidos(x, sr, entra=0.003, sale=0.04):
    x = x.copy()
    a = max(1, int(sr * entra)); b = max(1, min(len(x), int(sr * sale)))
    x[:a] *= np.linspace(0, 1, a, dtype=np.float32)
    x[-b:] *= np.linspace(1, 0, b, dtype=np.float32) ** 2
    return x

def bucle_cruzado(x, sr, cruce):
    """Funde el final sobre el principio (igual potencia). El resultado dura len(x)-cruce y enlaza sin chasquido."""
    c = int(sr * cruce); n = len(x)
    a = np.linspace(0, 1, c, dtype=np.float32)
    y = x[:n - c].copy()
    y[:c] = x[:c] * np.sqrt(a) + x[n - c:] * np.sqrt(1 - a)
    return y

def cierra(x, canales=1, ventana=None):
    """Gira un bucle (que ya es circular) para que empiece donde menos se nota la costura: el punto en que dos muestras
    seguidas se parecen mas y estan mas cerca de cero. No cambia el sonido, solo por donde se corta el archivo.
    ventana = segundos desde el principio en los que se puede buscar (None = todo el bucle)."""
    m = np.asarray(x, dtype=np.float32).reshape(-1, canales)
    ant = np.roll(m, 1, axis=0)
    coste = np.abs(m - ant).max(axis=1) + 0.25 * (np.abs(m).max(axis=1) + np.abs(ant).max(axis=1))
    if ventana is not None:
        coste = coste[:max(1, int(SR * ventana))]
    i = int(np.argmin(coste))
    return np.roll(m, -i, axis=0).reshape(-1) if canales > 1 else np.roll(m[:, 0], -i)

def escribe(rel, x, sr=SR, canales=1):
    ruta = os.path.join(SALIDA, rel)
    os.makedirs(os.path.dirname(ruta), exist_ok=True)
    d = np.clip(x, -1, 1)
    with wave.open(ruta, "wb") as w:
        w.setnchannels(canales); w.setsampwidth(2); w.setframerate(sr)
        w.writeframes((d * 32767.0).astype("<i2").tobytes())
    m = d if canales == 1 else d.reshape(-1, canales).mean(axis=1)
    c = d.reshape(-1, canales)
    manifiesto[rel.replace("\\", "/")] = {"s": round(len(m) / sr, 3), "pico_db": round(20 * np.log10(max(float(np.abs(d).max()), 1e-9)), 1),
                                          "rms_db": round(20 * np.log10(max(rms_de(d), 1e-9)), 1),   # de todas las muestras (en estereo, la media de los dos canales por separado)
                                          # salto entre la ultima muestra y la primera (el peor de los canales)
                                          "salto_bucle": round(float(np.abs(c[0] - c[-1]).max()), 4),
                                          # salto normal entre dos muestras seguidas (percentil 99): con que comparar el anterior
                                          "salto_normal": round(float(np.percentile(np.abs(np.diff(c, axis=0)), 99)), 4),
                                          "kb": os.path.getsize(ruta) // 1024}

def carga(ruta, mono=True):
    """Decodifica a float32 y baja a 22050 Hz (paso bajo a 10 kHz y una de cada dos muestras)."""
    s = aud.Sound(ruta)
    sr, ch = s.specs
    d = s.data()
    if mono:
        d = d.mean(axis=1, keepdims=True)
    cols = []
    for c in range(d.shape[1]):
        m = d[:, c].astype(np.float32)
        if int(sr) == 44100:
            m = filtro(m, 44100, bajo=10000, orden=8)[::2]
        cols.append(m)
    return np.stack(cols, axis=1) if not mono else cols[0]

def envolvente(m, sr, ms=5):
    n = int(sr * ms / 1000); k = len(m) // n
    return np.sqrt((m[:k * n].reshape(k, n) ** 2).mean(axis=1))

def detecta_pasos(m, sr):
    """Devuelve [(inicio, pico, fin, nivel_db)] de cada pisada (sobre la senal ya filtrada)."""
    e = envolvente(m, sr, 5)
    emax = e.max(); suelo = np.percentile(e, 20)
    th = max(emax * 10 ** (-20 / 20), suelo * 4); sep = 36
    res = []
    for i in range(1, len(e) - 1):
        if e[i] > th and e[i] == e[max(0, i - sep):i + sep].max():
            lim = max(e[i] * 10 ** (-26 / 20), suelo * 1.5)
            a = i
            while a > 0 and e[a] > lim and i - a < 30:
                a -= 1
            b = i
            while b < len(e) - 1 and e[b] > lim and b - i < 120:
                b += 1
            res.append((a * 0.005, i * 0.005, b * 0.005, 20 * np.log10(e[i] / emax)))
    return res

def pasos(origen, destino, cuantos, alto=90, min_db=-9.0, max_s=0.45, cola=0.05, sale=0.04):
    m = filtro(carga(origen), SR, alto=alto)
    ps = [p for p in detecta_pasos(m, SR) if p[3] >= min_db and 0.03 <= p[2] - p[0] and p[2] < len(m) / SR - 0.02]
    if len(ps) > cuantos:   # se cogen en orden, repartidos por el archivo
        idx = np.linspace(0, len(ps) - 1, cuantos).round().astype(int)
        ps = [ps[i] for i in idx]
    for k, (a, p, b, l) in enumerate(ps):
        i0 = max(0, int((a - 0.012) * SR)); i1 = min(len(m), int((min(b, a + max_s) + cola) * SR))
        escribe("%s_%d.wav" % (destino, k + 1), pico(fundidos(m[i0:i1], SR, 0.003, sale), P3))
    if len(ps) < cuantos:
        print("AVISO: %s solo da %d pisadas de %d" % (destino, len(ps), cuantos))
    return len(ps)

# ---------------------------------------------------------------- 1) de los assets descargados

def de_assets():
    # Pasos. Volumen 1 = pisadas sueltas con silencio entre ellas (limpias). Volumen 2 = cama de ruido y 85-94 % de la energia por debajo de 40 Hz: paso alto obligatorio.
    pasos(V1 + "/Wind Footsteps/Wind Footsteps (1).mp3", "Pasos/madera", 8, alto=70)          # tablas de la cabana (el nombre "Wind" es errata del autor: son golpes secos graves)
    pasos(V1 + "/Wind Footsteps/Wind Footsteps (4).mp3", "Pasos/madera_b", 6, alto=70)
    pasos(V1 + "/Metal Footsteps/Metal Footsteps (1).mp3", "Pasos/metal", 8, alto=70, max_s=0.40, sale=0.08)   # escalera y pasarela de la torre
    pasos(V1 + "/Metal Footsteps/Metal Footsteps (3).mp3", "Pasos/metal_b", 6, alto=70, min_db=-8.0, max_s=0.40, sale=0.08)
    pasos(V1 + "/Concrete Footsteps/Concrete Footsteps (1).mp3", "Pasos/hormigon", 8, alto=70)  # losa del generador / porche
    pasos(V2 + "/Gravel Footsteps/Gravel Footsteps (3).mp3", "Pasos/grava", 8, alto=120, min_db=-10.5)  # camino
    pasos(V1 + "/Leaves Footsteps/Leaves Footsteps (1).mp3", "Pasos/hojas", 8, alto=150, min_db=-6.0, max_s=0.34, sale=0.10)  # bosque

    # Viento. Ninguno es bucle limpio de origen: se recortan los fundidos y se cruza el final con el principio. Lechos a RMS -24 dBFS.
    v = carga(V1 + "/Ambient Wind/Ambient Wind (7).mp3")            # el unico estable de 22 s (rms por segundo -36.9..-35.7 dB)
    v = filtro(v[int(0.15 * SR):len(v) - int(0.10 * SR)], SR, alto=40)
    escribe("Ambiente/viento_bucle.wav", cierra(a_rms(bucle_cruzado(v, SR, 2.0), -24.0)))
    v = carga(V1 + "/Ambient Wind/Ambient Wind (5).mp3")            # rumor grave (61 % entre 40 y 150 Hz): viento contra las paredes, desde dentro
    v = filtro(v[int(0.6 * SR):len(v) - int(0.08 * SR)], SR, alto=35)
    escribe("Ambiente/viento_interior_bucle.wav", cierra(a_rms(bucle_cruzado(v, SR, 1.2), -24.0)))
    for k, n in enumerate((1, 9)):                                   # rachas sueltas (ya traen fundido de entrada y salida)
        v = filtro(carga(V1 + "/Ambient Wind/Ambient Wind (%d).mp3" % n), SR, alto=40)
        escribe("Ambiente/racha_%d.wav" % (k + 1), pico(fundidos(v, SR, 0.3, 1.0), P3))
    v = filtro(carga(V1 + "/Ambient Wind/Ambient Wind (6).mp3"), SR, alto=40)   # tono estrecho hacia 230 Hz: sirve de "aullido" lejano
    escribe("Ambiente/aullido_tonal.wav", pico(fundidos(v, SR, 0.5, 1.5), P3))

    # Grillos: crickets_1.mp3 ya es bucle limpio (lleva etiqueta LAME y el decodificador quita el relleno). Se deja en estereo.
    g = carga(ORIGEN + "/oga_crickets_cc0/crickets_1.mp3", mono=False)
    escribe("Ambiente/grillos_bucle.wav", cierra(a_rms(g.reshape(-1), -24.0), 2), SR, 2)

    # Puertas: todas son chirridos de bisagra (ninguna es un portazo). Niveles muy dispares: se normalizan.
    for nombre, n, ini, fin in (("Puertas/puerta_abrir", 4, 0.10, 1.75), ("Puertas/chirrido_corto_1", 6, 0.08, 1.07), ("Puertas/chirrido_corto_2", 12, 0.02, 1.02),
                                ("Puertas/chirrido_largo", 9, 0.03, 5.04)):
        p = filtro(carga(V1 + "/Creaking Door/Creaking Door (%d).mp3" % n), SR, alto=80)
        escribe(nombre + ".wav", pico(fundidos(p[int(ini * SR):int(fin * SR)], SR, 0.01, 0.12), P3))

    # Musica y golpes de susto (CC BY-SA 4.0, Rest!): los .ogg se copian sin recodificar. La musica empieza y acaba sin fundido: son bucles.
    for src, dst in (("2_Quiet_1_Master.ogg", "Musica/calma_pantano.ogg"), ("3_Quiet_2_Master.ogg", "Musica/calma_torre.ogg"),
                     ("4_Tension_1_Master.ogg", "Musica/tension.ogg"),
                     ("6_Stinger_Encounter_1_Master.ogg", "Sustos/golpe_1.ogg"), ("7_Stinger_Encounter_2_Master.ogg", "Sustos/golpe_2.ogg")):
        ruta = os.path.join(SALIDA, dst)
        os.makedirs(os.path.dirname(ruta), exist_ok=True)
        shutil.copyfile(os.path.join(MUS, src), ruta)
        info = {"copiado": src, "kb": os.path.getsize(ruta) // 1024}
        try:   # solo para el manifiesto: se decodifica para saber cuanto dura y como de alto viene (no se toca el archivo)
            s = aud.Sound(ruta); d = s.data(); m = d.mean(axis=1)
            info.update({"s": round(len(m) / float(s.specs[0]), 3), "pico_db": round(20 * np.log10(max(float(np.abs(d).max()), 1e-9)), 1),
                         "rms_db": round(20 * np.log10(max(rms_de(m), 1e-9)), 1), "hz": int(s.specs[0]), "canales": int(d.shape[1])})
        except Exception as x:
            info["sin_medir"] = str(x)
        manifiesto[dst] = info

# ---------------------------------------------------------------- 2) sintetizado con numpy

def ruido(n, semilla):
    return np.random.default_rng(semilla).standard_normal(n).astype(np.float32)

def t_de(seg):
    n = int(round(SR * seg))
    return n, np.arange(n, dtype=np.float32) / SR

def cae(t, T):
    """Caida exponencial que a los T segundos ha bajado 26 dB (a efectos de oido, 'se ha apagado')."""
    return np.exp(-3.0 * np.clip(t, 0, None) / T)

def golpe(n, t, t0, fuerza):
    """Clac de interruptor / rele: chasquido de contacto + dos resonancias de baquelita + toc grave."""
    u = np.clip(t - t0, 0, None); on = (t >= t0)
    chasq = ruido(n, int(t0 * 1e4) + 7) * np.exp(-u / 0.0015)
    res = np.sin(2 * np.pi * 2300 * u) * np.exp(-u / 0.006) + 0.6 * np.sin(2 * np.pi * 4700 * u) * np.exp(-u / 0.004)
    toc = 0.9 * np.sin(2 * np.pi * 190 * u) * np.exp(-u / 0.02)
    return fuerza * on * (0.8 * chasq + res + toc)

def armonicos(fase, finst, forma, fmax=5000.0):
    """Oscilador sin aliasing: suma de armonicos hasta fmax. forma = 'sierra' (todos, 1/k) o 'cuadrada' (impares, 1/k)."""
    y = np.zeros(len(fase), np.float64)
    for k in range(1, int(fmax / float(finst.min())) + 1):
        if forma == "cuadrada" and k % 2 == 0:
            continue
        y += np.where(k * finst < fmax, np.sin(k * fase) / k, 0.0)
    return y

VOCALES = {"a": (730, 1090, 2440), "e": (530, 1840, 2480), "i": (270, 2290, 3010), "o": (570, 840, 2410), "u": (300, 870, 2240)}

def suaviza(x, seg):
    k = max(1, int(SR * seg))
    return np.convolve(x, np.ones(k) / k, mode="same")

def habla(seg, semilla, f0=118.0, ritmo=5.2, entona=True, aspero=0.0, vibra=0.012):
    """Habla ininteligible: fuente glotal en diente de sierra + tres resonancias que saltan de vocal en vocal por silaba,
    soplos de consonante entre silabas y frases de 2-3 s separadas por pausas de 0,3 s. Empieza y acaba en pausa (0,15 s a cada
    lado, que al repetirse el bucle hacen otra pausa de 0,3 s). Devuelve la senal sin normalizar; las pausas son silencio exacto."""
    rng = np.random.default_rng(semilla)
    n, t = t_de(seg)
    frases = max(1, int(round(seg / 2.8)))
    pesos = rng.uniform(2.0, 3.0, frases)
    dur = pesos / pesos.sum() * (seg - 0.3 * frases)
    amp = np.zeros(n); ventana = np.zeros(n); f0t = np.full(n, float(f0)); soplo = np.zeros(n)
    F = np.zeros((3, n))
    for j in range(3):
        F[j, :] = VOCALES["a"][j]
    nombres = list(VOCALES)
    pos = 0.15
    silabas = 0
    for d in dur:
        ini, fin = pos, pos + d
        j0, j1 = int(ini * SR), int(fin * SR)
        u = np.arange(j1 - j0) / SR
        ventana[j0:j1] = np.minimum(1, u / 0.03) * np.minimum(1, (d - u) / 0.07).clip(0, 1)
        s = ini; ant = None
        while s < fin - 1e-3:
            L = (1.0 / ritmo) * rng.uniform(0.70, 1.20)
            if fin - (s + L) < 0.09:
                L = fin - s
            v = str(rng.choice([x for x in nombres if x != ant])); ant = v
            i0, i1 = int(s * SR), min(n, int((s + L) * SR))
            w = np.arange(i1 - i0) / SR
            fuerza = rng.uniform(0.6, 1.0)
            amp[i0:i1] = fuerza * (0.2 + 0.8 * np.minimum(1, w / 0.025) * np.minimum(1, (L - w) / 0.035).clip(0, 1))
            for j in range(3):
                F[j, i0:i1] = VOCALES[v][j] * rng.uniform(0.96, 1.04)
            if entona:
                x = (s - ini) / d                                    # 0..1 dentro de la frase
                contorno = 1.16 - 0.20 * x - 0.18 * max(0.0, (x - 0.75) / 0.25)   # baja poco a poco y cae al final de la frase
                f0t[i0:i1] = f0 * contorno * rng.uniform(0.95, 1.07)
            if s > ini and rng.random() < 0.65:                      # soplo de consonante (30 ms) en la union de dos silabas
                c0 = max(0, int((s - 0.015) * SR)); c1 = min(n, c0 + int(0.03 * SR))
                soplo[c0:c1] += np.hanning(c1 - c0) * rng.uniform(0.5, 1.0)
            s += L; silabas += 1
        pos = fin + 0.3
    amp = suaviza(amp, 0.008) * ventana
    f0t = suaviza(f0t, 0.03)
    for j in range(3):
        F[j] = suaviza(F[j], 0.03)
    f0t = f0t * (1.0 + vibra * np.sin(2 * np.pi * 5.1 * t))
    if aspero > 0:                                                   # voz rota: el tono tiembla sin orden
        f0t = f0t * (1.0 + 0.03 * aspero * suaviza(np.random.default_rng(semilla + 1).standard_normal(n), 0.004) * 8.0)
    fase = 2 * np.pi * np.cumsum(f0t) / SR
    y = np.zeros(n)
    anchos = (80.0, 100.0, 140.0)
    for k in range(1, int(5000.0 / float(f0t.min())) + 1):
        f = k * f0t
        g = np.ones(n)
        for j in range(3):
            g /= np.sqrt((1.0 - (f / F[j]) ** 2) ** 2 + (f * anchos[j] / F[j] ** 2) ** 2)
        g[f > 5000.0] = 0.0
        y += g / k * np.sin(k * fase)
    y *= amp
    cons = filtro(ruido(n, semilla + 2), SR, alto=2000, bajo=6000, orden=3)
    cons = cons / max(float(np.abs(cons).max()), 1e-9)
    y = y + cons * soplo * ventana * 0.5 * float(np.abs(y).max())
    if aspero > 0:                                                   # aire en la voz
        y = y + cons * amp * 0.10 * aspero * float(np.abs(y).max())
    print("  habla: %.1f s, %d frases, %d silabas (%.1f por segundo de voz)" % (seg, frases, silabas, silabas / float(dur.sum())))
    return y.astype(np.float32)

def sintetiza():
    # --- Generador de gasolina, bucle de 4 s. Monocilindrico de 4 tiempos a 3000 rpm = 25 explosiones por segundo (100 ciclos justos en 4 s).
    n, t = t_de(4.0)
    vaiven = 1.0 + 0.012 * np.sin(2 * np.pi * 0.75 * t)                 # el regulador "caza": 3 ciclos en el bucle
    fase = 2 * np.pi * 25.0 * np.cumsum(vaiven) / SR
    fase *= (2 * np.pi * 100) / fase[-1] * (n - 1) / n                   # obliga a 100 ciclos exactos
    arm = sum(np.sin(k * fase + k * 1.3) / k ** 1.1 for k in range(1, 13))
    pulso = np.exp(-((fase / (2 * np.pi)) % 1.0) * 9.0)                  # golpe de escape que decae dentro de cada ciclo
    escape = filtro(ruido(n, 1), SR, alto=120, bajo=900, orden=2) * pulso * 3.0
    traqueteo = filtro(ruido(n, 2), SR, alto=1500, bajo=3500, orden=2) * (0.35 + 0.65 * pulso) * 0.5
    alternador = 0.25 * np.sin(2 * np.pi * 50 * t) + 0.12 * np.sin(2 * np.pi * 100 * t + 0.5)   # 50 Hz de la red espanola
    gen = filtro(0.55 * arm + escape + traqueteo + alternador, SR, alto=35, bajo=5000, orden=2)
    escribe("Sint/generador_bucle.wav", cierra(pico(gen, 0.7)))

    # --- Estatica de radio / emisora, bucle de 3 s: ruido con forma de banda de voz, respiracion lenta y chasquidos.
    n, t = t_de(3.0)
    r = filtro(ruido(n, 3), SR, alto=350, bajo=4200, orden=2)
    respira = 0.7 + 0.3 * np.sin(2 * np.pi * (2 / 3.0) * t) * np.sin(2 * np.pi * (1 / 3.0) * t + 1.0)
    rng = np.random.default_rng(4)
    chas = np.zeros(n, np.float32)
    for p in rng.integers(0, n - 400, 14):
        chas[p:p + 300] += rng.uniform(0.4, 1.0) * np.exp(-np.arange(300) / 40.0) * rng.choice((-1, 1))
    silbido = 0.05 * np.sin(2 * np.pi * 1800 * t + 6 * np.sin(2 * np.pi * (1 / 3.0) * t))     # heterodino que deriva
    escribe("Sint/estatica_radio_bucle.wav", cierra(pico(r / np.abs(r).max() * 0.5 * respira + filtro(chas, SR, alto=600) * 0.6 + silbido, 0.6)))

    # --- Tele encendida, bucle de 2 s: nieve mas aguda + zumbido de cuadro a 50 Hz (barrido vertical PAL).
    n, t = t_de(2.0)
    nieve = filtro(ruido(n, 5), SR, alto=1200, bajo=9000, orden=2)
    sierra = 2.0 * ((50.0 * t) % 1.0) - 1.0
    cuadro = filtro(sierra.astype(np.float32), SR, alto=45, bajo=1400, orden=2)
    escribe("Sint/tele_nieve_bucle.wav", cierra(pico(nieve / np.abs(nieve).max() * 0.55 + cuadro * 0.22, 0.6)))
    escribe("Sint/tele_zumbido_bucle.wav", cierra(pico(cuadro + 0.15 * nieve / np.abs(nieve).max(), 0.35)))     # tele con imagen: solo el zumbido

    # --- Zumbido electrico (bombilla, cuadro, fluorescente), bucle de 2 s: 100 Hz y armonicos (red de 50 Hz rectificada).
    z = sum(a * np.sin(2 * np.pi * f * t + f) for f, a in ((50, 0.25), (100, 1.0), (200, 0.45), (300, 0.3), (400, 0.12), (600, 0.08)))
    z = z * (1.0 + 0.04 * np.sin(2 * np.pi * 0.5 * t)) + 0.03 * filtro(ruido(n, 6), SR, alto=2000, bajo=6000)
    escribe("Sint/zumbido_electrico_bucle.wav", cierra(pico(z.astype(np.float32), 0.4)))

    # --- Clic de interruptor de pared (0.14 s): contacto + rebote del muelle a los 22 ms, con cuerpo de baquelita.
    n, t = t_de(0.14)
    escribe("Sint/interruptor_clic.wav", pico(fundidos(filtro((golpe(n, t, 0.004, 1.0) + golpe(n, t, 0.026, 0.45)).astype(np.float32), SR, alto=120), SR, 0.001, 0.03), 0.7))
    n, t = t_de(0.10)
    escribe("Sint/boton_clic.wav", pico(fundidos(filtro(golpe(n, t, 0.003, 1.0).astype(np.float32), SR, alto=400), SR, 0.001, 0.03), 0.5))   # tecla de tele / walkie / interfaz

    # --- Dron ambiental, bucle de 16 s: graves desafinados que baten, banda de aire que respira y un parcial agudo que tiembla. Lecho: RMS -24.
    n, t = t_de(16.0)
    q = 1.0 / 16.0                                                       # todas las frecuencias son multiplos de 1/16 Hz: el bucle cierra solo
    d = sum(a * np.sin(2 * np.pi * round(f / q) * q * t + i) for i, (f, a) in enumerate(((55.0, 1.0), (55.44, 0.9), (82.41, 0.5), (82.9, 0.4), (110.31, 0.3), (164.0, 0.12))))
    aire = filtro(ruido(n, 8), SR, alto=180, bajo=420, orden=3)
    aire = aire / np.abs(aire).max() * (0.5 + 0.5 * np.sin(2 * np.pi * 2 * q * t)) * 0.9
    agudo = 0.05 * np.sin(2 * np.pi * round(1243 / q) * q * t) * (0.5 + 0.5 * np.sin(2 * np.pi * 5 * q * t)) ** 3
    escribe("Sint/dron_bucle.wav", cierra(a_rms(filtro((d * 0.35 + aire + agudo).astype(np.float32), SR, alto=35), -24.0)))

    # --- Walkie: pitido de fin de transmision (dos tonos) + cola de squelch, todo en banda de telefono (300-3400 Hz).
    n, t = t_de(0.36)
    fase_t = 2 * np.pi * np.cumsum(np.where(t < 0.09, 1050.0, 1400.0)) / SR      # fase continua: sin chasquido al cambiar de tono
    tono = np.tanh(np.sin(fase_t) * 3.0) * ((t < 0.18) * 1.0)
    cola = ruido(n, 9) * np.where(t >= 0.18, np.exp(-(t - 0.18) / 0.035), 0.0)
    escribe("Sint/walkie_pitido.wav", pico(fundidos(filtro((0.6 * tono + 0.9 * cola).astype(np.float32), SR, alto=300, bajo=3400, orden=3), SR, 0.002, 0.05), 0.6))
    n, t = t_de(0.22)                                                    # abrir canal: chasquido + rafaga de ruido que se cierra
    ab = ruido(n, 10) * np.exp(-t / 0.05) + 2.0 * (t < 0.004)
    escribe("Sint/walkie_abrir.wav", pico(fundidos(filtro(ab.astype(np.float32), SR, alto=400, bajo=3200, orden=3), SR, 0.001, 0.06), 0.55))

    # --- Apagon (0.9 s): el zumbido de la casa cae de tono y se apaga, con el clac del diferencial.
    n, t = t_de(0.9)
    f = 100.0 * np.exp(-t / 0.35)
    ph = 2 * np.pi * np.cumsum(f) / SR
    caida = (np.sin(ph) + 0.4 * np.sin(2 * ph) + 0.2 * np.sin(3 * ph)) * np.exp(-t / 0.3)
    clac = ruido(n, 11) * np.exp(-t / 0.004) + np.sin(2 * np.pi * 320 * t) * np.exp(-t / 0.03)
    escribe("Sint/apagon.wav", pico(fundidos(filtro((0.7 * caida + 0.9 * clac).astype(np.float32), SR, alto=30), SR, 0.001, 0.2), 0.7))

    # --- Liquido cayendo en el deposito (2.4 s): gorgoteo = ruido filtrado + burbujas (tonos cortos que suben).
    n, t = t_de(2.4)
    rng = np.random.default_rng(12)
    chorro = filtro(ruido(n, 13), SR, alto=500, bajo=3500, orden=2) * (0.6 + 0.4 * np.sin(2 * np.pi * 7 * t))
    burb = np.zeros(n, np.float32)
    for _ in range(46):
        p = int(rng.uniform(0.05, 2.2) * SR); L = int(rng.uniform(0.03, 0.07) * SR); u = np.arange(L) / SR
        f0 = rng.uniform(260, 620)
        burb[p:p + L] += np.sin(2 * np.pi * (f0 * u + 0.5 * f0 * 9 * u * u)) * np.hanning(L) * rng.uniform(0.3, 1.0)
    env = np.minimum(1, t / 0.15) * np.minimum(1, (2.4 - t) / 0.4)
    escribe("Sint/gasolina.wav", pico((chorro / np.abs(chorro).max() * 0.35 + burb * 0.5) * env, 0.6))

    # --- Tiron del arranque del generador (0.7 s): carraca de la cuerda + dos explosiones que no cuajan.
    n, t = t_de(0.7)
    carraca = filtro(ruido(n, 14), SR, alto=700, bajo=4000, orden=2) * (np.sin(2 * np.pi * 38 * t) > 0.2) * np.exp(-t / 0.3)
    tos = sum(np.exp(-np.clip(t - t0, 0, None) / 0.03) * (t >= t0) * np.sin(2 * np.pi * 70 * (t - t0)) for t0 in (0.28, 0.42))
    escribe("Sint/generador_tiron.wav", pico(fundidos(filtro((carraca / max(np.abs(carraca).max(), 1e-9) * 0.6 + tos * 0.8).astype(np.float32), SR, alto=40), SR, 0.002, 0.15), 0.7))

    # --- Papel / pasar pagina del diario (0.35 s).
    n, t = t_de(0.35)
    p = filtro(ruido(n, 15), SR, alto=1800, bajo=8000, orden=2) * (np.exp(-((t - 0.09) / 0.05) ** 2) + 0.6 * np.exp(-((t - 0.22) / 0.04) ** 2))
    escribe("Sint/papel.wav", pico(fundidos(p.astype(np.float32), SR, 0.005, 0.05), 0.5))

def sintetiza_nuevos():
    # --- Explosion lejana (4.5 s): el golpe que despierta a Daniel. Pico -3 dBFS.
    n, t = t_de(4.5)
    f = 28.0 + 27.0 * np.exp(-t / 0.45)                                  # seno que cae de 55 a 28 Hz
    seno = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.minimum(1, t / 0.030) * cae(t, 1.6)
    retumbo = filtro(ruido(n, 20), SR, alto=20, bajo=180, orden=3)
    retumbo = retumbo / np.abs(retumbo).max() * np.minimum(1, t / 0.030) * cae(t, 2.5)
    grave = seno + 0.9 * retumbo
    # Cuerpo audible en altavoces pequenos: ruido 120-400 Hz a -6 dB respecto al grave (no puede ser solo subgrave).
    cuerpo = filtro(ruido(n, 21), SR, alto=120, bajo=400, orden=3) * np.minimum(1, t / 0.015) * cae(t, 0.9)
    cuerpo = cuerpo * (db(-6.0) * rms_de(grave[:int(0.9 * SR)]) / rms_de(cuerpo[:int(0.9 * SR)]))
    x = grave + cuerpo
    eco = np.zeros(n); k = int(0.9 * SR)
    eco[k:] = x[:n - k]
    x = x + db(-8.0) * filtro(eco.astype(np.float32), SR, bajo=900, orden=2)   # eco a 0,9 s, a -8 dB y algo mas sordo (viene del valle)
    # Cristales que tiemblan: ruido en bandas estrechas modulado a 28 Hz, de 0,35 a 1,8 s, a -18 dB.
    cris = np.zeros(n)
    for i, fc in enumerate((2200.0, 3100.0, 4300.0)):
        cris += filtro(filtro(ruido(n, 22 + i), SR, alto=fc * 0.97, bajo=fc * 1.03, orden=4), SR, alto=fc * 0.97, bajo=fc * 1.03, orden=4)
    vent = np.clip((t - 0.35) / 0.15, 0, 1) * np.clip((1.8 - t) / 0.5, 0, 1)
    cris = cris * (0.5 + 0.5 * np.sin(2 * np.pi * 28.0 * t)) * vent
    m0 = (t > 0.5) & (t < 1.3)
    cris = cris * (db(-18.0) * float(np.abs(x).max()) / max(float(np.abs(cris[m0]).max()), 1e-9))
    x = filtro((x + cris).astype(np.float32), SR, alto=18, bajo=2500, orden=2)   # paso bajo final 2,5 kHz
    x = pico(fundidos(x, SR, 0.002, 0.8), P3)
    escribe("Sint/explosion_lejana.wav", x)
    X = np.abs(np.fft.rfft(x)) ** 2; fr = np.fft.rfftfreq(len(x), 1.0 / SR)
    tot = X.sum()
    print("  explosion: energia <120 Hz %.0f %%, 120-400 Hz %.0f %%, >400 Hz %.1f %%" % (
        100 * X[fr < 120].sum() / tot, 100 * X[(fr >= 120) & (fr < 400)].sum() / tot, 100 * X[fr >= 400].sum() / tot))

    # --- Voces sin palabras. RMS -20 dBFS. Empiezan y acaban en pausa.
    v = filtro(habla(9.0, 101, f0=118.0, ritmo=5.2), SR, alto=180, bajo=4500, orden=3)          # locutor del informativo
    escribe("Sint/voz_tele_bucle.wav", cierra(a_rms(v, -20.0), ventana=0.1))
    v = filtro(habla(8.0, 202, f0=100.0, ritmo=5.0, aspero=1.0), SR, alto=300, bajo=3000, orden=4)   # el cabo, por la emisora
    v = np.tanh(v / max(float(np.abs(v).max()), 1e-9) * 2.5)                                      # saturacion de radio
    v = a_rms(filtro(v.astype(np.float32), SR, alto=300, bajo=3000, orden=4), -20.0)
    n = len(v)
    suelo = filtro(ruido(n, 203), SR, alto=300, bajo=3000, orden=3)
    v = v + suelo * (db(-30.0) / rms_de(suelo))                                                   # suelo de ruido a -30 dBFS
    escribe("Sint/voz_radio_bucle.wav", cierra(a_rms(v, -20.0), ventana=0.1))
    n, t = t_de(6.0)
    v = habla(6.0, 303, f0=92.0, ritmo=3.2, entona=False, vibra=0.0)                              # la voz que no deberia estar
    v = v * np.sin(2 * np.pi * 38.0 * t)                                                          # modulacion en anillo a 38 Hz (228 ciclos justos)
    escribe("Sint/voz_intrusa_bucle.wav", cierra(a_rms(filtro(v.astype(np.float32), SR, alto=400, bajo=2600, orden=4), -20.0), ventana=0.1))

    # --- Emisora que llama, bucle de 3 s: 1 s de llamada selectiva (1240 + 1560 Hz, dos veces) y 2 s de estatica baja.
    n, t = t_de(3.0)
    tono = np.zeros(n)
    for i, fz in enumerate((1240.0, 1560.0, 1240.0, 1560.0)):
        u = t - 0.25 * i
        tono += np.sin(2 * np.pi * fz * u) * np.clip(u / 0.006, 0, 1) * np.clip((0.25 - u) / 0.006, 0, 1)
    est = filtro(ruido(n, 40), SR, alto=300, bajo=3400, orden=3)
    est = est / rms_de(est) * db(-34.0) * (1.0 + 0.3 * np.sin(2 * np.pi * (1 / 3.0) * t))
    x = filtro((np.tanh(tono * 1.5) * 0.5).astype(np.float32), SR, alto=300, bajo=3400, orden=3)
    escribe("Sint/emisora_llamada_bucle.wav", cierra(pico(x, P6) + est, ventana=None))

    # --- Sintonia del informativo (3.2 s): fanfarria de sintetizador de los 80 por el altavoz de una tele. Pico -6 dB.
    n, t = t_de(3.2)
    notas = ((0.00, 392.00), (0.18, 523.25), (0.36, 659.25), (0.54, 783.99))                      # sol4, do5, mi5, sol5
    fz = np.zeros(n); env = np.zeros(n)
    for i, (t0, nota) in enumerate(notas):
        t1 = notas[i + 1][0] if i + 1 < len(notas) else 3.2
        m = (t >= t0) & (t < t1); u = t[m] - t0
        fz[m] = nota
        if i + 1 < len(notas):
            env[m] = np.minimum(1, u / 0.008) * (0.65 + 0.35 * np.exp(-u / 0.08))
        else:                                                                                     # sol5 sostenido 1,6 s y luego se apaga
            env[m] = np.minimum(1, u / 0.008) * np.where(u < 1.6, 1.0, np.exp(-(u - 1.6) / 0.22))
    vib = 1.0 + 0.006 * np.sin(2 * np.pi * 5.5 * t) * np.clip((t - 0.8) / 0.4, 0, 1)             # vibrato de 5,5 Hz en la nota larga
    cents = 2.0 ** (4.0 / 1200.0)
    fa = fz * vib * cents; fb = fz * vib / cents
    syn = armonicos(2 * np.pi * np.cumsum(fa) / SR, fa, "cuadrada") + 0.8 * armonicos(2 * np.pi * np.cumsum(fb) / SR, fb, "sierra")
    syn = filtro((syn * env).astype(np.float32), SR, bajo=2200, orden=2)
    ft = 90.0 + 30.0 * np.exp(-t / 0.03)
    timbal = np.sin(2 * np.pi * np.cumsum(ft) / SR) * np.exp(-t / 0.16) + 0.3 * filtro(ruido(n, 41), SR, bajo=500, orden=2) / 0.2 * np.exp(-t / 0.02) * 0.2
    x = filtro((syn / np.abs(syn).max() + 0.9 * timbal).astype(np.float32), SR, alto=200, bajo=4000, orden=2)
    escribe("Sint/sintonia_informativo.wav", pico(fundidos(x, SR, 0.002, 0.3), P6))

    # --- Mision nueva (0.9 s): dos notas graves apagadas. Pico -9 dB.
    n, t = t_de(0.9)
    x = np.zeros(n)
    for t0, nota in ((0.0, 146.8), (0.22, 220.0)):
        u = np.clip(t - t0, 0, None)
        tri = 2.0 / np.pi * np.arcsin(np.sin(2 * np.pi * nota * u))
        x += tri * (t >= t0) * np.minimum(1, u / 0.020) * cae(u - 0.020, 0.5)
    escribe("Sint/mision_nueva.wav", pico(fundidos(filtro(x.astype(np.float32), SR, bajo=1200, orden=2), SR, 0.002, 0.15), P9))

    # --- Mision hecha (0.35 s): golpecito apagado + 330 Hz corto. Pico -9 dB.
    n, t = t_de(0.35)
    toque = filtro(ruido(n, 42), SR, bajo=700, orden=2) * np.exp(-t / 0.012) * 3.0 + np.sin(2 * np.pi * 110 * t) * np.exp(-t / 0.03)
    nota = np.sin(2 * np.pi * 330 * t) * np.minimum(1, t / 0.006) * np.exp(-t / 0.07)
    escribe("Sint/mision_hecha.wav", pico(fundidos(filtro((0.7 * toque + nota).astype(np.float32), SR, bajo=1500, orden=2), SR, 0.002, 0.1), P9))

    # --- Luz que se enciende (0.5 s): clac de rele (el golpe() del interruptor) + zumbido de 100 Hz que cae en 0,3 s.
    n, t = t_de(0.5)
    u = np.clip(t - 0.006, 0, None)
    zum = (np.sin(2 * np.pi * 100 * u) + 0.4 * np.sin(2 * np.pi * 200 * u + 0.5) + 0.2 * np.sin(2 * np.pi * 300 * u + 1.0)) * (t >= 0.006) * np.minimum(1, u / 0.004) * cae(u, 0.3)
    x = filtro((golpe(n, t, 0.004, 1.0) + 0.55 * zum).astype(np.float32), SR, alto=60)
    escribe("Sint/luz_enciende.wav", pico(fundidos(x, SR, 0.001, 0.12), P3))

    # --- Despertador de cuerda, bucle de 2.0 s exactos: tic, tac, tic, tac cada 0,5 s.
    n, t = t_de(2.0)
    x = np.zeros(n)
    for i in range(4):
        t0 = 0.05 + 0.5 * i; u = np.clip(t - t0, 0, None); on = (t >= t0)
        fr = 3200.0 if i % 2 == 0 else 2400.0                                                     # tic agudo, tac mas grave
        fuerza = 1.0 if i % 2 == 0 else 0.8
        chasq = ruido(n, 50 + i) * np.exp(-u / 0.001) * (u < 0.003)                               # ruido de 3 ms
        res = np.sin(2 * np.pi * fr * u) * np.exp(-u / 0.004) * (u < 0.012 * 3)                   # resonancia amortiguada (12 ms)
        anillo = 0.06 * np.sin(2 * np.pi * 5600.0 * u) * np.exp(-u / 0.010) * (u < 0.030 * 2)     # anillo metalico de 30 ms, muy bajo
        x += fuerza * on * (0.7 * chasq + res + anillo)
    assert n == 2 * SR
    escribe("Sint/reloj_bucle.wav", pico(filtro(x.astype(np.float32), SR, alto=600, orden=2), P3))

    # --- Brasas, bucle de 8 s: chasquidos al azar (unos 6 por segundo) y algun estallido grave sobre un fondo muy bajo. Todo circular.
    n, t = t_de(8.0)
    rng = np.random.default_rng(60)
    ch = np.zeros(n)
    for _ in range(48):
        p = int(rng.integers(0, n)); L = int(SR * rng.uniform(0.002, 0.008))
        idx = (p + np.arange(L)) % n                                                              # si cae al final, sigue por el principio
        ch[idx] += rng.uniform(0.15, 1.0) ** 2 * rng.standard_normal(L) * np.exp(-np.arange(L) / (L / 4.0))
    ch = filtro(ch.astype(np.float32), SR, alto=1000, bajo=6000, orden=2)
    est = np.zeros(n)
    for _ in range(5):
        p = int(rng.integers(0, n)); L = int(SR * 0.06); u = np.arange(L) / SR
        idx = (p + np.arange(L)) % n
        est[idx] += rng.uniform(0.4, 0.9) * np.sin(2 * np.pi * 400.0 * u) * np.exp(-u / 0.012)
    fondo = filtro(ruido(n, 61), SR, alto=40, bajo=300, orden=2)
    x = pico((ch / np.abs(ch).max() + 0.5 * est).astype(np.float32), P3 * 0.97) + fondo * (db(-40.0) / rms_de(fondo))
    escribe("Sint/brasas_bucle.wav", cierra(x))

    # --- Subsuelo (lo que zumba bajo la torre), bucle de 8 s: senos multiplos de 1/8 Hz con pulso lento + aire. Lecho: RMS -24.
    n, t = t_de(8.0)
    tonal = (np.sin(2 * np.pi * 47.0 * t) + 0.9 * np.sin(2 * np.pi * 47.25 * t + 1.0) + 0.45 * np.sin(2 * np.pi * 94.0 * t + 2.0)
             + 0.25 * np.sin(2 * np.pi * 141.0 * t + 0.5)) * (0.75 + 0.25 * np.sin(2 * np.pi * 0.5 * t))
    aire = filtro(ruido(n, 70), SR, alto=200, bajo=500, orden=3)
    x = tonal + aire * (db(-18.0) * rms_de(tonal) / rms_de(aire))
    escribe("Sint/subsuelo_bucle.wav", cierra(a_rms(x.astype(np.float32), -24.0)))

    # --- Ramas que crujen (0.3 s, dos semillas): chasquido 2-7 kHz de 12 ms, otro 40 ms despues y un golpe grave leve.
    for k, semilla in enumerate((80, 90)):
        n, t = t_de(0.3)
        rng = np.random.default_rng(semilla)
        x = np.zeros(n)
        for t0, fuerza in ((0.004, 1.0), (0.044 + rng.uniform(-0.004, 0.004), rng.uniform(0.5, 0.8))):
            u = np.clip(t - t0, 0, None)
            x += fuerza * ruido(n, semilla + int(t0 * 1000)) * (t >= t0) * np.exp(-u / 0.003) * (u < 0.012 * 2)
        x = filtro(x.astype(np.float32), SR, alto=2000, bajo=7000, orden=2)
        u = np.clip(t - 0.004, 0, None)
        grave = np.sin(2 * np.pi * rng.uniform(140, 190) * u) * (t >= 0.004) * np.exp(-u / 0.03)
        fibras = filtro(ruido(n, semilla + 5), SR, alto=1500, bajo=5000, orden=2) * np.exp(-t / 0.06) * 0.08   # astillas que siguen cediendo
        x = x / np.abs(x).max() + 0.22 * grave + fibras / max(float(np.abs(fibras).max()), 1e-9) * 0.08
        escribe("Sint/rama_%d.wav" % (k + 1), pico(fundidos(x.astype(np.float32), SR, 0.001, 0.08), P3))

# ---------------------------------------------------------------- 3) comprobacion con numeros

ESPERADOS = (["Pasos/madera_%d.wav" % i for i in range(1, 9)] + ["Pasos/madera_b_%d.wav" % i for i in range(1, 7)]
             + ["Pasos/metal_%d.wav" % i for i in range(1, 9)] + ["Pasos/metal_b_%d.wav" % i for i in range(1, 7)]
             + ["Pasos/hormigon_%d.wav" % i for i in range(1, 9)] + ["Pasos/grava_%d.wav" % i for i in range(1, 9)]
             + ["Pasos/hojas_%d.wav" % i for i in range(1, 9)]
             + ["Ambiente/" + x + ".wav" for x in ("viento_bucle", "viento_interior_bucle", "racha_1", "racha_2", "aullido_tonal", "grillos_bucle")]
             + ["Puertas/" + x + ".wav" for x in ("puerta_abrir", "chirrido_corto_1", "chirrido_corto_2", "chirrido_largo")]
             + ["Sustos/golpe_1.ogg", "Sustos/golpe_2.ogg", "Musica/calma_pantano.ogg", "Musica/tension.ogg", "Musica/calma_torre.ogg"])
SINT = ["generador_bucle", "generador_tiron", "estatica_radio_bucle", "tele_nieve_bucle", "tele_zumbido_bucle", "zumbido_electrico_bucle",
        "interruptor_clic", "boton_clic", "dron_bucle", "walkie_pitido", "walkie_abrir", "apagon", "gasolina", "papel",
        "explosion_lejana", "voz_tele_bucle", "voz_radio_bucle", "voz_intrusa_bucle", "emisora_llamada_bucle", "sintonia_informativo",
        "mision_nueva", "mision_hecha", "luz_enciende", "reloj_bucle", "brasas_bucle", "subsuelo_bucle", "rama_1", "rama_2"]
DURACION = {"explosion_lejana": 4.5, "voz_tele_bucle": 9.0, "voz_radio_bucle": 8.0, "voz_intrusa_bucle": 6.0, "emisora_llamada_bucle": 3.0,
            "sintonia_informativo": 3.2, "mision_nueva": 0.9, "mision_hecha": 0.35, "luz_enciende": 0.5, "reloj_bucle": 2.0, "brasas_bucle": 8.0,
            "subsuelo_bucle": 8.0, "rama_1": 0.3, "rama_2": 0.3}
RMS = {"Ambiente/viento_bucle.wav": -24.0, "Ambiente/viento_interior_bucle.wav": -24.0, "Ambiente/grillos_bucle.wav": -24.0,
       "Sint/dron_bucle.wav": -24.0, "Sint/subsuelo_bucle.wav": -24.0,
       "Sint/voz_tele_bucle.wav": -20.0, "Sint/voz_radio_bucle.wav": -20.0, "Sint/voz_intrusa_bucle.wav": -20.0}

def comprueba():
    pegas = []
    quiero = (ESPERADOS if aud is not None else []) + ["Sint/%s.wav" % x for x in SINT]
    for rel in quiero:
        if not os.path.isfile(os.path.join(SALIDA, rel)):
            pegas.append("FALTA %s" % rel)
    for rel in sorted(manifiesto):
        v = manifiesto[rel]
        copiado = "copiado" in v
        if "pico_db" in v and v["pico_db"] > -0.5:
            pegas.append("%s %s: pico %.1f dBFS (por encima de -0,5)" % ("AVISO (ogg copiado sin recodificar)" if copiado else "PICO", rel, v["pico_db"]))
        if "_bucle" in rel and v.get("salto_bucle", 0) > 0.05:
            pegas.append("BUCLE %s: salto %.4f (mas de 0,05)" % (rel, v["salto_bucle"]))
        nombre = os.path.splitext(os.path.basename(rel))[0]
        if rel.startswith("Sint/") and nombre in DURACION and abs(v["s"] - DURACION[nombre]) > 0.0005:
            pegas.append("DURACION %s: %.3f s y deberia durar %.3f" % (rel, v["s"], DURACION[nombre]))
        if rel in RMS and abs(v["rms_db"] - RMS[rel]) > 0.6:
            pegas.append("RMS %s: %.1f dBFS y deberia estar a %.1f" % (rel, v["rms_db"], RMS[rel]))
    return pegas

os.makedirs(SALIDA, exist_ok=True)
if aud is not None:
    de_assets()
sintetiza()
sintetiza_nuevos()
if len(args) > 1:   # segundo argumento opcional: ruta de un .json con el manifiesto (fuera del proyecto, para no meter un TextAsset de mas)
    json.dump({k: {a: (float(b) if isinstance(b, (np.floating, float)) else b) for a, b in v.items()} for k, v in manifiesto.items()},
              open(args[1], "w", encoding="utf-8"), indent=1)
total = 0
for k in sorted(manifiesto):
    v = manifiesto[k]; total += v["kb"]
    print(k, v)
print("ARCHIVOS", len(manifiesto), "TOTAL_KB", total, "aud" if aud is not None else "SIN aud: solo Sint/")
pegas = comprueba()
print("COMPROBACION:", "todo en norma" if not pegas else "%d cosas que mirar" % len(pegas))
for p in pegas:
    print("  " + p)
