using UnityEngine;
using UnityEngine.UI;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>
    /// El texto de la escena usa la traducción y el tamaño elegidos por el jugador.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        /// <summary>
        /// El componente de texto asignado en la escena o el prefab.
        /// </summary>
        public Text target;

        /// <summary>
        /// La clave de este texto en el archivo de traducciones.
        /// </summary>
        public string key;

        /// <summary>
        /// El tamaño original del texto cuando la opción está en normal.
        /// </summary>
        public int baseFontSize = 24;

        private object[] arguments;

        /// <summary>
        /// Al activar el texto, se empieza a escuchar cambios de opciones y se actualiza de entrada.
        /// </summary>
        private void OnEnable()
        {
            if (target == null)
            {
                Debug.LogError("No hay referencia de texto.", this);
                enabled = false;
                return;
            }

            GamePreferences.Changed += Refresh;
            Refresh();
        }

        /// <summary>
        /// Al desactivar el texto, se deja de escuchar cambios para no mantener referencias innecesarias.
        /// </summary>
        private void OnDisable()
        {
            GamePreferences.Changed -= Refresh;
        }

        /// <summary>
        /// Se cambian los valores que van dentro del texto, pero se mantiene la misma clave de traducción.
        /// </summary>
        /// <param name="args">Los valores que se mantienen hasta recibir otros, incluso si cambia el idioma.</param>
        public void SetArguments(params object[] args)
        {
            arguments = args;
            Refresh();
        }

        /// <summary>
        /// Se actualizan el contenido y el tamaño del texto con las opciones actuales.
        /// </summary>
        public void Refresh()
        {
            if (target == null) 
                return;

            target.text = GamePreferences.Translate(key, arguments);
            // El tamaño se calcula siempre desde la base para que los cambios no se acumulen.
            target.fontSize = Mathf.Max(1, Mathf.RoundToInt(baseFontSize * GamePreferences.TextScale));
        }
    }
}
