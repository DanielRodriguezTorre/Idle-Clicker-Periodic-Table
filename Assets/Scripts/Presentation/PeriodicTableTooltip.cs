using System.Text;
using IdleClickerPeriodicTable.Core;
using UnityEngine;
using UnityEngine.UI;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>Junto a la casilla aparece el aviso de bloqueo, la ficha del elemento o su plano.</summary>
    public sealed class PeriodicTableTooltip : MonoBehaviour
    {
        const float Gap = 8;

        /// <summary>El fondo y el mensaje de la versión compacta para una casilla bloqueada.</summary>
        [SerializeField] Image lockedBackground;
        [SerializeField] GameObject lockedMessage;

        /// <summary>El panel y el texto traducible de la ficha científica.</summary>
        [SerializeField] RectTransform detailPanel;
        [SerializeField] LocalizedText details;

        /// <summary>El plano dispone de espacio para mostrar sus pasos en líneas separadas.</summary>
        [SerializeField] RectTransform planPanel;
        [SerializeField] LocalizedText plan;

        RectTransform source;
        string pinnedElement, pinnedNameKey;

        /// <summary>La consulta se actualiza si cambia el idioma mientras está abierta.</summary>
        void OnEnable() => GamePreferences.Changed += RefreshPlan;

        /// <summary>Se deja de escuchar los cambios de idioma cuando el panel ya no está visible.</summary>
        void OnDisable() => GamePreferences.Changed -= RefreshPlan;

        /// <summary>El plano fijado se mantiene sobre su casilla, pasar a otra casilla cambia el contenido del aviso.</summary>
        /// <param name="cell">Casilla que solicita el aviso.</param>
        /// <param name="factKey">Clave de la ficha o valor nulo para el aviso compacto.</param>
        /// <param name="nameKey">Clave del nombre traducido.</param>
        /// <param name="symbol">Símbolo químico del elemento.</param>
        /// <param name="atomicNumber">Número atómico del elemento.</param>
        /// <param name="oxidationStates">Estados de oxidación habituales.</param>
        public void Show(RectTransform cell, string factKey, string nameKey, string symbol, int atomicNumber, string oxidationStates)
        {
            if (pinnedElement != null)
            {
                if (source == cell)
                    return;

                pinnedElement = null;
                pinnedNameKey = null;
            }

            bool showDetails = !string.IsNullOrEmpty(factKey);

            if (showDetails)
            {
                details.key = factKey;
                details.SetLocalizedArguments(nameKey, symbol, atomicNumber, oxidationStates, "\n");
            }

            Display(cell, showDetails ? detailPanel : null);
        }

        /// <summary>Los pasos del elemento quedan fijados en el panel, un segundo clic en su casilla los cierra.</summary>
        /// <param name="cell">Casilla que recibe el clic.</param>
        /// <param name="elementId">Identidad del elemento en el catálogo.</param>
        /// <param name="nameKey">Clave del nombre traducido.</param>
        /// <returns>Si la consulta queda abierta tras el clic.</returns>
        public bool TogglePlan(RectTransform cell, string elementId, string nameKey)
        {
            if (pinnedElement == elementId)
            {
                Dismiss(cell);
                return false;
            }

            pinnedElement = elementId;
            pinnedNameKey = nameKey;
            RefreshPlan();
            Display(cell, planPanel);
            return true;
        }

        /// <summary>Los pasos aparecen en el mismo orden que tienen en el catálogo del plano.</summary>
        void RefreshPlan()
        {
            if (pinnedElement == null)
                return;

            var steps = new StringBuilder();
            int stepNumber = 0;

            foreach (ProductionDefinition entry in ProductionCatalog.Entries)
            {
                // Los pasos principales tienen un número tras el símbolo. Los tratamientos y el depósito tienen un sufijo separado por punto.
                if (entry.Element != pinnedElement || !entry.HasMachine || entry.Id.Length != pinnedElement.Length + 1)
                    continue;

                if (steps.Length > 0)
                    steps.Append('\n');

                steps.Append(++stepNumber).Append(". ").Append(GamePreferences.Translate(entry.NameKey));
            }

            plan.SetLocalizedArguments(pinnedNameKey, "\n", steps.ToString());
        }

        /// <summary>Solo queda visible el panel que corresponde a la consulta actual.</summary>
        void Display(RectTransform cell, RectTransform panel)
        {
            source = cell;
            bool locked = panel == null;
            lockedBackground.enabled = locked;
            lockedMessage.SetActive(locked);
            detailPanel.gameObject.SetActive(panel == detailPanel);
            planPanel.gameObject.SetActive(panel == planPanel);
            Position(cell, panel);
            gameObject.SetActive(true);
        }

        /// <summary>El panel aparece por encima o por debajo de la casilla según el espacio disponible.</summary>
        void Position(RectTransform cell, RectTransform panel)
        {
            var root = (RectTransform)transform;
            var canvas = (RectTransform)root.parent;
            Rect bounds = canvas.rect;
            Vector2 halfSize = (panel != null ? panel : root).rect.size / 2;
            Vector2 position = canvas.InverseTransformPoint(cell.TransformPoint(0, cell.rect.yMax, 0));

            position.x = Mathf.Clamp(position.x, bounds.xMin + halfSize.x + Gap, bounds.xMax - halfSize.x - Gap);
            position.y += halfSize.y + Gap;

            if (position.y + halfSize.y + Gap > bounds.yMax)
                position.y = canvas.InverseTransformPoint(cell.TransformPoint(0, cell.rect.yMin, 0)).y - halfSize.y - Gap;

            position.y = Mathf.Clamp(position.y, bounds.yMin + halfSize.y + Gap, bounds.yMax - halfSize.y - Gap);
            root.anchoredPosition = position - bounds.center;
        }

        /// <summary>La ficha se oculta al retirar el puntero, salvo si quedó fijada con un clic.</summary>
        /// <param name="cell">Casilla que dejó de estar bajo el puntero.</param>
        public void Hide(RectTransform cell)
        {
            if (pinnedElement == null)
                Dismiss(cell);
        }

        /// <summary>Se cierra la ficha o el plano de esa casilla, también al cambiar de escena.</summary>
        /// <param name="cell">Casilla a la que corresponde la ficha.</param>
        public void Dismiss(RectTransform cell)
        {
            // Al pasar de una casilla a otra, la salida de la anterior no debe cerrar el aviso nuevo.
            if (source != cell)
                return;

            source = null;
            pinnedElement = null;
            pinnedNameKey = null;
            gameObject.SetActive(false);
        }
    }
}
