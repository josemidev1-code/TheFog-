#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheFog.Entorno;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace TheFog.EditorTools
{
    /// <summary>
    /// Prueba automática de la primera versión del bosque: entra en Play, recorre la partida con un teclado simulado y
    /// los atajos de prueba (F9, F10, F11), saca capturas en docs/capturas/primera_version/ y deja un informe en
    /// docs/entorno/primera_version_prueba.json. Lanzar con el editor CON ventana (sin -batchmode):
    ///   Unity.exe -projectPath . -executeMethod TheFog.EditorTools.BosquePrueba.Ejecutar
    /// </summary>
    [InitializeOnLoad]
    public static class BosquePrueba
    {
        const string Marca = "TheFog.BosquePrueba";
        static BosquePrueba()
        {
            EditorApplication.playModeStateChanged += estado =>
            {
                if (estado != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Marca, false)) return;
                SessionState.SetBool(Marca, false);
                new GameObject("Prueba automatica del bosque").AddComponent<BosquePruebaDriver>();
            };
        }

        [MenuItem("THE FOG/Bosque/4. Prueba automática (Play, ~5 min)")]
        public static void Ejecutar()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Bosque.unity");
            SessionState.SetBool(Marca, true);
            SessionState.SetBool(Marca + ".salir", Environment.GetCommandLineArgs().Contains("-executeMethod"));
            EditorApplication.EnterPlaymode();
        }
    }

    public class BosquePruebaDriver : MonoBehaviour
    {
        [Serializable]
        public class Informe
        {
            public string unity; public bool superada; public float segundos;
            public List<string> pasos = new List<string>(); public List<string> fallos = new List<string>(); public List<string> avisos = new List<string>();
        }

        const string Carpeta = "docs/capturas/primera_version";
        readonly Informe informe = new Informe();
        Keyboard teclado;
        BosqueJuego juego;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            informe.unity = Application.unityVersion;
            Directory.CreateDirectory(Carpeta);
            teclado = InputSystem.AddDevice<Keyboard>("TecladoPruebaBosque");
            Application.logMessageReceived += AlRegistrar;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        }

        void AlRegistrar(string mensaje, string pila, LogType tipo)
        {
            string m = mensaje.Length > 300 ? mensaje.Substring(0, 300) : mensaje;
            if (tipo == LogType.Exception || tipo == LogType.Assert || tipo == LogType.Error)
                informe.fallos.Add(m + " | " + (pila ?? "").Split('\n').FirstOrDefault());
            else if (tipo == LogType.Warning && m.Contains("[Bosque]")) informe.avisos.Add(m);
        }

        IEnumerator Start()
        {
            yield return Guiado();
            Terminar();
        }

        IEnumerator Pulsar(Key tecla)
        {
            InputSystem.QueueStateEvent(teclado, new KeyboardState(tecla));
            yield return null; yield return null; yield return null;
            InputSystem.QueueStateEvent(teclado, new KeyboardState());
            yield return null; yield return null;
        }

        IEnumerator Foto(string nombre)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Carpeta + "/" + nombre + ".png");
            yield return null; yield return null;
            var cam = Camera.main;
            informe.pasos.Add("captura " + nombre + " (fase " + (juego != null ? juego.FaseActual.ToString() : "?") + ")"
                + (juego != null ? " pos " + juego.transform.position.ToString("0.0") : "")
                + (cam != null ? " mira " + cam.transform.forward.ToString("0.00") : ""));
        }

        IEnumerator Esperar(float s) { yield return new WaitForSecondsRealtime(s); }

        IEnumerator HastaFase(BosqueJuego.Fase fase, float limite)
        {
            float t0 = Time.realtimeSinceStartup;
            while (juego != null && juego.FaseActual < fase && Time.realtimeSinceStartup - t0 < limite) yield return null;
            bool ok = juego != null && juego.FaseActual >= fase;
            (ok ? informe.pasos : informe.fallos).Add((ok ? "llega a la fase " : "NO llega a la fase ") + fase + " en " + (Time.realtimeSinceStartup - t0).ToString("0") + " s");
        }

        IEnumerator Guiado()
        {
            yield return Esperar(1f);
            juego = FindAnyObjectByType<BosqueJuego>();
            if (juego == null) { informe.fallos.Add("No hay BosqueJuego en la escena"); yield break; }
            informe.pasos.Add("BosqueJuego presente; sin scripts perdidos: " +
                FindObjectsByType<Transform>(FindObjectsSortMode.None).All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0));

            yield return Esperar(10f);
            var mando = Gamepad.current;
            informe.pasos.Add(mando == null ? "sin mando conectado" :
                "mando «" + mando.displayName + "»: palanca izquierda " + mando.leftStick.ReadValue().ToString("0.000") +
                ", derecha " + mando.rightStick.ReadValue().ToString("0.000") + " (zona muerta del juego " + TheFog.Puesto.PInput.ZonaMuerta + ")");
            yield return Foto("juego_01_inicio");
            yield return Pulsar(Key.J); yield return Esperar(1.5f);
            yield return Foto("juego_02_diario");
            yield return Pulsar(Key.D); yield return Esperar(1f);
            yield return Foto("juego_03_diario_pagina2");
            yield return Pulsar(Key.E); yield return Esperar(1f);
            yield return Pulsar(Key.M); yield return Esperar(1.5f);
            yield return Foto("juego_04_mapa");
            yield return Pulsar(Key.M); yield return Esperar(1f);
            yield return Pulsar(Key.T); yield return Esperar(1.5f);
            yield return Foto("juego_05_walkie");
            yield return Pulsar(Key.E); yield return Esperar(1f);

            var tele = FindObjectsByType<Accionable>(FindObjectsSortMode.None).FirstOrDefault(a => a.clave == "tele");
            if (tele != null) { tele.Usar(juego); informe.pasos.Add("usa la tele"); }
            else informe.fallos.Add("No hay Accionable «tele»");
            yield return Esperar(4f);
            yield return Foto("juego_06_tele");
            yield return Pulsar(Key.E); yield return Esperar(8f);
            yield return Foto("juego_07_informativo_de_fondo");
            yield return Esperar(12f);
            yield return Foto("juego_08_informativo_subtitulo");

            yield return Pulsar(Key.F9);
            informe.pasos.Add("F9: a dormir");
            yield return Esperar(6f);
            yield return Foto("juego_09_durmiendo");
            yield return HastaFase(BosqueJuego.Fase.Madrugada, 120f);
            yield return Esperar(6f);
            yield return Foto("juego_10_madrugada_apagon");

            yield return Pulsar(Key.F10);
            informe.pasos.Add("F10: al generador con linterna y bidón");
            yield return Esperar(3f);
            yield return Foto("juego_11_junto_al_generador");
            var gen = FindAnyObjectByType<GeneradorBosque>();
            if (gen == null) informe.fallos.Add("No hay GeneradorBosque");
            else
            {
                for (int i = 0; i < 4 && !gen.enMarcha; i++) { gen.Usar(juego); yield return Esperar(3f); }
                (gen.enMarcha ? informe.pasos : informe.fallos).Add(gen.enMarcha ? "el generador arranca" : "el generador NO arranca");
            }
            yield return Esperar(8f);
            yield return Foto("juego_12_vuelve_la_luz");
            yield return Esperar(25f);
            yield return Foto("juego_13_luces_del_sendero");
            int encendidas = gen != null && gen.luces != null ? gen.luces.Count(l => l != null && l.enabled) : -1;
            informe.pasos.Add("luces del generador encendidas: " + encendidas + " de " + (gen != null && gen.luces != null ? gen.luces.Length : 0));
            yield return Esperar(25f);

            yield return Pulsar(Key.F11);
            informe.pasos.Add("F11: a la cabina de la torre");
            yield return Esperar(4f);
            yield return Foto("juego_14_cabina");
            if (juego.radioTorre != null) { juego.UsarRadio(juego.radioTorre); informe.pasos.Add("usa la emisora"); }
            else informe.avisos.Add("radioTorre sin enlazar: se espera a que la llamada entre sola");
            yield return Esperar(12f);
            yield return Foto("juego_15_llamada_1");
            yield return Esperar(30f);
            yield return Foto("juego_16_llamada_2");
            yield return HastaFase(BosqueJuego.Fase.Fin, 260f);
            yield return Esperar(9f);
            yield return Foto("juego_17_final");
        }

        void Terminar()
        {
            informe.segundos = Time.realtimeSinceStartup;
            informe.superada = informe.fallos.Count == 0;
            File.WriteAllText("docs/entorno/primera_version_prueba.json", JsonUtility.ToJson(informe, true));
            Application.logMessageReceived -= AlRegistrar;
            if (teclado != null) InputSystem.RemoveDevice(teclado);
            bool salir = SessionState.GetBool("TheFog.BosquePrueba.salir", false);
            EditorApplication.ExitPlaymode();
            if (salir) EditorApplication.delayCall += () => EditorApplication.Exit(informe.superada ? 0 : 1);
        }
    }
}
#endif
