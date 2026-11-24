using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>Controla los botones del inicio y el aviso que sale antes de reemplazar una partida.</summary>
    public sealed class StartMenu : MonoBehaviour
    {
        /// <summary>Continuar solo aparece cuando hay un archivo de partida guardado.</summary>
        public Button continueButton;
        /// <summary>El panel de confirmación que ya está montado en la escena.</summary>
        public GameObject confirmationPanel;
        /// <summary>El texto que avisa si la partida no se puede leer o guardar.</summary>
        public LocalizedText errorMessage;

        void Start()
        {
            continueButton.gameObject.SetActive(GameSaveStore.Exists);
            confirmationPanel.SetActive(false);
            errorMessage.gameObject.SetActive(false);
        }

        /// <summary>Si ya hay una partida, se pregunta antes de reemplazarla, si no, se empieza directamente.</summary>
        public void NewGame()
        {
            if (GameSaveStore.Exists)
                confirmationPanel.SetActive(true);
            else
                ConfirmNewGame();
        }

        /// <summary>Se empieza de cero y se entra en Economía solo si la partida nueva se ha podido guardar.</summary>
        public void ConfirmNewGame()
        {
            confirmationPanel.SetActive(false);

            if (GameSession.Instance.NewGame()) 
                SceneManager.LoadScene("Economia");
            else 
                ShowError("save.error");
        }

        /// <summary>Se cierra el aviso y la partida anterior queda como estaba.</summary>
        public void CancelNewGame() => confirmationPanel.SetActive(false);

        /// <summary>Se recupera la partida y se entra en la tabla periódica, si no se puede leer, aparece el error.</summary>
        public void ContinueGame()
        {
            if (GameSession.Instance.ContinueGame()) 
                SceneManager.LoadScene("TablaPeriodica");
            else 
                ShowError("save.corrupt");
        }

        /// <summary>Se intenta guardar la partida en marcha antes de cerrar el juego.</summary>
        public void Quit()
        {
            GameSession.Instance.Save();
            Application.Quit();
            // En el editor no se cierra Unity, solo se sale del modo Play.
            #if UNITY_EDITOR
            EditorApplication.isPlaying = false;
            #endif
        }

        void ShowError(string key)
        {
            errorMessage.key = key;
            errorMessage.gameObject.SetActive(true);
            errorMessage.Refresh();
        }
    }
}
