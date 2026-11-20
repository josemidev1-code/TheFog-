using System.Collections.Generic;
using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>
    /// Todo el sonido del bosque: efectos sueltos, pasos por superficie, bucles colocados en el mundo («emisores»),
    /// ambiente de interior/exterior y música. Carga los clips de Assets/Resources/Audio/ y, si falta alguno,
    /// usa la síntesis de SintesisBosque o se queda en silencio. Nunca lanza una excepción.
    /// </summary>
    public class BosqueSonido : MonoBehaviour
    {
        public enum Efecto
        {
            Clic, Boton, Interruptor, Papel, PuertaAbrir, PuertaChirrido, PuertaSola, Liquido, Tiron, Rama,
            Apagon, Explosion, LuzEnciende, WalkieAbrir, WalkiePitido, MisionNueva, MisionHecha,
            Susto1, Susto2, Racha, Sintonia, VozLejana
        }
        public enum Bucle
        {
            Generador, TeleVoz, TeleNieve, TeleZumbido, EstaticaRadio, VozRadio, VozIntrusa,
            LlamadaEmisora, ZumbidoElectrico, Dron, Subsuelo, Reloj, Brasas
        }
        public enum Superficie { Madera, Grava, Hojas, Metal, Hormigon }
        public enum Pista { Ninguna, Calma, Tension, Torre }
        public enum Momento { Silencio, Tarde, Madrugada }

        /// <summary>Multiplicadores globales (1 = normal, 0 = mudo). VolumenVoces afecta a TeleVoz, VozRadio y VozIntrusa.</summary>
        public static float VolumenVoces = 0f, VolumenMusica = 1f;   // voces de fondo apagadas: a Josemi no le gustan (20/11)

        /// <summary>
        /// Crea (o devuelve, si ya existe) el gestor como hijo «SONIDO» del oyente (Daniel). Nunca devuelve null.
        /// De «SONIDO» cuelgan solo las fuentes sin posición. Las fuentes 3D de SonarEn y los emisores con alcance mayor que 0
        /// van bajo un objeto raíz «SONIDO MUNDO» (sin padre, en el origen), para que no se muevan ni giren con Daniel.
        /// </summary>
        public static BosqueSonido Crear(Transform oyente)
        {
            var t = oyente != null ? oyente.Find("SONIDO") : null;
            var go = t != null ? t.gameObject : new GameObject("SONIDO");
            if (t == null && oyente != null) go.transform.SetParent(oyente, false);
            var s = go.GetComponent<BosqueSonido>();
            return s != null ? s : go.AddComponent<BosqueSonido>();
        }

        /// <summary>Efecto sin posición (interfaz, cosas que Daniel lleva encima). volumen = volumen del AudioSource (0-1).</summary>
        public void Sonar(Efecto e, float volumen = 1f)
        {
            try
            {
                Preparar();
                float tono;
                var clip = ClipDe(e, out tono);
                if (clip == null) return;
                // Los efectos con tono propio (Boton) van por otra fuente para no desafinar lo que ya esté sonando.
                var f = Mathf.Approximately(tono, 1f) ? efectos : efectosTono;
                if (f == null) return;
                f.pitch = tono;
                f.PlayOneShot(clip, Mathf.Clamp01(volumen));
            }
            catch (System.Exception x) { Fallo("Sonar", x); }
        }

        /// <summary>Efecto en un punto del mundo, con caída lineal hasta «alcance» metros.</summary>
        public void SonarEn(Efecto e, Vector3 donde, float volumen = 1f, float alcance = 25f)
        {
            try
            {
                Preparar();
                float tono;
                var clip = ClipDe(e, out tono);
                if (clip == null) return;
                // La que no esté sonando; si suenan las ocho, la más antigua.
                int k = -1;
                for (int i = 0; i < puntuales.Length; i++)
                {
                    if (puntuales[i] == null) Puntual(i);
                    if (!puntuales[i].isPlaying) { k = i; break; }
                    if (k < 0 || puntualInicio[i] < puntualInicio[k]) k = i;
                }
                if (k < 0) return;
                var f = puntuales[k];
                f.Stop();
                f.transform.position = donde;
                f.maxDistance = Mathf.Max(alcance, MinDistancia + 0.1f);
                f.pitch = tono;
                f.clip = clip;
                f.volume = Mathf.Clamp01(volumen);
                f.Play();
                puntualInicio[k] = Time.unscaledTime;
            }
            catch (System.Exception x) { Fallo("SonarEn", x); }
        }

        /// <summary>Una pisada: variante al azar de la superficie, sin repetir la anterior, tono 0,94-1,06. Sin posición.</summary>
        public void Paso(Superficie s, float volumen = 1f)
        {
            try
            {
                Preparar();
                if (fuentePasos == null) return;
                CargarPasos();
                int k = (int)s;
                AudioClip clip = null;
                if (k >= 0 && k < pisadas.Length && pisadas[k].Count > 0)
                {
                    var lista = pisadas[k];
                    int i = Random.Range(0, lista.Count);
                    if (lista.Count > 1 && i == ultimaPisada[k]) i = (i + 1 + Random.Range(0, lista.Count - 1)) % lista.Count;
                    ultimaPisada[k] = i;
                    clip = lista[i];
                }
                if (clip == null) clip = Sintetico("pa_paso");
                if (clip == null) return;
                fuentePasos.pitch = Random.Range(0.94f, 1.06f);
                fuentePasos.PlayOneShot(clip, Mathf.Clamp01(volumen));
            }
            catch (System.Exception x) { Fallo("Paso", x); }
        }

        /// <summary>
        /// Enciende o actualiza un bucle con nombre («tele», «generador», «voz»…). alcance menor o igual que 0 = sin posición (2D);
        /// si no, 3D con caída lineal (minDistance 1,5; maxDistance = alcance). Si el emisor no existe se crea con volumen 0,
        /// tono 1 y empieza a sonar en un punto al azar del clip. Si existe se mueve a «donde» y, si el bucle es otro, cambia de clip
        /// en el acto. En los dos casos el volumen va al valor pedido en «segundos» (0 = inmediato).
        /// Reutilizar un id que se está apagando cancela su destrucción pendiente. Sobre un id que ya existe se actualizan siempre
        /// la posición, el modo (2D si alcance es 0 o menos, 3D si no: spatialBlend y padre) y maxDistance.
        /// </summary>
        public void Emisor(string id, Bucle b, Vector3 donde, float volumen, float alcance, float segundos = 0.4f)
        {
            if (string.IsNullOrEmpty(id)) return;
            try
            {
                Preparar();
                Canal c;
                bool nuevo = !canales.TryGetValue(id, out c) || c == null || c.objeto == null || c.fuente == null;
                if (nuevo)
                {
                    c = new Canal();
                    c.objeto = new GameObject("Emisor " + id);
                    c.fuente = c.objeto.AddComponent<AudioSource>();
                    Basica(c.fuente, true);
                    c.fuente.volume = 0f;
                    c.fuente.pitch = 1f;
                    c.nivel = 0f;
                    canales[id] = c;
                }
                c.muriendo = false;                      // reutilizar un id cancela su destrucción pendiente
                Colocar(c, donde, alcance);              // siempre: posición, modo, padre y maxDistance
                if (nuevo || c.bucle != b)
                {
                    c.bucle = b;
                    c.voz = b == Bucle.TeleVoz || b == Bucle.VozRadio || b == Bucle.VozIntrusa;
                    var clip = ClipDe(b);
                    c.fuente.Stop();
                    c.fuente.clip = clip;
                    if (clip != null)
                    {
                        // Uno nuevo empieza en un punto al azar; un cambio de bucle, desde el principio (las voces empiezan en pausa).
                        c.fuente.timeSamples = nuevo && clip.samples > 1 ? Random.Range(0, clip.samples - 1) : 0;
                        c.fuente.Play();
                    }
                }
                else if (c.fuente.clip != null && !c.fuente.isPlaying) c.fuente.Play();
                Destino(c, volumen, segundos);
            }
            catch (System.Exception x) { Fallo("Emisor", x); }
        }

        /// <summary>Lleva el volumen de un emisor a otro valor (a 0 calla pero no lo destruye). No hace nada si no existe.</summary>
        public void EmisorVolumen(string id, float volumen, float segundos = 0.4f)
        {
            Canal c;
            if (string.IsNullOrEmpty(id) || !canales.TryGetValue(id, out c) || c == null || c.muriendo) return;
            Destino(c, volumen, segundos);
        }

        /// <summary>Tono (pitch) inmediato de un emisor. No hace nada si no existe.</summary>
        public void EmisorTono(string id, float tono)
        {
            Canal c;
            if (string.IsNullOrEmpty(id) || !canales.TryGetValue(id, out c) || c == null || c.fuente == null) return;
            if (float.IsNaN(tono)) return;
            c.fuente.pitch = Mathf.Clamp(tono, 0.05f, 3f);
        }

        /// <summary>Apaga un emisor con fundido y lo destruye al llegar a 0. No hace nada si no existe.</summary>
        public void Apagar(string id, float segundos = 0.4f)
        {
            Canal c;
            if (string.IsNullOrEmpty(id) || !canales.TryGetValue(id, out c)) return;
            if (c == null || c.objeto == null) { canales.Remove(id); return; }
            c.muriendo = true;
            Destino(c, 0f, segundos);
            if (c.nivel <= Minimo)
            {
                Destroy(c.objeto);
                canales.Remove(id);
            }
        }

        /// <summary>Apaga todos los emisores.</summary>
        public void ApagarTodo(float segundos = 0.4f)
        {
            var ids = new List<string>(canales.Keys);
            for (int i = 0; i < ids.Count; i++) Apagar(ids[i], segundos);
        }

        /// <summary>Cambia de música con fundido cruzado. Pista.Ninguna la quita. Si falta el archivo, no suena nada.</summary>
        public void Musica(Pista p, float volumen = 0.25f, float segundos = 4f)
        {
            try
            {
                Preparar();
                if (musica[0] == null || musica[1] == null) return;
                volumen = Limpio(volumen);
                if (p == pistaActual)
                {
                    if (p != Pista.Ninguna && musicaSuena) DestinoMusica(musicaActiva, volumen, segundos);
                    return;
                }
                DestinoMusica(musicaActiva, 0f, segundos);     // la que hubiera se va
                pistaActual = p;
                musicaSuena = false;
                if (p == Pista.Ninguna) return;
                string ruta = p == Pista.Calma ? "Audio/Musica/calma_pantano" : p == Pista.Tension ? "Audio/Musica/tension" : "Audio/Musica/calma_torre";
                var clip = Cargar(ruta, false);
                if (clip == null) return;
                int otra = 1 - musicaActiva;
                musica[otra].Stop();
                musica[otra].clip = clip;
                musica[otra].volume = 0f;
                musicaNivel[otra] = 0f;
                musica[otra].Play();
                musicaActiva = otra;
                musicaSuena = true;
                DestinoMusica(otra, volumen, segundos);
            }
            catch (System.Exception x) { Fallo("Musica", x); }
        }

        /// <summary>
        /// Lecho de ambiente. Tarde: grillos y viento flojo. Madrugada: sin grillos, más viento, rachas sueltas y un dron grave.
        /// Silencio: todo el lecho a 0. Los emisores y la música no cambian.
        /// </summary>
        public void Escena(Momento m, float segundos = 3f)
        {
            try
            {
                Preparar();
                if (m == Momento.Madrugada && momento != Momento.Madrugada) esperaRacha = Random.Range(35f, 80f);
                momento = m;
                for (int i = 0; i < NLechos; i++)
                {
                    float v = NivelLecho(m, i);
                    lechoObjetivo[i] = v;
                    if (segundos <= 0f) { lechoNivel[i] = v; lechoVel[i] = 0f; }
                    else lechoVel[i] = Mathf.Abs(v - lechoNivel[i]) / segundos;
                    if (v > 0f) ArrancarLecho(i);
                }
            }
            catch (System.Exception x) { Fallo("Escena", x); }
        }

        /// <summary>0 = al aire libre, 1 = bajo techo. Se llama cada frame; el gestor suaviza el cambio (0,6 s).</summary>
        public void Interior(float cuanto)
        {
            techoObjetivo = Limpio(cuanto);
        }

        // ================================================================== de aquí abajo, todo privado

        const float MinDistancia = 1.5f, Minimo = 0.0001f;
        const int NLechos = 4;                              // 0 viento exterior, 1 viento interior, 2 grillos, 3 dron

        static readonly string[] RutaLecho =
        {
            "Audio/Ambiente/viento_bucle", "Audio/Ambiente/viento_interior_bucle", "Audio/Ambiente/grillos_bucle", "Audio/Sint/dron_bucle"
        };
        static readonly string[] RutasChirrido = { "Audio/Puertas/chirrido_corto_1", "Audio/Puertas/chirrido_corto_2" };
        static readonly string[] RutasRama = { "Audio/Sint/rama_1", "Audio/Sint/rama_2" };
        static readonly string[] RutasRacha = { "Audio/Ambiente/racha_1", "Audio/Ambiente/racha_2", "Audio/Ambiente/aullido_tonal" };

        /// <summary>Un bucle con nombre: su objeto, su fuente y hacia dónde va su volumen.</summary>
        class Canal
        {
            public GameObject objeto;
            public AudioSource fuente;
            public Bucle bucle;
            public bool voz, muriendo;
            public float nivel, objetivo, vel;              // vel en unidades de volumen por segundo
        }

        bool preparado;
        Transform mundo;                                    // «SONIDO MUNDO»: raíz, en el origen, no se mueve con Daniel
        AudioSource efectos, efectosTono, fuentePasos;      // 2D, hijas de «SONIDO»
        readonly AudioSource[] musica = new AudioSource[2];
        readonly float[] musicaNivel = new float[2], musicaObjetivo = new float[2], musicaVel = new float[2];
        int musicaActiva;
        bool musicaSuena;
        Pista pistaActual = Pista.Ninguna;

        readonly AudioSource[] lechos = new AudioSource[NLechos];
        readonly float[] lechoNivel = new float[NLechos], lechoObjetivo = new float[NLechos], lechoVel = new float[NLechos];
        readonly bool[] lechoCargado = new bool[NLechos];
        Momento momento = Momento.Silencio;
        float techo, techoObjetivo, esperaRacha = 60f;

        readonly AudioSource[] puntuales = new AudioSource[8];   // 3D, hijas de «SONIDO MUNDO»
        readonly float[] puntualInicio = new float[8];

        readonly Dictionary<string, Canal> canales = new Dictionary<string, Canal>();
        readonly List<string> sobran = new List<string>();

        readonly Dictionary<string, AudioClip> cargados = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, AudioClip> sinteticos = new Dictionary<string, AudioClip>();
        readonly HashSet<string> avisados = new HashSet<string>();

        readonly List<AudioClip>[] pisadas =
        {
            new List<AudioClip>(), new List<AudioClip>(), new List<AudioClip>(), new List<AudioClip>(), new List<AudioClip>()
        };
        readonly int[] ultimaPisada = { -1, -1, -1, -1, -1 };
        bool pasosCargados;

        void Awake()
        {
            try { Preparar(); }
            catch (System.Exception x) { Fallo("Awake", x); }
        }

        void OnDestroy()
        {
            if (mundo != null) Destroy(mundo.gameObject);
        }

        /// <summary>Crea las fuentes una sola vez. Se llama desde Awake y, por si acaso, desde cada método público.</summary>
        void Preparar()
        {
            if (preparado) return;
            preparado = true;
            efectos = Plana("Efectos", false);
            efectosTono = Plana("Efectos con tono", false);
            fuentePasos = Plana("Pasos", false);
            musica[0] = Plana("Música A", true);
            musica[1] = Plana("Música B", true);
            musica[0].priority = 64; musica[1].priority = 64;
            lechos[0] = Plana("Lecho viento", true);
            lechos[1] = Plana("Lecho viento interior", true);
            lechos[2] = Plana("Lecho grillos", true);
            lechos[3] = Plana("Lecho dron", true);
            for (int i = 0; i < NLechos; i++) lechos[i].volume = 0f;
            musica[0].volume = 0f; musica[1].volume = 0f;
            Mundo();
            for (int i = 0; i < puntuales.Length; i++) Puntual(i);
        }

        /// <summary>El objeto raíz del que cuelga todo lo que suena en un punto del mundo. Se rehace si alguien lo destruye.</summary>
        Transform Mundo()
        {
            if (mundo == null)
            {
                var go = new GameObject("SONIDO MUNDO");
                go.transform.SetParent(null, false);
                go.transform.position = Vector3.zero;
                go.transform.rotation = Quaternion.identity;
                mundo = go.transform;
            }
            return mundo;
        }

        /// <summary>Fuente sin posición, hija de «SONIDO».</summary>
        AudioSource Plana(string nombre, bool enBucle)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            var f = go.AddComponent<AudioSource>();
            Basica(f, enBucle);
            f.spatialBlend = 0f;
            return f;
        }

        /// <summary>Fuente 3D reutilizable de SonarEn, hija de «SONIDO MUNDO».</summary>
        void Puntual(int i)
        {
            var go = new GameObject("Efecto 3D " + i);
            go.transform.SetParent(Mundo(), false);
            var f = go.AddComponent<AudioSource>();
            Basica(f, false);
            f.spatialBlend = 1f;
            puntuales[i] = f;
            puntualInicio[i] = -1f;
        }

        static void Basica(AudioSource f, bool enBucle)
        {
            f.playOnAwake = false;
            f.loop = enBucle;
            f.rolloffMode = AudioRolloffMode.Linear;
            f.minDistance = MinDistancia;
            f.maxDistance = 25f;
            f.dopplerLevel = 0f;
        }

        /// <summary>Posición, modo (2D o 3D), padre y alcance de un emisor.</summary>
        void Colocar(Canal c, Vector3 donde, float alcance)
        {
            bool espacial = alcance > 0f;
            Transform padre = espacial ? Mundo() : transform;
            if (c.objeto.transform.parent != padre) c.objeto.transform.SetParent(padre, false);
            if (espacial) c.objeto.transform.position = donde;
            else c.objeto.transform.localPosition = Vector3.zero;
            c.fuente.spatialBlend = espacial ? 1f : 0f;
            c.fuente.maxDistance = espacial ? Mathf.Max(alcance, MinDistancia + 0.1f) : 500f;
        }

        static float Limpio(float v) { return float.IsNaN(v) ? 0f : Mathf.Clamp01(v); }

        void Destino(Canal c, float volumen, float segundos)
        {
            c.objetivo = Limpio(volumen);
            if (segundos <= 0f)
            {
                c.nivel = c.objetivo; c.vel = 0f;
                if (c.fuente != null) c.fuente.volume = c.nivel * (c.voz ? Limpio(VolumenVoces) : 1f);
            }
            else c.vel = Mathf.Abs(c.objetivo - c.nivel) / segundos;
        }

        void DestinoMusica(int i, float volumen, float segundos)
        {
            musicaObjetivo[i] = Limpio(volumen);
            if (segundos <= 0f) { musicaNivel[i] = musicaObjetivo[i]; musicaVel[i] = 0f; }
            else musicaVel[i] = Mathf.Abs(musicaObjetivo[i] - musicaNivel[i]) / segundos;
        }

        static float Hacia(float nivel, float objetivo, float vel, float dt)
        {
            if (nivel == objetivo) return nivel;
            return vel <= 0f ? objetivo : Mathf.MoveTowards(nivel, objetivo, vel * dt);
        }

        // ------------------------------------------------------------------ mezcla (7.4)

        static float NivelLecho(Momento m, int i)
        {
            if (m == Momento.Tarde) return i == 0 ? 0.30f : i == 1 ? 0.18f : i == 2 ? 0.18f : 0f;
            if (m == Momento.Madrugada) return i == 0 ? 0.50f : i == 1 ? 0.22f : i == 2 ? 0f : 0.10f;
            return 0f;
        }

        static float FactorTecho(int i, float c)
        {
            if (i == 1) return c;                           // el viento interior solo se oye bajo techo
            if (i == 3) return 1f - 0.4f * c;               // el dron pasa casi entero
            return 1f - 0.75f * c;                          // viento exterior y grillos
        }

        void ArrancarLecho(int i)
        {
            var f = lechos[i];
            if (f == null) return;
            if (!lechoCargado[i])
            {
                lechoCargado[i] = true;
                f.clip = Cargar(RutaLecho[i], i == 0) ?? (i == 0 ? Sintetico("viento") : null);
            }
            if (f.clip != null && !f.isPlaying)
            {
                f.timeSamples = f.clip.samples > 1 ? Random.Range(0, f.clip.samples - 1) : 0;
                f.Play();
            }
        }

        void Update()
        {
            if (!preparado) return;
            try { Mezclar(); }
            catch (System.Exception x) { Fallo("Update", x); }
        }

        void Mezclar()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);    // un tirón de carga no se come un fundido entero

            techo = Mathf.MoveTowards(techo, techoObjetivo, dt / 0.6f);

            // Lechos.
            for (int i = 0; i < NLechos; i++)
            {
                var f = lechos[i];
                if (f == null) continue;
                lechoNivel[i] = Hacia(lechoNivel[i], lechoObjetivo[i], lechoVel[i], dt);
                f.volume = lechoNivel[i] * FactorTecho(i, techo);
                if (lechoNivel[i] <= Minimo && lechoObjetivo[i] <= Minimo && f.isPlaying) f.Stop();
            }

            // Rachas sueltas: solo de madrugada.
            if (momento == Momento.Madrugada)
            {
                esperaRacha -= Time.deltaTime;
                if (esperaRacha <= 0f)
                {
                    esperaRacha = Random.Range(35f, 80f);
                    Sonar(Efecto.Racha, 0.25f * (1f - 0.75f * techo));
                }
            }

            // Música.
            float vm = Limpio(VolumenMusica);
            for (int i = 0; i < 2; i++)
            {
                var f = musica[i];
                if (f == null) continue;
                musicaNivel[i] = Hacia(musicaNivel[i], musicaObjetivo[i], musicaVel[i], dt);
                f.volume = musicaNivel[i] * vm;
                if (musicaNivel[i] <= Minimo && musicaObjetivo[i] <= Minimo && f.isPlaying) f.Stop();
            }

            // Emisores.
            float vv = Limpio(VolumenVoces);
            sobran.Clear();
            foreach (var par in canales)
            {
                var c = par.Value;
                if (c == null || c.objeto == null || c.fuente == null) { sobran.Add(par.Key); continue; }
                c.nivel = Hacia(c.nivel, c.objetivo, c.vel, dt);
                c.fuente.volume = c.nivel * (c.voz ? vv : 1f);
                if (c.muriendo && c.nivel <= Minimo)
                {
                    Destroy(c.objeto);
                    sobran.Add(par.Key);
                }
            }
            for (int i = 0; i < sobran.Count; i++) canales.Remove(sobran[i]);
        }

        // ------------------------------------------------------------------ clips: carga perezosa con caché y respaldo

        /// <summary>Clip de Resources (ruta sin extensión) o null. Avisa una sola vez por ruta que falte.</summary>
        AudioClip Cargar(string ruta, bool hayRespaldo)
        {
            AudioClip clip;
            if (cargados.TryGetValue(ruta, out clip)) return clip != null ? clip : null;
            try { clip = Resources.Load<AudioClip>(ruta); }
            catch (System.Exception) { clip = null; }
            if (clip == null)
            {
                clip = null;
                if (avisados.Add(ruta))
                    Debug.LogWarning("[Bosque] Falta el sonido Resources/" + ruta + (hayRespaldo ? ": se usa el respaldo sintetizado." : ": no sonará."));
            }
            cargados[ruta] = clip;
            return clip;
        }

        /// <summary>Uno al azar de varias rutas; si el elegido falta, el siguiente que exista.</summary>
        AudioClip AlAzar(string[] rutas, bool hayRespaldo)
        {
            int k = Random.Range(0, rutas.Length);
            for (int i = 0; i < rutas.Length; i++)
            {
                var clip = Cargar(rutas[(k + i) % rutas.Length], hayRespaldo);
                if (clip != null) return clip;
            }
            return null;
        }

        /// <summary>Respaldos sintetizados, hechos la primera vez que hacen falta. Clave vacía = silencio.</summary>
        AudioClip Sintetico(string clave)
        {
            if (string.IsNullOrEmpty(clave)) return null;
            AudioClip clip;
            if (sinteticos.TryGetValue(clave, out clip) && clip != null) return clip;
            clip = null;
            try
            {
                switch (clave)
                {
                    case "clic": clip = SintesisBosque.Golpe("clic", 0.05f, 1800f, 0.6f); break;
                    case "papel": clip = SintesisBosque.Golpe("papel", 0.3f, 3000f, 0.3f); break;
                    case "puerta": clip = SintesisBosque.Golpe("puerta", 0.5f, 140f, 0.9f); break;
                    case "liquido": clip = SintesisBosque.Golpe("liquido", 1.6f, 420f, 0.35f); break;
                    case "tiron": clip = SintesisBosque.Golpe("tiron", 0.7f, 90f, 1f); break;
                    case "rama": clip = SintesisBosque.Golpe("rama", 0.28f, 900f, 1f); break;
                    case "mision": clip = SintesisBosque.Golpe("mision", 0.5f, 180f, 0.2f); break;
                    case "motor": clip = SintesisBosque.Bucle("motor", 2f, SintesisBosque.Motor); break;
                    case "estatica": clip = SintesisBosque.Bucle("estatica", 4f, SintesisBosque.Estatica); break;
                    case "viento": clip = SintesisBosque.Bucle("viento", 9f, SintesisBosque.Viento); break;
                    case "voz": clip = SintesisBosque.Voz(); break;
                    case "pa_clic": clip = TheFog.Puesto.ProcAudio.Click; break;
                    case "pa_golpe": clip = TheFog.Puesto.ProcAudio.Sting; break;
                    case "pa_pitido": clip = TheFog.Puesto.ProcAudio.Beep; break;
                    case "pa_zumbido": clip = TheFog.Puesto.ProcAudio.Hum; break;
                    case "pa_paso": clip = TheFog.Puesto.ProcAudio.Step; break;
                }
            }
            catch (System.Exception x) { clip = null; Fallo("Sintetico " + clave, x); }
            sinteticos[clave] = clip;
            return clip;
        }

        AudioClip Con(string ruta, string respaldo)
        {
            var clip = Cargar(ruta, !string.IsNullOrEmpty(respaldo));
            return clip != null ? clip : Sintetico(respaldo);
        }

        /// <summary>Tabla Efecto → clip (7.3). «tono» es el pitch con el que debe sonar.</summary>
        AudioClip ClipDe(Efecto e, out float tono)
        {
            tono = 1f;
            switch (e)
            {
                case Efecto.Clic: return Con("Audio/Sint/boton_clic", "clic");
                case Efecto.Boton: tono = 0.9f; return Con("Audio/Sint/boton_clic", "clic");
                case Efecto.Interruptor: return Con("Audio/Sint/interruptor_clic", "clic");
                case Efecto.Papel: return Con("Audio/Sint/papel", "papel");
                case Efecto.PuertaAbrir: return Con("Audio/Puertas/puerta_abrir", "puerta");
                case Efecto.PuertaChirrido: return AlAzar(RutasChirrido, false);
                case Efecto.PuertaSola: return Con("Audio/Puertas/chirrido_largo", "puerta");
                case Efecto.Liquido: return Con("Audio/Sint/gasolina", "liquido");
                case Efecto.Tiron: return Con("Audio/Sint/generador_tiron", "tiron");
                case Efecto.Rama: { var c = AlAzar(RutasRama, true); return c != null ? c : Sintetico("rama"); }
                case Efecto.Apagon: return Con("Audio/Sint/apagon", "pa_clic");
                case Efecto.Explosion: return Con("Audio/Sint/explosion_lejana", "pa_golpe");
                case Efecto.LuzEnciende: return Con("Audio/Sint/luz_enciende", "pa_clic");
                case Efecto.WalkieAbrir: return Con("Audio/Sint/walkie_abrir", "pa_clic");
                case Efecto.WalkiePitido: return Con("Audio/Sint/walkie_pitido", "pa_pitido");
                case Efecto.MisionNueva: return Con("Audio/Sint/mision_nueva", "mision");
                case Efecto.MisionHecha: return Con("Audio/Sint/mision_hecha", null);
                case Efecto.Susto1: return Con("Audio/Sustos/golpe_1", "pa_golpe");
                case Efecto.Susto2: return Con("Audio/Sustos/golpe_2", "pa_golpe");
                case Efecto.Racha: return AlAzar(RutasRacha, false);
                case Efecto.Sintonia: return Con("Audio/Sint/sintonia_informativo", null);
                case Efecto.VozLejana: return Sintetico("voz");
            }
            return null;
        }

        /// <summary>Tabla Bucle → clip (7.3).</summary>
        AudioClip ClipDe(Bucle b)
        {
            switch (b)
            {
                case Bucle.Generador: return Con("Audio/Sint/generador_bucle", "motor");
                case Bucle.TeleVoz: return Con("Audio/Sint/voz_tele_bucle", "voz");
                case Bucle.TeleNieve: return Con("Audio/Sint/tele_nieve_bucle", "estatica");
                case Bucle.TeleZumbido: return Con("Audio/Sint/tele_zumbido_bucle", "pa_zumbido");
                case Bucle.EstaticaRadio: return Con("Audio/Sint/estatica_radio_bucle", "estatica");
                case Bucle.VozRadio: return Con("Audio/Sint/voz_radio_bucle", "voz");
                case Bucle.VozIntrusa: return Con("Audio/Sint/voz_intrusa_bucle", "voz");
                case Bucle.LlamadaEmisora: return Con("Audio/Sint/emisora_llamada_bucle", "estatica");
                case Bucle.ZumbidoElectrico: return Con("Audio/Sint/zumbido_electrico_bucle", "pa_zumbido");
                case Bucle.Dron: return Con("Audio/Sint/dron_bucle", null);
                case Bucle.Subsuelo: return Con("Audio/Sint/subsuelo_bucle", "pa_zumbido");
                case Bucle.Reloj: return Con("Audio/Sint/reloj_bucle", null);
                case Bucle.Brasas: return Con("Audio/Sint/brasas_bucle", null);
            }
            return null;
        }

        // ------------------------------------------------------------------ pasos

        /// <summary>Carga de una vez Audio/Pasos y agrupa por prefijo («madera_3» y «madera_b_2» son Madera).</summary>
        void CargarPasos()
        {
            if (pasosCargados) return;
            pasosCargados = true;
            AudioClip[] todos = null;
            try { todos = Resources.LoadAll<AudioClip>("Audio/Pasos"); }
            catch (System.Exception) { todos = null; }
            if (todos != null)
            {
                for (int i = 0; i < todos.Length; i++)
                {
                    var clip = todos[i];
                    if (clip == null || string.IsNullOrEmpty(clip.name)) continue;
                    string n = clip.name.ToLowerInvariant();
                    int raya = n.LastIndexOf('_');
                    string prefijo = raya > 0 ? n.Substring(0, raya) : n;
                    int k = -1;
                    switch (prefijo)
                    {
                        case "madera": case "madera_b": k = (int)Superficie.Madera; break;
                        case "grava": k = (int)Superficie.Grava; break;
                        case "hojas": k = (int)Superficie.Hojas; break;
                        case "metal": case "metal_b": k = (int)Superficie.Metal; break;
                        case "hormigon": k = (int)Superficie.Hormigon; break;
                    }
                    if (k >= 0) pisadas[k].Add(clip);
                }
            }
            string faltan = "";
            for (int k = 0; k < pisadas.Length; k++)
                if (pisadas[k].Count == 0) faltan += (faltan.Length > 0 ? ", " : "") + ((Superficie)k);
            if (faltan.Length > 0 && avisados.Add("Audio/Pasos"))
                Debug.LogWarning("[Bosque] Faltan pisadas en Resources/Audio/Pasos (" + faltan + "): se usa la pisada sintetizada.");
        }

        // ------------------------------------------------------------------ avisos

        /// <summary>Un único aviso por sitio si algo inesperado sale mal: el juego sigue sin ese sonido.</summary>
        void Fallo(string sitio, System.Exception x)
        {
            if (avisados.Add("fallo:" + sitio))
                Debug.LogWarning("[Bosque] Sonido: algo ha salido mal en " + sitio + " (" + (x != null ? x.Message : "?") + "). Se sigue sin ese sonido.");
        }
    }
}
