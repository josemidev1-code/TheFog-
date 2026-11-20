using System.Collections.Generic;
using UnityEngine;

namespace TheFog.Entorno
{
    public struct EntradaDiario
    {
        public string fecha, texto;
        public EntradaDiario(string fecha, string texto) { this.fecha = fecha; this.texto = texto; }
    }

    public struct CanalWalkie
    {
        public string nombre, estado, texto;
        public bool alerta;   // estado en rojo y parpadeando (llamada entrante, sin repetidor)
        public CanalWalkie(string nombre, string estado, string texto, bool alerta = false)
        { this.nombre = nombre; this.estado = estado; this.texto = texto; this.alerta = alerta; }
    }

    /// <summary>
    /// Toda la pantalla del bosque, en IMGUI: misión, aviso de interacción, pensamientos, subtítulos con hablante,
    /// fundidos y tarjetas, y las pantallas modales (nota, diario, mapa, walkie, tele, monitor de cámaras).
    /// No decide nada de la partida: BosqueJuego le dice qué enseñar. No reproduce sonido.
    /// Solo lee el teclado dentro de ProcesarEntrada().
    /// (Este archivo: API, estado, teclas, HUD, tarjeta, pensamientos y subtítulos. Las pantallas, en BosqueInterfazPantallas.cs.)
    /// </summary>
    public partial class BosqueInterfaz : MonoBehaviour
    {
        public enum Modal { Ninguno, Nota, Diario, Mapa, Walkie, Tele, Monitor }

        [System.Flags]
        public enum NotaMapa { Nada = 0, SinLuz = 1, LucesFuncionan = 2, ValdaraSinLuz = 4, Explosion = 8 }

        /// <summary>Añade el componente al anfitrión (Daniel) o devuelve el que ya tenga. Nunca devuelve null.</summary>
        public static BosqueInterfaz Crear(GameObject anfitrion)
        {
            var i = anfitrion.GetComponent<BosqueInterfaz>();
            return i != null ? i : anfitrion.AddComponent<BosqueInterfaz>();
        }

        // Fuentes: la que llegue null se busca en Resources/Fuentes/ y, si tampoco está, se usa la de GUI.skin.
        public Font fuenteHud, fuenteTitulo, fuenteNota, fuenteMonitor;
        /// <summary>Para marcar la posición y el rumbo en el mapa.</summary>
        public Transform jugador;

        // ---- HUD
        /// <summary>Rótulo de lo que se tiene delante («abrir», «coger la linterna»); null o vacío = nada. Se pinta como «[E]  rótulo».</summary>
        public string aviso;
        /// <summary>Línea fija de teclas, abajo a la derecha; null = nada.</summary>
        public string teclas;
        /// <summary>Fundido a negro, de 0 a 1. Tapa el mundo y el HUD; no tapa tarjeta, subtítulos ni pensamientos.</summary>
        public float negro;
        /// <summary>Rótulos de la pantalla de la tele cuando no hay vídeo; teleCabecera null = nieve sin señal.</summary>
        public string teleCabecera, teleSubcabecera, telePie;

        // ------------------------------------------------------------ Colores (5.3)
        static Color Hex(int rgb, float alfa = 1f)
        {
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, alfa);
        }

        static readonly Color cTexto = Hex(0xD4CEC0), cResaltado = Hex(0xE3DFD6), cPensamiento = Hex(0xDBD4B3), cApagado = Hex(0x92929A),
                              cRojo = Hex(0xD63929), cAmbar = Hex(0xE8B565), cSombra = Hex(0x0A0A09, 0.85f), cTinta = Hex(0x392921),
                              cLapiz = Hex(0x842921), cFosforo = Hex(0xB8F2B8), cVelo = new Color(0f, 0f, 0f, 0.72f),
                              cLocutor = Hex(0xC6AD94), cRelevo = Hex(0xA5947B), cRespaldo = Hex(0x181810, 0.9f), cFilete = Hex(0xC6AD94),
                              cPapel = Hex(0xDECEA5);

        const string TeclaUsar = "[E]  ";

        // ------------------------------------------------------------ Estado
        struct Pensamiento { public string texto; public float entra, sale; }
        readonly List<Pensamiento> pensamientos = new List<Pensamiento>(4);

        string subHabla, subTexto, subOrigen;
        float subEntra, subSale;

        string misTitulo, misDetalle, antTitulo, antDetalle;
        float misEntra, antHasta;

        string pistaTexto;
        float pistaSale;

        string tarTitulo, tarCuerpo, tarPie;
        float tarAlfa, tarObjetivo;

        Modal abierto = Modal.Ninguno;
        int frameAbierto = -1;
        System.Action alCerrar;

        /// <summary>Pensamiento de Daniel, sin caja. Se apilan hasta tres; el cuarto echa al más viejo.</summary>
        public void Pensar(string texto, float segundos)
        {
            if (string.IsNullOrEmpty(texto)) return;
            float ahora = Time.time;
            pensamientos.Add(new Pensamiento { texto = texto, entra = ahora, sale = ahora + Mathf.Max(0.5f, segundos) });
            while (pensamientos.Count > 3) pensamientos.RemoveAt(0);
        }

        /// <summary>
        /// Subtítulo en caja con el nombre de quien habla. Sustituye al que hubiera. origen = etiqueta pequeña sobre la caja
        /// («TELE · CADENA REGIONAL», «WALKIE · CANAL 5»); null = sin etiqueta. hablante null o vacío = solo el texto.
        /// </summary>
        public void Subtitulo(string hablante, string texto, float segundos, string origen = null)
        {
            if (string.IsNullOrEmpty(texto)) { QuitarSubtitulo(); return; }
            float ahora = Time.time;
            bool habia = subTexto != null && ahora < subSale;
            subHabla = hablante; subTexto = texto; subOrigen = origen;
            subEntra = habia ? ahora - 0.15f : ahora;   // si ya había caja en pantalla, la nueva no parpadea
            subSale = ahora + Mathf.Max(0.3f, segundos);
        }

        public void QuitarSubtitulo()
        {
            if (subTexto != null) subSale = Mathf.Min(subSale, Time.time + 0.25f);
        }

        /// <summary>
        /// Misión en pantalla. Título distinto del actual: la anterior se marca «HECHO» 1,2 s y entra la nueva con «NUEVA MISIÓN».
        /// Mismo título: solo cambia el detalle. Título null o vacío: se retira la que haya (con su «HECHO»).
        /// </summary>
        public void Mision(string titulo, string detalle)
        {
            bool vacio = string.IsNullOrEmpty(titulo);
            if (!vacio && titulo == misTitulo) { misDetalle = detalle; return; }
            if (vacio && misTitulo == null) return;
            float ahora = Time.time;
            // Solo se despide con «HECHO» la que ya estaba a la vista (no una que esperaba turno ni mientras se despide otra).
            if (misTitulo != null && ahora >= misEntra && (antTitulo == null || ahora >= antHasta))
            {
                antTitulo = misTitulo; antDetalle = misDetalle; antHasta = ahora + 1.2f;
            }
            misTitulo = vacio ? null : titulo;
            misDetalle = vacio ? null : detalle;
            misEntra = antTitulo != null && antHasta > ahora ? antHasta : ahora;
        }

        /// <summary>Aviso pequeño y pasajero, arriba a la derecha («Nueva entrada en el diario», «[F] linterna»). El nuevo sustituye al anterior.</summary>
        public void Pista(string texto, float segundos)
        {
            if (string.IsNullOrEmpty(texto)) { pistaTexto = null; return; }
            pistaTexto = texto;
            pistaSale = Time.time + Mathf.Max(0.5f, segundos);
        }

        /// <summary>Tarjeta de texto centrada, por encima del negro; entra con un fundido de 1 s. Cualquier parte puede ser null.</summary>
        public void Tarjeta(string titulo, string cuerpo, string pie)
        {
            tarTitulo = titulo; tarCuerpo = cuerpo; tarPie = pie;
            tarObjetivo = 1f;
        }

        /// <summary>Retira la tarjeta con un fundido de 0,6 s.</summary>
        public void QuitarTarjeta()
        {
            tarObjetivo = 0f;
        }

        // ---- Pantallas modales
        public Modal Abierto { get { return abierto; } }

        /// <summary>
        /// Lo llama BosqueJuego una vez por frame, al principio de su Update. Atiende las teclas de la pantalla abierta
        /// (pasar página, cambiar de canal, cerrar) y devuelve true si había una pantalla abierta al entrar, aunque se haya
        /// cerrado en esta misma llamada: en ese caso BosqueJuego no procesa nada más ese frame.
        /// Con Modal.Monitor devuelve true sin leer teclas (las lleva BosqueJuego). Sin pantalla devuelve false.
        /// </summary>
        public bool ProcesarEntrada()
        {
            if (abierto == Modal.Ninguno) return false;
            if (abierto == Modal.Monitor) return true;
            if (Time.frameCount == frameAbierto) return true;   // la tecla que la abrió no la cierra

            var k = UnityEngine.InputSystem.Keyboard.current;
            bool usar = TheFog.Puesto.PInput.Interact;
            bool salir = TheFog.Puesto.PInput.Pause;
            bool izq = k != null && (k.aKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame);
            bool der = k != null && (k.dKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame);

            switch (abierto)
            {
                case Modal.Nota:
                case Modal.Tele:
                    if (usar || salir) Cerrar();
                    break;
                case Modal.Diario:
                    if (usar || salir || (k != null && k.jKey.wasPressedThisFrame)) { Cerrar(); break; }
                    int total = diario != null ? diario.Count : 0;
                    int destino = diarioPagina + (der ? 1 : 0) - (izq ? 1 : 0);
                    if (destino != diarioPagina && destino >= 0 && destino < total)   // sin dar la vuelta
                    {
                        diarioPagina = destino;
                        papelClave = null;
                        var pasar = alPasar;
                        if (pasar != null) Llamar(() => pasar(destino));
                    }
                    break;
                case Modal.Mapa:
                    if (usar || salir || (k != null && k.mKey.wasPressedThisFrame)) Cerrar();
                    break;
                case Modal.Walkie:
                    if (usar || salir || (k != null && k.tKey.wasPressedThisFrame)) { Cerrar(); break; }
                    int n = canales != null ? canales.Length : 0;
                    int paso = (der ? 1 : 0) - (izq ? 1 : 0);
                    if (paso != 0 && n > 1)   // da la vuelta
                    {
                        canal = ((canal + paso) % n + n) % n;
                        int elegido = canal;
                        var sintonizar = alSintonizar;
                        if (sintonizar != null) Llamar(() => sintonizar(elegido));
                    }
                    break;
            }
            return true;
        }

        /// <summary>Cierra la pantalla abierta: primero Abierto pasa a Ninguno y después se llama a su «alCerrar» (que puede abrir otra).</summary>
        public void Cerrar()
        {
            if (abierto == Modal.Ninguno) return;
            var fin = alCerrar;
            abierto = Modal.Ninguno;
            alCerrar = null; alPasar = null; alSintonizar = null;
            diario = null; canales = null; imagen = null;
            notaTitulo = null; notaTexto = null; papelClave = null;
            if (fin != null) Llamar(fin);
        }

        /// <summary>Hoja de papel con título y texto. Abrir una pantalla con otra abierta cierra antes la anterior (con su alCerrar).</summary>
        public void AbrirNota(string titulo, string texto, System.Action alCerrar = null)
        {
            Abrir(Modal.Nota, alCerrar);
            notaTitulo = titulo; notaTexto = texto ?? string.Empty;
        }

        /// <summary>
        /// Una entrada por página. «pagina» = índice con el que se abre (se acota al rango).
        /// alPasar recibe el índice de la página que se acaba de enseñar (también la primera, al abrir).
        /// </summary>
        public void AbrirDiario(IList<EntradaDiario> entradas, int pagina, System.Action<int> alPasar, System.Action alCerrar)
        {
            Abrir(Modal.Diario, alCerrar);
            diario = entradas;
            int total = entradas != null ? entradas.Count : 0;
            diarioPagina = Mathf.Clamp(pagina, 0, Mathf.Max(0, total - 1));
            this.alPasar = alPasar;
            if (total > 0 && alPasar != null)
            {
                int primera = diarioPagina;
                Llamar(() => alPasar(primera));
            }
        }

        /// <summary>objetivo: posición de mundo del destino de la misión (marca roja que parpadea) si hayObjetivo.</summary>
        public void AbrirMapa(NotaMapa notas, bool hayObjetivo, Vector3 objetivo, System.Action alCerrar)
        {
            Abrir(Modal.Mapa, alCerrar);
            mapaNotas = notas; mapaHayObjetivo = hayObjetivo; mapaObjetivo = objetivo;
        }

        /// <summary>Se guarda una copia de «canales». alSintonizar recibe el índice del canal al cambiar (no al abrir).</summary>
        public void AbrirWalkie(CanalWalkie[] canales, int canal, System.Action<int> alSintonizar, System.Action alCerrar)
        {
            Abrir(Modal.Walkie, alCerrar);
            this.canales = canales != null ? (CanalWalkie[])canales.Clone() : new CanalWalkie[0];
            this.canal = Mathf.Clamp(canal, 0, Mathf.Max(0, this.canales.Length - 1));
            this.alSintonizar = alSintonizar;
        }

        /// <summary>imagen = RenderTexture del vídeo; null = se pintan los rótulos teleCabecera/teleSubcabecera/telePie sobre ruido.</summary>
        public void AbrirTele(Texture imagen, System.Action alCerrar)
        {
            Abrir(Modal.Tele, alCerrar);
            this.imagen = imagen;
        }

        /// <summary>Monitor de cámaras: solo dibuja. Las teclas las lleva BosqueJuego, que cierra con Cerrar().</summary>
        public void AbrirMonitor(Texture imagen, string nombre)
        {
            Abrir(Modal.Monitor, null);
            this.imagen = imagen;
            monitorNombre = nombre;
        }

        public void MonitorNombre(string nombre)
        {
            monitorNombre = nombre;
        }

        // ------------------------------------------------------------ Interno: abrir, llamar, tiempos

        void Abrir(Modal m, System.Action fin)
        {
            // Cerrar la anterior puede abrir otra desde su alCerrar: se insiste unas pocas veces y, si no, se pisa.
            for (int i = 0; i < 4 && abierto != Modal.Ninguno; i++) Cerrar();
            abierto = m;
            alCerrar = fin;
            alPasar = null; alSintonizar = null;
            diario = null; canales = null; imagen = null;
            notaTitulo = null; notaTexto = null; papelClave = null;
            frameAbierto = Time.frameCount;
        }

        /// <summary>Un delegado de BosqueJuego que falle no puede dejar la interfaz a medias.</summary>
        static void Llamar(System.Action a)
        {
            try { a(); }
            catch (System.Exception ex) { Debug.LogWarning("[Bosque] Un aviso de la interfaz ha fallado: " + ex); }
        }

        void Update()
        {
            float ahora = Time.time;
            for (int i = pensamientos.Count - 1; i >= 0; i--) if (ahora >= pensamientos[i].sale) pensamientos.RemoveAt(i);
            if (subTexto != null && ahora >= subSale) { subTexto = null; subHabla = null; subOrigen = null; }
            if (antTitulo != null && ahora >= antHasta) { antTitulo = null; antDetalle = null; }
            if (pistaTexto != null && ahora >= pistaSale) pistaTexto = null;

            float dt = Time.unscaledDeltaTime;
            tarAlfa = Mathf.MoveTowards(tarAlfa, tarObjetivo, dt / (tarObjetivo > tarAlfa ? 1f : 0.6f));
            if (tarAlfa <= 0f && tarObjetivo <= 0f) { tarTitulo = null; tarCuerpo = null; tarPie = null; }

            // Nieve de la tele: un desplazamiento nuevo por frame (aquí, no en OnGUI, que se llama varias veces).
            ruidoU = (float)azar.NextDouble(); ruidoV = (float)azar.NextDouble();
        }

        void OnDestroy()
        {
            estilosHechos = false;
            UiFog.Vaciar();
            barrido = null; ruido = null; flecha = null;
        }

        // ------------------------------------------------------------ Estilos y medidas

        int e = 1;                       // escala entera del kit: 4 a 1080p, 3 a 720p
        int anchoHecho, altoHecho;
        bool estilosHechos;
        GUIStyle sHud, sMisTitulo, sPeq, sSub, sSubHabla, sVtG, sVtP, sTarTitulo, sTarCuerpo, sTarPie, sPapel, sPapelCuerpo, sMapa, sBarra;
        int tamPapel;
        Texture2D barrido, ruido, flecha;
        readonly System.Random azar = new System.Random(1987);
        float ruidoU, ruidoV;
        readonly GUIContent medida = new GUIContent();

        void Preparar()
        {
            if (estilosHechos && anchoHecho == Screen.width && altoHecho == Screen.height && sHud != null) return;
            anchoHecho = Screen.width; altoHecho = Screen.height;
            UiFog.Vaciar();
            barrido = null; ruido = null; flecha = null;
            float h = Screen.height;
            e = Mathf.Max(1, Mathf.FloorToInt(h / 240f));

            Font hud = UiFog.Fuente(fuenteHud, "BarlowCondensed-Medium");
            Font tit = UiFog.Fuente(fuenteTitulo, "BarlowCondensed-SemiBold");
            Font nota = UiFog.Fuente(fuenteNota, "SpecialElite-Regular");
            Font mon = UiFog.Fuente(fuenteMonitor, "VT323-Regular");

            int tSub = Mathf.Max(17, Mathf.RoundToInt(h / 32f));
            int vtG = 16 * Mathf.Max(1, Mathf.RoundToInt(h / 24f / 16f));
            int vtP = 16 * Mathf.Max(1, Mathf.RoundToInt(h / 45f / 16f));
            while (vtP > 16 && vtP > tSub) vtP -= 16;   // la etiqueta de origen nunca mayor que el texto del subtítulo
            tamPapel = Mathf.Max(15, Mathf.RoundToInt(h / 40f));

            sHud = Estilo(hud, Mathf.Max(16, Mathf.RoundToInt(h / 36f)));
            sMisTitulo = Estilo(tit, Mathf.Max(16, Mathf.RoundToInt(h / 36f)));
            sBarra = Estilo(tit, Mathf.Max(14, Mathf.RoundToInt(h / 40f)));
            sPeq = Estilo(hud, Mathf.Max(13, Mathf.RoundToInt(h / 50f)));
            sSub = Estilo(hud, tSub);
            sSubHabla = Estilo(tit, Mathf.Max(14, Mathf.RoundToInt(h / 40f)));
            sVtG = Estilo(mon, vtG);
            sVtP = Estilo(mon, vtP);
            sTarTitulo = Estilo(tit, Mathf.Max(28, Mathf.RoundToInt(h / 13f)));
            sTarCuerpo = Estilo(nota, Mathf.Max(18, Mathf.RoundToInt(h / 30f)));
            sTarPie = Estilo(hud, Mathf.Max(12, Mathf.RoundToInt(h / 60f)));
            sPapel = Estilo(nota, tamPapel);
            sPapelCuerpo = Estilo(nota, tamPapel);
            sMapa = Estilo(nota, Mathf.Max(11, Mathf.RoundToInt(h / 60f)));
            papelClave = null;
            estilosHechos = true;
        }

        static GUIStyle Estilo(Font fuente, int tam)
        {
            var s = new GUIStyle(GUI.skin.label);
            if (fuente != null) s.font = fuente;
            s.fontSize = tam;
            s.fontStyle = FontStyle.Normal;
            s.richText = false;
            s.wordWrap = false;
            s.clipping = TextClipping.Overflow;
            s.alignment = TextAnchor.UpperLeft;
            s.padding = new RectOffset(0, 0, 0, 0);
            s.margin = new RectOffset(0, 0, 0, 0);
            s.border = new RectOffset(0, 0, 0, 0);
            s.normal.background = null;
            return s;
        }

        float Renglon(GUIStyle s) { return Mathf.Ceil(s.lineHeight); }

        float Alto(GUIStyle s, string t, float ancho)
        {
            if (string.IsNullOrEmpty(t)) return 0f;
            s.wordWrap = true;
            medida.text = t;
            return Mathf.Ceil(s.CalcHeight(medida, ancho));
        }

        float Ancho(GUIStyle s, string t)
        {
            if (string.IsNullOrEmpty(t)) return 0f;
            s.wordWrap = false;
            medida.text = t;
            return Mathf.Ceil(s.CalcSize(medida).x);
        }

        static Rect R(float x, float y, float w, float h)
        {
            return new Rect(Mathf.Round(x), Mathf.Round(y), Mathf.Round(w), Mathf.Round(h));
        }

        // ------------------------------------------------------------ Primitivas de dibujo

        void Caja(Rect r, Color c)
        {
            if (c.a <= 0.003f) return;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        void Texto(Rect r, string t, GUIStyle s, Color c, float alfa, TextAnchor ancla, bool envolver, bool sombra)
        {
            if (string.IsNullOrEmpty(t) || alfa <= 0.003f || s == null) return;
            s.alignment = ancla;
            s.wordWrap = envolver;
            if (sombra)
            {
                // La sombra se separa según el tamaño de la letra (con letra pequeña, «e» píxeles la dejaban doble) y
                // se va antes que el texto al fundirse, para que no quede un fantasma.
                float d = Mathf.Clamp(Mathf.Round(s.fontSize / 18f), 1f, e);
                s.normal.textColor = new Color(cSombra.r, cSombra.g, cSombra.b, cSombra.a * alfa * alfa);
                GUI.Label(new Rect(r.x + d, r.y + d, r.width, r.height), t, s);
            }
            s.normal.textColor = new Color(c.r, c.g, c.b, c.a * alfa);
            GUI.Label(r, t, s);
        }

        /// <summary>Marco de 9 cortes del kit; si falta la pieza, rectángulo liso con filete.</summary>
        void Marco(string ruta, int izq, int arr, int der, int aba, Rect r, float alfa)
        {
            if (alfa <= 0.003f) return;
            var estilo = UiFog.Marco(ruta, izq, arr, der, aba, e);
            if (estilo != null)
            {
                GUI.color = new Color(1f, 1f, 1f, alfa);
                GUI.Box(r, GUIContent.none, estilo);
                GUI.color = Color.white;
            }
            else Respaldo(r, cRespaldo, alfa);
        }

        void Respaldo(Rect r, Color fondo, float alfa)
        {
            Caja(r, new Color(fondo.r, fondo.g, fondo.b, fondo.a * alfa));
            Color f = new Color(cFilete.r, cFilete.g, cFilete.b, alfa);
            Caja(new Rect(r.x, r.y, r.width, e), f);
            Caja(new Rect(r.x, r.yMax - e, r.width, e), f);
            Caja(new Rect(r.x, r.y, e, r.height), f);
            Caja(new Rect(r.xMax - e, r.y, e, r.height), f);
        }

        /// <summary>Pieza suelta u horneada, de un solo golpe. Devuelve false si falta (quien llama pinta el respaldo).</summary>
        bool Pieza(string ruta, Rect r, float alfa)
        {
            var t = UiFog.Textura(ruta);
            if (t == null) return false;
            GUI.color = new Color(1f, 1f, 1f, alfa);
            GUI.DrawTexture(r, t, ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
            return true;
        }

        // ------------------------------------------------------------ Dibujo

        void OnGUI()
        {
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            Preparar();
            float w = Screen.width, h = Screen.height;
            float ahora = Time.time;
            Matrix4x4 matriz = GUI.matrix;
            Color color = GUI.color;
            GUI.color = Color.white;

            bool lectura = abierto == Modal.Nota || abierto == Modal.Diario || abierto == Modal.Mapa;

            // 1. HUD
            if (negro <= 0.01f && (abierto == Modal.Ninguno || abierto == Modal.Walkie)) DibujaHud(w, h, ahora);

            // 2. Pantalla modal
            switch (abierto)
            {
                case Modal.Nota: DibujaNota(w, h); break;
                case Modal.Diario: DibujaDiario(w, h); break;
                case Modal.Mapa: DibujaMapa(w, h, ahora); break;
                case Modal.Walkie: DibujaWalkie(w, h, ahora); break;
                case Modal.Tele: DibujaTele(w, h, ahora); break;
                case Modal.Monitor: DibujaMonitor(w, h, ahora); break;
            }

            // 3. Negro
            if (negro > 0f) Caja(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, Mathf.Clamp01(negro)));

            // 4. Tarjeta
            if (tarAlfa > 0.003f) DibujaTarjeta(w, h);

            // Con el walkie abierto, pensamientos y subtítulos se centran en el hueco que deja el aparato a su izquierda.
            float zonaX = 0f, zonaAncho = w;
            if (abierto == Modal.Walkie) zonaAncho = Mathf.Max(w * 0.4f, RectWalkie(w, h).x - 4 * e);

            bool haySub = subTexto != null && ahora < subSale;

            // La caja del subtítulo se mide antes: los pensamientos se apoyan encima de ella.
            float techoSub = h;
            if (haySub) techoSub = MideSubtitulo(w, h, lectura, zonaX, zonaAncho);

            // 5. Pensamientos
            if (!lectura && pensamientos.Count > 0) DibujaPensamientos(w, h, ahora, haySub, techoSub, zonaX, zonaAncho);

            // 6. Subtítulo
            if (haySub) DibujaSubtitulo(ahora);

            GUI.matrix = matriz;
            GUI.color = color;
        }

        void DibujaHud(float w, float h, float ahora)
        {
            // ---- Misión
            if (antTitulo != null && ahora < antHasta)
                DibujaMision(w, antTitulo, antDetalle, BosqueGuion.MisionHecha, cAmbar, Mathf.Clamp01((antHasta - ahora) / 0.3f));
            else if (misTitulo != null && ahora >= misEntra)
            {
                bool nueva = ahora - misEntra < 4f;
                DibujaMision(w, misTitulo, misDetalle, nueva ? BosqueGuion.MisionNueva : BosqueGuion.MisionCabecera,
                             nueva ? cRojo : cApagado, Mathf.Clamp01((ahora - misEntra) / 0.6f));
            }

            // ---- Aviso «[E]  rótulo»
            if (!string.IsNullOrEmpty(aviso))
            {
                string t = TeclaUsar + aviso;
                float tw = Ancho(sHud, t), th = Renglon(sHud);
                Rect r = R(w * 0.5f - tw * 0.5f - 5 * e, h * 0.60f - th * 0.5f - 3 * e, tw + 10 * e, th + 6 * e);
                Marco("UI/fog/tooltip", 4, 4, 4, 4, r, 1f);
                Texto(new Rect(r.x, r.y, r.width, r.height), t, sHud, cResaltado, 1f, TextAnchor.MiddleCenter, false, true);
            }

            // ---- Pista
            if (pistaTexto != null && ahora < pistaSale)
            {
                float a = Mathf.Clamp01((pistaSale - ahora) / 0.5f);
                Texto(R(w * 0.3f, 6 * e, w * 0.7f - 6 * e, Renglon(sPeq)), pistaTexto, sPeq, cAmbar, a, TextAnchor.UpperRight, false, true);
            }

            // ---- Teclas (con el walkie abierto no: el aparato ocupa esa esquina)
            if (!string.IsNullOrEmpty(teclas) && abierto == Modal.Ninguno)
            {
                float th = Renglon(sPeq);
                Texto(R(w * 0.3f, h - 10 * e - th, w * 0.7f - 6 * e, th), teclas, sPeq, cApagado, 0.5f, TextAnchor.LowerRight, false, true);
            }
        }

        void DibujaMision(float w, string titulo, string detalle, string cabecera, Color colorCabecera, float alfa)
        {
            if (alfa <= 0.003f) return;
            float ancho = Mathf.Round(Mathf.Min(130 * e, 0.36f * w));
            float m = 6 * e;
            int ep = Mathf.Max(1, (e + 1) / 2);                 // el puntero, a media escala: casa con la altura de la letra
            float sangria = 6 * ep + 2 * e;
            float dentro = ancho - 2 * m;
            float hCab = Renglon(sPeq);
            float hTit = Alto(sMisTitulo, titulo, dentro - sangria);
            float hDet = Alto(sPeq, detalle, dentro);
            float alto = m + hCab + e + hTit + (hDet > 0f ? e + hDet : 0f) + m;
            Rect r = R(6 * e, 6 * e, ancho, alto);
            Marco("UI/fog/panel", 5, 5, 5, 5, r, alfa);
            float x = r.x + m, y = r.y + m;
            Texto(new Rect(x, y, dentro, hCab), cabecera, sPeq, colorCabecera, alfa, TextAnchor.UpperLeft, false, true);
            y += hCab + e;
            float hRenglon = Renglon(sMisTitulo);
            if (!Pieza("UI/fog/pointer", R(x, y + (hRenglon - 11 * ep) * 0.5f, 6 * ep, 11 * ep), alfa))
                Caja(R(x, y + hRenglon * 0.5f - ep, 4 * ep, 2 * ep), new Color(cRojo.r, cRojo.g, cRojo.b, alfa));
            Texto(new Rect(x + sangria, y, dentro - sangria, hTit), titulo, sMisTitulo, cResaltado, alfa, TextAnchor.UpperLeft, true, true);
            y += hTit + e;
            if (hDet > 0f) Texto(new Rect(x, y, dentro, hDet), detalle, sPeq, cApagado, alfa, TextAnchor.UpperLeft, true, true);
        }

        void DibujaTarjeta(float w, float h)
        {
            float a = Mathf.Clamp01(tarAlfa);
            float hTit = string.IsNullOrEmpty(tarTitulo) ? 0f : Renglon(sTarTitulo);
            float yTit = h * 0.30f - hTit * 0.5f;
            if (hTit > 0f) Texto(R(0, yTit, w, hTit), tarTitulo, sTarTitulo, cResaltado, a, TextAnchor.MiddleCenter, false, true);
            if (!string.IsNullOrEmpty(tarCuerpo))
            {
                float hc = Alto(sTarCuerpo, tarCuerpo, w * 0.6f);
                Texto(R(w * 0.2f, yTit + hTit + 4 * e, w * 0.6f, hc), tarCuerpo, sTarCuerpo, cTexto, a, TextAnchor.UpperCenter, true, true);
            }
            if (!string.IsNullOrEmpty(tarPie))
                Texto(R(w * 0.1f, h * 0.78f, w * 0.8f, h * 0.19f), tarPie, sTarPie, cApagado, a, TextAnchor.MiddleCenter, true, true);
        }

        void DibujaPensamientos(float w, float h, float ahora, bool haySub, float techoSub, float zonaX, float zonaAncho)
        {
            float ancho = Mathf.Min(0.7f * w, zonaAncho - 12 * e);
            float x = zonaX + (zonaAncho - ancho) * 0.5f;
            // Borde inferior del más reciente (los viejos, encima): su renglón empieza en 0,80h; con subtítulo se apoya
            // justo encima de la caja (y de su etiqueta), que según los renglones que tenga cae entre 0,70h y 0,80h.
            float y = h * 0.80f + Renglon(sHud);
            if (haySub) y = Mathf.Min(y, techoSub - 2 * e);
            for (int i = pensamientos.Count - 1; i >= 0; i--)
            {
                var p = pensamientos[i];
                float a = Mathf.Min(Mathf.Clamp01((ahora - p.entra) / 0.2f), Mathf.Clamp01((p.sale - ahora) / 0.2f));
                float alto = Alto(sHud, p.texto, ancho);
                y -= alto;
                Texto(R(x, y, ancho, alto), p.texto, sHud, cPensamiento, a, TextAnchor.UpperCenter, true, true);
                y -= e;
            }
        }

        Color ColorHablante(string hablante)
        {
            if (hablante == BosqueGuion.Locutor) return cLocutor;
            if (hablante == BosqueGuion.Municipal || hablante == BosqueGuion.Cabo) return cAmbar;
            if (hablante == BosqueGuion.Relevo) return cRelevo;
            if (hablante == BosqueGuion.Daniel) return cTexto;
            if (hablante == BosqueGuion.Voz) return cRojo;
            return cResaltado;
        }

        const string Separador = "  ·  ";

        // Medidas de la caja del subtítulo (las calcula MideSubtitulo en cada repintado y las usa DibujaSubtitulo).
        Rect subCaja;
        float subPad, subAltoHabla, subAltoTexto;
        bool subCompacto;

        /// <summary>Mide la caja del subtítulo y devuelve la y más alta que ocupa (contando la etiqueta de origen de fuera).</summary>
        float MideSubtitulo(float w, float h, bool compacto, float zonaX, float zonaAncho)
        {
            bool hayHabla = !string.IsNullOrEmpty(subHabla), hayOrigen = !string.IsNullOrEmpty(subOrigen);
            float ancho = Mathf.Round(Mathf.Min(Mathf.Min(300 * e, 0.72f * w), zonaAncho - 8 * e));
            subCompacto = compacto;
            subPad = (compacto ? 3 : 4) * e;                    // el filete de la caja ocupa las dos primeras filas
            subAltoTexto = Alto(sSub, subTexto, ancho - 24 * e);
            subAltoHabla = hayHabla ? Renglon(sSubHabla) : 0f;
            if (compacto && hayOrigen) subAltoHabla = Mathf.Max(subAltoHabla, Renglon(sVtP));
            float alto = subPad + subAltoHabla + subAltoTexto + subPad;
            float y1 = h - (compacto ? 3 : 22) * e;
            subCaja = R(zonaX + (zonaAncho - ancho) * 0.5f, y1 - alto, ancho, alto);
            return subCaja.y - (hayOrigen && !compacto ? Renglon(sVtP) : 0f);
        }

        void DibujaSubtitulo(float ahora)
        {
            float a = Mathf.Min(Mathf.Clamp01((ahora - subEntra) / 0.15f), Mathf.Clamp01((subSale - ahora) / 0.25f));
            if (a <= 0.003f) return;
            bool hayHabla = !string.IsNullOrEmpty(subHabla), hayOrigen = !string.IsNullOrEmpty(subOrigen);
            bool compacto = subCompacto;
            Rect r = subCaja;
            float mx = 12 * e;
            float pad = subPad;
            float anchoTexto = r.width - 2 * mx;
            float hTexto = subAltoTexto;
            bool origenDentro = compacto && hayOrigen;
            float hHabla = subAltoHabla;

            // Refuerzo oscuro bajo la caja (la del kit es translúcida y debajo puede haber papel claro o nieve de la tele).
            Caja(new Rect(r.x, r.y + e, r.width, r.height - 2 * e), new Color(0f, 0f, 0f, 0.5f * a));
            Marco("UI/fog/dialogue", 12, 4, 12, 4, r, a);

            float y = r.y + pad;
            if (origenDentro)
            {
                // «LOCUTOR  ·  TELE · CADENA REGIONAL» en un renglón, centrado como grupo.
                float wh = hayHabla ? Ancho(sSubHabla, subHabla) : 0f;
                float ws = hayHabla ? Ancho(sSubHabla, Separador) : 0f;
                float wo = Ancho(sVtP, subOrigen);
                float x = r.x + (r.width - (wh + ws + wo)) * 0.5f;
                if (hayHabla)
                {
                    Texto(R(x, y, wh, hHabla), subHabla, sSubHabla, ColorHablante(subHabla), a, TextAnchor.MiddleLeft, false, true);
                    Texto(R(x + wh, y, ws, hHabla), Separador, sSubHabla, cApagado, a, TextAnchor.MiddleLeft, false, true);
                }
                Texto(R(x + wh + ws, y, wo, hHabla), subOrigen, sVtP, cApagado, a, TextAnchor.MiddleLeft, false, true);
            }
            else if (hayHabla)
                Texto(R(r.x + mx, y, anchoTexto, hHabla), subHabla, sSubHabla, ColorHablante(subHabla), a, TextAnchor.MiddleCenter, false, true);

            Texto(R(r.x + mx, y + hHabla, anchoTexto, hTexto), subTexto, sSub, cResaltado, a, TextAnchor.UpperCenter, true, true);

            if (hayOrigen && !compacto)
            {
                float ho = Renglon(sVtP);
                Texto(R(r.x + 2 * e, r.y - ho, r.width, ho), subOrigen, sVtP, cApagado, a, TextAnchor.LowerLeft, false, true);
            }
        }
    }
}
