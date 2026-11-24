using System;
using IdleClickerPeriodicTable.Core;
using UnityEngine;
using UnityEngine.UI;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>Las filas de la escena muestran los modelos descubiertos que todavía quedan por investigar.</summary>
    public sealed class ResearchListView : MonoBehaviour
    {
        /// <summary>Una investigación de producción y las referencias de su fila precolocada.</summary>
        [Serializable]
        sealed class ProductionRow
        {
            /// <summary>El identificador de catálogo permanece estable si cambia el orden de la lista.</summary>
            public string id = null;

            /// <summary>El botón abarca toda la fila.</summary>
            public Button button = null;

            /// <summary>Etiquetas visibles de la investigación pendiente.</summary>
            public LocalizedText name = null, cost = null, description = null, status = null;

            /// <summary>Datos del catálogo resueltos una sola vez por escena.</summary>
            public ProductionDefinition Entry { get; set; }
        }

        /// <summary>Las filas de los automatizadores ya guardadas al comienzo de la lista desplazable.</summary>
        public GameObject[] rows;

        /// <summary>Los botones de investigación, con sus acciones conectadas desde el editor.</summary>
        public Button[] buttons;

        /// <summary>Textos de coste, producción desbloqueada y estado de cada automatizador.</summary>
        public LocalizedText[] costs, descriptions, statuses;

        /// <summary>El confeti que celebra una investigación completada, sus piezas ya están en la escena.</summary>
        public ResearchConfetti confetti;

        /// <summary>El mensaje que aparece cuando no quedan descubrimientos por investigar.</summary>
        public GameObject emptyMessage;

        /// <summary>Las investigaciones de producción comparten el mismo contenido vertical.</summary>
        [SerializeField] ProductionRow[] productionRows;

        GameSession session;

        /// <summary>Vincula cada fila precolocada a la entrada identificada en el catálogo.</summary>
        void Awake()
        {
            foreach (ProductionRow row in productionRows)
                row.Entry = ProductionCatalog.Find(row.id);
        }

        /// <summary>La lista escucha los cambios de la partida mientras está activa.</summary>
        void OnEnable()
        {
            session = GameSession.Instance;
            if (session == null)
                return;

            for (int index = 0; index < rows.Length; index++)
            {
                costs[index].SetArguments(session.Automation.ResearchCost(index));
                descriptions[index].SetArguments(session.Automation.MachineRate(index));
            }

            session.Changed += Refresh;
            Refresh();
        }

        /// <summary>Se deja de escuchar a la sesión al salir de la pantalla.</summary>
        void OnDisable()
        {
            if (session != null)
                session.Changed -= Refresh;
        }

        /// <summary>Completa la investigación de automatización y celebra la compra.</summary>
        /// <param name="index">Índice de la familia de automatizadores.</param>
        public void Research(int index)
        {
            if (session != null && session.ResearchAutomation(index))
                confetti.Play();
        }

        /// <summary>Completa una investigación de producto usando el identificador serializado en su botón.</summary>
        /// <param name="id">Identificador estable de la investigación.</param>
        public void ResearchProduction(string id)
        {
            ProductionDefinition entry = ProductionCatalog.Find(id);
            if (session != null && entry != null && session.ResearchProduction(entry))
                confetti.Play();
        }

        /// <summary>Se actualizan las filas y sus requisitos, sin volver a crearlas.</summary>
        void Refresh()
        {
            bool hasAutomation = RefreshAutomation();
            bool hasProduction = RefreshProduction();
            SetVisible(emptyMessage, !hasAutomation && !hasProduction);
        }

        /// <summary>Actualiza la visibilidad y el estado de los descubrimientos de automatización pendientes.</summary>
        /// <returns>Si queda al menos un automatizador por investigar.</returns>
        bool RefreshAutomation()
        {
            bool any = false;
            for (int index = 0; index < rows.Length; index++)
            {
                bool visible = index >= session.State.researchLevel && index < session.State.discoveryLevel;
                SetVisible(rows[index], visible);
                if (!visible)
                    continue;

                any = true;
                bool previous = index > session.State.researchLevel;
                bool available = !previous && session.Automation.CanResearch(index);
                buttons[index].interactable = session.IsPlaying && available;

                string key;

                if (previous)
                    key = "research.previous";
                else
                    key = available ? "research.available" : "research.insufficient";

                SetLabel(statuses[index], key);
            }

            return any;
        }

        /// <summary>Actualiza las filas pendientes de los cuatro elementos en el mismo listado.</summary>
        /// <returns>Si queda al menos un descubrimiento de producción por investigar.</returns>
        bool RefreshProduction()
        {
            ProductionResearchService research = session.ProductionResearch;
            bool any = false;
            foreach (ProductionRow row in productionRows)
            {
                ProductionDefinition entry = row.Entry;
                bool visible = entry != null && research.IsDiscovered(entry.Id) && !research.IsResearched(entry.Id);
                if (visible)
                {
                    any = true;
                    if (!row.button.gameObject.activeSelf)
                        PrepareProductionRow(row, entry);

                    bool available = research.CanResearch(entry);
                    row.button.interactable = session.IsPlaying && available;
                    SetLabel(row.status, available ? "research.available" : "research.insufficient");
                }

                SetVisible(row.button.gameObject, visible);
            }

            return any;
        }

        /// <summary>Prepara las etiquetas de catálogo al mostrar por primera vez el descubrimiento.</summary>
        static void PrepareProductionRow(ProductionRow row, ProductionDefinition entry)
        {
            SetLabel(row.name, entry.NameKey);
            row.cost.SetArguments(entry.Science);
            row.description.key = entry.HasMachine ? "production.research_model" : "production.research_element";
            row.description.SetLocalizedArguments("periodic_table.name." + entry.Element.ToLowerInvariant());
        }

        /// <summary>Actualiza una etiqueta solo si cambia la traducción que debe mostrar.</summary>
        static void SetLabel(LocalizedText label, string key)
        {
            if (label.key == key)
                return;

            label.key = key;
            label.Refresh();
        }

        /// <summary>Evita recalcular el diseño del listado si la fila no cambia de estado.</summary>
        static void SetVisible(GameObject row, bool visible)
        {
            if (row.activeSelf != visible)
                row.SetActive(visible);
        }
    }
}
