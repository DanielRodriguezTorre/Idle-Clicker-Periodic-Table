using IdleClickerPeriodicTable.Core;
using UnityEngine;
using UnityEngine.UI;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>La fabricación y las mejoras se muestran con las filas que ya trae la escena.</summary>
    public sealed class EngineeringListView : MonoBehaviour
    {
        /// <summary>Los componentes de cada fila de mejora, ya preparada en la lista.</summary>
        [System.Serializable]
        sealed class UpgradeRow
        {
            /// <summary>El botón de compra, su objeto es también la fila completa.</summary>
            public Button button = null;

            /// <summary>Los textos traducibles con la producción y el coste de la mejora.</summary>
            public LocalizedText description = null, cost = null;
        }

        const int MachineCount = 4;
        const int UpgradeCount = MachineCount * AutomationService.UpgradesPerMachine;

        /// <summary>Las cuatro filas de la lista desplazable de Ingeniería.</summary>
        public GameObject[] rows;

        /// <summary>Los botones de compra, con sus acciones conectadas desde el editor.</summary>
        public Button[] buttons;

        /// <summary>Los textos con el coste, la producción por máquina y la cantidad de cada familia.</summary>
        public LocalizedText[] costs, descriptions, counts;

        /// <summary>Las cuatro mejoras de cada familia van seguidas, del nivel 2 al 5.</summary>
        [SerializeField] UpgradeRow[] upgrades;

        GameSession session;

        /// <summary>Las filas se actualizan con los cambios de la partida mientras la lista está activa.</summary>
        void OnEnable()
        {
            session = GameSession.Instance;

            if (session == null)
            {
                Debug.LogError("La lista de ingeniería necesita una sesión de partida.", this);
                enabled = false;
                return;
            }

            PrepareUpgradeTexts();
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
            if (session != null && index >= 0 && index < MachineCount)
                session.BuyAutomationMachine(index);
        }

        /// <summary>Se pide la mejora que corresponde al botón pulsado.</summary>
        /// <param name="slot">Posición de la mejora en el listado serializado.</param>
        public void BuyUpgrade(int slot)
        {
            if (session != null && slot >= 0 && slot < UpgradeCount)
                session.BuyAutomationUpgrade(slot / AutomationService.UpgradesPerMachine, UpgradeLevel(slot));
        }

        /// <summary>Los textos de cada mejora muestran su familia, su nivel y los valores ajustados en el balance.</summary>
        void PrepareUpgradeTexts()
        {
            for (int slot = 0; slot < UpgradeCount; slot++)
            {
                int index = slot / AutomationService.UpgradesPerMachine;
                int level = UpgradeLevel(slot);
                long cost = session.Automation.UpgradeCost(index, level);
                upgrades[slot].description.SetLocalizedArguments($"machine.{index + 1}", level, session.Automation.MachineRate(index, level));
                upgrades[slot].cost.SetArguments(cost, cost);
            }
        }

        /// <summary>Se actualizan los modelos que ya se pueden fabricar y las mejoras que quedan por comprar.</summary>
        void Refresh()
        {
            for (int index = 0; index < MachineCount; index++)
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

            RefreshUpgrades();
        }

        /// <summary>Aparecen las mejoras pendientes de los modelos investigados, solo se puede comprar la siguiente si alcanzan los recursos.</summary>
        void RefreshUpgrades()
        {
            for (int slot = 0; slot < UpgradeCount; slot++)
            {
                int index = slot / AutomationService.UpgradesPerMachine;
                int level = UpgradeLevel(slot);
                Button button = upgrades[slot].button;
                bool visible = index < session.State.researchLevel && level > session.State.machineLevels[index];

                button.interactable = visible && session.IsPlaying && session.Automation.CanBuyUpgrade(index, level);

                if (button.gameObject.activeSelf != visible)
                    button.gameObject.SetActive(visible);
            }
        }

        /// <summary>La posición dentro del grupo de cuatro indica el nivel de producción de la mejora.</summary>
        static int UpgradeLevel(int slot) => slot % AutomationService.UpgradesPerMachine + 2;
    }
}
