using UnityEngine;
using UnityEngine.UI;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>Las filas de la escena muestran los modelos descubiertos que todavía quedan por investigar.</summary>
    public sealed class ResearchListView : MonoBehaviour
    {
        /// <summary>Las cuatro filas que ya están guardadas en la lista desplazable.</summary>
        public GameObject[] rows;

        /// <summary>Los botones de investigación, con sus acciones conectadas desde el editor.</summary>
        public Button[] buttons;

        /// <summary>Textos de nombre, coste, producción desbloqueada y estado de cada investigación.</summary>
        public LocalizedText[] names, costs, descriptions, statuses;

        /// <summary>El confeti que celebra una investigación completada, sus piezas ya están en la escena.</summary>
        public ResearchConfetti confetti;

        /// <summary>El mensaje que aparece cuando no quedan descubrimientos por investigar.</summary>
        public GameObject emptyMessage;

        GameSession session;

        /// <summary>La lista escucha los cambios de la partida mientras está activa.</summary>
        void OnEnable()
        {
            session = GameSession.Instance;
            session.Changed += Refresh;
            Refresh();
        }

        /// <summary>Se deja de escuchar a la sesión al salir de la pantalla.</summary>
        void OnDisable()
        {
            if (session != null)
                session.Changed -= Refresh;
        }

        /// <summary>Se pide la investigación a la sesión, el confeti solo sale si se ha podido completar.</summary>
        /// <param name="index">Índice de la investigación, de cero a tres.</param>
        public void Research(int index)
        {
            if (session != null && index >= 0 && index < 4 && session.ResearchAutomation(index))
                confetti.Play();
        }

        /// <summary>Se actualizan las filas y sus requisitos, sin volver a crearlas.</summary>
        void Refresh()
        {
            bool empty = session.State.researchLevel >= session.State.discoveryLevel;

            if (emptyMessage.activeSelf != empty)
                emptyMessage.SetActive(empty);

            for (int index = 0; index < 4; index++)
            {
                bool visible = index >= session.State.researchLevel && index < session.State.discoveryLevel;

                if (rows[index].activeSelf != visible)
                    rows[index].SetActive(visible);

                if (!visible)
                    continue;

                costs[index].SetArguments(session.Automation.ResearchCost(index));
                descriptions[index].SetArguments(session.Automation.MachineRate(index));
                bool previous = index > session.State.researchLevel;
                bool available = !previous && session.Automation.CanResearch(index);
                buttons[index].interactable = session.IsPlaying && available;

                string key;

                if (previous)
                    key = "research.previous";
                else
                    key = available ? "research.available" : "research.insufficient";

                if (statuses[index].key == key)
                    continue;

                statuses[index].key = key;
                statuses[index].Refresh();
            }
        }
    }
}
