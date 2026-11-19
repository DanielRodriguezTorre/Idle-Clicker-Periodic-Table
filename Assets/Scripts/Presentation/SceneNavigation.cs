using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>Este botón lleva a otra pantalla y resalta su pestaña cuando esa pantalla ya está abierta.</summary>
    public sealed class SceneNavigation : MonoBehaviour
    {
        /// <summary>El nombre exacto de la escena de destino, tiene que estar incluida en la compilación.</summary>
        public string sceneName;

        /// <summary>El borde que se enciende para marcar la pestaña actual.</summary>
        public Outline activeOutline;

        // Se conserva la pantalla de origen para que Volver lleve al mismo sitio.
        static string settingsOrigin = "Inicio";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOrigin() => settingsOrigin = "Inicio";

        void OnEnable()
        {
            if (activeOutline != null)
                activeOutline.enabled = SceneManager.GetActiveScene().name == sceneName;
        }

        /// <summary>Se abre la pantalla elegida, al entrar en las opciones se conserva la pantalla de origen.</summary>
        public void Open()
        {
            string currentScene = SceneManager.GetActiveScene().name;

            if (currentScene == sceneName)
                return;

            if (sceneName == "Configuracion") 
                settingsOrigin = currentScene;

            // Para volver al inicio hay que guardar antes, si falla, no se cambia de pantalla.
            if (sceneName == "Inicio" && !GameSession.Instance.ReturnToTitle()) 
                return;

            SceneManager.LoadScene(sceneName);
        }

        /// <summary>Se sale de las opciones y se vuelve a la pantalla desde la que se entró.</summary>
        public void Back() => SceneManager.LoadScene(settingsOrigin);
    }
}
