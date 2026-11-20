using System.Collections;
using System.Collections.Generic;
using TheFog.Puesto;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheFog.Entorno
{
    /// <summary>
    /// Director del prólogo, «la noche del incidente»: Daniel en la cabaña al caer la noche, el diario, el informativo,
    /// se acuesta, la explosión y el apagón, comprueba que no hay luz, linterna, bidón, generador, luces del sendero,
    /// torre y la llamada del cabo por la emisora. Lleva las fases, las misiones, la interacción (E), la linterna (F)
    /// y las teclas del diario (J), el mapa (M) y el walkie (T). Va en Daniel.
    /// La pantalla la dibuja BosqueInterfaz y el sonido lo pone BosqueSonido: aquí solo se decide qué pasa.
    /// El mundo (luces, niebla, pasos, sucesos) está en BosqueJuegoMundo.cs y las secuencias en BosqueJuegoFases.cs.
    /// </summary>
    public partial class BosqueJuego : MonoBehaviour
    {
        public enum Sonido { Clic, Puerta, Liquido, Tiron, Rama, Papel, Paso }
        public enum Fase { Inicio, Diario, Noticia, Acostarse, Durmiendo, Madrugada, Llamada, Fin }

        public Font fuenteHud, fuenteNota, fuenteMonitor;
        public float alcance = 2.6f;
        public GameObject figuraSendero;      // la silueta entre los árboles
        public Vector3 zonaSilueta;           // sigue sin usarse
        public RadioBosque radioCasa, radioTorre;
        public Transform generador;

        public Fase FaseActual { get { return fase; } }
        /// <summary>Tarde = true (red del pueblo), madrugada = false, tras arrancar el generador = true.</summary>
        public bool HayCorriente { get { return hayCorriente; } }
        /// <summary>Escena sin reconstruir: la garrafa sigue dentro de la cabaña.</summary>
        public bool BidonEnCasa { get { return bidonEnCasa; } }

        EntornoWalker walker;
        Camera cam;
        CharacterController cuerpo;
        Light linterna;
        BosqueSonido sonido;
        BosqueInterfaz ui;
        Interactuable mirando;

        // ---- Estado
        Fase fase;
        bool bloqueado;                                   // cinemática (fundidos, dormir, final)
        bool hayCorriente = true;
        readonly HashSet<string> inventario = new HashSet<string>();
        readonly HashSet<string> comprobaciones = new HashSet<string>();
        bool diarioAbierto, leidoTodo, puedeDormir, noticiaAcabada, durmiendo;
        bool comprobado, concluyendo, salio, arrancando, lucesHechas, sonando, cabina, llamando, llamadaHecha, finalizando;
        bool bidonEnCasa, generadorEnTorre, bajoTecho;
        bool adormecido, salir3Dicho, pieTorreDicho, cabinaDicha, walkieSinLuzDicho, walkieNoOyeDicho, avisoSinGenerador;
        Coroutine informativo;                            // para poder pararla al dormir
        float tBloqueo, tAyuda, tMadrugada, tArranque, tLuces, tEnCabina, tRecordatorio, tLlamada;
        float tDiario, tNoticia, tSueno, tFuera, tSalio, tConcluir, tLlamadaHecha;
        int pistasDiario;
        bool bidonPerdido;                                // último recurso: el bidón no aparece y se reposta con la lata de reserva
        string ultimaMision, ultimoDetalle;

        // ---- Diario
        readonly List<EntradaDiario> diario = new List<EntradaDiario>();
        readonly List<bool> vista = new List<bool>();

        // ---- Monitor de cámaras
        MonitorCamaras monitor;
        RenderTexture rtMonitor;
        int camActual;
        float tiempoEnCasa;
        bool figuraVista, figuraRetirada;

        // ---- Vídeo de la tele (hueco: normalmente no hay)
        Component video;
        RenderTexture rtVideo;
        AudioSource altavozTele;

        void Awake()
        {
            walker = GetComponent<EntornoWalker>();
            cam = walker != null && walker.cam != null ? walker.cam : GetComponentInChildren<Camera>();
            cuerpo = GetComponent<CharacterController>();
            ultimaPos = transform.position;
            Seguro(CrearLinterna);

            sonido = BosqueSonido.Crear(transform);
            ui = BosqueInterfaz.Crear(gameObject);
            ui.fuenteHud = fuenteHud; ui.fuenteNota = fuenteNota; ui.fuenteMonitor = fuenteMonitor;
            ui.jugador = transform; ui.negro = 1f;

            // Cada búsqueda por separado: que falte una cosa no deja sin cablear las demás.
            Seguro(BuscarEdificios);
            Seguro(BuscarRadios);
            Seguro(BuscarGenerador);
            Seguro(BuscarAccionables);
            Seguro(BuscarResto);
            Seguro(CalcularPuntos);
            // El constructor deja la figura activa: si no se apaga aquí, está plantada en el sendero desde la tarde.
            if (figuraSendero != null) figuraSendero.SetActive(false);
            Seguro(GuardarAmbiente);
            Seguro(PrepararLuces);
            Seguro(CrearLuzTele);
            Seguro(PrepararCristales);
            Seguro(PonerAyuda);

            foreach (var e in BosqueGuion.Diario) { diario.Add(new EntradaDiario(e[0], e[1])); vista.Add(true); }

            Debug.Log("[Bosque] Primera versión: casa=" + (casa != null) + ", torre=" + (torre != null) + ", tele=" + (tele != null)
                + ", cama=" + (cama != null) + ", generador=" + (generadorB != null) + ", luces=" + luces.Count
                + ", vídeo=" + VideoHueco.Existe);
        }

        // ------------------------------------------------------------ API para los objetos

        public bool Tiene(string clave) { return inventario.Contains(clave); }

        /// <summary>¿Queda en la escena un Recogible con esa clave?</summary>
        public bool HayRecogible(string clave)
        {
            if (clave == "garrafa" && bidonPerdido) return false;
            foreach (var r in recogibles) if (r != null && r.clave == clave && r.gameObject.activeInHierarchy) return true;
            return false;
        }

        /// <summary>Quita el bidón del inventario (lo llama GeneradorBosque al repostar).</summary>
        public void GastarBidon() { inventario.Remove("garrafa"); }

        public void Decir(string texto, float segundos)
        {
            if (string.IsNullOrEmpty(texto)) return;
            ui.Pensar(texto, segundos);
        }

        public void Objetivo(string texto)
        {
            ultimaMision = texto; ultimoDetalle = "";
            ui.Mision(texto, "");
        }

        public void Leer(string titulo, string texto)
        {
            if (bloqueado || fase == Fase.Fin || (llamando && !llamadaHecha)) return;
            sonido.Sonar(BosqueSonido.Efecto.Papel, 0.5f);
            ui.AbrirNota(titulo, texto, null);
        }

        public void Recoger(string clave)
        {
            if (string.IsNullOrEmpty(clave)) return;
            inventario.Add(clave);
            sonido.Sonar(BosqueSonido.Efecto.Clic, 0.7f);
            if (clave == "linterna")
            {
                Decir(BosqueGuion.Linterna, 4f);
                ui.Pista(BosqueGuion.LinternaTecla, 6f);
                if (linterna != null) linterna.enabled = true;
            }
            else if (clave == "garrafa")
            {
                Decir(BosqueGuion.Bidon, 4f);
                if (fase == Fase.Madrugada && tPuerta < 0f) tPuerta = Time.time + 2.5f;   // la puerta, que se cierra sola
            }
        }

        public void Sonar(Sonido s, Vector3 donde, float volumen)
        {
            switch (s)
            {
                case Sonido.Clic: sonido.SonarEn(BosqueSonido.Efecto.Clic, donde, volumen, 25f); break;
                case Sonido.Puerta:
                    sonido.SonarEn(BosqueSonido.Efecto.PuertaAbrir, donde, volumen, 25f);
                    sonido.SonarEn(BosqueSonido.Efecto.PuertaChirrido, donde, volumen * 0.6f, 25f);
                    break;
                case Sonido.Liquido: sonido.SonarEn(BosqueSonido.Efecto.Liquido, donde, volumen, 25f); break;
                case Sonido.Tiron: sonido.SonarEn(BosqueSonido.Efecto.Tiron, donde, volumen, 25f); break;
                case Sonido.Rama: sonido.SonarEn(BosqueSonido.Efecto.Rama, donde, volumen, 40f); break;
                case Sonido.Papel: sonido.Sonar(BosqueSonido.Efecto.Papel, volumen); break;
                case Sonido.Paso: break;   // los pasos los lleva Pasos()
            }
        }

        public void GeneradorEnMarcha(GeneradorBosque g)
        {
            if (arrancando) return;
            arrancando = true; tArranque = Time.time;
            QuitarMision();
            StartCoroutine(VuelveLaLuz(g));
        }

        /// <summary>Lo que hace cada objeto «accionable» según el momento de la partida. a puede ser null (tecla).</summary>
        public void Accion(string clave, Accionable a)
        {
            if (string.IsNullOrEmpty(clave) || bloqueado || fase == Fase.Fin || (llamando && !llamadaHecha)) return;
            bool tarde = fase < Fase.Durmiendo;
            bool apagon = fase == Fase.Madrugada && !hayCorriente;
            Vector3 punto = a != null ? a.transform.position : transform.position;
            switch (clave)
            {
                case "tele":
                    if (tarde) ui.AbrirTele(VideoHueco.Reproduciendo(video) ? rtVideo : null, null);
                    else if (apagon) Comprobar("tele", BosqueSonido.Efecto.Boton, tele != null ? posTele : punto, BosqueGuion.TeleMuerta);
                    else Decir(BosqueGuion.TeleVuelve, 4f);
                    break;
                case "cama":
                    if (!tarde) Decir(BosqueGuion.CamaDespierto, 3f);
                    else if (fase <= Fase.Diario) Decir(BosqueGuion.CamaPronto, 3f);
                    else if (!puedeDormir) Decir(BosqueGuion.CamaInformativo, 3f);
                    else StartCoroutine(Dormir());
                    break;
                case "ventana":
                    if (tarde) Decir(BosqueGuion.VentanaNoche, 4f);
                    else if (apagon) { comprobaciones.Add("ventana"); Decir(BosqueGuion.VentanaApagon, 4.5f); }
                    else Decir(BosqueGuion.VentanaLuz, 4f);
                    break;
                case "interruptor":
                    if (apagon)
                    {
                        bool primera = !comprobaciones.Contains("interruptor");
                        Comprobar("interruptor", BosqueSonido.Efecto.Interruptor, punto, primera ? BosqueGuion.Interruptor1 : BosqueGuion.Interruptor2);
                    }
                    else
                    {
                        sonido.SonarEn(BosqueSonido.Efecto.Interruptor, punto, 0.7f, 10f);
                        if (hayCorriente)
                        {
                            var l = LuzPorNombre("Luz interior");
                            if (l != null) l.enabled = !l.enabled;
                        }
                    }
                    break;
                case "cuadro":
                    if (apagon) Comprobar("cuadro", BosqueSonido.Efecto.Interruptor, punto, BosqueGuion.CuadroBien);
                    else Decir(BosqueGuion.CuadroLuz, 3f);
                    break;
                case "diario": AbrirDiario(); break;
                case "mapa": AbrirMapa(); break;
                case "walkie": AbrirWalkie(); break;
            }
        }

        /// <summary>Una de las comprobaciones del apagón: cuenta una vez, suena en el objeto y Daniel dice lo que ve.</summary>
        void Comprobar(string clave, BosqueSonido.Efecto e, Vector3 punto, string texto)
        {
            comprobaciones.Add(clave);
            sonido.SonarEn(e, punto, 0.7f, 10f);
            Decir(texto, 3.5f);
        }

        public void UsarRadio(RadioBosque r)
        {
            if (r == null || bloqueado || fase == Fase.Fin || (llamando && !llamadaHecha)) return;
            if (!r.deTorre)
            {
                if (fase < Fase.Durmiendo) Decir(BosqueGuion.RadioNoche, 4.5f);
                else if (fase == Fase.Madrugada && !hayCorriente) Comprobar("radio", BosqueSonido.Efecto.Boton, r.transform.position, BosqueGuion.RadioMuerta);
                else Decir(BosqueGuion.RadioVuelve, 3.5f);
                return;
            }
            if (!hayCorriente) { Decir(BosqueGuion.EmisoraSinLuz, 4.5f); return; }
            if (fase < Fase.Durmiendo) { Decir(BosqueGuion.EmisoraCalla, 4f); return; }
            if (llamadaHecha) { Decir(BosqueGuion.TrasLlamada, 4f); return; }
            if (!lucesHechas) { Decir(BosqueGuion.EmisoraCalla, 4f); return; }
            if (!llamando) StartCoroutine(Llamada());
        }

        // ------------------------------------------------------------ Misiones y diario

        void Mision(string[] m)
        {
            if (m == null || m.Length < 2) return;
            if (m[0] != ultimaMision)
            {
                ultimaMision = m[0]; ultimoDetalle = m[1];
                ui.Mision(m[0], m[1]);
                sonido.Sonar(BosqueSonido.Efecto.MisionNueva, 0.5f);
            }
            else if (m[1] != ultimoDetalle)
            {
                ultimoDetalle = m[1];
                ui.Mision(m[0], m[1]);
            }
        }

        /// <summary>Único camino (con Final) por el que se retira la misión de la pantalla.</summary>
        void QuitarMision()
        {
            if (ultimaMision != null)
            {
                ultimaMision = null; ultimoDetalle = null;
                ui.Mision(null, null);
                sonido.Sonar(BosqueSonido.Efecto.MisionHecha, 0.5f);
            }
        }

        void NuevaEntrada(string[] e)
        {
            if (e == null || e.Length < 2) return;
            foreach (var d in diario) if (d.fecha == e[0]) return;
            diario.Add(new EntradaDiario(e[0], e[1]));
            vista.Add(false);
            ui.Pista(BosqueGuion.DiarioNueva, 6f);
        }

        void AbrirDiario()
        {
            if (bloqueado || fase == Fase.Fin || (llamando && !llamadaHecha)) return;
            diarioAbierto = true;
            if (fase == Fase.Inicio) fase = Fase.Diario;      // lo abre antes de que acabe la entrada: vale igual
            if (fase == Fase.Diario) Mision(BosqueGuion.MLeer);
            int pagina = 0;
            for (int i = 0; i < vista.Count; i++) if (!vista[i]) { pagina = i; break; }
            ui.AbrirDiario(diario, pagina, AlPasarPagina, AlCerrarDiario);
        }

        void AlPasarPagina(int i)
        {
            sonido.Sonar(BosqueSonido.Efecto.Papel, 0.4f);
            if (i >= 0 && i < vista.Count) vista[i] = true;
            if (i >= 3) leidoTodo = true;
        }

        void AlCerrarDiario()
        {
            if (fase != Fase.Diario) return;
            Decir(leidoTodo ? BosqueGuion.DiarioCerrado : BosqueGuion.DiarioSinLeer, 3f);
            PasarANoticia();
        }

        void PasarANoticia()
        {
            fase = Fase.Noticia; tNoticia = 0f;
            if (informativo == null) informativo = StartCoroutine(Informativo());
        }

        // ------------------------------------------------------------ Mapa y walkie

        void AbrirMapa()
        {
            if (bloqueado || fase == Fase.Fin || (llamando && !llamadaHecha)) return;
            var notas = BosqueInterfaz.NotaMapa.Nada;
            if (fase >= Fase.Madrugada && !hayCorriente) notas |= BosqueInterfaz.NotaMapa.SinLuz;
            if (lucesHechas) notas |= BosqueInterfaz.NotaMapa.LucesFuncionan;
            if (llamadaHecha) notas |= BosqueInterfaz.NotaMapa.ValdaraSinLuz | BosqueInterfaz.NotaMapa.Explosion;

            bool hay = false; Vector3 objetivo = Vector3.zero;
            string m = ultimaMision;
            if (m != null)
            {
                if (m == BosqueGuion.MBidon[0] || m == BosqueGuion.MBidonDentro[0])
                {
                    hay = true;
                    objetivo = new Vector3(MundoBosque.Bidon.x, 0f, MundoBosque.Bidon.y);
                    foreach (var r in recogibles) if (r != null && r.clave == "garrafa") { objetivo = r.transform.position; break; }
                }
                else if (m == BosqueGuion.MRepostar[0] || m == BosqueGuion.MRepostarLejos[0] || m == BosqueGuion.MArrancar[0])
                { hay = true; objetivo = posGenerador; }
                else if (m == BosqueGuion.MTorre[0] || m == BosqueGuion.MEmisora[0])
                { hay = true; objetivo = posPieTorre; }
            }
            sonido.Sonar(BosqueSonido.Efecto.Papel, 0.6f);
            ui.AbrirMapa(notas, hay, objetivo, AlCerrarMapa);
        }

        void AlCerrarMapa() { sonido.Sonar(BosqueSonido.Efecto.Papel, 0.4f); }

        void AbrirWalkie()
        {
            if (bloqueado || fase == Fase.Fin || (llamando && !llamadaHecha)) return;
            string[] n = BosqueGuion.WalkieCanales;
            CanalWalkie[] canales;
            int canal = 0; bool senal = true;
            if (fase < Fase.Durmiendo)
            {
                canales = new[]
                {
                    new CanalWalkie(n[0], BosqueGuion.WalkieSenal, BosqueGuion.WalkieTardePuesto),
                    new CanalWalkie(n[1], BosqueGuion.WalkieSenal, BosqueGuion.WalkieTardeValdara),
                    new CanalWalkie(n[2], BosqueGuion.WalkieSenal, BosqueGuion.WalkieTardeEmergencias),
                };
            }
            else if (llamadaHecha)
            {
                canales = new[]
                {
                    new CanalWalkie(n[0], BosqueGuion.WalkieSinSenal, BosqueGuion.WalkieMudo),
                    new CanalWalkie(n[1], BosqueGuion.WalkieSinSenal, BosqueGuion.WalkiePortadora, true),
                    new CanalWalkie(n[2], BosqueGuion.WalkieSinSenal, BosqueGuion.WalkieMudo),
                };
                canal = 1; senal = false;
            }
            else if (hayCorriente)
            {
                canales = new[]
                {
                    new CanalWalkie(n[0], BosqueGuion.WalkieSenal, BosqueGuion.WalkieNadie),
                    new CanalWalkie(n[1], BosqueGuion.WalkieLlamada, BosqueGuion.WalkieConteste, true),
                    new CanalWalkie(n[2], BosqueGuion.WalkieSenal, BosqueGuion.WalkieNadie),
                };
                canal = 1;
                if (!walkieNoOyeDicho) { walkieNoOyeDicho = true; Decir(BosqueGuion.WalkieNoOye, 4.5f); }
            }
            else
            {
                canales = new[]
                {
                    new CanalWalkie(n[0], BosqueGuion.WalkieSinRepetidor, BosqueGuion.WalkieMudo, true),
                    new CanalWalkie(n[1], BosqueGuion.WalkieSinRepetidor, BosqueGuion.WalkieMudo, true),
                    new CanalWalkie(n[2], BosqueGuion.WalkieSinRepetidor, BosqueGuion.WalkieMudo, true),
                };
                senal = false;
                if (!walkieSinLuzDicho) { walkieSinLuzDicho = true; Decir(BosqueGuion.WalkieSinCorriente, 5f); }
            }
            sonido.Sonar(BosqueSonido.Efecto.WalkieAbrir, 0.6f);
            sonido.Emisor("walkie", BosqueSonido.Bucle.EstaticaRadio, Vector3.zero, senal ? 0.06f : 0.2f, 0f);
            ui.AbrirWalkie(canales, canal, AlSintonizar, AlCerrarWalkie);
        }

        void AlSintonizar(int canal) { sonido.Sonar(BosqueSonido.Efecto.Boton, 0.5f); }

        void AlCerrarWalkie()
        {
            sonido.Apagar("walkie", 0.2f);
            sonido.Sonar(BosqueSonido.Efecto.WalkiePitido, 0.4f);
        }

        // ------------------------------------------------------------ Monitor de cámaras

        public void AbrirMonitor(MonitorCamaras m)
        {
            if (m == null || m.camaras == null || m.camaras.Length == 0) return;
            if (bloqueado || fase == Fase.Fin || (llamando && !llamadaHecha)) return;
            if (!hayCorriente) { Decir(BosqueGuion.MonitorSinLuz, 4f); return; }
            int primera = -1;
            for (int i = 0; i < m.camaras.Length; i++) if (m.camaras[i] != null) { primera = i; break; }
            if (primera < 0) return;
            monitor = m; camActual = primera; tiempoEnCasa = 0f;
            if (lucesHechas && !figuraRetirada && m.figura != null) m.figura.SetActive(true);
            if (rtMonitor == null) rtMonitor = new RenderTexture(320, 180, 16) { filterMode = FilterMode.Point, name = "Monitor" };
            foreach (var c in m.camaras) if (c != null) { c.targetTexture = rtMonitor; c.enabled = false; }
            m.camaras[primera].enabled = true;
            sonido.Sonar(BosqueSonido.Efecto.Clic, 0.8f);
            ui.AbrirMonitor(rtMonitor, NombreCamara());
        }

        string NombreCamara()
        {
            if (monitor != null && monitor.nombres != null && camActual >= 0 && camActual < monitor.nombres.Length) return monitor.nombres[camActual];
            return BosqueGuion.MonitorCamara + " " + (camActual + 1);
        }

        void CerrarMonitor()
        {
            if (monitor == null) return;
            var m = monitor;
            monitor = null;
            if (ui.Abierto == BosqueInterfaz.Modal.Monitor) ui.Cerrar();
            Seguro(() => { if (m.camaras != null) foreach (var c in m.camaras) if (c != null) c.enabled = false; });
        }

        void ActualizarMonitor()
        {
            if (monitor == null) return;
            if (PInput.Pause || PInput.Quit || monitor.camaras == null || monitor.camaras.Length == 0) { CerrarMonitor(); return; }
            int n = monitor.camaras.Length;
            if (camActual < 0 || camActual >= n) camActual = 0;
            int paso = 0;
            var k = Keyboard.current;
            if (PInput.Interact || (k != null && (k.dKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame))) paso = 1;
            else if (k != null && (k.aKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame)) paso = -1;
            bool enCasa = camActual == n - 1;
            if (enCasa) { tiempoEnCasa += Time.deltaTime; if (monitor.figura != null && monitor.figura.activeSelf) figuraVista = true; }
            if (paso == 0) return;
            // Al apartar la vista de la cámara de la casa, lo que había se va. Y deja la puerta abierta.
            if (enCasa && figuraVista && !figuraRetirada && tiempoEnCasa > 1.2f)
            {
                figuraRetirada = true;
                if (monitor.figura != null) monitor.figura.SetActive(false);
                if (monitor.puertaCasa != null) monitor.puertaCasa.abierta = true;
            }
            if (monitor.camaras[camActual] != null) monitor.camaras[camActual].enabled = false;
            for (int i = 0; i < n; i++)   // salta los huecos de la lista
            {
                camActual = (camActual + paso + n) % n;
                if (monitor.camaras[camActual] != null) break;
            }
            if (monitor.camaras[camActual] != null) monitor.camaras[camActual].enabled = true;
            if (camActual == n - 1) tiempoEnCasa = 0f;
            sonido.Sonar(BosqueSonido.Efecto.Clic, 0.6f);
            ui.MonitorNombre(NombreCamara());
        }

        // ------------------------------------------------------------ Bucle

        void Update()
        {
            // 1. Fin: solo queda soltar el ratón o salir.
            if (fase == Fase.Fin)
            {
                if (walker != null) walker.congelado = true;
                if (PInput.Pause) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
                if (PInput.Quit) Application.Quit();
                return;
            }

            // Un monitor destruido con su pantalla abierta dejaría la partida parada: se cierra.
            if (monitor == null && ui.Abierto == BosqueInterfaz.Modal.Monitor) ui.Cerrar();

            // 2. Con pantalla abierta o en una cinemática, Daniel ni anda ni mira.
            if (walker != null) walker.congelado = bloqueado || monitor != null || ui.Abierto != BosqueInterfaz.Modal.Ninguno;

            // 3. Mundo que se mueve solo y línea de teclas.
            TeleLuz();
            Ambiente();
            var k = Keyboard.current;
            if (k != null && k.f1Key.wasPressedThisFrame) tAyuda = Time.unscaledTime + 16f;
            ui.teclas = Time.unscaledTime > tAyuda ? BosqueGuion.TeclasHud : null;

            // 4. Guardián de cinemáticas: ninguna dura más de 60 s.
            if (bloqueado) tBloqueo += Time.deltaTime; else tBloqueo = 0f;
            if (tBloqueo > 90f)
            {
                tBloqueo = 0f;
                Debug.LogWarning("[Bosque] Cinemática atascada: se libera al jugador.");
                if (llamadaHecha)
                {
                    fase = Fase.Fin;
                    finalizando = true;
                    ui.negro = 1f; ui.aviso = null; ui.teclas = null;
                    ui.Tarjeta(BosqueGuion.FinTitulo, BosqueGuion.FinCuerpo, BosqueGuion.FinPie);
                    return;
                }
                bool despierta = durmiendo && fase < Fase.Madrugada;
                if (fase == Fase.Inicio)
                {
                    fase = Fase.Diario;
                    Mision(Accionable_("diario") != null ? BosqueGuion.MDiario : BosqueGuion.MDiarioTecla);
                }
                if (despierta) { hayCorriente = false; tMadrugada = Time.time; fase = Fase.Madrugada; }
                ui.negro = 0f; ui.QuitarTarjeta();
                bloqueado = false;
                if (despierta) { Seguro(() => Corriente(false, false)); Seguro(() => Ambientar(true)); Seguro(Despertar); }
            }

            // 5. Monitor de cámaras: lleva sus propias teclas.
            if (monitor != null) { Redes(); ActualizarMonitor(); return; }
            // 6. Pantalla abierta: la interfaz atiende sus teclas y se corta el frame.
            if (ui.ProcesarEntrada()) { ui.aviso = null; Redes(); return; }
            // 7. Cinemática.
            if (bloqueado) { ui.aviso = null; return; }

            // 8. Durante la llamada Daniel tiene el micrófono en la mano: no usa ni abre nada.
            if (llamando && !llamadaHecha) { mirando = null; ui.aviso = null; }
            else
            {
                // 9. Qué tiene delante.
                mirando = Delante();
                ui.aviso = mirando != null ? mirando.Rotulo : null;
                // 10. Teclas: solo una por frame.
                if (PInput.Interact) { if (mirando != null) mirando.Usar(this); }
                else if (k != null)
                {
                    if (k.jKey.wasPressedThisFrame) AbrirDiario();
                    else if (k.mKey.wasPressedThisFrame) AbrirMapa();
                    else if (k.tKey.wasPressedThisFrame) AbrirWalkie();
                    else if ((k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)
                             && puedeDormir && fase < Fase.Durmiendo && (cama == null || camaEnganchada)) StartCoroutine(Dormir());
                }
            }

            // 11. Linterna.
            if (PInput.Flashlight && Tiene("linterna") && linterna != null)
            {
                linterna.enabled = !linterna.enabled;
                sonido.Sonar(BosqueSonido.Efecto.Clic, 0.6f);
            }

            // 12. Lo demás.
            Pasos();
            Sucesos();
            Vigilar();
            if (Application.isEditor) Depuracion();
        }

        /// <summary>Lo que avanza la partida sin que el jugador pulse nada: misiones derivadas del estado, respaldos por tiempo, la torre.</summary>
        void Vigilar()
        {
            Redes();
            Vector3 pos = transform.position;
            bool enCasa = MundoBosque.DentroDeCasa(casa, pos);

            // ---- Tarde-noche
            if (fase == Fase.Diario)
            {
                tDiario += Time.deltaTime;
                if (!diarioAbierto && ((pistasDiario == 0 && tDiario > 25f) || (pistasDiario == 1 && tDiario > 70f)))
                { pistasDiario++; ui.Pista(BosqueGuion.DiarioRecuerda, 5f); }
                if (tDiario > 180f) PasarANoticia();
            }
            else if (fase == Fase.Noticia)
            {
                // Red: el informativo dura unos 85 s. Si se ha quedado por el camino, se da por acabado.
                tNoticia += Time.deltaTime;
                if (tNoticia > 150f && !noticiaAcabada)
                {
                    Debug.LogWarning("[Bosque] El informativo no ha terminado a tiempo: se da por acabado.");
                    puedeDormir = true; noticiaAcabada = true;
                    fase = Fase.Acostarse;
                    Mision(cama != null ? BosqueGuion.MAcostarse : BosqueGuion.MAcostarseTecla);
                }
            }
            if (fase < Fase.Durmiendo && noticiaAcabada && !durmiendo)
            {
                if (fase == Fase.Noticia) fase = Fase.Acostarse;
                tSueno += Time.deltaTime;
                if (cama == null) { if (tSueno > 25f) StartCoroutine(Dormir()); }
                else if (!adormecido && ((tSueno > 240f && enCasa) || tSueno > 480f))
                { adormecido = true; StartCoroutine(Adormecer()); }
            }

            // ---- Madrugada: la misión sale del estado, no de un orden fijo.
            if (fase == Fase.Madrugada && !llamando)
            {
                int presentes = (tele != null ? 1 : 0) + (radioCasa != null ? 1 : 0) + (ventana != null ? 1 : 0)
                              + (Accionable_("interruptor") != null ? 1 : 0) + (Accionable_("cuadro") != null ? 1 : 0);
                int necesarias = Mathf.Min(2, presentes);
                if (!comprobado && !concluyendo && (comprobaciones.Count >= necesarias || Time.time - tMadrugada > 90f))
                    StartCoroutine(ConcluirApagon());
                if (concluyendo && !comprobado && Time.time - tConcluir > 20f) comprobado = true;   // red: ConcluirApagon dura 6 s

                if (!salio && !enCasa)
                {
                    salio = true; tSalio = Time.time;
                    StartCoroutine(Salida());
                }
                if (!enCasa)
                {
                    tFuera += Time.deltaTime;
                    if (!salir3Dicho && tFuera > 20f) { salir3Dicho = true; Decir(BosqueGuion.Salir3, 3.5f); }
                }

                // Sin generador en la escena: la luz vuelve sola al poco de salir.
                if (generadorB == null && salio && !arrancando && Time.time - tSalio > 25f)
                {
                    if (!avisoSinGenerador) { avisoSinGenerador = true; Debug.LogWarning("[Bosque] Sin generador: vuelve la luz sola."); }
                    GeneradorEnMarcha(null);
                }

                bool repostado = generadorB != null ? generadorB.Repostado : arrancando;
                // Último recurso: diez minutos de madrugada sin dar con el bidón (enterrado, fuera del mapa). Se deja repostar sin él.
                if (!bidonPerdido && !repostado && !Tiene("garrafa") && Time.time - tMadrugada > 600f)
                {
                    bidonPerdido = true;
                    Debug.LogWarning("[Bosque] El bidón no aparece: el generador se podrá repostar sin él.");
                }
                if (!comprobado) Mision(BosqueGuion.MComprobar);
                else if (!Tiene("linterna") && HayRecogible("linterna")) Mision(BosqueGuion.MLinterna);
                else if (!repostado && !Tiene("garrafa") && HayRecogible("garrafa")) Mision(bidonEnCasa ? BosqueGuion.MBidonDentro : BosqueGuion.MBidon);
                else if (!repostado) Mision(generadorEnTorre ? BosqueGuion.MRepostarLejos : BosqueGuion.MRepostar);
                else if (!arrancando) Mision(BosqueGuion.MArrancar);
                else if (!lucesHechas) { }   // la misión se retiró al arrancar; no hay ninguna mientras vuelven las luces
                else if (!cabina) Mision(BosqueGuion.MTorre);
                else Mision(BosqueGuion.MEmisora);
            }

            // ---- Camino a la torre
            if (lucesHechas && !llamando && fase == Fase.Madrugada)
            {
                Vector2 xz = new Vector2(pos.x, pos.z);
                float d = Vector2.Distance(xz, new Vector2(posPieTorre.x, posPieTorre.z));
                bool hayEmisora = radioTorre != null || torre != null;
                bool enEmisora = MundoBosque.EnCabina(torre, pos) || (hayEmisora && Vector3.Distance(pos, posEmisora) < 3.5f);

                if (!sonando && d < 32f)
                {
                    sonando = true;
                    sonido.Emisor("llamada", BosqueSonido.Bucle.LlamadaEmisora, posEmisora, 0.6f, 40f);
                    sonido.EmisorVolumen("emisora", 0.45f, 1f);
                    if (radioTorre != null) radioTorre.rotulo = BosqueGuion.RotEmisoraContestar;
                }
                if (!pieTorreDicho && d < 10f && pos.y - posPieTorre.y < 5f)
                {
                    pieTorreDicho = true;
                    Decir(generadorEnTorre ? BosqueGuion.PieTorreConMotor : BosqueGuion.PieTorre, 6f);
                    sonido.Musica(BosqueSonido.Pista.Torre, 0.40f, 5f);
                }
                if (enEmisora && !cabinaDicha)
                {
                    cabinaDicha = true; cabina = true;
                    Decir(BosqueGuion.Cabina, 5f);
                }
                if (!cabina && d > 32f && Time.time - tRecordatorio > 120f)
                {
                    tRecordatorio = Time.time;
                    StartCoroutine(Recordatorio());
                }
                // Sin bloqueo: si está junto a la emisora y no la usa, acaba cogiendo el micrófono él solo.
                if (enEmisora)
                {
                    tEnCabina += Time.deltaTime;
                    if (tEnCabina > (radioTorre != null ? 90f : 8f))
                    {
                        Decir(BosqueGuion.EmisoraCoges, 3f);
                        StartCoroutine(Llamada());
                    }
                }
                else if (!hayEmisora && Time.time - tLuces > 240f) StartCoroutine(Llamada());
                else if (Time.time - tLuces > 1200f)
                {
                    // Último recurso: veinte minutos con luz y sin llegar a la emisora (torre inaccesible). La llamada entra igual.
                    Debug.LogWarning("[Bosque] Veinte minutos sin llegar a la emisora: la llamada entra donde esté Daniel.");
                    StartCoroutine(Llamada());
                }
            }
        }

        /// <summary>Redes por tiempo, por si una corrutina muere: se miran también con una pantalla abierta.</summary>
        void Redes()
        {
            if (arrancando && !lucesHechas && Time.time - tArranque > 75f)     // VuelveLaLuz dura unos 40 s
            {
                Debug.LogWarning("[Bosque] Las luces no han vuelto a tiempo: se encienden todas de golpe.");
                hayCorriente = true;
                tLuces = Time.time; tRecordatorio = Time.time;
                lucesHechas = true;
                Seguro(() => TodasLasLuces(true));
                Seguro(VuelveLaCasa);
                Seguro(() => { if (figuraSendero != null) figuraSendero.SetActive(true); });
            }
            if (llamando && !llamadaHecha && Time.time - tLlamada > 150f)      // la llamada dura unos 95 s
            {
                Debug.LogWarning("[Bosque] La llamada no ha terminado a tiempo: se pasa al final.");
                if (walker != null) walker.sinMover = false;
                tLlamadaHecha = Time.time;
                llamadaHecha = true;
                StartCoroutine(Final());
            }
            if (llamadaHecha && !finalizando && Time.time - tLlamadaHecha > 130f)   // tras la llamada: 20 s + 90 s de tope
            {
                Debug.LogWarning("[Bosque] El final no ha llegado a tiempo: se lanza.");
                StartCoroutine(Final());
            }
        }

        /// <summary>Atajos de prueba, solo en el editor: F9 dormir, F10 al generador con todo, F11 a la cabina.</summary>
        void Depuracion()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.f9Key.wasPressedThisFrame && fase >= Fase.Diario && fase < Fase.Durmiendo)
            {
                noticiaAcabada = true; puedeDormir = true;
                StartCoroutine(Dormir());
            }
            else if (k.f10Key.wasPressedThisFrame && fase == Fase.Madrugada)
            {
                comprobado = true; concluyendo = true;
                inventario.Add("linterna"); inventario.Add("garrafa");
                if (walker != null) walker.Teleport(posGenerador + new Vector3(1.5f, 0.3f, 0f));
            }
            else if (k.f11Key.wasPressedThisFrame)
            {
                if (walker != null && torre != null) walker.Teleport(torre.TransformPoint(new Vector3(0.3f, 17.9f, 0.3f)));
            }
        }
    }
}
