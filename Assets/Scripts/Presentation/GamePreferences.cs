using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>
    /// El idioma, el tamaño del texto y los colores se guardan aparte de la partida.
    /// </summary>
    public static class GamePreferences
    {
        private const string LanguagePreference = "Presentation.Language";
        private const string TextSizePreference = "Presentation.TextSize";
        private const string ColorPreference = "Presentation.ColorMode";
        // Este orden tiene que coincidir con las columnas del archivo de traducciones y los botones de idioma.
        private static readonly string[] LanguageCodes = { "es", "en", "de", "da", "fr", "it" };
        private static readonly float[] TextScales = { 0.85f, 1f, 1.2f };
        private static readonly Dictionary<string, string[]> Translations = new Dictionary<string, string[]>(StringComparer.Ordinal);
        private static bool loaded;
        private static int languageIndex;
        private static int textSizeIndex = 1;
        private static int colorMode;

        /// <summary>
        /// Se avisa a las pantallas cuando cambia alguna opción.
        /// </summary>
        public static event Action Changed;

        /// <summary>
        /// El idioma elegido: español, inglés, alemán, danés, francés o italiano, en ese orden.
        /// </summary>
        public static int LanguageIndex { get { Load(); return languageIndex; } }

        /// <summary>
        /// El tamaño elegido: cero para pequeño, uno para normal y dos para grande.
        /// </summary>
        public static int TextSizeIndex { get { Load(); return textSizeIndex; } }

        /// <summary>
        /// La paleta elegida: normal, protanopia, deuteranopia o tritanopia, en ese orden.
        /// </summary>
        public static int ColorMode { get { Load(); return colorMode; } }

        /// <summary>
        /// Cuánto se agranda o reduce el texto respecto al tamaño del diseño.
        /// </summary>
        public static float TextScale { get { Load(); return TextScales[textSizeIndex]; } }

        /// <summary>
        /// Se limpia lo cargado al entrar en Play, aunque Unity no recargue el dominio.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            loaded = false;
            Translations.Clear();
            Changed = null;
        }

        /// <summary>
        /// Las opciones y las traducciones se cargan una sola vez, las siguientes llamadas no repiten el trabajo.
        /// </summary>
        public static void Load()
        {
            if (loaded) 
                return;

            loaded = true;
            languageIndex = Mathf.Clamp(PlayerPrefs.GetInt(LanguagePreference, 0), 0, LanguageCodes.Length - 1);
            textSizeIndex = Mathf.Clamp(PlayerPrefs.GetInt(TextSizePreference, 1), 0, TextScales.Length - 1);
            colorMode = Mathf.Clamp(PlayerPrefs.GetInt(ColorPreference, 0), 0, 3);
            LoadTranslations();
        }

        /// <summary>
        /// Se cambia el idioma y se guarda para la próxima vez.
        /// </summary>
        /// <param name="index">La posición del idioma en la lista de seis idiomas.</param>
        public static void SetLanguage(int index)
        {
            Load();
            SetPreference(ref languageIndex, Mathf.Clamp(index, 0, LanguageCodes.Length - 1), LanguagePreference);
        }

        /// <summary>
        /// Se cambia el tamaño de los textos y se guarda la elección.
        /// </summary>
        /// <param name="index">Cero para pequeño, uno para normal y dos para grande.</param>
        public static void SetTextSize(int index)
        {
            Load();
            SetPreference(ref textSizeIndex, Mathf.Clamp(index, 0, TextScales.Length - 1), TextSizePreference);
        }

        /// <summary>
        /// Se cambia la paleta de la interfaz y se guarda la elección.
        /// </summary>
        /// <param name="index">Cero mantiene los colores normales, del uno al tres están las alternativas.</param>
        public static void SetColorMode(int index)
        {
            Load();
            SetPreference(ref colorMode, Mathf.Clamp(index, 0, 3), ColorPreference);
        }

        /// <summary>
        /// El texto se busca en el idioma elegido, los números también usan el formato de ese idioma.
        /// </summary>
        /// <param name="key">La clave tal como aparece en el archivo de traducciones.</param>
        /// <param name="args">Los valores que van en los huecos del texto, si los hay.</param>
        /// <returns>El texto traducido, o la clave misma si no hay traducción.</returns>
        public static string Translate(string key, params object[] args)
        {
            Load();

            if (string.IsNullOrEmpty(key)) 
                return string.Empty;

            string template = Translations.TryGetValue(key, out string[] values) ? values[languageIndex] : key;

            if (args == null || args.Length == 0) 
                return template;

            try
            {
                return string.Format(CultureInfo.GetCultureInfo(LanguageCodes[languageIndex]), template, args);
            }
            catch (FormatException)
            {
                // Una plantilla mal escrita no debe romper la pantalla, se muestra el texto sin formatear.
                Debug.LogError("Formato de traducción no válido: " + key);
                return template;
            }
        }

        /// <summary>
        /// Solo se guarda y se avisa si la opción ha cambiado de verdad.
        /// </summary>
        private static void SetPreference(ref int field, int value, string preferenceKey)
        {
            if (field == value) 
                return;

            field = value;
            PlayerPrefs.SetInt(preferenceKey, value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>
        /// El archivo de traducciones lleva una columna por idioma, separada con tabuladores.
        /// </summary>
        private static void LoadTranslations()
        {
            TextAsset table = Resources.Load<TextAsset>("Traducciones");

            if (table == null)
            {
                Debug.LogError("No se ha encontrado el recurso Traducciones.");
                return;
            }

            string[] lines = table.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Se quita la posible marca BOM antes de comprobar el orden de las columnas.
            if (lines.Length == 0 || lines[0].TrimStart('\uFEFF') != "key\tes\ten\tde\tda\tfr\tit")
            {
                Debug.LogError("La cabecera del recurso Traducciones no es válida.");
                return;
            }

            for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                string[] columns = lines[lineIndex].Split('\t');

                // Si una fila viene mal, se avisa y se sigue leyendo el resto.
                if (columns.Length != LanguageCodes.Length + 1 || string.IsNullOrEmpty(columns[0]))
                {
                    Debug.LogError("Fila de traducción no válida: " + (lineIndex + 1));
                    continue;
                }

                string[] values = new string[LanguageCodes.Length];
                Array.Copy(columns, 1, values, 0, values.Length);
                Translations[columns[0]] = values;
            }
        }
    }
}
