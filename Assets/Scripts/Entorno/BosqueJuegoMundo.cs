using System.Collections;
using System.Collections.Generic;
using TheFog.Puesto;
using UnityEngine;

namespace TheFog.Entorno
{
    /// <summary>
    /// Parte de BosqueJuego que toca el mundo: lo que encuentra solo en la escena (por componente o por nombre),
    /// las luces y sus dos circuitos, los cristales, la niebla, los pasos y los sucesos de la noche.
    /// Todo tolera que falte cualquier objeto: la escena en disco puede ser anterior al constructor.
    /// </summary>
    public partial class BosqueJuego
    {
        // ---- Lo que se encuentra en la escena (cualquiera puede ser null)
        Transform casa, torre;
        GeneradorBosque generadorB;
        Accionable tele, cama, ventana;
        bool camaEnganchada;        // escena sin reconstruir: la cama es un modelo al que se le ha puesto el accionable aquí; INTRO también vale
        readonly Dictionary<string, Accionable> accionables = new Dictionary<string, Accionable>();
        PuertaAbatible puertaCasa;
        Light luna, brasas, luzTele;
        Recogible[] recogibles = new Recogible[0];
        MonitorCamaras[] monitores = new MonitorCamaras[0];
        readonly List<Light> luces = new List<Light>();

        class Cristal { public Renderer r; public Material m; public bool on; }
        bool cristalesDentroCasa, cristalesEnCabina;   // dónde estaba Daniel la última vez que se pintaron
        readonly List<Cristal> cristales = new List<Cristal>();
        Color brilloCristal = new Color(0.95f, 0.58f, 0.24f);

        Vector3 posTele, posMesilla, posBrasas, posPuerta, posBombilla, posEmisora, posGenerador, posPieTorre;

        // ---- Ambiente de la escena, tal como llega
        float d0, lunaL0;
        Color c0, amb0, ambCielo0, ambEcuador0, ambSuelo0, brasasColor0;
        bool ambienteGuardado, brasasGuardadas;

        // ---- Luz de la tele
        bool teleNieve;
        float tTeleLuz;

        // ---- Pasos y sucesos
        float proximaRama, andado, tPuerta = -1f;
        Vector3 ultimaPos;
        bool puertaHecha, puertaSola, puertaDicha;
        bool siluetaVista, ramaSilueta;

        /// <summary>Ejecuta algo que toca objetos de la escena sin dejar que una excepción mate la corrutina que lo llama.</summary>
        void Seguro(System.Action a)
        {
            try { a(); }
            catch (System.Exception ex) { Debug.LogWarning("[Bosque] " + ex.Message); }
        }

        // ------------------------------------------------------------ Autocableado (Awake)

        void CrearLinterna()
        {
            if (cam == null) { Debug.LogWarning("[Bosque] Daniel no tiene cámara: no habrá linterna ni interacción."); return; }
            var lgo = new GameObject("Linterna");
            lgo.transform.SetParent(cam.transform, false);
            lgo.transform.localPosition = new Vector3(0.18f, -0.12f, 0.1f);
            linterna = lgo.AddComponent<Light>();
            linterna.type = LightType.Spot; linterna.spotAngle = 58f; linterna.innerSpotAngle = 30f;
            linterna.range = 26f; linterna.intensity = 3.6f; linterna.color = new Color(1f, 0.93f, 0.76f);
            linterna.shadows = LightShadows.Soft; linterna.enabled = false;
        }

        void BuscarEdificios()
        {
            var c = GameObject.Find("CASA DE DANIEL");
            if (c != null) casa = c.transform;
            var t = GameObject.Find("TORRE DE DANIEL");
            if (t != null) torre = t.transform;
        }

        void BuscarRadios()
        {
            foreach (var r in Object.FindObjectsByType<RadioBosque>(FindObjectsInactive.Include))
            {
                if (r == null) continue;
                if (r.deTorre) { if (radioTorre == null) radioTorre = r; }
                else if (radioCasa == null) radioCasa = r;
            }
        }

        void BuscarGenerador()
        {
            // Un generador desactivado no se puede usar: cuenta como si no hubiera (la luz volverá sola).
            if (generador != null) generadorB = generador.GetComponent<GeneradorBosque>();
            if (generadorB != null && !(generadorB.enabled && generadorB.gameObject.activeInHierarchy)) generadorB = null;
            if (generadorB == null)
                foreach (var g in Object.FindObjectsByType<GeneradorBosque>(FindObjectsInactive.Include))
                    if (g != null && g.enabled && g.gameObject.activeInHierarchy) { generadorB = g; break; }
            if (generadorB != null) brilloCristal = generadorB.brillo;
            else Debug.LogWarning("[Bosque] No hay generador en la escena: de madrugada la luz volverá sola al poco de salir de la casa.");
        }

        void BuscarAccionables()
        {
            foreach (var a in Object.FindObjectsByType<Accionable>(FindObjectsInactive.Include))
            {
                if (a == null || string.IsNullOrEmpty(a.clave)) continue;
                if (!accionables.ContainsKey(a.clave)) accionables[a.clave] = a;
            }
            tele = Accionable_("tele");
            if (tele == null) tele = Enganchar(BosqueGuion.RotTeleMirar, "tele", "Televisor");
            cama = Accionable_("cama");
            if (cama == null) { cama = Enganchar(BosqueGuion.RotCama, "cama", "Cama", "cama", "Bed_003"); camaEnganchada = cama != null; }
            ventana = Accionable_("ventana");
            if (ventana == null) ventana = Enganchar(BosqueGuion.RotVentana, "ventana", "Ventana", "ventana_cortinas", "Windows_C_001");
        }

        /// <summary>El accionable de esa clave, o null si no hay o lo han destruido.</summary>
        Accionable Accionable_(string clave)
        {
            Accionable a;
            return accionables.TryGetValue(clave, out a) && a != null ? a : null;
        }

        /// <summary>
        /// Escena sin reconstruir: convierte en accionable un objeto que solo existe como modelo. Lo busca primero entre los
        /// descendientes de la casa (la torre usa el mismo pack y podría repetir nombres) y solo después en toda la escena.
        /// </summary>
        Accionable Enganchar(string rotulo, string clave, params string[] nombres)
        {
            foreach (var nombre in nombres)
            {
                Transform t = casa != null ? BuscarHijo(casa, nombre) : null;
                if (t == null) { var go = GameObject.Find(nombre); if (go != null) t = go.transform; }
                if (t == null) continue;
                var a = t.GetComponent<Accionable>();
                if (a == null)
                {
                    if (t.GetComponent<Interactuable>() != null) continue;   // ya es otra cosa (una puerta, una nota): no se pisa
                    a = t.gameObject.AddComponent<Accionable>();
                }
                a.clave = clave; a.rotulo = rotulo;
                if (t.GetComponentInChildren<Collider>(true) == null) PonerCaja(t);
                accionables[clave] = a;
                return a;
            }
            return null;
        }

        static Transform BuscarHijo(Transform padre, string nombre)
        {
            if (padre == null) return null;
            for (int i = 0; i < padre.childCount; i++)
            {
                var h = padre.GetChild(i);
                if (h == null) continue;
                if (h.name == nombre) return h;
                var dentro = BuscarHijo(h, nombre);
                if (dentro != null) return dentro;
            }
            return null;
        }

        /// <summary>Caja de colisión con los límites de los renderers (mínimo 0,22 m por eje; sin renderers, cubo de 0,3 m).</summary>
        static void PonerCaja(Transform t)
        {
            var caja = t.gameObject.AddComponent<BoxCollider>();
            Bounds b;
            if (!Limites(t, out b)) { caja.center = Vector3.zero; caja.size = Dividir(Vector3.one * 0.3f, t.lossyScale); return; }
            Vector3 tam = new Vector3(Mathf.Max(0.22f, b.size.x), Mathf.Max(0.22f, b.size.y), Mathf.Max(0.22f, b.size.z));
            caja.center = t.InverseTransformPoint(b.center);
            // Los límites están en ejes del mundo: se pasan a los del objeto (vale para giros de cualquier ángulo, por exceso).
            Vector3 x = t.InverseTransformVector(new Vector3(tam.x, 0f, 0f)), y = t.InverseTransformVector(new Vector3(0f, tam.y, 0f)),
                    z = t.InverseTransformVector(new Vector3(0f, 0f, tam.z));
            caja.size = new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x), Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
                                    Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
        }

        static Vector3 Dividir(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x / Mathf.Max(0.001f, Mathf.Abs(b.x)), a.y / Mathf.Max(0.001f, Mathf.Abs(b.y)), a.z / Mathf.Max(0.001f, Mathf.Abs(b.z)));
        }

        static bool Limites(Transform t, out Bounds b)
        {
            b = new Bounds();
            bool hay = false;
            if (t == null) return false;
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                if (!hay) { b = r.bounds; hay = true; } else b.Encapsulate(r.bounds);
            }
            return hay;
        }

        void BuscarResto()
        {
            if (casa != null) puertaCasa = casa.GetComponentInChildren<PuertaAbatible>(true);
            if (figuraSendero == null) figuraSendero = GameObject.Find("Silueta del sendero");
            var l = GameObject.Find("Luna");
            if (l != null) luna = l.GetComponent<Light>();
            recogibles = Object.FindObjectsByType<Recogible>(FindObjectsInactive.Include);
            monitores = Object.FindObjectsByType<MonitorCamaras>(FindObjectsInactive.Include);
            if (casa != null)
            {
                var b = BuscarHijo(casa, "Brasas");
                if (b != null) brasas = b.GetComponent<Light>();
            }
        }

        void CalcularPuntos()
        {
            Vector3 aqui = transform.position;
            posTele = casa != null ? casa.TransformPoint(MundoBosque.LocalTele) : aqui;
            Bounds b;
            if (tele != null && Limites(tele.transform, out b)) posTele = b.center;
            else if (tele != null) posTele = tele.transform.position;
            posMesilla = casa != null ? casa.TransformPoint(MundoBosque.LocalMesilla) : aqui;
            posBrasas = casa != null ? casa.TransformPoint(MundoBosque.LocalBrasas) : aqui;
            posPuerta = casa != null ? casa.TransformPoint(MundoBosque.LocalPuerta) : aqui;
            posBombilla = casa != null ? casa.TransformPoint(MundoBosque.LocalBombilla) : aqui;
            posEmisora = radioTorre != null ? radioTorre.transform.position
                       : torre != null ? torre.TransformPoint(MundoBosque.LocalEmisora) : aqui;
            posGenerador = generadorB != null ? generadorB.transform.position
                         : new Vector3(MundoBosque.Generador.x, MundoBosque.Altura(MundoBosque.Generador), MundoBosque.Generador.y);
            posPieTorre = torre != null ? torre.position
                        : new Vector3(MundoBosque.Torre.x, MundoBosque.Altura(MundoBosque.Torre), MundoBosque.Torre.y);

            foreach (var r in recogibles)
                if (r != null && r.clave == "garrafa" && MundoBosque.DentroDeCasa(casa, r.transform.position)) bidonEnCasa = true;
            generadorEnTorre = Vector2.Distance(new Vector2(posGenerador.x, posGenerador.z), MundoBosque.Torre) < 30f;
        }

        void GuardarAmbiente()
        {
            d0 = RenderSettings.fogDensity; c0 = RenderSettings.fogColor;
            amb0 = RenderSettings.ambientLight; ambCielo0 = RenderSettings.ambientSkyColor;
            ambEcuador0 = RenderSettings.ambientEquatorColor; ambSuelo0 = RenderSettings.ambientGroundColor;
            if (luna != null) lunaL0 = luna.intensity;
            if (brasas != null) { brasasColor0 = brasas.color; brasasGuardadas = true; }
            ambienteGuardado = true;
        }

        static readonly string[] NombresLuces =
        {
            "Luz generador", "Luz interior", "Luz porche", "Luz poste 0", "Luz poste 1", "Luz poste 2", "Luz poste 3", "Luz poste 4",
            "Luz pie de torre", "Luz cabina",
        };

        void PrepararLuces()
        {
            luces.Clear();
            if (generadorB != null && generadorB.luces != null)
                foreach (var l in generadorB.luces) if (l != null && !luces.Contains(l)) luces.Add(l);
            if (luces.Count == 0)
            {
                var todas = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
                foreach (var nombre in NombresLuces)
                    foreach (var l in todas)
                        if (l != null && l.name == nombre && !luces.Contains(l)) { luces.Add(l); break; }
            }
            bool hayPostes = false;
            foreach (var l in luces) if (l != null && l.name.StartsWith("Luz poste")) hayPostes = true;
            if (!hayPostes)
            {
                // Escena sin reconstruir: las luces del sendero no existen. Se crean en los postes del tendido.
                for (int k = 0; k < MundoBosque.Postes.Length; k++)
                {
                    Vector2 p = MundoBosque.Postes[k];
                    Vector2 h = (MundoBosque.PuntoDelRamal(p) - p).normalized;
                    var go = new GameObject("Luz poste " + k);
                    go.transform.position = new Vector3(p.x + h.x * 0.9f, MundoBosque.Altura(p) + 4.6f, p.y + h.y * 0.9f);
                    var l = go.AddComponent<Light>();
                    l.type = LightType.Point; l.color = new Color(1f, 0.74f, 0.42f); l.intensity = 5.5f; l.range = 16f;
                    l.shadows = LightShadows.None; l.enabled = false;
                    luces.Add(l);
                }
            }
        }

        void CrearLuzTele()
        {
            if (tele == null) return;
            Vector3 hacia = casa != null ? casa.position - posTele : Vector3.zero;
            hacia.y = 0f;
            if (hacia.sqrMagnitude > 1e-4f) hacia = hacia.normalized * 0.45f; else hacia = Vector3.zero;
            var go = new GameObject("Luz tele");
            go.transform.position = posTele + hacia + Vector3.up * 0.15f;
            luzTele = go.AddComponent<Light>();
            luzTele.type = LightType.Point; luzTele.color = new Color(0.55f, 0.68f, 1f); luzTele.intensity = 1.6f; luzTele.range = 4.5f;
            luzTele.shadows = LightShadows.None; luzTele.enabled = false;
        }

        void PrepararCristales()
        {
            cristales.Clear();
            var vistos = new HashSet<Renderer>();
            if (generadorB != null && generadorB.cristales != null)
            {
                foreach (var r in generadorB.cristales) AnotarCristal(r, vistos);
            }
            else
            {
                if (casa != null) foreach (var r in casa.GetComponentsInChildren<Renderer>(true)) AnotarCristal(r, vistos);
                if (torre != null) foreach (var r in torre.GetComponentsInChildren<Renderer>(true)) AnotarCristal(r, vistos);
            }
        }

        void AnotarCristal(Renderer r, HashSet<Renderer> vistos)
        {
            if (r == null || !vistos.Add(r)) return;
            bool tiene = false;
            foreach (var m in r.sharedMaterials) if (m != null && m.name.StartsWith("Glass")) tiene = true;
            if (!tiene) return;
            foreach (var m in r.materials)      // se instancian una sola vez
                if (m != null && m.name.StartsWith("Glass")) cristales.Add(new Cristal { r = r, m = m });
        }

        void PonerAyuda()
        {
            foreach (var a in Object.FindObjectsByType<AyudaControles>(FindObjectsInactive.Include))
                if (a != null) { a.texto = BosqueGuion.Ayuda; a.segundos = 16f; }
        }

        // ------------------------------------------------------------ Luces, cristales, niebla

        /// <summary>Red del pueblo: las únicas encendidas por la tarde y las que se apagan con la explosión.</summary>
        static bool DeRed(Light l)
        {
            if (l == null) return false;
            string n = l.name;
            return n == "Luz interior" || n == "Luz porche" || n == "Luz cabina";
        }

        Light LuzPorNombre(string nombre)
        {
            foreach (var l in luces) if (l != null && l.name == nombre) return l;
            return null;
        }

        void Cristales(bool on, Transform de = null)
        {
            foreach (var c in cristales)
            {
                if (c.m == null) continue;
                if (de != null && (c.r == null || !c.r.transform.IsChildOf(de))) continue;
                c.on = on;
                PintarCristal(c);
            }
        }

        /// <summary>
        /// El brillo de un cristal es para verlo encendido DESDE FUERA. El vidrio es una sola malla para las dos caras, así
        /// que con Daniel dentro del edificio se apaga: si no, las ventanas se ven blancas, como de día, desde dentro.
        /// </summary>
        void PintarCristal(Cristal c)
        {
            if (c == null || c.m == null) return;
            bool desdeDentro = c.r != null &&
                ((cristalesDentroCasa && casa != null && c.r.transform.IsChildOf(casa)) ||
                 (cristalesEnCabina && torre != null && c.r.transform.IsChildOf(torre)));
            if (c.on && !desdeDentro) { c.m.EnableKeyword("_EMISSION"); c.m.SetColor("_EmissionColor", brilloCristal); }
            else c.m.SetColor("_EmissionColor", Color.black);
        }

        void CristalesSegunDondeEste(Vector3 pos)
        {
            bool enCasa = MundoBosque.DentroDeCasa(casa, pos), enCabina = MundoBosque.EnCabina(torre, pos);
            if (enCasa == cristalesDentroCasa && enCabina == cristalesEnCabina) return;
            cristalesDentroCasa = enCasa; cristalesEnCabina = enCabina;
            foreach (var c in cristales) PintarCristal(c);
        }

        void TodasLasLuces(bool on)
        {
            foreach (var l in luces)
            {
                if (l == null) continue;
                l.enabled = on;
            }
            Cristales(on);
        }

        void Corriente(bool on, bool conSonido)
        {
            hayCorriente = on;
            if (on)
            {
                foreach (var l in luces)
                {
                    if (l == null) continue;
                    l.enabled = DeRed(l);
                }
                Cristales(true);
                if (luzTele != null) luzTele.enabled = true;
            }
            else
            {
                if (luzTele != null) luzTele.enabled = false;
                sonido.Apagar("tele", 0.05f); sonido.Apagar("tele_z", 0.05f); sonido.Apagar("zumbido", 0.05f); sonido.Apagar("emisora", 0.05f);
                if (conSonido) sonido.Sonar(BosqueSonido.Efecto.Apagon, 0.8f);
                TodasLasLuces(false);
            }
        }

        void EncenderLuz(Light l, bool on) { if (l != null) l.enabled = on; }
        void AlternarLuz(Light l) { if (l != null) l.enabled = !l.enabled; }

        /// <summary>Luz y sombra de fondo según la hora. madrugada = false: la tarde-noche del principio.</summary>
        void Ambientar(bool madrugada)
        {
            if (!ambienteGuardado) return;
            float kNiebla = madrugada ? 1.75f : 0.6f, kAmb = madrugada ? 0.55f : 1.25f;
            RenderSettings.fogDensity = d0 * kNiebla;
            Color niebla = madrugada ? c0 * 0.62f : c0; niebla.a = 1f;
            RenderSettings.fogColor = niebla;
            RenderSettings.ambientLight = Por(amb0, kAmb);
            RenderSettings.ambientSkyColor = Por(ambCielo0, kAmb);
            RenderSettings.ambientEquatorColor = Por(ambEcuador0, kAmb);
            RenderSettings.ambientGroundColor = Por(ambSuelo0, kAmb);
            if (luna != null) luna.intensity = madrugada ? lunaL0 * 0.45f : lunaL0;
            if (madrugada && brasas != null && brasasGuardadas)
            {
                // No se toca su intensidad: LuzParpadeo la pisa cada frame.
                Color c = brasasColor0 * 0.7f; c.a = brasasColor0.a;
                brasas.color = c; brasas.range = 3.2f;
            }
            if (cam != null) cam.backgroundColor = RenderSettings.fogColor;
            foreach (var m in monitores)
            {
                if (m == null || m.camaras == null) continue;
                foreach (var c in m.camaras) if (c != null) c.backgroundColor = RenderSettings.fogColor;
            }
        }

        static Color Por(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, c.a); }

        IEnumerator Niebla(float densidad, float segundos)
        {
            float desde = d0;
            Seguro(() => desde = RenderSettings.fogDensity);
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.1f, segundos))
            {
                float v = Mathf.Lerp(desde, densidad, t);
                Seguro(() => RenderSettings.fogDensity = v);
                yield return null;
            }
            Seguro(() => RenderSettings.fogDensity = densidad);
        }

        /// <summary>Temblor de la luz de la tele: lento y amplio con programa, nervioso con nieve.</summary>
        void TeleLuz()
        {
            if (luzTele == null || !luzTele.enabled || Time.time < tTeleLuz) return;
            if (teleNieve) { luzTele.intensity = Random.Range(1.0f, 1.6f); tTeleLuz = Time.time + Random.Range(0.03f, 0.08f); }
            else { luzTele.intensity = Random.Range(1.2f, 2.0f); tTeleLuz = Time.time + Random.Range(0.08f, 0.5f); }
        }

        /// <summary>Bajo techo o al aire libre: lo oye el gestor de sonido, y el motor baja dentro (no hay oclusión).</summary>
        void Ambiente()
        {
            Vector3 pos = transform.position;
            bool antes = bajoTecho;
            bajoTecho = MundoBosque.DentroDeCasa(casa, pos) || MundoBosque.EnCabina(torre, pos);
            Seguro(() => CristalesSegunDondeEste(pos));
            sonido.Interior(bajoTecho ? 1f : 0f);
            if (bajoTecho != antes && arrancando && !finalizando && Time.time - tArranque > 2.6f)
                sonido.EmisorVolumen("generador", bajoTecho ? 0.28f : 0.55f, 0.6f);
        }

        // ------------------------------------------------------------ Pasos

        /// <summary>Un paso cada 80 cm andados; más seguidos al correr porque se recorre antes esa distancia.</summary>
        void Pasos()
        {
            Vector3 d = transform.position - ultimaPos; d.y = 0f;
            ultimaPos = transform.position;
            if (cuerpo == null || !cuerpo.isGrounded || d.magnitude > 1f) return; // un salto de posición no es andar
            andado += d.magnitude;
            if (andado < 0.8f) return;
            andado = 0f;
            float v = Random.Range(0.45f, 0.6f);
            if (PInput.Sprint) v *= 1.2f;
            sonido.Paso(Suelo(), v);
        }

        BosqueSonido.Superficie Suelo()
        {
            RaycastHit h;
            if (!Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, out h, 1.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                || h.collider == null)
                return BosqueSonido.Superficie.Hojas;
            Transform t = h.collider.transform;
            if (casa != null && t.IsChildOf(casa)) return BosqueSonido.Superficie.Madera;
            if (torre != null && t.IsChildOf(torre))
                return transform.position.y - torre.position.y > 17.2f ? BosqueSonido.Superficie.Madera : BosqueSonido.Superficie.Metal;
            if (h.collider is TerrainCollider)
            {
                Vector2 xz = new Vector2(transform.position.x, transform.position.z);
                bool grava = MundoBosque.DistanciaASendero(xz) < 1.7f || Vector2.Distance(xz, MundoBosque.Casa) < 6.5f
                          || Vector2.Distance(xz, MundoBosque.Torre) < 9f;
                return grava ? BosqueSonido.Superficie.Grava : BosqueSonido.Superficie.Hojas;
            }
            return BosqueSonido.Superficie.Hormigon;
        }

        // ------------------------------------------------------------ Sucesos de la noche

        void Sucesos()
        {
            Vector3 pos = transform.position;
            bool fuera = !MundoBosque.DentroDeCasa(casa, pos);

            // Ramas que crujen, lejos, sin que se vea nada.
            if (fase == Fase.Madrugada && !bajoTecho && Time.time > proximaRama)
            {
                proximaRama = Time.time + Random.Range(18f, 45f);
                Vector2 d = Random.insideUnitCircle.normalized * Random.Range(14f, 26f);
                sonido.SonarEn(BosqueSonido.Efecto.Rama, pos + new Vector3(d.x, 0.5f, d.y), 0.9f, 40f);
            }

            // La puerta de la cabaña, que se cierra sola al poco de coger el bidón.
            if (!puertaHecha && tPuerta > 0f && Time.time > tPuerta && fase == Fase.Madrugada)
            {
                puertaHecha = true;
                if (puertaCasa != null && puertaCasa.abierta)
                {
                    if (fuera)
                    {
                        puertaCasa.abierta = false; puertaSola = true;
                        sonido.SonarEn(BosqueSonido.Efecto.PuertaSola, posPuerta, 0.9f, 40f);
                    }
                }
                else sonido.SonarEn(BosqueSonido.Efecto.Rama, pos - transform.forward * 9f + Vector3.up * 0.5f, 1f, 30f);
            }
            if (puertaSola && !puertaDicha && Vector3.Distance(pos, posPuerta) < 4f)
            {
                puertaDicha = true;
                Decir(BosqueGuion.PuertaCerrada, 4f);
            }

            // La silueta del sendero: quieta entre los árboles hasta que la alumbras o te acercas.
            if (figuraSendero != null && lucesHechas && !siluetaVista && figuraSendero.activeSelf)
            {
                // Siempre de cara a Daniel. Una rama cruje cuando ya está a la vista.
                Vector3 mira = pos - figuraSendero.transform.position; mira.y = 0f;
                if (mira.sqrMagnitude > 1f) figuraSendero.transform.rotation = Quaternion.LookRotation(mira);
                if (!ramaSilueta && mira.magnitude < 30f)
                {
                    ramaSilueta = true;
                    sonido.SonarEn(BosqueSonido.Efecto.Rama, figuraSendero.transform.position, 1f, 40f);
                }
                Vector3 ojos = cam != null ? cam.transform.position : pos + Vector3.up * 1.6f;
                Vector3 frente = cam != null ? cam.transform.forward : transform.forward;
                Vector3 a = figuraSendero.transform.position + Vector3.up * 1.2f - ojos;
                bool alumbrada = linterna != null && linterna.enabled && Vector3.Angle(frente, a) < 7f && a.magnitude < 24f;
                if (alumbrada || a.magnitude < 10f)
                {
                    siluetaVista = true;
                    StartCoroutine(QuitarSilueta());
                }
            }
        }

        IEnumerator QuitarSilueta()
        {
            // La linterna y las farolas fallan un instante; cuando vuelven, ya no hay nadie.
            bool estaba = false;
            var postes = new List<Light>();
            yield return new WaitForSeconds(0.35f);
            Seguro(() =>
            {
                if (linterna != null) { estaba = linterna.enabled; linterna.enabled = false; }
                foreach (var l in luces)
                {
                    if (l == null) continue;
                    if (l.enabled && l.name.StartsWith("Luz poste")) { postes.Add(l); l.enabled = false; }
                }
            });
            sonido.Sonar(BosqueSonido.Efecto.Susto1, 0.5f);
            yield return new WaitForSeconds(0.25f);
            Seguro(() => { if (figuraSendero != null) figuraSendero.SetActive(false); });
            yield return new WaitForSeconds(0.25f);
            Seguro(() =>
            {
                if (fase == Fase.Fin || !hayCorriente) return;   // el final ya lo ha apagado todo
                if (linterna != null) linterna.enabled = estaba;
                foreach (var l in postes) if (l != null) l.enabled = true;
            });
            yield return new WaitForSeconds(1.5f);
            if (fase != Fase.Fin) Decir(BosqueGuion.Silueta, 3.5f);
        }

        // ------------------------------------------------------------ Lo que Daniel tiene delante

        /// <summary>
        /// Lo que Daniel tiene delante para usar. Se busca con algo de holgura (los objetos pequeños no exigen puntería),
        /// pero nunca a través de una pared o un mueble.
        /// </summary>
        Interactuable Delante()
        {
            if (cam == null) return null;
            Vector3 o = cam.transform.position, f = cam.transform.forward;
            float tope = alcance;
            RaycastHit directo;
            if (Physics.Raycast(o, f, out directo, alcance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && directo.collider != null)
            {
                var d = directo.collider.GetComponentInParent<Interactuable>();
                if (d != null && d.isActiveAndEnabled) return d;
                tope = directo.distance + 0.3f;
            }
            Interactuable mejor = null; float mejorDist = float.MaxValue;
            foreach (var h in Physics.SphereCastAll(o, 0.2f, f, alcance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (h.collider == null || h.distance > tope || h.distance >= mejorDist) continue;
                var i = h.collider.GetComponentInParent<Interactuable>();
                if (i != null && i.isActiveAndEnabled) { mejor = i; mejorDist = h.distance; }
            }
            return mejor;
        }
    }
}
