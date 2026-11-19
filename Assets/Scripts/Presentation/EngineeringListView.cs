using UnityEngine;
using UnityEngine.UI;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>Las máquinas ya investigadas se fabrican desde las filas que trae la escena.</summary>
    public sealed class EngineeringListView : MonoBehaviour
    {
        /// <summary>Las cuatro filas de la lista desplazable de Ingeniería.</summary>
        public GameObject[] rows;

        /// <summary>Los botones de compra, con sus acciones conectadas desde el editor.</summary>
        public Button[] buttons;

        /// <summary>Los textos con el coste, la producción por máquina y la cantidad de cada familia.</summary>
        public LocalizedText[] costs, descriptions, counts;

        GameSession session;

        /// <summary>Las filas se actualizan con los cambios de la partida mientras la lista está activa.</summary>
        void OnEnable()
        {
            session = GameSession.Instance;

            if (session == null || !Complete(rows) || !Complete(buttons) || !Complete(costs) || !Complete(descriptions) || !Complete(counts))
            {
                Debug.LogError("Faltan referencias de la lista de ingeniería.", this);
                enabled = false;
                return;
            }

            session.Changed += Refresh;
            Refresh();
        }

        /// <summary>Se deja de escuchar a la sesión al desactivar la lista o cambiar de pantalla.</summary>
        void OnDisable()
        {
            if (session != null)
                session.Changed -= Refresh;
        }

        /// <summary>Se pide una máquina, la sesión comprueba y cobra tanto el dinero como el ingenio.</summary>
        /// <param name="index">Familia de la máquina, de cero a tres.</param>
        public void Buy(int index)
        {
            if (session != null && index >= 0 && index < 4)
                session.BuyAutomationMachine(index);
        }

        /// <summary>Solo se muestran los modelos investigados, los botones indican si se pueden comprar.</summary>
        void Refresh()
        {
            for (int index = 0; index < 4; index++)
            {
                bool visible = index < session.State.researchLevel;

                if (rows[index].activeSelf != visible)
                    rows[index].SetActive(visible);

                if (!visible)
                    continue;

                long cost = session.Automation.MachineCost(index);
                costs[index].SetArguments(cost, cost);
                descriptions[index].SetArguments(session.Automation.MachineRate(index));
                counts[index].SetArguments(session.State.machines[index]);
                buttons[index].interactable = session.IsPlaying && session.Automation.CanBuyMachine(index);
            }
        }

        /// <summary>Se comprueba que estén asignadas las cuatro referencias, sin buscar objetos en la escena.</summary>
        static bool Complete<T>(T[] references) where T : Object
        {
            if (references == null || references.Length != 4)
                return false;

            for (int index = 0; index < references.Length; index++)
                if (references[index] == null)
                    return false;

            return true;
        }
    }
}
