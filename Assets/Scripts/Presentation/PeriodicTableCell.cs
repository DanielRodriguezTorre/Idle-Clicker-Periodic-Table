using UnityEngine;
using UnityEngine.EventSystems;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>Una casilla por descubrir muestra la indicación de investigación al pasar el puntero.</summary>
    public sealed class PeriodicTableCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>El aviso compartido que ya está colocado en la escena.</summary>
        [SerializeField] PeriodicTableTooltip tooltip;

        /// <summary>El aviso aparece cuando el puntero entra en la casilla.</summary>
        /// <param name="eventData">Datos del puntero recibidos del sistema de eventos.</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltip != null)
                tooltip.Show((RectTransform)transform);
        }

        /// <summary>El aviso se oculta cuando el puntero sale de la casilla.</summary>
        /// <param name="eventData">Datos del puntero recibidos del sistema de eventos.</param>
        public void OnPointerExit(PointerEventData eventData) => HideTooltip();

        /// <summary>El aviso se cierra si la casilla deja de estar visible.</summary>
        void OnDisable() => HideTooltip();

        /// <summary>Se retira el aviso que corresponde a esta casilla.</summary>
        void HideTooltip()
        {
            if (tooltip != null)
                tooltip.Hide((RectTransform)transform);
        }
    }
}
