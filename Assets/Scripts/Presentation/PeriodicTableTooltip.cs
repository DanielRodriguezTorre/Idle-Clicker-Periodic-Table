using UnityEngine;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>El aviso compartido aparece junto a la casilla que está bajo el puntero.</summary>
    public sealed class PeriodicTableTooltip : MonoBehaviour
    {
        const float Gap = 8;
        RectTransform source;

        /// <summary>El panel aparece sobre la casilla o debajo si no hay sitio arriba, dejando margen a los lados.</summary>
        /// <param name="cell">Casilla que solicita el aviso.</param>
        public void Show(RectTransform cell)
        {
            source = cell;
            var panel = (RectTransform)transform;
            var canvas = (RectTransform)panel.parent;
            Rect bounds = canvas.rect;
            Vector2 halfSize = panel.rect.size / 2;
            Vector2 position = canvas.InverseTransformPoint(cell.TransformPoint(0, cell.rect.yMax, 0));

            position.x = Mathf.Clamp(position.x, bounds.xMin + halfSize.x + Gap, bounds.xMax - halfSize.x - Gap);
            position.y += halfSize.y + Gap;

            if (position.y + halfSize.y + Gap > bounds.yMax)
                position.y = canvas.InverseTransformPoint(cell.TransformPoint(0, cell.rect.yMin, 0)).y - halfSize.y - Gap;

            panel.anchoredPosition = position - bounds.center;
            gameObject.SetActive(true);
        }

        /// <summary>Solo se oculta el panel si todavía corresponde a la casilla de la que salió el puntero.</summary>
        /// <param name="cell">Casilla que dejó de estar bajo el puntero.</param>
        public void Hide(RectTransform cell)
        {
            // Al pasar de una casilla a otra, la salida de la anterior no debe cerrar el aviso nuevo.
            if (source != cell)
                return;

            source = null;
            gameObject.SetActive(false);
        }
    }
}
