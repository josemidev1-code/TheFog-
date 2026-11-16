using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TheFog.EditorTools.Entorno
{
    /// <summary>
    /// Al pulsar Play en el editor siempre arranca el juego (escena TorreDaniel), tengas abierta la escena que tengas.
    /// Al salir de Play vuelves a la escena en la que estabas. Desactivar: THE FOG → Play arranca el juego.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayArrancaJuego
    {
        const string Escena = "Assets/Scenes/TorreDaniel.unity";
        const string Pref = "TheFog.PlayArrancaJuego";
        const string Menu = "THE FOG/Play arranca el juego";

        static PlayArrancaJuego() => EditorApplication.delayCall += Aplicar;

        static bool Activo
        {
            get => EditorPrefs.GetBool(Pref, true);
            set => EditorPrefs.SetBool(Pref, value);
        }

        static void Aplicar()
        {
            EditorSceneManager.playModeStartScene = Activo ? AssetDatabase.LoadAssetAtPath<SceneAsset>(Escena) : null;
        }

        [MenuItem(Menu)]
        static void Alternar() { Activo = !Activo; Aplicar(); }

        [MenuItem(Menu, true)]
        static bool AlternarCheck() { Menu.ToString(); UnityEditor.Menu.SetChecked(Menu, Activo); return true; }
    }
}
