using System.Collections;
using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>
    /// Las secuencias de BosqueJuego, en el orden de la noche: entrada, informativo, dormir, apagón, salida, vuelta de la luz,
    /// llamada y final. Norma de todas: dentro de una corrutina, lo que toca objetos de la escena va en un método sin yield
    /// llamado con Seguro(...), y los cambios de estado van en sentencias propias, antes del efecto que los acompaña.
    /// Una excepción dentro de una corrutina la mata en silencio: es el peor fallo posible.
    /// </summary>
    public partial class BosqueJuego
    {
        static WaitForSeconds Espera(float segundos) { return new WaitForSeconds(segundos); }

        IEnumerator Fundido(float desde, float hasta, float segundos)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.05f, segundos))
            {
                ui.negro = Mathf.Lerp(desde, hasta, t);
                yield return null;
            }
            ui.negro = hasta;
        }

        /// <summary>Una línea con voz de radio. alcance 0 = sin posición (el walkie).</summary>
        IEnumerator PorRadio(BosqueGuion.Linea l, string origen, Vector3 donde, float alcance, float volumen)
        {
            ui.Subtitulo(l.hablante, l.texto, l.segundos, origen);
            sonido.Emisor("voz", BosqueSonido.Bucle.VozRadio, donde, volumen, alcance, 0.05f);
            if (l.corte)
            {
                yield return Espera(l.segundos * 0.45f);
                sonido.EmisorVolumen("voz", 0f, 0.05f);
                yield return Espera(0.4f);
                sonido.EmisorVolumen("voz", volumen, 0.05f);
                yield return Espera(Mathf.Max(0.1f, l.segundos * 0.55f - 0.4f));
            }
            else yield return Espera(l.segundos);
            sonido.EmisorVolumen("voz", 0f, 0.1f);
            yield return Espera(0.35f);
        }

        // ------------------------------------------------------------ INICIO

        IEnumerator Start()
        {
            bloqueado = true;
            tAyuda = Time.unscaledTime + 16f;
            proximaRama = Time.time + 25f;
            Seguro(() => Corriente(true, false));
            Seguro(() => Ambientar(false));
            sonido.Escena(BosqueSonido.Momento.Tarde, 0f);
            if (casa != null)
            {
                sonido.Emisor("reloj", BosqueSonido.Bucle.Reloj, posMesilla, 0.45f, 7f);
                sonido.Emisor("brasas", BosqueSonido.Bucle.Brasas, posBrasas, 0.30f, 7f);
                sonido.Emisor("zumbido", BosqueSonido.Bucle.ZumbidoElectrico, posBombilla, 0.06f, 5f);
            }
            if (tele != null)
            {
                sonido.Emisor("tele", BosqueSonido.Bucle.TeleVoz, posTele, 0.22f, 11f);
                sonido.Emisor("tele_z", BosqueSonido.Bucle.TeleZumbido, posTele, 0.15f, 6f);
                ui.teleCabecera = BosqueGuion.TeleCadena;      // hay programa
            }

            yield return Espera(0.8f);
            ui.Tarjeta(BosqueGuion.InicioTitulo, BosqueGuion.InicioCuerpo, null);
            yield return Espera(3.5f);
            ui.QuitarTarjeta();
            yield return Espera(0.7f);
            sonido.Musica(BosqueSonido.Pista.Calma, 0.30f, 6f);
            yield return Fundido(1f, 0f, 3f);
            bloqueado = false;
            yield return Espera(1f);
            if (fase == Fase.Inicio) Decir(BosqueGuion.Inicio1, 4f);
            yield return Espera(4.4f);
            if (fase == Fase.Inicio)       // nunca se pisa una fase más avanzada
            {
                Decir(BosqueGuion.Inicio2, 4f);
                fase = Fase.Diario;
                Mision(Accionable_("diario") != null ? BosqueGuion.MDiario : BosqueGuion.MDiarioTecla);
            }
        }

        // ------------------------------------------------------------ NOTICIA

        void PrepararVideo()
        {
            video = null;
            if (tele == null || !VideoHueco.Existe) return;
            if (rtVideo == null) rtVideo = new RenderTexture(320, 240, 0) { filterMode = FilterMode.Point, name = "Tele" };
            if (altavozTele == null)
            {
                var go = new GameObject("Altavoz tele");
                go.transform.position = posTele;
                altavozTele = go.AddComponent<AudioSource>();
                altavozTele.spatialBlend = 1f; altavozTele.rolloffMode = AudioRolloffMode.Linear;
                altavozTele.minDistance = 1.5f; altavozTele.maxDistance = 11f; altavozTele.playOnAwake = false;
            }
            altavozTele.volume = 0.45f * BosqueSonido.VolumenVoces;
            video = VideoHueco.Crear(tele.gameObject, rtVideo, altavozTele);
        }

        bool CercaDeLaTele() { return Vector3.Distance(transform.position, posTele) < 10f; }

        /// <summary>El informativo de la tele. No depende del jugador; Dormir lo para siempre.</summary>
        IEnumerator Informativo()
        {
            Mision(BosqueGuion.MInformativo);
            yield return Espera(1.5f);

            // El relevo, por el walkie.
            sonido.Sonar(BosqueSonido.Efecto.WalkieAbrir, 0.7f);
            foreach (var l in BosqueGuion.WalkieRelevo)
                yield return PorRadio(l, BosqueGuion.OrigenWalkie3, Vector3.zero, 0f, 0.5f);
            sonido.Apagar("voz", 0.1f);
            sonido.Sonar(BosqueSonido.Efecto.WalkiePitido, 0.6f);
            ui.Pista(BosqueGuion.WalkieMapaTeclas, 7f);
            yield return Espera(2f);

            if (tele == null)
            {
                Debug.LogWarning("[Bosque] No hay televisor en la escena: se salta el informativo. Reconstruye la escena (menú 2).");
                puedeDormir = true; noticiaAcabada = true;
                if (fase == Fase.Noticia)
                {
                    fase = Fase.Acostarse;
                    Mision(cama != null ? BosqueGuion.MAcostarse : BosqueGuion.MAcostarseTecla);
                }
                yield break;
            }

            ui.teleSubcabecera = BosqueGuion.TelePrograma;
            Seguro(PrepararVideo);
            if (video != null)
            {
                VideoHueco.Preparar(video);
                for (float t = 0f; t < 3f && !VideoHueco.Preparado(video); t += Time.deltaTime) yield return null;
                if (!VideoHueco.Preparado(video)) video = null;     // se sigue sin vídeo
            }
            bool habiaVideo = video != null;
            if (habiaVideo) { VideoHueco.Reproducir(video); sonido.Apagar("tele", 0.3f); }
            else sonido.SonarEn(BosqueSonido.Efecto.Sintonia, posTele, 0.8f, 12f);
            yield return Espera(3.6f);
            if (!habiaVideo) sonido.EmisorVolumen("tele", 0.45f, 0.5f);

            for (int i = 0; i < BosqueGuion.Noticia.Length; i++)
            {
                if (!hayCorriente) yield break;
                var l = BosqueGuion.Noticia[i];
                if (habiaVideo && !VideoHueco.Reproduciendo(video))
                {
                    // El vídeo ha fallado en marcha: vuelve la voz de respaldo. La tele nunca se queda muda.
                    habiaVideo = false; video = null;
                    sonido.Emisor("tele", BosqueSonido.Bucle.TeleVoz, posTele, 0.45f, 11f);
                }
                if (i == BosqueGuion.NoticiaTiempo)
                {
                    puedeDormir = true;
                    if (fase == Fase.Noticia) Mision(cama != null ? BosqueGuion.MAcostarse : BosqueGuion.MAcostarseTecla);
                }
                ui.telePie = i < BosqueGuion.TeleRotulos.Length ? BosqueGuion.TeleRotulos[i] : null;
                bool cerca = CercaDeLaTele();
                if (cerca) ui.Subtitulo(l.hablante, l.texto, l.segundos, BosqueGuion.OrigenTele);
                yield return Espera(l.segundos + 0.35f);
                if (i == BosqueGuion.NoticiaValdara && cerca && !durmiendo) Decir(BosqueGuion.PiensaValdara, 4f);
            }

            if (!hayCorriente) yield break;
            ui.telePie = BosqueGuion.TeleFin;
            VideoHueco.Parar(video);
            sonido.Emisor("tele", BosqueSonido.Bucle.TeleNieve, posTele, 0.28f, 10f, 0.3f);
            yield return Espera(2f);
            ui.teleCabecera = null;         // nieve
            teleNieve = true;
            noticiaAcabada = true;
            if (fase == Fase.Noticia)
            {
                Decir(BosqueGuion.FinEmision, 3.5f);
                fase = Fase.Acostarse;
                Mision(cama != null ? BosqueGuion.MAcostarse : BosqueGuion.MAcostarseTecla);
            }
        }

        // ------------------------------------------------------------ DURMIENDO

        /// <summary>Con cama: si pasa mucho rato sin acostarse, se le cierran los ojos.</summary>
        IEnumerator Adormecer()
        {
            if (!durmiendo) Decir(BosqueGuion.CamaSueno, 3f);
            yield return Espera(2f);
            if (!durmiendo && fase < Fase.Durmiendo) yield return Dormir();
        }

        IEnumerator Dormir()
        {
            if (durmiendo || fase >= Fase.Durmiendo) yield break;
            durmiendo = true;
            fase = Fase.Durmiendo;
            bloqueado = true;
            if (monitor != null) CerrarMonitor();
            if (ui.Abierto != BosqueInterfaz.Modal.Ninguno) ui.Cerrar();
            QuitarMision();
            Decir(tele != null ? BosqueGuion.CamaDormir : BosqueGuion.CamaDormirSinTele, 4f);
            yield return Espera(1.5f);
            yield return Fundido(0f, 1f, 2.5f);
            Seguro(() => { if (linterna != null) linterna.enabled = false; });
            sonido.Musica(BosqueSonido.Pista.Ninguna, 0f, 3f);

            // El informativo se sigue oyendo y subtitulando sobre negro hasta que acaba (o 30 s).
            for (float t = 0f; t < 30f && !noticiaAcabada; t += Time.deltaTime) yield return null;

            // Se para el informativo pase lo que pase.
            if (informativo != null) StopCoroutine(informativo);
            informativo = null;
            noticiaAcabada = true; puedeDormir = true;
            Seguro(() => VideoHueco.Parar(video));
            sonido.Apagar("voz", 0.2f);
            ui.QuitarSubtitulo();
            ui.teleCabecera = null; ui.telePie = null;
            teleNieve = true;
            if (tele != null) sonido.Emisor("tele", BosqueSonido.Bucle.TeleNieve, posTele, 0.28f, 10f, 0.3f);
            yield return Espera(4f);        // nieve de tele y tic-tac

            sonido.Escena(BosqueSonido.Momento.Silencio, 3f);
            sonido.EmisorVolumen("tele", 0.12f, 3f);
            sonido.EmisorVolumen("brasas", 0.1f, 3f);
            yield return Espera(3.5f);
            sonido.Sonar(BosqueSonido.Efecto.Explosion, 0.9f);
            yield return Espera(0.6f);
            hayCorriente = false;
            Seguro(() => Corriente(false, true));
            yield return Espera(4.5f);      // solo queda el despertador

            Seguro(() => Ambientar(true));
            sonido.Escena(BosqueSonido.Momento.Madrugada, 5f);
            Seguro(Despertar);
            ui.Tarjeta(BosqueGuion.MadrugadaTitulo, BosqueGuion.MadrugadaCuerpo, null);
            yield return Espera(3.5f);
            ui.QuitarTarjeta();
            yield return Espera(0.7f);
            // Abre los ojos.
            yield return Fundido(1f, 0.35f, 1.2f);
            yield return Fundido(0.35f, 0.8f, 0.5f);
            yield return Fundido(0.8f, 0f, 2.5f);
            tMadrugada = Time.time;
            fase = Fase.Madrugada;
            bloqueado = false;
            Decir(BosqueGuion.Despertar1, 3f);
            yield return Espera(3.3f);
            Decir(tele != null ? BosqueGuion.Despertar2 : BosqueGuion.Despertar2SinTele, 4f);
        }

        void Despertar()   // sin yield; cada línea con su guarda
        {
            if (puertaCasa != null) puertaCasa.abierta = false;
            if (tele != null) tele.rotulo = BosqueGuion.RotTeleEncender;
            if (radioCasa != null) radioCasa.rotulo = BosqueGuion.RotRadioEncender;
            if (walker != null && casa != null) { walker.Teleport(casa.TransformPoint(MundoBosque.LocalDespertar)); walker.LookAtPoint(posTele); }
        }

        // ------------------------------------------------------------ MADRUGADA

        IEnumerator ConcluirApagon()
        {
            if (concluyendo || fase != Fase.Madrugada) yield break;
            concluyendo = true; tConcluir = Time.time;
            yield return Espera(1.2f);
            Decir(BosqueGuion.Apagon1, 4.5f);
            yield return Espera(4.8f);
            Decir(BosqueGuion.Apagon2, 4f);
            comprobado = true;
            NuevaEntrada(BosqueGuion.DiarioApagon);
            if (!arrancando) sonido.Musica(BosqueSonido.Pista.Tension, 0.34f, 8f);
        }

        /// <summary>La primera vez que sale de la cabaña de madrugada.</summary>
        IEnumerator Salida()
        {
            if (!comprobado)
            {
                if (!concluyendo) StartCoroutine(ConcluirApagon());
                for (float t = 0f; t < 15f && !comprobado; t += Time.deltaTime) yield return null;
                comprobado = true;
            }
            if (!Tiene("linterna") && HayRecogible("linterna"))
            {
                Decir(BosqueGuion.SinLinterna, 4f);
                yield return Espera(4.3f);
            }
            Decir(BosqueGuion.Salir1, 3.5f);
            yield return Espera(4f);
            Decir(BosqueGuion.Salir2, 4f);
        }

        // ------------------------------------------------------------ VUELVE LA LUZ

        void VuelveLaCasa()   // sin yield
        {
            Cristales(true, casa);                            // solo los de la casa; los de la torre, después
            if (tele != null)
            {
                teleNieve = true;
                if (luzTele != null) luzTele.enabled = true;  // en modo nieve
                ui.teleCabecera = null;
                sonido.Emisor("tele", BosqueSonido.Bucle.TeleNieve, posTele, 0.28f, 10f);
                tele.rotulo = BosqueGuion.RotTeleMirar;
            }
            if (radioCasa != null) radioCasa.rotulo = BosqueGuion.RotRadioEscuchar;
        }

        /// <summary>Arranca el generador y las luces vuelven de una en una. No bloquea al jugador. g puede ser null y no se usa.</summary>
        IEnumerator VuelveLaLuz(GeneradorBosque g)
        {
            // 1. El motor.
            hayCorriente = true;
            sonido.Emisor("generador", BosqueSonido.Bucle.Generador, posGenerador, 0f, 60f, 0f);
            Decir(BosqueGuion.Arranca, 2.5f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.5f)
            {
                sonido.EmisorVolumen("generador", (bajoTecho ? 0.28f : 0.55f) * t, 0f);
                sonido.EmisorTono("generador", Mathf.Lerp(0.55f, 1f, t));
                yield return null;
            }
            sonido.EmisorVolumen("generador", bajoTecho ? 0.28f : 0.55f, 0f);
            sonido.EmisorTono("generador", 1f);
            sonido.Musica(BosqueSonido.Pista.Ninguna, 0f, 2f);

            // 2. Las luces, en orden, temblando.
            bool hayPorche = false, casaVuelta = false;
            Seguro(() => hayPorche = LuzPorNombre("Luz porche") != null);
            int encendidas = 0;
            int total = luces.Count;
            for (int n = 0; n < total; n++)
            {
                Light l = n < luces.Count ? luces[n] : null;
                if (l == null) continue;
                string nombre = ""; Vector3 donde = transform.position;
                Seguro(() => { if (l != null) { nombre = l.name; donde = l.transform.position; } });
                for (int i = 0; i < 4; i++)
                {
                    Seguro(() => AlternarLuz(l));
                    yield return Espera(Random.Range(0.04f, 0.16f));
                }
                Seguro(() => EncenderLuz(l, true));
                sonido.SonarEn(BosqueSonido.Efecto.LuzEnciende, donde, 0.6f, 45f);
                encendidas++;
                if (!casaVuelta && (nombre == "Luz porche" || (!hayPorche && encendidas >= 3)))
                {
                    casaVuelta = true;
                    Seguro(VuelveLaCasa);
                    if (casa != null) sonido.Emisor("zumbido", BosqueSonido.Bucle.ZumbidoElectrico, posBombilla, 0.06f, 5f);
                }
                if (nombre == "Luz poste 1") Decir(BosqueGuion.Luces1, 5f);
                yield return Espera(nombre.StartsWith("Luz poste") ? 0.9f : 0.45f);
            }
            if (!casaVuelta)
            {
                Seguro(VuelveLaCasa);
                if (casa != null) sonido.Emisor("zumbido", BosqueSonido.Bucle.ZumbidoElectrico, posBombilla, 0.06f, 5f);
            }

            // 3. La torre vuelve a zumbar.
            Seguro(() => Cristales(true));
            sonido.Emisor("emisora", BosqueSonido.Bucle.EstaticaRadio, posEmisora, 0.18f, 16f);
            sonido.Emisor("subsuelo", BosqueSonido.Bucle.Subsuelo, posPieTorre + Vector3.down * 0.5f, 0.55f, 22f);
            yield return Espera(2f);
            Decir(BosqueGuion.Luces2, 4f);
            yield return Espera(4.5f);

            // 4. Algo responde desde el bosque, tapado por el motor.
            Vector3 lejos = posGenerador - transform.position; lejos.y = 0f;
            lejos = lejos.sqrMagnitude > 0.01f ? lejos.normalized : Vector3.forward;
            sonido.SonarEn(BosqueSonido.Efecto.VozLejana, posGenerador + lejos * 30f + Vector3.up * 2f, 1f, 60f);
            yield return Espera(2.5f);

            // 5. La llamada, rota, por el walkie.
            sonido.Sonar(BosqueSonido.Efecto.WalkieAbrir, 0.7f);
            foreach (var l in BosqueGuion.WalkieCabo)
                yield return PorRadio(l, BosqueGuion.OrigenWalkie5, Vector3.zero, 0f, 0.5f);
            sonido.Apagar("voz", 0.1f);
            sonido.Sonar(BosqueSonido.Efecto.WalkiePitido, 0.6f);
            ui.Pista(BosqueGuion.WalkieAviso, 7f);
            Decir(BosqueGuion.WalkieLlamando, 4.5f);
            yield return Espera(5f);
            NuevaEntrada(BosqueGuion.DiarioGenerador);

            // 6. Ya se puede ir a la torre (la misión pasa sola a MTorre).
            if (!lucesHechas)
            {
                tLuces = Time.time; tRecordatorio = Time.time;
                lucesHechas = true;
                sonido.Musica(BosqueSonido.Pista.Tension, 0.36f, 6f);
                Seguro(() => { if (figuraSendero != null) figuraSendero.SetActive(true); });
            }
        }

        /// <summary>Si anda perdido, el cabo insiste por el walkie cada dos minutos. El mapa marca la torre.</summary>
        IEnumerator Recordatorio()
        {
            if (llamando) yield break;
            sonido.Sonar(BosqueSonido.Efecto.WalkieAbrir, 0.7f);
            yield return PorRadio(BosqueGuion.WalkieCabo[1], BosqueGuion.OrigenWalkie5, Vector3.zero, 0f, 0.5f);
            if (llamando) yield break;      // la emisora ya tiene la voz: no se le quita
            sonido.Apagar("voz", 0.1f);
            sonido.Sonar(BosqueSonido.Efecto.WalkiePitido, 0.6f);
            ui.Pista(BosqueGuion.WalkieMapaTeclas, 7f);
        }

        // ------------------------------------------------------------ LLAMADA

        IEnumerator Llamada()
        {
            if (llamando) yield break;
            llamando = true; tLlamada = Time.time;
            fase = Fase.Llamada;
            cabina = true;
            if (monitor != null) CerrarMonitor();
            if (ui.Abierto != BosqueInterfaz.Modal.Ninguno) ui.Cerrar();
            QuitarMision();
            if (walker != null) walker.sinMover = true;     // puede mirar, no andar
            sonido.Apagar("llamada", 0.2f);
            sonido.EmisorVolumen("emisora", 0.35f, 0.5f);
            sonido.SonarEn(BosqueSonido.Efecto.WalkieAbrir, posEmisora, 0.8f, 14f);
            yield return Espera(0.6f);

            Light luzCabina = null;
            Seguro(() => luzCabina = LuzPorNombre("Luz cabina"));
            for (int i = 0; i < BosqueGuion.Llamada.Length; i++)
            {
                var l = BosqueGuion.Llamada[i];
                if (i == BosqueGuion.LlamadaNiebla) StartCoroutine(Niebla(d0 * 3.2f, 40f));
                if (l.hablante == BosqueGuion.Daniel)
                {
                    // Daniel no tiene voz: solo subtítulo.
                    sonido.Sonar(BosqueSonido.Efecto.WalkieAbrir, 0.3f);
                    ui.Subtitulo(BosqueGuion.Daniel, l.texto, l.segundos, null);
                    yield return Espera(l.segundos);
                    sonido.Sonar(BosqueSonido.Efecto.WalkiePitido, 0.3f);
                    yield return Espera(0.35f);
                }
                else if (l.hablante == BosqueGuion.Voz)
                {
                    // La voz que se mete en el canal: limpia, sin estática, y la luz de la cabina tiembla.
                    sonido.Apagar("voz", 0.05f);
                    sonido.EmisorVolumen("emisora", 0f, 0.15f);
                    yield return Espera(0.8f);
                    for (int k = 0; k < 4; k++)
                    {
                        Seguro(() => AlternarLuz(luzCabina));
                        yield return Espera(0.1f);
                    }
                    Seguro(() => EncenderLuz(luzCabina, true));
                    sonido.Sonar(BosqueSonido.Efecto.Susto2, 0.3f);
                    ui.Subtitulo(BosqueGuion.Voz, l.texto, l.segundos, BosqueGuion.OrigenEmisora);
                    sonido.Emisor("voz", BosqueSonido.Bucle.VozIntrusa, posEmisora, 0.8f, 14f, 0.05f);
                    yield return Espera(l.segundos);
                    sonido.Apagar("voz", 0.1f);
                    sonido.EmisorVolumen("emisora", 0.5f, 0.1f);
                    yield return Espera(0.4f);
                }
                else
                {
                    // El cabo («MUNICIPAL» hasta que dice su nombre), por la emisora.
                    yield return PorRadio(l, BosqueGuion.OrigenEmisora, posEmisora, 14f, 0.7f);
                    sonido.SonarEn(BosqueSonido.Efecto.WalkiePitido, posEmisora, 0.4f, 14f);
                }
            }

            yield return Espera(3f);        // estática sola
            if (walker != null) walker.sinMover = false;
            if (llamadaHecha) yield break;  // la red por tiempo ya ha pasado al final
            tLlamadaHecha = Time.time;
            llamadaHecha = true;
            ui.QuitarSubtitulo();
            sonido.Apagar("voz", 0.1f);
            sonido.EmisorVolumen("emisora", 0.12f, 2f);
            Seguro(() => { if (radioTorre != null) radioTorre.rotulo = BosqueGuion.RotEmisoraUsar; });
            Decir(BosqueGuion.TrasLlamada, 4.5f);
            Mision(BosqueGuion.MCorriente);
            NuevaEntrada(BosqueGuion.DiarioLlamada);

            // Tiempo para abrir el diario: la última entrada es el remate del guion.
            yield return Espera(20f);
            for (float t = 0f; t < 90f && (ui.Abierto != BosqueInterfaz.Modal.Ninguno || monitor != null); t += Time.deltaTime) yield return null;
            yield return Final();
        }

        // ------------------------------------------------------------ FINAL

        void ApagonFinal()   // sin yield
        {
            foreach (var l in luces)
            {
                if (l == null) continue;
                if (l.name != "Luz cabina") l.enabled = false;
            }
            Cristales(false, casa);
            if (luzTele != null) luzTele.enabled = false;
        }

        IEnumerator Final()
        {
            if (finalizando) yield break;
            finalizando = true;
            bloqueado = true;
            if (walker != null) walker.sinMover = false;
            if (monitor != null) CerrarMonitor();
            if (ui.Abierto != BosqueInterfaz.Modal.Ninguno) ui.Cerrar();

            if (BosqueGuion.FinalConApagon)
            {
                Seguro(ApagonFinal);
                sonido.Apagar("generador", 2.5f);
                sonido.Apagar("subsuelo", 2.5f);
                sonido.Apagar("tele", 0.05f);
                sonido.Apagar("zumbido", 0.05f);
                yield return Espera(1.5f);
                Light luzCabina = null;
                Seguro(() => luzCabina = LuzPorNombre("Luz cabina"));
                if (luzCabina != null)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Seguro(() => AlternarLuz(luzCabina));
                        yield return Espera(Random.Range(0.05f, 0.14f));
                    }
                    Seguro(() => EncenderLuz(luzCabina, false));
                }
                hayCorriente = false;
                Seguro(() => Cristales(false));
                sonido.Sonar(BosqueSonido.Efecto.Apagon, 0.8f);
                sonido.Apagar("emisora", 0.05f);
                sonido.Musica(BosqueSonido.Pista.Ninguna, 0f, 0.5f);
                yield return Espera(1.8f);  // a oscuras
            }

            fase = Fase.Fin;
            ui.negro = 1f;                  // corte seco
            sonido.Sonar(BosqueSonido.Efecto.Susto2, 0.7f);
            ui.aviso = null; ui.teclas = null;
            ultimaMision = null; ultimoDetalle = null;
            ui.Mision(null, null);
            ui.QuitarSubtitulo();
            sonido.ApagarTodo(1.5f);
            sonido.Escena(BosqueSonido.Momento.Silencio, 2f);
            yield return Espera(2.5f);
            sonido.Musica(BosqueSonido.Pista.Torre, 0.50f, 4f);
            ui.Tarjeta(BosqueGuion.FinTitulo, BosqueGuion.FinCuerpo, BosqueGuion.FinPie);
        }
    }
}
