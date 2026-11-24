using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>Los datos de cada casilla aparecen al pagar la investigación de su elemento.</summary>
    public sealed class PeriodicTableCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        /// <summary>El aviso compartido que ya está colocado en la escena.</summary>
        [SerializeField] PeriodicTableTooltip tooltip;

        /// <summary>El símbolo enlaza la casilla con su investigación, las que no tienen investigación quedan vacías.</summary>
        [SerializeField] string elementId;

        /// <summary>Los datos de la casilla según la tabla de la Royal Society of Chemistry.</summary>
        [SerializeField] int atomicNumber;
        [SerializeField] string oxidationStates;

        /// <summary>Las etiquetas ya preparadas en el prefab de la casilla.</summary>
        [SerializeField] Text numberLabel, symbolLabel, nameLabel, oxidationLabel;

        GameSession session;
        string nameKey, factKey;
        bool researched, hovered;

        /// <summary>Solo las casillas con investigación escuchan los cambios de la partida y del idioma.</summary>
        void OnEnable()
        {
            if (string.IsNullOrEmpty(elementId))
                return;

            string suffix = elementId.ToLowerInvariant();
            nameKey = "periodic_table.name." + suffix;
            factKey = "periodic_table.fact." + suffix;
            session = GameSession.Instance;

            if (session != null)
                session.Changed += RefreshResearch;

            GamePreferences.Changed += RefreshName;
            RefreshResearch();
        }

        /// <summary>Se dejan de escuchar los cambios de partida e idioma y se cierra la ficha al salir de la pantalla.</summary>
        void OnDisable()
        {
            if (session != null)
                session.Changed -= RefreshResearch;

            session = null;
            GamePreferences.Changed -= RefreshName;
            hovered = false;

            if (tooltip != null)
                tooltip.Dismiss((RectTransform)transform);
        }

        /// <summary>Al pasar el puntero se muestra la ficha o el aviso de investigación pendiente.</summary>
        /// <param name="eventData">Datos del puntero recibidos del sistema de eventos.</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            ShowTooltip();
        }

        /// <summary>El aviso se oculta cuando el puntero sale de la casilla.</summary>
        /// <param name="eventData">Datos del puntero recibidos del sistema de eventos.</param>
        public void OnPointerExit(PointerEventData eventData) => HideTooltip();

        /// <summary>Pulsar un elemento investigado abre o cierra la consulta de su receta.</summary>
        /// <param name="eventData">Datos del clic enviados por el sistema de eventos.</param>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !researched || tooltip == null)
                return;

            if (!tooltip.TogglePlan((RectTransform)transform, elementId, nameKey) && hovered)
                ShowTooltip();
        }

        /// <summary>Las etiquetas y la ficha abierta se actualizan cuando se completa la investigación.</summary>
        void RefreshResearch()
        {
            bool revealed = session?.ProductionResearch?.IsResearched(elementId) == true;

            if (revealed == researched)
                return;

            researched = revealed;
            numberLabel.text = revealed ? atomicNumber.ToString() : string.Empty;
            symbolLabel.text = revealed ? elementId : string.Empty;
            oxidationLabel.text = revealed ? oxidationStates : string.Empty;
            RefreshName();

            if (hovered)
                ShowTooltip();
        }

        /// <summary>El nombre visible sigue el idioma seleccionado.</summary>
        void RefreshName()
        {
            nameLabel.text = researched ? GamePreferences.Translate(nameKey) : string.Empty;
        }

        /// <summary>Se muestra el contenido que corresponde en el panel compartido de la escena.</summary>
        void ShowTooltip()
        {
            if (tooltip != null)
                tooltip.Show((RectTransform)transform, researched ? factKey : null, nameKey, elementId, atomicNumber, oxidationStates);
        }

        /// <summary>Se retira el aviso que corresponde a esta casilla.</summary>
        void HideTooltip()
        {
            hovered = false;

            if (tooltip != null)
                tooltip.Hide((RectTransform)transform);
        }
    }
}
