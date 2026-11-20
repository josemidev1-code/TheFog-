using System.Collections.Generic;
using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>
    /// Las pantallas modales de BosqueInterfaz: nota, diario, mapa, walkie, tele y monitor de cámaras. Solo dibujo:
    /// aquí no se lee el teclado ni se cambia el estado (salvo el ajuste del tamaño de letra del papel, que es una caché).
    /// </summary>
    public partial class BosqueInterfaz
    {
        // ---- Estado de las pantallas (lo fijan los Abrir… de BosqueInterfaz.cs)
        string notaTitulo, notaTexto;
        IList<EntradaDiario> diario;
        int diarioPagina;
        System.Action<int> alPasar;
        NotaMapa mapaNotas;
        bool mapaHayObjetivo;
        Vector3 mapaObjetivo;
        CanalWalkie[] canales;
        int canal;
        System.Action<int> alSintonizar;
        Texture imagen;
        string monitorNombre;

        string papelClave;   // texto para el que ya se ajustó el tamaño de letra del cuerpo
        float papelAncho, papelAlto;

        static readonly Color cFiloClaro = Hex(0x7B6B52), cFiloOscuro = Hex(0x080808), cVerdeMonitor = new Color(0.62f, 0.85f, 0.62f);
        const string GrabandoReloj = "03:1", GrabandoRotulo = "   REC", Barra = "/";

        // ------------------------------------------------------------ Papel (nota y diario)

        /// <summary>Papel de 230e × 200e y mapa de 210e × 223e: centrados en horizontal y subidos, para dejar sitio al subtítulo compacto.</summary>
        Rect RectSubido(float w, float h, int ancho, int alto)
        {
            float y0 = Mathf.Max(2 * e, (h - alto * e) * 0.5f - 12 * e);
            return R((w - ancho * e) * 0.5f, y0, ancho * e, alto * e);
        }

        void Hoja(Rect r)
        {
            if (!Pieza("UI/papel", r, 1f)) Respaldo(r, cPapel, 1f);
        }

        /// <summary>Baja la letra del cuerpo de 1 en 1 hasta que el texto quepa en el rectángulo (mínimo 11).</summary>
        void AjustaPapel(string texto, float ancho, float alto)
        {
            if (ReferenceEquals(papelClave, texto) && papelAncho == ancho && papelAlto == alto) return;
            int tam = tamPapel;
            sPapelCuerpo.fontSize = tam;
            while (tam > 11 && Alto(sPapelCuerpo, texto, ancho) > alto)
            {
                tam--;
                sPapelCuerpo.fontSize = tam;
            }
            papelClave = texto; papelAncho = ancho; papelAlto = alto;
        }

        /// <summary>
        /// Filete «divider» del kit a lo ancho de r (alto 5e). La pieza tiene un rombo en el centro, así que no se estira:
        /// se pintan sus dos puntas, el centro tal cual y dos tramos lisos entre medias.
        /// </summary>
        void Divisor(Rect r)
        {
            var t = UiFog.Textura("UI/fog/divider");
            float c = 8 * e;
            if (t == null || r.width < 3 * c)
            {
                Caja(new Rect(r.x, r.y + 2 * e, r.width, e), cFiloClaro);
                return;
            }
            float medio = Mathf.Round(r.x + (r.width - c) * 0.5f);
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y, c, 5 * e), t, new Rect(0f, 0f, 1f / 3f, 1f));
            GUI.DrawTextureWithTexCoords(new Rect(medio, r.y, c, 5 * e), t, new Rect(1f / 3f, 0f, 1f / 3f, 1f));
            GUI.DrawTextureWithTexCoords(new Rect(r.xMax - c, r.y, c, 5 * e), t, new Rect(2f / 3f, 0f, 1f / 3f, 1f));
            float a0 = r.x + c, a1 = medio, b0 = medio + c, b1 = r.xMax - c;
            Caja(new Rect(a0, r.y + 2 * e, a1 - a0, e), cFiloClaro);
            Caja(new Rect(a0, r.y + 3 * e, a1 - a0, e), cFiloOscuro);
            Caja(new Rect(b0, r.y + 2 * e, b1 - b0, e), cFiloClaro);
            Caja(new Rect(b0, r.y + 3 * e, b1 - b0, e), cFiloOscuro);
        }

        void PieDePantalla(float w, float y, string texto)
        {
            Texto(R(0, y, w, Renglon(sPeq)), texto, sPeq, cTexto, 0.85f, TextAnchor.UpperCenter, false, true);
        }

        void DibujaNota(float w, float h)
        {
            Caja(new Rect(0, 0, w, h), cVelo);
            Rect p = RectSubido(w, h, 230, 200);
            Hoja(p);
            float x = p.x + 16 * e, ancho = 198 * e;
            float y = p.y + 16 * e;
            if (!string.IsNullOrEmpty(notaTitulo))
            {
                float ht = Alto(sPapel, notaTitulo.ToUpperInvariant(), ancho);
                Texto(R(x, y, ancho, ht), notaTitulo.ToUpperInvariant(), sPapel, cTinta, 1f, TextAnchor.UpperLeft, true, false);
                y += ht + 2 * e;
                Divisor(R(x, y, ancho, 5 * e));
                y += 8 * e;
            }
            float alto = p.yMax - 16 * e - y;
            string cuerpo = notaTexto ?? string.Empty;
            AjustaPapel(cuerpo, ancho, alto);
            Texto(R(x, y, ancho, alto), cuerpo, sPapelCuerpo, cTinta, 1f, TextAnchor.UpperLeft, true, false);
            PieDePantalla(w, p.yMax + e, BosqueGuion.NotaPie);
        }

        void DibujaDiario(float w, float h)
        {
            Caja(new Rect(0, 0, w, h), cVelo);
            Rect p = RectSubido(w, h, 230, 200);
            Hoja(p);
            float x = p.x + 16 * e, ancho = 198 * e;
            float hr = Renglon(sPapel);
            // Cabecera: título a la izquierda y, en el mismo renglón, el subtítulo pequeño a la derecha.
            Texto(R(x, p.y + 13 * e, ancho, hr), BosqueGuion.DiarioTitulo, sPapel, cTinta, 1f, TextAnchor.LowerLeft, false, false);
            Texto(R(x, p.y + 13 * e, ancho, hr), BosqueGuion.DiarioSubtitulo, sMapa, cTinta, 0.75f, TextAnchor.LowerRight, false, false);

            int total = diario != null ? diario.Count : 0;
            if (total > 0)
            {
                int i = Mathf.Clamp(diarioPagina, 0, total - 1);
                EntradaDiario en = diario[i];
                Texto(R(x, p.y + 22 * e, ancho, hr), en.fecha, sPapel, cLapiz, 1f, TextAnchor.LowerLeft, false, false);
                Divisor(R(x, p.y + 32 * e, ancho, 5 * e));
                string cuerpo = en.texto ?? string.Empty;
                AjustaPapel(cuerpo, ancho, 136 * e);
                Texto(R(x, p.y + 40 * e, ancho, 136 * e), cuerpo, sPapelCuerpo, cTinta, 1f, TextAnchor.UpperLeft, true, false);
                Texto(R(p.x, p.y + 179 * e, p.width, 8 * e), (i + 1) + Barra + total, sMapa, cTinta, 0.8f, TextAnchor.MiddleCenter, false, false);
            }
            else Divisor(R(x, p.y + 32 * e, ancho, 5 * e));
            PieDePantalla(w, p.yMax + e, BosqueGuion.DiarioPie);
        }

        // ------------------------------------------------------------ Mapa

        void Rotulo(Rect mapa, float u, float v, string texto, Color color)
        {
            Texto(R(mapa.x + (5 + u) * e, mapa.y + (18 + v) * e, 120 * e, Renglon(sMapa)), texto, sMapa, color, 1f, TextAnchor.UpperLeft, false, false);
        }

        Vector2 EnMapa(Rect mapa, Vector3 mundo)
        {
            float u = Mathf.Clamp(mundo.x + 125f, 3f, 197f);   // fuera de la ventana: sujeto al borde
            float v = Mathf.Clamp(100f - mundo.z, 3f, 197f);
            return new Vector2(Mathf.Round(mapa.x + (5 + u) * e), Mathf.Round(mapa.y + (18 + v) * e));
        }

        void DibujaMapa(float w, float h, float ahora)
        {
            Caja(new Rect(0, 0, w, h), cVelo);
            Rect m = RectSubido(w, h, 210, 223);
            if (!Pieza("UI/mapa", m, 1f))
            {
                Respaldo(m, cRespaldo, 1f);
                Caja(R(m.x + 5 * e, m.y + 18 * e, 200 * e, 200 * e), cPapel);   // hoja lisa: los rótulos y las marcas siguen sirviendo
            }
            Texto(R(m.x + 8 * e, m.y + 3 * e, 194 * e, 12 * e), BosqueGuion.MapaTitulo, sBarra, cResaltado, 1f, TextAnchor.MiddleLeft, false, true);
            Texto(R(m.x + 8 * e, m.y + 3 * e, 194 * e, 12 * e), BosqueGuion.MapaPuesto, sPeq, cApagado, 1f, TextAnchor.MiddleRight, false, true);

            // Posiciones en px del plano (esquina superior izquierda del texto). Comprobadas en la maqueta: la torre, las peñas,
            // el cruce y dos notas se han movido respecto a la tabla del diseño porque pisaban las peñas, el tendido o el sendero.
            // Si se cambian, cambiar también los huecos sin arbolado (CALVAS) de tools/genera_mapa.py.
            Rotulo(m, 142, 112, BosqueGuion.MapaCasa, cTinta);
            Rotulo(m, 6, 132, BosqueGuion.MapaTorre, cTinta);
            Rotulo(m, 106, 88, BosqueGuion.MapaCruce, cTinta);
            Rotulo(m, 48, 82, BosqueGuion.MapaSendero, cTinta);
            Rotulo(m, 150, 24, BosqueGuion.MapaCarbonera, cTinta);
            Rotulo(m, 6, 74, BosqueGuion.MapaPenas, cTinta);
            Rotulo(m, 132, 190, BosqueGuion.MapaValdara, cTinta);
            Rotulo(m, 128, 5, BosqueGuion.MapaPista, cTinta);
            Rotulo(m, 150, 11, BosqueGuion.MapaCortada, cRojo);

            // Anotaciones a lápiz de Daniel.
            if ((mapaNotas & NotaMapa.SinLuz) != 0) Rotulo(m, 16, 95, BosqueGuion.MapaNotaSinLuz, cLapiz);
            if ((mapaNotas & NotaMapa.LucesFuncionan) != 0) Rotulo(m, 58, 108, BosqueGuion.MapaNotaLuces, cLapiz);
            if ((mapaNotas & NotaMapa.ValdaraSinLuz) != 0)
                Texto(R(m.x + (5 + 139) * e, m.y + (18 + 171) * e, 56 * e, 16 * e), BosqueGuion.MapaNotaValdara, sMapa, cLapiz, 1f, TextAnchor.UpperLeft, true, false);
            if ((mapaNotas & NotaMapa.Explosion) != 0) Rotulo(m, 140, 17, BosqueGuion.MapaNotaExplosion, cLapiz);

            // Objetivo de la misión: marca que parpadea a 2 Hz.
            if (mapaHayObjetivo && Mathf.Repeat(ahora * 2f, 1f) < 0.5f)
            {
                Vector2 c = EnMapa(m, mapaObjetivo);
                Rect ro = R(c.x - 2.5f * e, c.y - 2.5f * e, 5 * e, 5 * e);
                if (!Pieza("UI/fog/mark_item", ro, 1f)) Caja(ro, cRojo);
            }

            // Jugador: triángulo que apunta según el rumbo (0 = +z = arriba).
            if (jugador != null)
            {
                Vector2 c = EnMapa(m, jugador.position);
                Matrix4x4 antes = GUI.matrix;
                GUIUtility.RotateAroundPivot(jugador.eulerAngles.y, c);
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(c.x - 3.5f * e, c.y - 3.5f * e, 7 * e, 7 * e), Flecha(), ScaleMode.StretchToFill, true);
                GUI.matrix = antes;
                float hr = Renglon(sMapa);
                Texto(R(c.x + 5 * e, c.y - hr * 0.5f, 40 * e, hr), BosqueGuion.MapaYo, sMapa, cLapiz, 1f, TextAnchor.MiddleLeft, false, false);
            }

            PieDePantalla(w, m.yMax + e, BosqueGuion.MapaPie);
        }

        /// <summary>Triángulo de 7×7 creado por código, apuntando hacia arriba: rojo con borde negro.</summary>
        Texture2D Flecha()
        {
            if (flecha != null) return flecha;
            var px = new Color32[49];
            Color32 rojo = cRojo, negro32 = new Color32(8, 8, 8, 255), nada = new Color32(0, 0, 0, 0);
            for (int j = 0; j < 7; j++)             // j = fila desde arriba
            {
                int k = (j + 1) / 2;                // semianchura: 0,1,1,2,2,3,3
                for (int i = 0; i < 7; i++)
                {
                    Color32 c = nada;
                    if (i >= 3 - k && i <= 3 + k) c = (i == 3 - k || i == 3 + k || j == 6) ? negro32 : rojo;
                    px[(6 - j) * 7 + i] = c;        // las texturas van de abajo arriba
                }
            }
            flecha = UiFog.Nueva(7, 7, px, TextureWrapMode.Clamp);
            return flecha;
        }

        // ------------------------------------------------------------ Walkie

        /// <summary>El aparato, abajo a la derecha (margen 8e), dejando debajo el renglón de su pie de teclas.</summary>
        Rect RectWalkie(float w, float h)
        {
            return R(w - 8 * e - 160 * e, h - 8 * e - Renglon(sPeq) - e - 100 * e, 160 * e, 100 * e);
        }

        void DibujaWalkie(float w, float h, float ahora)
        {
            Rect r = RectWalkie(w, h);
            if (!Pieza("UI/walkie", r, 1f))
            {
                Respaldo(r, cRespaldo, 1f);
                Caja(R(r.x + 8 * e, r.y + 24 * e, 144 * e, 52 * e), Color.black);
            }
            Texto(R(r.x + 8 * e, r.y + 3 * e, 144 * e, 12 * e), BosqueGuion.WalkieTitulo, sBarra, cResaltado, 1f, TextAnchor.MiddleLeft, false, true);
            Texto(R(r.x + 8 * e, r.y + 3 * e, 144 * e, 12 * e), BosqueGuion.WalkieAparato, sPeq, cApagado, 1f, TextAnchor.MiddleRight, false, true);

            int n = canales != null ? canales.Length : 0;
            if (n > 0)
            {
                CanalWalkie c = canales[Mathf.Clamp(canal, 0, n - 1)];
                float x = r.x + 11 * e, y = r.y + 27 * e, ancho = 138 * e;
                float hr = Renglon(sVtP);
                Texto(R(x, y, ancho, hr), c.nombre, sVtP, cFosforo, 1f, TextAnchor.UpperLeft, false, false);
                if (c.alerta) Texto(R(x, y + hr, ancho, hr), c.estado, sVtP, cRojo, Mathf.Repeat(ahora * 2f, 1f) < 0.5f ? 1f : 0.25f, TextAnchor.UpperLeft, false, false);
                else Texto(R(x, y + hr, ancho, hr), c.estado, sVtP, cFosforo, 0.7f, TextAnchor.UpperLeft, false, false);
                Texto(R(x, y + 2 * hr, ancho, 2 * hr), c.texto, sVtP, cFosforo, 1f, TextAnchor.UpperLeft, true, false);

                // Selector de canal: un cuadradito por canal (sin texto), encendido el sintonizado.
                float lado = 3 * e, hueco = 3 * e;
                float x0 = r.x + r.width * 0.5f - (n * lado + (n - 1) * hueco) * 0.5f;
                for (int i = 0; i < n; i++)
                    Caja(R(x0 + i * (lado + hueco), r.y + 84 * e, lado, lado), i == canal ? cFosforo : new Color(cFosforo.r, cFosforo.g, cFosforo.b, 0.2f));
            }
            Texto(R(r.x, r.yMax + e, r.width, Renglon(sPeq)), BosqueGuion.WalkiePie, sPeq, cTexto, 0.85f, TextAnchor.UpperCenter, false, true);
        }

        // ------------------------------------------------------------ Tele y monitor

        /// <summary>Líneas de barrido con una sola textura de 1×4 repetida, y la banda clara que baja.</summary>
        void Barrido(Rect r, float h, float ahora)
        {
            if (barrido == null)
            {
                var px = new Color32[4];
                px[0] = new Color32(0, 0, 0, 71);   // 0,28 de alfa
                barrido = UiFog.Nueva(1, 4, px, TextureWrapMode.Repeat);
            }
            float texel = Mathf.Max(1, e / 2);
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(r, barrido, new Rect(0f, 0f, 1f, r.height / (4f * texel)));
            float y = Mathf.Repeat(ahora * 0.23f, 1f);
            float alto = Mathf.Min(h * 0.03f, r.height * (1f - y));
            Caja(new Rect(r.x, r.y + r.height * y, r.width, alto), new Color(1f, 1f, 1f, 0.045f));
        }

        void Nieve(Rect r, float alfa)
        {
            if (ruido == null)
            {
                var rnd = new System.Random(4);
                var px = new Color32[64 * 48];
                for (int i = 0; i < px.Length; i++)
                {
                    byte g = (byte)rnd.Next(20, 236);
                    px[i] = new Color32(g, g, g, 255);
                }
                ruido = UiFog.Nueva(64, 48, px, TextureWrapMode.Repeat);
            }
            GUI.color = new Color(1f, 1f, 1f, alfa);
            GUI.DrawTextureWithTexCoords(r, ruido, new Rect(ruidoU, ruidoV, 4f, 4f));
            GUI.color = Color.white;
        }

        void DibujaTele(float w, float h, float ahora)
        {
            Caja(new Rect(0, 0, w, h), Color.black);
            Rect r = R(w * 0.06f, h * 0.06f, w * 0.88f, h * 0.88f);
            if (imagen != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(r, imagen, ScaleMode.ScaleToFit, false);
            }
            else if (teleCabecera == null) Nieve(r, 0.8f);
            else
            {
                Nieve(r, 0.25f);
                float hg = Renglon(sVtG), hp = Renglon(sVtP);
                Texto(R(r.x, r.y + r.height * 0.10f, r.width, hg), teleCabecera, sVtG, cFosforo, 1f, TextAnchor.UpperCenter, false, true);
                Texto(R(r.x, r.y + r.height * 0.10f + hg + 2 * e, r.width, hg), teleSubcabecera, sVtG, cFosforo, 0.8f, TextAnchor.UpperCenter, false, true);
                if (!string.IsNullOrEmpty(telePie))
                {
                    // Rótulo de la noticia: franja a 0,62h, por encima de donde caen los subtítulos.
                    Rect f = R(r.x, h * 0.62f, r.width, hp + 4 * e);
                    Caja(f, new Color(0f, 0f, 0f, 0.6f));
                    Caja(new Rect(f.x, f.y, f.width, e), new Color(cFosforo.r, cFosforo.g, cFosforo.b, 0.55f));
                    Texto(R(f.x + 10 * e, f.y + e, f.width - 20 * e, f.height - e), telePie, sVtP, cFosforo, 1f, TextAnchor.MiddleLeft, false, false);
                }
            }
            Barrido(r, h, ahora);
            float ht = Renglon(sPeq);
            Texto(R(w * 0.3f, h - 2 * e - ht, w * 0.7f - 6 * e, ht), BosqueGuion.TelePieTeclas, sPeq, cApagado, 1f, TextAnchor.LowerRight, false, true);
        }

        void DibujaMonitor(float w, float h, float ahora)
        {
            Caja(new Rect(0, 0, w, h), Color.black);
            Rect r = R(w * 0.06f, h * 0.06f, w * 0.88f, h * 0.88f);
            if (imagen != null)
            {
                GUI.color = cVerdeMonitor;
                GUI.DrawTexture(r, imagen, ScaleMode.StretchToFill, false);
                GUI.color = Color.white;
            }
            Barrido(r, h, ahora);
            float hg = Renglon(sVtG);
            Texto(R(w * 0.08f, h * 0.08f, w * 0.8f, hg), monitorNombre, sVtG, cFosforo, 1f, TextAnchor.UpperLeft, false, true);
            string reloj = GrabandoReloj + (int)Mathf.Repeat(ahora / 60f, 10f) + GrabandoRotulo;
            float yPie = h * 0.92f - hg;
            Texto(R(w * 0.08f, yPie, w * 0.5f, hg), reloj, sVtG, cFosforo, 1f, TextAnchor.UpperLeft, false, true);
            // El piloto de grabación: un cuadradito rojo dibujado (la fuente no tiene el círculo).
            float lado = Mathf.Round(hg * 0.34f);
            Caja(R(w * 0.08f + Ancho(sVtG, reloj) + 3 * e, yPie + (hg - lado) * 0.5f, lado, lado), cRojo);
            Texto(R(w * 0.3f, yPie, w * 0.62f, hg), BosqueGuion.MonitorPie, sVtG, cFosforo, 1f, TextAnchor.UpperRight, false, true);
        }
    }
}
