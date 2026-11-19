using System.Collections;
using System.Collections.Generic;
using TheFog.Puesto;
using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>
    /// La primera noche en el bosque: Daniel despierta sin luz, coge la linterna y la garrafa, cruza el sendero,
    /// arranca el generador, sube a la cabina y mira las cámaras. Lleva la interacción (E), la linterna (F),
    /// los textos en pantalla, el sonido y los sucesos de la noche. Va en Daniel.
    /// </summary>
    public class BosqueJuego : MonoBehaviour
    {
        public enum Sonido { Clic, Puerta, Liquido, Tiron, Rama, Papel, Paso }

        public Font fuenteHud, fuenteNota, fuenteMonitor;
        public float alcance = 2.6f;
        public GameObject figuraSendero;      // la silueta entre los árboles
        public Vector3 zonaSilueta;           // al pasar por aquí con la garrafa, aparece
        public RadioBosque radioCasa, radioTorre;
        public Transform generador;

        EntornoWalker walker;
        Camera cam;
        Light linterna;
        Interactuable mirando;
        readonly HashSet<string> inventario = new HashSet<string>();
        readonly List<(string texto, float hasta)> frases = new List<(string, float)>();
        string objetivo = "";
        float objetivoT;
        string notaTitulo, notaTexto;
        float negro = 1f;
        string rotuloInicio = "VALDARA\n\nfinales de los ochenta";
        bool hayCorriente, siluetaVista, siluetaHecha, ramaSilueta, transmision, fin;

        // Monitor
        MonitorCamaras monitor;
        RenderTexture rtMonitor;
        int camActual;
        float tiempoEnCasa;
        bool figuraVista, figuraRetirada;

        // Sonido
        AudioSource ambiente, motor, estaticaCasa, estaticaTorre, unDisparo;
        readonly Dictionary<Sonido, AudioClip> clips = new Dictionary<Sonido, AudioClip>();
        AudioClip voz;
        float proximaRama, andado;
        Vector3 ultimaPos;
        CharacterController cuerpo;

        GUIStyle sHud, sNota, sTitulo, sMonitor, sObjetivo;
        Texture2D blanco;

        void Awake()
        {
            walker = GetComponent<EntornoWalker>();
            cam = walker != null && walker.cam != null ? walker.cam : GetComponentInChildren<Camera>();
            blanco = Texture2D.whiteTexture;

            var lgo = new GameObject("Linterna");
            lgo.transform.SetParent(cam.transform, false);
            lgo.transform.localPosition = new Vector3(0.18f, -0.12f, 0.1f);
            linterna = lgo.AddComponent<Light>();
            linterna.type = LightType.Spot; linterna.spotAngle = 58f; linterna.innerSpotAngle = 30f;
            linterna.range = 26f; linterna.intensity = 3.6f; linterna.color = new Color(1f, 0.93f, 0.76f);
            linterna.shadows = LightShadows.Soft; linterna.enabled = false;

            CrearSonido();
            cuerpo = GetComponent<CharacterController>();
            ultimaPos = transform.position;
            if (figuraSendero != null) figuraSendero.SetActive(false);
        }

        IEnumerator Start()
        {
            proximaRama = Time.time + 25f;
            yield return new WaitForSeconds(2.2f);
            rotuloInicio = null;
            while (negro > 0f) { negro -= Time.deltaTime / 2.5f; yield return null; }
            Decir("Otra vez sin luz.", 3.5f);
            yield return new WaitForSeconds(3.8f);
            Decir("El generador de la torre se habrá quedado seco.", 4.5f);
            if (inventario.Count == 0) Objetivo("Coge la linterna de la mesilla, junto a la cama");
        }

        // ------------------------------------------------------------ API para los objetos

        public bool Tiene(string clave) => inventario.Contains(clave);

        public void Decir(string texto, float segundos)
        {
            frases.Add((texto, Time.time + segundos));
            if (frases.Count > 3) frases.RemoveAt(0);
        }

        public void Objetivo(string texto) { objetivo = texto; objetivoT = Time.time; }

        public void Leer(string titulo, string texto)
        {
            notaTitulo = titulo; notaTexto = texto;
            Sonar(Sonido.Papel, transform.position, 0.5f);
            if (walker != null) walker.enabled = false;
        }

        public void Recoger(string clave)
        {
            inventario.Add(clave);
            Sonar(Sonido.Clic, transform.position, 0.7f);
            if (clave == "linterna")
            {
                Decir("Linterna de petaca. Aún alumbra.   [F]", 5f);
                linterna.enabled = true;
                Objetivo("Busca la garrafa de combustible. Está junto a la puerta");
            }
            else if (clave == "garrafa")
            {
                Decir("Llena. Suficiente para esta noche.", 4f);
                Objetivo("Lleva la garrafa al generador, al pie de la torre. Sigue los postes");
            }
        }

        public void Sonar(Sonido s, Vector3 donde, float volumen)
        {
            if (clips.TryGetValue(s, out var c)) AudioSource.PlayClipAtPoint(c, donde, volumen);
        }

        public void GeneradorEnMarcha(GeneradorBosque g) => StartCoroutine(ArrancarGenerador(g));

        IEnumerator ArrancarGenerador(GeneradorBosque g)
        {
            motor.transform.position = g.transform.position;
            motor.volume = 0f; motor.Play();
            Decir("Arranca.", 2.5f);
            for (float t = 0; t < 1f; t += Time.deltaTime / 2.5f) { motor.volume = t * 0.85f; motor.pitch = Mathf.Lerp(0.55f, 1f, t); yield return null; }
            hayCorriente = true;
            // Las luces vuelven de una en una, temblando.
            foreach (var l in g.luces)
            {
                if (l == null) continue;
                for (int i = 0; i < 5; i++) { l.enabled = !l.enabled; yield return new WaitForSeconds(Random.Range(0.04f, 0.16f)); }
                l.enabled = true;
                yield return new WaitForSeconds(0.45f);
            }
            if (g.cristales != null)
                foreach (var r in g.cristales)
                {
                    if (r == null) continue;
                    foreach (var m in r.materials)
                        if (m.name.StartsWith("Glass")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", g.brillo); }
                }
            if (estaticaTorre != null) estaticaTorre.Play();
            yield return new WaitForSeconds(3.5f);
            // Algo responde desde el bosque, tapado por el motor.
            Vector3 lejos = g.transform.position + (g.transform.position - transform.position).normalized * 30f + Vector3.up * 2f;
            AudioSource.PlayClipAtPoint(voz, lejos, 1f);
            yield return new WaitForSeconds(2.2f);
            Decir("Debajo del motor hay otro sonido. No viene del bosque. Viene de debajo de la torre.", 6f);
            yield return new WaitForSeconds(5.5f);
            Objetivo("Sube a la cabina y enciende el monitor de las cámaras");
        }

        public void UsarRadio(RadioBosque r)
        {
            if (!r.deTorre)
            {
                Leer("Radio municipal", "«Atención a los vecinos de Valdara.\n\nSe recomienda permanecer en los domicilios hasta recibir nuevas indicaciones.\n\nEl personal sanitario solicita que no se traslade a los enfermos por cuenta propia.\n\nRogamos que dejen libre la carretera de la mina.\n\nRepetimos: la carretera de la mina debe permanecer libre.»\n\nEs una grabación. Lleva días repitiéndose.");
                return;
            }
            if (!hayCorriente) { Decir("Muerta. Sin el generador aquí arriba no funciona nada.", 4.5f); return; }
            if (!transmision) { Decir("La emisora zumba. Nadie llama. Todavía.", 4f); return; }
            if (!fin) StartCoroutine(Transmision());
        }

        IEnumerator Transmision()
        {
            fin = true;
            Decir("«…uesto forestal tres… ¿me recibe?…»", 4.5f);
            yield return new WaitForSeconds(5f);
            Decir("«…mantenga la corriente… repito…»", 4.5f);
            yield return new WaitForSeconds(5f);
            Decir("«…no interrumpan… no interr—»", 4f);
            yield return new WaitForSeconds(4.5f);
            if (estaticaTorre != null) estaticaTorre.volume = 0.05f;
            Decir("La señal se corta. Venía de Valdara.", 4f);
            yield return new WaitForSeconds(4.5f);
            Objetivo("Investiga la señal");
            yield return new WaitForSeconds(6f);
            rotuloInicio = "THE FOG\n\ncontinuará";
            for (float t = 0; t < 1f; t += Time.deltaTime / 4f) { negro = t * 0.82f; yield return null; }
            yield return new WaitForSeconds(5f);
            rotuloInicio = null;
            while (negro > 0f) { negro -= Time.deltaTime / 2f; yield return null; }
        }

        public void AbrirMonitor(MonitorCamaras m)
        {
            if (!hayCorriente) { Decir("Pantalla negra. No hay corriente.", 4f); return; }
            monitor = m; camActual = 0; tiempoEnCasa = 0f;
            if (!figuraRetirada && m.figura != null) m.figura.SetActive(true);
            if (rtMonitor == null) rtMonitor = new RenderTexture(320, 180, 16) { filterMode = FilterMode.Point, name = "Monitor" };
            foreach (var c in m.camaras) if (c != null) { c.targetTexture = rtMonitor; c.enabled = false; }
            m.camaras[0].enabled = true;
            if (walker != null) walker.enabled = false;
            Sonar(Sonido.Clic, transform.position, 0.8f);
        }

        void CerrarMonitor()
        {
            foreach (var c in monitor.camaras) if (c != null) c.enabled = false;
            monitor = null;
            if (walker != null) walker.enabled = true;
            if (figuraRetirada && !transmision)
            {
                transmision = true;
                if (estaticaTorre != null) estaticaTorre.volume = 0.5f;
                Decir("La emisora cruje. Alguien intenta entrar por el canal de Valdara.", 5f);
                Objetivo("Contesta a la emisora");
            }
        }

        // ------------------------------------------------------------ Bucle

        void Update()
        {
            if (notaTexto != null)
            {
                if (PInput.Interact || PInput.Pause) { notaTexto = null; if (walker != null) walker.enabled = true; }
                return;
            }
            if (monitor != null) { ActualizarMonitor(); return; }

            // Qué tiene delante.
            mirando = Delante();
            if (mirando != null && PInput.Interact) mirando.Usar(this);

            if (PInput.Flashlight && Tiene("linterna"))
            {
                linterna.enabled = !linterna.enabled;
                Sonar(Sonido.Clic, transform.position, 0.6f);
            }

            Pasos();
            Sucesos();
        }

        /// <summary>Un paso cada 80 cm andados; más seguidos al correr porque se recorre antes esa distancia.</summary>
        void Pasos()
        {
            Vector3 d = transform.position - ultimaPos; d.y = 0f;
            ultimaPos = transform.position;
            if (cuerpo == null || !cuerpo.isGrounded || d.magnitude > 1f) return; // un salto de posición no es andar
            andado += d.magnitude;
            if (andado < 0.8f) return;
            andado = 0f;
            Sonar(Sonido.Paso, transform.position + Vector3.down * 0.8f, Random.Range(0.22f, 0.34f));
        }

        /// <summary>
        /// Lo que Daniel tiene delante para usar. Se busca con algo de holgura (los objetos pequeños no exigen puntería),
        /// pero nunca a través de una pared o un mueble.
        /// </summary>
        Interactuable Delante()
        {
            Vector3 o = cam.transform.position, f = cam.transform.forward;
            float tope = alcance;
            if (Physics.Raycast(o, f, out var directo, alcance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                var d = directo.collider.GetComponentInParent<Interactuable>();
                if (d != null) return d;
                tope = directo.distance + 0.3f;
            }
            Interactuable mejor = null; float mejorDist = float.MaxValue;
            foreach (var h in Physics.SphereCastAll(o, 0.2f, f, alcance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (h.distance > tope || h.distance >= mejorDist) continue;
                var i = h.collider.GetComponentInParent<Interactuable>();
                if (i != null) { mejor = i; mejorDist = h.distance; }
            }
            return mejor;
        }

        void Sucesos()
        {
            // Ramas que crujen, lejos, sin que se vea nada.
            if (Time.time > proximaRama)
            {
                proximaRama = Time.time + Random.Range(18f, 45f);
                Vector2 d = Random.insideUnitCircle.normalized * Random.Range(14f, 26f);
                Sonar(Sonido.Rama, transform.position + new Vector3(d.x, 0.5f, d.y), 0.9f);
            }

            // La silueta del sendero: quieta entre los árboles hasta que la alumbras o te acercas.
            if (figuraSendero == null) return;
            if (!siluetaHecha && Tiene("garrafa"))
            {
                siluetaHecha = true;
                figuraSendero.SetActive(true);
            }
            if (siluetaHecha && !siluetaVista && figuraSendero.activeSelf)
            {
                // Siempre de cara a Daniel. Una rama cruje cuando ya está a la vista.
                Vector3 mira = transform.position - figuraSendero.transform.position; mira.y = 0f;
                if (mira.sqrMagnitude > 1f) figuraSendero.transform.rotation = Quaternion.LookRotation(mira);
                if (!ramaSilueta && mira.magnitude < 30f) { ramaSilueta = true; Sonar(Sonido.Rama, figuraSendero.transform.position, 1f); }
            }
            if (siluetaHecha && !siluetaVista && figuraSendero.activeSelf)
            {
                Vector3 a = figuraSendero.transform.position + Vector3.up * 1.2f - cam.transform.position;
                bool alumbrada = linterna.enabled && Vector3.Angle(cam.transform.forward, a) < 7f && a.magnitude < 24f;
                if (alumbrada || a.magnitude < 10f)
                {
                    siluetaVista = true;
                    StartCoroutine(QuitarSilueta());
                }
            }
        }

        IEnumerator QuitarSilueta()
        {
            // La linterna falla un instante; cuando vuelve, ya no hay nadie.
            bool estaba = linterna.enabled;
            yield return new WaitForSeconds(0.35f);
            linterna.enabled = false;
            yield return new WaitForSeconds(0.25f);
            figuraSendero.SetActive(false);
            yield return new WaitForSeconds(0.2f);
            linterna.enabled = estaba;
            yield return new WaitForSeconds(1.5f);
            Decir("Ahí había alguien.", 3.5f);
        }

        void ActualizarMonitor()
        {
            if (PInput.Pause || PInput.Quit) { CerrarMonitor(); return; }
            int paso = 0;
            var k = UnityEngine.InputSystem.Keyboard.current;
            if (PInput.Interact || (k != null && (k.dKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame))) paso = 1;
            if (k != null && (k.aKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame)) paso = -1;
            bool enCasa = camActual == monitor.camaras.Length - 1;
            if (enCasa) { tiempoEnCasa += Time.deltaTime; if (monitor.figura != null && monitor.figura.activeSelf) figuraVista = true; }
            if (paso != 0)
            {
                // Al apartar la vista de la cámara de la casa, lo que había se va. Y deja la puerta abierta.
                if (enCasa && figuraVista && !figuraRetirada && tiempoEnCasa > 1.2f)
                {
                    figuraRetirada = true;
                    if (monitor.figura != null) monitor.figura.SetActive(false);
                    if (monitor.puertaCasa != null) monitor.puertaCasa.abierta = true;
                }
                monitor.camaras[camActual].enabled = false;
                camActual = (camActual + paso + monitor.camaras.Length) % monitor.camaras.Length;
                monitor.camaras[camActual].enabled = true;
                Sonar(Sonido.Clic, transform.position, 0.6f);
            }
        }

        // ------------------------------------------------------------ Pantalla

        void Estilos()
        {
            if (sHud != null) return;
            int h = Screen.height;
            sHud = new GUIStyle(GUI.skin.label) { font = fuenteHud, fontSize = Mathf.Max(16, h / 34), alignment = TextAnchor.MiddleCenter, wordWrap = true };
            sHud.normal.textColor = new Color(0.86f, 0.83f, 0.70f);
            sObjetivo = new GUIStyle(sHud) { fontSize = Mathf.Max(14, h / 44), alignment = TextAnchor.UpperLeft };
            sNota = new GUIStyle(GUI.skin.label) { font = fuenteNota, fontSize = Mathf.Max(16, h / 36), alignment = TextAnchor.UpperLeft, wordWrap = true };
            sNota.normal.textColor = new Color(0.13f, 0.12f, 0.10f);
            sTitulo = new GUIStyle(sHud) { fontSize = Mathf.Max(22, h / 16) };
            sMonitor = new GUIStyle(GUI.skin.label) { font = fuenteMonitor, fontSize = Mathf.Max(18, h / 26), alignment = TextAnchor.UpperLeft };
            sMonitor.normal.textColor = new Color(0.72f, 0.95f, 0.72f);
        }

        void Caja(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, blanco); GUI.color = Color.white; }

        void Sombra(Rect r, string t, GUIStyle s)
        {
            var c = s.normal.textColor;
            s.normal.textColor = new Color(0, 0, 0, 0.85f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), t, s);
            s.normal.textColor = c;
            GUI.Label(r, t, s);
        }

        void OnGUI()
        {
            Estilos();
            float w = Screen.width, h = Screen.height;

            if (monitor != null)
            {
                Caja(new Rect(0, 0, w, h), Color.black);
                GUI.color = new Color(0.62f, 0.85f, 0.62f);
                GUI.DrawTexture(new Rect(w * 0.06f, h * 0.06f, w * 0.88f, h * 0.88f), rtMonitor, ScaleMode.StretchToFill);
                GUI.color = Color.white;
                // Líneas de barrido y ruido.
                for (int i = 0; i < 60; i++) Caja(new Rect(w * 0.06f, h * 0.06f + i * h * 0.88f / 60f, w * 0.88f, 1.5f), new Color(0, 0, 0, 0.28f));
                float y = Mathf.Repeat(Time.time * 0.23f, 1f);
                Caja(new Rect(w * 0.06f, h * (0.06f + 0.88f * y), w * 0.88f, h * 0.03f), new Color(1, 1, 1, 0.045f));
                string nombre = monitor.nombres != null && camActual < monitor.nombres.Length ? monitor.nombres[camActual] : "CAM";
                Sombra(new Rect(w * 0.08f, h * 0.08f, w, 60), nombre, sMonitor);
                Sombra(new Rect(w * 0.08f, h * 0.86f, w, 60), "03:1" + (int)Mathf.Repeat(Time.time / 60f, 10f) + "   REC ●", sMonitor);
                Sombra(new Rect(w * 0.60f, h * 0.86f, w, 60), "[E] cámara   [ESC] salir", sMonitor);
                return;
            }

            if (negro > 0f) Caja(new Rect(0, 0, w, h), new Color(0, 0, 0, Mathf.Clamp01(negro)));
            if (rotuloInicio != null) Sombra(new Rect(0, h * 0.3f, w, h * 0.4f), rotuloInicio, sTitulo);

            if (notaTexto != null)
            {
                Caja(new Rect(0, 0, w, h), new Color(0, 0, 0, 0.72f));
                var r = new Rect(w * 0.27f, h * 0.14f, w * 0.46f, h * 0.68f);
                Caja(r, new Color(0.78f, 0.74f, 0.60f));
                Caja(new Rect(r.x, r.y, r.width, 4), new Color(0.35f, 0.32f, 0.24f));
                var dentro = new Rect(r.x + r.width * 0.08f, r.y + r.height * 0.07f, r.width * 0.84f, r.height * 0.86f);
                GUI.Label(dentro, (string.IsNullOrEmpty(notaTitulo) ? "" : notaTitulo.ToUpperInvariant() + "\n\n") + notaTexto, sNota);
                Sombra(new Rect(0, h * 0.86f, w, 40), "[E] dejar", sHud);
                return;
            }

            if (mirando != null) Sombra(new Rect(0, h * 0.57f, w, 44), "[E]  " + mirando.Rotulo, sHud);

            frases.RemoveAll(f => Time.time > f.hasta);
            float fy = h * 0.80f;
            for (int i = frases.Count - 1; i >= 0; i--)
            {
                Sombra(new Rect(w * 0.15f, fy, w * 0.7f, h * 0.06f), frases[i].texto, sHud);
                fy -= h * 0.062f;
            }

            if (!string.IsNullOrEmpty(objetivo) && negro <= 0.01f)
            {
                float a = Mathf.Clamp01((Time.time - objetivoT) / 1.2f);
                var c = sObjetivo.normal.textColor; c.a = Mathf.Lerp(0f, 0.9f, a); sObjetivo.normal.textColor = c;
                GUI.Label(new Rect(w * 0.03f, h * 0.04f, w * 0.6f, 60), "›  " + objetivo, sObjetivo);
            }
        }

        // ------------------------------------------------------------ Sonido (todo generado, sin archivos)

        void CrearSonido()
        {
            ambiente = Fuente("Viento", Bucle("viento", 9f, Viento), 0.35f, false);
            ambiente.Play();
            motor = Fuente("Motor", Bucle("motor", 2f, Motor), 0f, true);
            motor.maxDistance = 70f; motor.minDistance = 3f;
            var estatica = Bucle("estatica", 4f, Estatica);
            if (radioCasa != null) { estaticaCasa = Fuente("Estática casa", estatica, 0.32f, true); estaticaCasa.transform.position = radioCasa.transform.position; estaticaCasa.maxDistance = 14f; estaticaCasa.Play(); }
            if (radioTorre != null) { estaticaTorre = Fuente("Estática torre", estatica, 0.18f, true); estaticaTorre.transform.position = radioTorre.transform.position; estaticaTorre.maxDistance = 16f; }

            clips[Sonido.Clic] = Golpe("clic", 0.05f, 1800f, 0.6f);
            clips[Sonido.Puerta] = Golpe("puerta", 0.5f, 140f, 0.9f);
            clips[Sonido.Liquido] = Golpe("liquido", 1.6f, 420f, 0.35f);
            clips[Sonido.Tiron] = Golpe("tiron", 0.7f, 90f, 1f);
            clips[Sonido.Rama] = Golpe("rama", 0.28f, 900f, 1f);
            clips[Sonido.Papel] = Golpe("papel", 0.3f, 3000f, 0.3f);
            clips[Sonido.Paso] = Golpe("paso", 0.16f, 260f, 0.85f);
            voz = Voz();
        }

        AudioSource Fuente(string nombre, AudioClip clip, float volumen, bool espacial)
        {
            var go = new GameObject("Sonido " + nombre);
            if (!espacial) go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.clip = clip; s.loop = true; s.volume = volumen; s.playOnAwake = false;
            s.spatialBlend = espacial ? 1f : 0f; s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 1.5f; s.maxDistance = 30f;
            return s;
        }

        delegate float Onda(float t, float fase, System.Random rnd, ref float estado);

        static AudioClip Bucle(string nombre, float segundos, Onda onda)
        {
            const int sr = 22050;
            int n = (int)(sr * segundos);
            var d = new float[n];
            var rnd = new System.Random(1987);
            float estado = 0f;
            for (int i = 0; i < n; i++) d[i] = onda(i / (float)sr, i / (float)n, rnd, ref estado);
            // Cruce suave entre el final y el principio para que el bucle no dé un chasquido.
            int cruce = sr / 5;
            for (int i = 0; i < cruce; i++) { float a = i / (float)cruce; d[i] = d[i] * a + d[n - cruce + i] * (1f - a); }
            var clip = AudioClip.Create(nombre, n - cruce, 1, sr, false);
            var recorte = new float[n - cruce]; System.Array.Copy(d, recorte, n - cruce);
            clip.SetData(recorte, 0);
            return clip;
        }

        static float Viento(float t, float fase, System.Random rnd, ref float e)
        {
            float ruido = (float)rnd.NextDouble() * 2f - 1f;
            e += (ruido - e) * 0.018f;                       // paso bajo: soplo grave
            float racha = 0.55f + 0.45f * Mathf.Sin(fase * Mathf.PI * 2f) * Mathf.Sin(fase * Mathf.PI * 6f + 1f);
            return e * 3.2f * racha;
        }

        static float Motor(float t, float fase, System.Random rnd, ref float e)
        {
            float ruido = (float)rnd.NextDouble() * 2f - 1f;
            e += (ruido - e) * 0.12f;
            float golpes = Mathf.Pow(Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f * 23f)), 6f);   // explosiones del cilindro
            return 0.45f * Mathf.Sin(t * Mathf.PI * 2f * 46f) + 0.25f * Mathf.Sin(t * Mathf.PI * 2f * 92f + 0.7f) + e * 0.9f * (0.4f + golpes);
        }

        static float Estatica(float t, float fase, System.Random rnd, ref float e)
        {
            float ruido = (float)rnd.NextDouble() * 2f - 1f;
            e += (ruido - e) * 0.5f;
            float chasquido = rnd.NextDouble() > 0.9992 ? 0.9f : 0f;
            float portadora = 0.06f * Mathf.Sin(t * Mathf.PI * 2f * 610f) * (0.5f + 0.5f * Mathf.Sin(t * 2.1f));
            float respira = 0.5f + 0.5f * Mathf.Sin(fase * Mathf.PI * 2f * 2f);   // sube y baja como una respiración
            return e * 0.28f * (0.55f + 0.45f * respira) + chasquido + portadora;
        }

        static AudioClip Golpe(string nombre, float segundos, float tono, float aspereza)
        {
            const int sr = 22050;
            int n = (int)(sr * segundos);
            var d = new float[n];
            var rnd = new System.Random(nombre.GetHashCode());
            float e = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr, env = Mathf.Exp(-t * 6f / segundos);
                float ruido = (float)rnd.NextDouble() * 2f - 1f;
                e += (ruido - e) * Mathf.Clamp01(tono / 4000f);
                float grano = rnd.NextDouble() > 0.97 ? 1f : 0.55f;
                d[i] = env * (e * aspereza * 2.2f * grano + (1f - aspereza) * Mathf.Sin(t * Mathf.PI * 2f * tono));
            }
            var clip = AudioClip.Create(nombre, n, 1, sr, false);
            clip.SetData(d, 0);
            return clip;
        }

        /// <summary>Algo parecido a una voz: un tono que sube y cae con dos resonancias que se mueven, sin palabras.</summary>
        static AudioClip Voz()
        {
            const int sr = 22050;
            int n = (int)(sr * 2.6f);
            var d = new float[n];
            float fase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr, u = i / (float)n;
                float f0 = Mathf.Lerp(150f, 92f, u) + 9f * Mathf.Sin(t * 31f);
                fase += f0 / sr;
                float sierra = 2f * Mathf.Repeat(fase, 1f) - 1f;
                float f1 = Mathf.Lerp(700f, 320f, Mathf.PingPong(u * 3f, 1f)), f2 = Mathf.Lerp(1100f, 2100f, Mathf.PingPong(u * 2f + 0.3f, 1f));
                float vocal = sierra * (0.6f + 0.4f * Mathf.Sin(t * Mathf.PI * 2f * f1)) * (0.7f + 0.3f * Mathf.Sin(t * Mathf.PI * 2f * f2));
                float silabas = Mathf.Pow(Mathf.Abs(Mathf.Sin(u * Mathf.PI * 5f)), 0.6f);
                float env = Mathf.Sin(u * Mathf.PI);
                d[i] = vocal * silabas * env * 0.5f;
            }
            var clip = AudioClip.Create("voz", n, 1, sr, false);
            clip.SetData(d, 0);
            return clip;
        }
    }
}
