namespace TheFog.Entorno
{
    /// <summary>
    /// Todos los textos de la primera versión jugable (el prólogo: la noche del incidente). Solo datos, sin lógica.
    /// Fechas: llegada el lunes 2/11/1987; la partida empieza la noche del miércoles 11 y acaba la madrugada del jueves 12.
    /// No usar aquí los caracteres ● ■ ► ← → ↑ ↓: las fuentes del proyecto no los tienen.
    /// </summary>
    public static class BosqueGuion
    {
        public struct Linea
        {
            public string hablante, texto;
            public float segundos;
            public bool corte;   // la señal se corta a media frase (donde está el «… …»)
            public Linea(string hablante, string texto, float segundos, bool corte = false)
            { this.hablante = hablante; this.texto = texto; this.segundos = segundos; this.corte = corte; }
        }

        public const string Locutor = "LOCUTOR", Cabo = "CABO SALCEDO", Daniel = "DANIEL", Voz = "VOZ", Relevo = "RELEVO";
        /// <summary>El cabo, mientras no ha dicho quién es (el aviso por el walkie y el principio de la llamada).</summary>
        public const string Municipal = "MUNICIPAL";
        // Etiqueta pequeña sobre la caja del subtítulo: de dónde sale la voz.
        public const string OrigenTele = "TELE · CADENA REGIONAL", OrigenWalkie3 = "WALKIE · CANAL 3",
                            OrigenWalkie5 = "WALKIE · CANAL 5", OrigenEmisora = "EMISORA · CANAL 5";

        /// <summary>Remate: tras la llamada se va la luz de la cabina antes del corte a negro. false = corte a negro sin apagón.</summary>
        public static readonly bool FinalConApagon = true;

        // ------------------------------------------------------------ Tarjetas sobre negro
        public const string InicioTitulo = "VALDARA";
        public const string InicioCuerpo = "Miércoles, 11 de noviembre de 1987";
        public const string MadrugadaTitulo = "Jueves, 12 de noviembre";
        public const string MadrugadaCuerpo = "04:17";
        public const string FinTitulo = "THE FOG";
        public const string FinCuerpo = "Jueves, 12 de noviembre de 1987 · 05:26\n\nEsa mañana no llegó a amanecer en Valdara.\n\nSolo clareó la niebla.";
        public const string FinPie = "Fin de la primera versión · Continuará\n\n"
            + "Interfaz: PSX Horror UI Free, heyheythere (CC BY 4.0) · Música: Distant Screams, Rest! (CC BY-SA 4.0)\n\n"
            + "[ESC] soltar el ratón     [Q] salir";

        // ------------------------------------------------------------ Diario (fecha, texto). Las cuatro primeras están desde el principio.
        public const string DiarioTitulo = "DIARIO";
        public const string DiarioSubtitulo = "Daniel · Valdara, 1987";
        public const string DiarioPie = "[A] [D] pasar página     [E] cerrar";
        public const string DiarioNueva = "Nueva entrada en el diario   [J]";
        public const string DiarioRecuerda = "[J] abrir el diario";

        public static readonly string[][] Diario =
        {
            new[] { "Lunes, 2 de noviembre de 1987",
                "He vuelto a Valdara. Catorce años fuera y la carretera sigue igual de mala.\n\n"
                + "El pueblo es más pequeño de lo que recordaba. O hay menos gente: media calle Mayor tiene las persianas bajadas.\n\n"
                + "En el bar me han mirado como se mira a alguien que se parece a otro. Nadie me ha preguntado por el abuelo. Yo tampoco he preguntado por él." },
            new[] { "Martes, 3 de noviembre",
                "Guarda forestal y vigilante. Eso pone en el título, con el sello todavía fresco. Un año de curso en la capital para acabar donde empecé.\n\n"
                + "La plaza salió en septiembre: vigilante en la torre de control del monte de Valdara. Puesto Forestal 03, según los papeles. No la pidió nadie más.\n\n"
                + "Con la plaza va la casa del guarda: una cabaña de troncos al otro extremo del sendero, con estufa de leña.\n\n"
                + "El contrato no lo firma el Servicio Forestal. Lo firma una contrata de la mina. No pregunté por qué." },
            new[] { "Lunes, 9 de noviembre",
                "Tres noches en la torre con el guarda que se jubila. Cámaras, emisora y el parte de incidencias. No hay mucho más que aprender.\n\n"
                + "Solo insistió en una cosa: si se va la luz, lo primero es el generador. Antes que avisar a nadie. Está aquí, a un lado de la cabaña, y alimenta la torre por el tendido. La torre no puede quedarse sin corriente.\n\n"
                + "Le pregunté por qué. Me dijo que eran órdenes, y que él tampoco lo había preguntado nunca.\n\n"
                + "El jueves entro solo, a las seis de la mañana." },
            new[] { "Miércoles, 11 de noviembre",
                "San Martín. En casa se mataba el cerdo este día y el abuelo nunca estaba: tenía turno en la mina, o decía que lo tenía.\n\n"
                + "De crío me subía a este monte y me mandaba callar. Decía que la mina respiraba, que de noche se oía si paraba el viento. Yo no oía nada y él se enfadaba.\n\n"
                + "En la cabaña había una caja con mi nombre. Dentro, un cuaderno suyo, una fotografía de la bocamina y un aparato con una aguja. El guarda viejo jura que él no la dejó.\n\n"
                + "La furgoneta no arranca desde ayer. Le eché gasolina del bidón por si era eso. No era, y el bidón se quedó allí al lado. Mañana iré andando.\n\n"
                + "Ahora, cuando para el viento, me quedo escuchando sin querer." },
        };

        /// <summary>Se añade al dar por comprobado el apagón.</summary>
        public static readonly string[] DiarioApagon =
        {
            "Jueves, 12 de noviembre. De madrugada",
            "Me ha despertado un golpe. Sordo, lejos, como un portón. Puede que lo haya soñado.\n\n"
            + "No hay luz. Ni la bombilla, ni la tele, ni la radio. Solo anda el despertador, que es de cuerda.\n\n"
            + "No es cosa de la casa: por la ventana no se ve la luz de la torre, y esa no se apaga nunca.\n\n"
            + "Hay niebla. Anoche no la había.\n\n"
            + "Lo primero, el generador. Linterna y gasolina. El bidón sigue junto a la furgoneta."
        };

        /// <summary>Se añade cuando han vuelto todas las luces.</summary>
        public static readonly string[] DiarioGenerador =
        {
            "Jueves, 12 de noviembre. Antes de amanecer",
            "Depósito lleno. Arrancó al tercer tirón.\n\n"
            + "Ha vuelto la luz a la cabaña y se han encendido las del sendero, una detrás de otra, hasta la torre.\n\n"
            + "Hacia Valdara no se ve nada. Ni una ventana. Y huele a tierra mojada sin que haya llovido.\n\n"
            + "Subo a la torre a llamar al pueblo."
        };

        /// <summary>Se añade al acabar la llamada.</summary>
        public static readonly string[] DiarioLlamada =
        {
            "Jueves, 12 de noviembre. En la torre",
            "Ha llamado el cabo Salcedo, el municipal. Explosión en la mina, de madrugada. Valdara entera sin luz y sin teléfono. Arriba no dejan subir a nadie.\n\n"
            + "Me ha mandado quedarme aquí, a la escucha.\n\n"
            + "Antes de cortarse se metió otra voz en el canal. No dio indicativo. «Mantenga la corriente. No interrumpan.» El cabo no la oyó.\n\n"
            + "Es lo mismo que me dijo el guarda viejo.\n\n"
            + "La niebla sube del valle. Viene de donde la mina."
        };

        // ------------------------------------------------------------ Informativo de la tele (cadena inventada)
        public const string TeleCadena = "CADENA REGIONAL";
        public const string TelePrograma = "INFORMATIVO · ÚLTIMA EDICIÓN";
        public const string TeleFin = "DESPEDIDA Y CIERRE";
        public const string TelePieTeclas = "[E] dejar de mirar";
        /// <summary>Rótulo inferior que acompaña a cada línea de Noticia (mismo índice); null = sin rótulo.</summary>
        public static readonly string[] TeleRotulos = { null, "FERIA DE SAN MARTÍN", "FERIA DE SAN MARTÍN", "TRANSPORTE", "VALDARA", "VALDARA", "VALDARA", "EL TIEMPO", "EL TIEMPO", null };

        public static readonly Linea[] Noticia =
        {
            new Linea(Locutor, "Buenas noches. Cadena Regional, última edición del informativo.", 5.0f),
            new Linea(Locutor, "La feria de ganado de San Martín ha cerrado hoy con más de mil reses vendidas.", 6.5f),
            new Linea(Locutor, "Los ganaderos se quejan del precio: el ternero se pagó peor que hace un año.", 6.0f),
            new Linea(Locutor, "El coche de línea de la sierra cambia de horario el lunes. Saldrá a las siete.", 6.5f),
            new Linea(Locutor, "En Valdara, varios vecinos han dado aviso de temblores leves durante la noche.", 6.5f),
            new Linea(Locutor, "La concesionaria de la antigua mina los atribuye al asiento de galerías viejas.", 6.5f),
            new Linea(Locutor, "La empresa insiste: en la galería clausurada no se realiza trabajo alguno.", 6.0f),
            new Linea(Locutor, "El tiempo. Nieblas densas en los valles a partir de la medianoche.", 5.5f),
            new Linea(Locutor, "Se recomienda no circular de madrugada por las carreteras de montaña.", 5.5f),
            new Linea(Locutor, "Es todo por hoy. Que tengan ustedes buenas noches.", 4.0f),
        };
        /// <summary>Índice de Noticia tras el que Daniel piensa PiensaValdara.</summary>
        public const int NoticiaValdara = 4;
        /// <summary>Índice de Noticia a partir del cual ya se puede acostar (empieza «El tiempo»).</summary>
        public const int NoticiaTiempo = 7;

        // ------------------------------------------------------------ Llamada final por la emisora
        public static readonly Linea[] Llamada =
        {
            new Linea(Municipal, "Puesto Forestal Tres, aquí la Municipal de Valdara. ¿Me recibe? Cambio.", 5.5f),
            new Linea(Daniel, "Aquí Forestal Tres. Le recibo con mucho ruido. Cambio.", 4.5f),
            new Linea(Cabo, "Menos mal. Soy el cabo Salcedo. Llevo una hora llamando desde el coche, Daniel.", 6.0f),
            new Linea(Daniel, "Estaba sin corriente. Acabo de arrancar el generador. ¿Qué ha pasado?", 5.5f),
            new Linea(Cabo, "Una explosión en la mina. De madrugada, a eso de las cua… …tembló el pueblo entero.", 7.0f, true),
            new Linea(Daniel, "Se le ha cortado. ¿Una explosión? ¿Había gente dentro?", 4.5f),
            new Linea(Cabo, "No lo sé. Arriba están la Guardia Civil y unos de la empresa. No dejan subir a nadie.", 7.0f),
            new Linea(Daniel, "¿Y el pueblo? Desde aquí no se ve ni una luz.", 4.0f),
            new Linea(Cabo, "Cristales rotos y la gente en la plaza. No hay luz en todo Valdara. Ni teléfono.", 6.5f),
            new Linea(Daniel, "La furgoneta no me arranca, pero bajo andando. En una hora estoy ahí.", 5.5f),
            new Linea(Cabo, "Ni se te ocurra. Sube una niebla del valle que no… …no se ve ni la fuente de la plaza.", 7.0f, true),
            new Linea(Daniel, "Aquí también ha entrado. Dígame qué hago.", 4.0f),
            new Linea(Cabo, "Quedarte a la escucha. Esa torre es lo único que funciona. Y otra cosa, Daniel…", 6.5f),
            new Linea(Voz, "…mantenga la corriente… repito… no interrumpan…", 5.0f),
            new Linea(Daniel, "¿Cabo? Esa no era su voz. ¿Quién más está en este canal?", 5.0f),
        };
        /// <summary>Índice de Llamada en el que la niebla empieza a espesar.</summary>
        public const int LlamadaNiebla = 10;

        // ------------------------------------------------------------ Walkie
        public const string WalkieTitulo = "WALKIE";
        public const string WalkieAparato = "PORTÁTIL · PUESTO FORESTAL 03";
        public const string WalkiePie = "[A] [D] canal     [E] guardar";
        public static readonly string[] WalkieCanales = { "CANAL 3 · PUESTO 03", "CANAL 5 · VALDARA", "CANAL 9 · EMERGENCIAS" };
        public const string WalkieSenal = "SEÑAL", WalkieSinSenal = "SIN SEÑAL", WalkieSinRepetidor = "REPETIDOR SIN CORRIENTE",
                            WalkieLlamada = "LLAMADA ENTRANTE";
        public const string WalkieTardePuesto = "Relevo fuera de servicio hasta las seis.";
        public const string WalkieTardeValdara = "Ayuntamiento de Valdara. Sin servicio hasta las ocho.";
        public const string WalkieTardeEmergencias = "Canal de emergencias. En escucha. Nadie habla.";
        public const string WalkieMudo = "Nadie. Solo ruido.";
        public const string WalkieNadie = "Nadie en el canal.";
        public const string WalkieConteste = "…conteste desde la torre… …con el portátil no le oigo…";
        public const string WalkiePortadora = "Portadora abierta. No habla nadie.";
        public const string WalkieAviso = "WALKIE · llamada entrante por el canal 5   [T]";
        public const string WalkieMapaTeclas = "[T] walkie     [M] mapa";

        /// <summary>Tarde-noche, canal 3: llega sola al cerrar el diario, antes del informativo.</summary>
        public static readonly Linea[] WalkieRelevo =
        {
            new Linea(Relevo, "Portátil Tres, aquí Relevo. Te dejo la torre cerrada y el parte en la mesa.", 6.0f),
            new Linea(Relevo, "Mañana a las seis es tuya. Y lo dicho: que no se quede sin corriente. Corto.", 6.5f),
        };
        /// <summary>Nada más volver la luz: llega rota por el canal 5. La segunda línea se repite como recordatorio si Daniel no sube a la torre.</summary>
        public static readonly Linea[] WalkieCabo =
        {
            new Linea(Municipal, "…restal Tres… aquí la Municip… …¿me recibe?…", 5.0f, true),
            new Linea(Municipal, "…conteste desde la torre… …con el portátil no le oigo…", 5.5f, true),
        };

        // ------------------------------------------------------------ Misiones (título, detalle)
        public const string MisionCabecera = "MISIÓN", MisionNueva = "NUEVA MISIÓN", MisionHecha = "HECHO";
        public static readonly string[] MDiario = { "Abre el diario", "Está en el escritorio. También se abre con [J]." };
        public static readonly string[] MDiarioTecla = { "Abre el diario", "Pulsa [J]. Lo llevas siempre encima." };
        public static readonly string[] MLeer = { "Lee lo que escribiste al llegar", "[A] [D] pasan página. La última entrada es la de hoy." };
        public static readonly string[] MInformativo = { "Escucha el informativo", "La tele está puesta de fondo. Mientras, mira lo que quieras por la cabaña." };
        public static readonly string[] MAcostarse = { "Acuéstate. Mañana entras a las seis", "La cama está junto a la ventana." };
        public static readonly string[] MAcostarseTecla = { "Acuéstate. Mañana entras a las seis", "Pulsa [INTRO] para acostarte." };
        public static readonly string[] MComprobar = { "Comprueba por qué no hay luz", "Prueba la tele y la radio. Mira por la ventana." };
        public static readonly string[] MLinterna = { "Coge la linterna", "Está en la mesilla, junto al despertador." };
        public static readonly string[] MBidon = { "Sal y busca el bidón de gasolina", "Lo dejaste fuera, junto a la furgoneta." };
        /// <summary>Escena sin reconstruir: la garrafa sigue dentro de la cabaña.</summary>
        public static readonly string[] MBidonDentro = { "Coge la garrafa de gasolina", "Está en la cabaña, junto a la puerta." };
        public static readonly string[] MRepostar = { "Llena el generador de gasolina", "Está en el costado de la cabaña, junto a la leñera." };
        public static readonly string[] MRepostarLejos = { "Llena el generador de gasolina", "Está al pie de la torre. Sigue los postes del tendido." };
        public static readonly string[] MArrancar = { "Arranca el generador", "Tira del arranque hasta que prenda." };
        public static readonly string[] MTorre = { "Ve a la torre de control", "Sigue las luces del sendero y sube a la cabina." };
        public static readonly string[] MEmisora = { "Contesta a la emisora", "Alguien llama por el canal de Valdara." };
        public static readonly string[] MCorriente = { "Mantén la corriente", "Que el generador no se pare." };

        // ------------------------------------------------------------ Pensamientos de Daniel: tarde-noche
        public const string Inicio1 = "Ya es de noche. Mañana, a las seis, la torre.";
        public const string Inicio2 = "El diario. Hace días que no escribo.";
        public const string DiarioCerrado = "Ya está escrito.";
        public const string DiarioSinLeer = "Ya lo leeré despacio.";
        public const string PiensaValdara = "Valdara en el informativo. Será la primera vez.";
        public const string FinEmision = "Despedida y cierre. Es tarde.";
        public const string CamaPronto = "Todavía no. Antes, el diario.";
        public const string CamaInformativo = "Aún no. Que acabe el informativo.";
        public const string CamaDormir = "Dejo la tele puesta. Me duermo con el ruido.";
        public const string CamaDormirSinTele = "Me duermo enseguida.";
        public const string CamaSueno = "Se me cierran los ojos.";
        public const string CamaDespierto = "Ya no hay quien duerma.";
        public const string RadioNoche = "Radio comarcal. Coplas y los precios de la lonja. La apago: ya está la tele.";
        public const string VentanaNoche = "Noche cerrada. Al fondo del sendero se ve la luz de la torre.";
        public const string GeneradorRed = "No hace falta. Hay luz de la red.";
        public const string EmisoraCalla = "La emisora zumba. Nadie llama.";

        // ------------------------------------------------------------ Pensamientos: despertar y apagón
        public const string Despertar1 = "Un golpe. Lejos.";
        public const string Despertar2 = "Está todo oscuro. La tele estaba puesta.";
        public const string Despertar2SinTele = "Está todo oscuro.";
        public const string Interruptor1 = "Clic. Nada.";
        public const string Interruptor2 = "Arriba, abajo. Nada. No es la bombilla.";
        public const string CuadroBien = "Los plomos están bien. No es la casa.";
        public const string CuadroLuz = "Todo en su sitio.";
        public const string TeleMuerta = "Ni el punto blanco del centro. Muerta.";
        public const string RadioMuerta = "Enchufada y muda. Ni un zumbido.";
        public const string VentanaApagon = "No se ve la luz de la torre. Esa no se apaga nunca.";
        public const string Apagon1 = "No es la casa. Se ha ido la luz en todo el monte.";
        public const string Apagon2 = "El generador. Lo primero, el generador.";

        // ------------------------------------------------------------ Pensamientos: linterna, salida, bidón, generador
        public const string Linterna = "Linterna de petaca. Aún alumbra.";
        public const string LinternaTecla = "[F] linterna";
        public const string SinLinterna = "A oscuras no veo nada. La linterna está en la mesilla.";
        public const string Salir1 = "Niebla. Anoche no la había.";
        public const string Salir2 = "Huele a tierra mojada. No ha llovido.";
        public const string Salir3 = "Ni un grillo. Anoche los había.";
        public const string WalkieSinCorriente = "El walkie solo da ruido. Sin corriente en la torre no hay repetidor.";
        public const string Bidon = "El bidón de gasolina. Queda más de medio.";
        public const string PuertaCerrada = "La puerta. La dejé abierta.";
        public const string GeneradorSeco = "El depósito está seco. El bidón de gasolina se quedó junto a la furgoneta.";
        public const string GeneradorSecoDentro = "El depósito está seco. La garrafa de gasolina se quedó en casa, junto a la puerta.";
        public const string GeneradorSinBidon = "Queda un resto de gasolina en la lata de reserva. Sirve.";
        public const string Repostar = "Vacío el bidón en el depósito. Huele a gasolina y a tierra mojada.";
        public const string Tiron1 = "Tiro del arranque. Nada.";
        public const string Tiron2 = "Tose. Se cala.";
        public const string Arranca = "Arranca.";
        public const string GeneradorBien = "Ronronea. Mientras tenga gasolina, hay luz.";
        public const string Luces1 = "Las luces del sendero. Una detrás de otra, hasta la torre.";
        public const string Luces2 = "Hacia el pueblo, nada. Ni una ventana.";
        public const string WalkieLlamando = "El walkie. Alguien llama, pero no llega entero.";
        public const string WalkieNoOye = "No me oye. Hay que contestar desde la emisora de la torre.";
        public const string TeleVuelve = "Ha vuelto sola. Nieve. La cadena ya no emite.";
        public const string RadioVuelve = "Solo estática. Ni la comarcal.";
        public const string VentanaLuz = "Vuelve a verse la luz de la torre.";
        public const string Silueta = "Ahí había alguien.";

        // ------------------------------------------------------------ Pensamientos: torre
        public const string PieTorre = "Aquí ya no llega el motor. Y algo zumba. Viene de debajo de la torre.";
        /// <summary>Escena sin reconstruir: el generador sigue al pie de la torre y el motor se oye.</summary>
        public const string PieTorreConMotor = "Debajo del motor zumba otra cosa. Viene de debajo de la torre.";
        public const string Cabina = "La emisora está encendida. Alguien llama por el canal de Valdara.";
        public const string EmisoraSinLuz = "Muerta. Sin el generador aquí arriba no funciona nada.";
        public const string EmisoraCoges = "Cojo el micrófono.";
        public const string MonitorSinLuz = "Pantalla negra. No hay corriente.";
        public const string TrasLlamada = "Solo ruido. La niebla ya tapa el sendero.";

        // ------------------------------------------------------------ Rótulos de interacción (van detrás de «[E]  »)
        public const string RotTeleMirar = "mirar la tele", RotTeleEncender = "encender la tele";
        public const string RotRadioEscuchar = "escuchar la radio", RotRadioEncender = "encender la radio";
        public const string RotEmisoraUsar = "usar la emisora", RotEmisoraContestar = "contestar a la emisora";
        public const string RotCama = "acostarse", RotVentana = "mirar por la ventana", RotInterruptor = "dar al interruptor",
                            RotCuadro = "abrir el cuadro", RotDiario = "abrir el diario", RotMapa = "mirar el mapa", RotWalkie = "coger el walkie",
                            RotLinterna = "coger la linterna", RotBidon = "coger el bidón";
        public const string RotGenRepostar = "repostar", RotGenArrancar = "tirar del arranque", RotGenerador = "generador";

        // ------------------------------------------------------------ Pies de pantalla y ayuda
        public const string NotaPie = "[E] dejar";
        public const string MonitorPie = "[E] cámara   [ESC] salir";
        /// <summary>Añadida por C: nombre de una cámara del monitor que no trae el suyo (va seguido de su número).</summary>
        public const string MonitorCamara = "CAM";
        public const string Ayuda = "WASD  andar     RATÓN  mirar     MAYÚS  correr     E  usar     F  linterna     J  diario     M  mapa     T  walkie     F1  ayuda";
        public const string TeclasHud = "[J] diario   [M] mapa   [T] walkie";

        // ------------------------------------------------------------ Mapa
        public const string MapaTitulo = "MONTE DE VALDARA";
        public const string MapaPuesto = "PUESTO 03";
        public const string MapaPie = "[M] cerrar";
        public const string MapaCasa = "CASA DEL GUARDA", MapaTorre = "TORRE DE CONTROL", MapaCruce = "CRUCE", MapaSendero = "SENDERO DEL TENDIDO",
                            MapaCarbonera = "LA CARBONERA", MapaPenas = "LAS PEÑAS", MapaValdara = "VALDARA (sur) · 4 km",
                            MapaPista = "PISTA DE LA MINA (norte)", MapaCortada = "CORTADA", MapaYo = "yo";
        // Anotaciones a lápiz de Daniel, según avanza la noche.
        public const string MapaNotaSinLuz = "sin luz", MapaNotaLuces = "luces: funcionan",
                            MapaNotaValdara = "Valdara: sin luz, sin teléfono", MapaNotaExplosion = "explosión. no subir";

        // ------------------------------------------------------------ Reservado para la versión siguiente (no se usa en el prólogo)
        public const string RadioMunicipal = "«Atención a los vecinos de Valdara.\n\nSe recomienda permanecer en los domicilios hasta recibir nuevas indicaciones.\n\nEl personal sanitario solicita que no se traslade a los enfermos por cuenta propia.\n\nRogamos que dejen libre la carretera de la mina.\n\nRepetimos: la carretera de la mina debe permanecer libre.»";
    }
}
