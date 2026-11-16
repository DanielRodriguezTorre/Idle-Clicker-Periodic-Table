using UnityEngine;
using UnityEngine.UI;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>
    /// Las opciones se eligen con los botones que ya trae la escena, sin crear desplegables al vuelo.
    /// </summary>
    public sealed class SettingsView : MonoBehaviour
    {
        /// <summary>
        /// Los bordes que marcan el idioma, el tamaño y la paleta elegidos.
        /// </summary>
        public Outline[] languageOutlines, sizeOutlines, colorOutlines;

        private void OnEnable()
        {
            GamePreferences.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() => GamePreferences.Changed -= Refresh;

        /// <summary>
        /// Se cambia el idioma, las pantallas abiertas se enteran del cambio y las siguientes usan el mismo.
        /// </summary>
        public void SetLanguage(int index) => GamePreferences.SetLanguage(index);

        /// <summary>
        /// Se elige si el texto se ve pequeño, normal o grande.
        /// </summary>
        public void SetTextSize(int index) => GamePreferences.SetTextSize(index);

        /// <summary>
        /// Se eligen los colores normales o una de las tres paletas alternativas.
        /// </summary>
        public void SetColorMode(int index) => GamePreferences.SetColorMode(index);

        private void Refresh()
        {
            Highlight(languageOutlines, GamePreferences.LanguageIndex);
            Highlight(sizeOutlines, GamePreferences.TextSizeIndex);
            Highlight(colorOutlines, GamePreferences.ColorMode);
        }

        private static void Highlight(Outline[] outlines, int selected)
        {
            // Solo queda encendido el borde de la opción seleccionada.
            for (int index = 0; index < outlines.Length; index++)
                outlines[index].enabled = index == selected;
        }
    }
}
