using IdleClickerPeriodicTable.Core;
using UnityEngine;
using UnityEngine.UI;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>Presenta la fabricación y las mejoras en las filas precolocadas del mismo listado.</summary>
    public sealed class EngineeringListView : MonoBehaviour
    {
        /// <summary>Botón y textos de una mejora preparada en el listado.</summary>
        [System.Serializable]
        sealed class UpgradeRow
        {
            public Button button;
            public LocalizedText description, cost;
        }

        /// <summary>Una máquina de producción y sus tres mejoras precolocadas consecutivamente.</summary>
        [System.Serializable]
        sealed class ProductionRow
        {
            /// <summary>Identidad que vincula las referencias de escena con el catálogo y el guardado.</summary>
            public string id;
            public Button button;
            public LocalizedText name, description, cost, count;
            public UpgradeRow[] upgrades;

            [System.NonSerialized] public ProductionDefinition entry;
            [System.NonSerialized] public int shownLevel;
            [System.NonSerialized] public int shownCount;
        }

        const int MachineCount = 4;
        const int UpgradeCount = MachineCount * AutomationService.UpgradesPerMachine;
        const int ProductionUpgradeCount = ProductionResearchService.MaxLevel - 1;

        /// <summary>Filas y botones de los cuatro automatizadores económicos.</summary>
        public GameObject[] rows;
        public Button[] buttons;

        /// <summary>Coste, producción y cantidad de cada familia de automatizadores.</summary>
        public LocalizedText[] costs, descriptions, counts;

        /// <summary>Mejoras económicas, agrupadas por familia y nivel.</summary>
        [SerializeField] UpgradeRow[] upgrades;

        /// <summary>Modelos de los cuatro elementos que aparecen en el mismo contenido vertical.</summary>
        [SerializeField] ProductionRow[] productionRows;

        GameSession session;

        /// <summary>Prepara el listado y escucha la sesión mientras la pantalla está activa.</summary>
        void OnEnable()
        {
            session = GameSession.Instance;
            PrepareAutomationUpgrades();

            foreach (ProductionRow row in productionRows)
                row.entry = ProductionCatalog.Find(row.id);

            session.Changed += Refresh;
            Refresh();
        }

        /// <summary>Deja de escuchar a la sesión al salir de la pantalla.</summary>
        void OnDisable()
        {
            if (session != null)
                session.Changed -= Refresh;
        }

        /// <summary>Compra una unidad del automatizador vinculado al botón.</summary>
        public void Buy(int index)
        {
            if (session != null && index >= 0 && index < MachineCount)
                session.BuyAutomationMachine(index);
        }

        /// <summary>Compra la mejora económica vinculada al botón.</summary>
        public void BuyUpgrade(int slot)
        {
            if (session != null && slot >= 0 && slot < UpgradeCount)
                session.BuyAutomationUpgrade(slot / AutomationService.UpgradesPerMachine, AutomationUpgradeLevel(slot));
        }

        /// <summary>Compra una unidad del modelo de producción vinculado al botón.</summary>
        public void BuyProduction(int index)
        {
            if (session != null && index >= 0 && index < productionRows.Length && productionRows[index].entry is ProductionDefinition entry)
                session.BuildProductionMachine(entry);
        }

        /// <summary>Compra el nivel correspondiente a una de las tres mejoras de producción de la fila.</summary>
        public void BuyProductionUpgrade(int slot)
        {
            int index = slot / ProductionUpgradeCount;

            if (session != null && slot >= 0 && index < productionRows.Length && productionRows[index].entry is ProductionDefinition entry)
                session.UpgradeProductionMachine(entry, slot % ProductionUpgradeCount + 2);
        }

        /// <summary>Configura la producción y el precio de cada mejora económica.</summary>
        void PrepareAutomationUpgrades()
        {
            for (int slot = 0; slot < UpgradeCount; slot++)
            {
                int index = slot / AutomationService.UpgradesPerMachine;
                int level = AutomationUpgradeLevel(slot);
                long cost = session.Automation.UpgradeCost(index, level);
                upgrades[slot].description.SetLocalizedArguments($"machine.{index + 1}", level, session.Automation.MachineRate(index, level));
                upgrades[slot].cost.SetArguments(cost, cost);
            }
        }

        /// <summary>Actualiza las filas económicas y de producción en el mismo listado.</summary>
        void Refresh()
        {
            for (int index = 0; index < MachineCount; index++)
            {
                bool visible = index < session.State.researchLevel;
                SetVisible(rows[index], visible);

                if (!visible)
                    continue;

                long cost = session.Automation.MachineCost(index);
                costs[index].SetArguments(cost, cost);
                descriptions[index].SetArguments(session.Automation.MachineRate(index));
                counts[index].SetArguments(session.State.machines[index]);
                buttons[index].interactable = session.IsPlaying && session.Automation.CanBuyMachine(index);
            }

            RefreshAutomationUpgrades();
            RefreshProduction();
        }

        /// <summary>Muestra las mejoras económicas pendientes de las familias investigadas.</summary>
        void RefreshAutomationUpgrades()
        {
            for (int slot = 0; slot < UpgradeCount; slot++)
            {
                int index = slot / AutomationService.UpgradesPerMachine;
                int level = AutomationUpgradeLevel(slot);
                Button button = upgrades[slot].button;
                bool visible = index < session.State.researchLevel && level > session.State.machineLevels[index];

                button.interactable = visible && session.IsPlaying && session.Automation.CanBuyUpgrade(index, level);
                SetVisible(button.gameObject, visible);
            }
        }

        /// <summary>Muestra cada modelo de producción investigado y actualiza su nivel y cantidad.</summary>
        void RefreshProduction()
        {
            var research = session.ProductionResearch;

            foreach (ProductionRow row in productionRows)
            {
                var entry = row.entry;
                bool visible = entry != null && research.IsResearched(entry.Id);

                if (!visible)
                {
                    SetVisible(row.button.gameObject, false);

                    foreach (UpgradeRow upgrade in row.upgrades)
                        SetVisible(upgrade.button.gameObject, false);

                    continue;
                }

                bool firstDisplay = row.shownLevel == 0;

                if (firstDisplay)
                    PrepareProductionRow(row);

                int level = research.Level(entry.Id);

                if (row.shownLevel != level)
                {
                    row.shownLevel = level;
                    row.cost.SetArguments(entry.Money * level, entry.Ingenuity * level);
                    row.description.SetLocalizedArguments("periodic_table.name." + entry.Element.ToLowerInvariant(), level);
                }

                int count = research.Count(entry.Id);

                if (firstDisplay || row.shownCount != count)
                {
                    row.shownCount = count;
                    row.count.SetArguments(count);
                }

                row.button.interactable = session.IsPlaying && research.CanBuild(entry);
                SetVisible(row.button.gameObject, true);
                RefreshProductionUpgrades(row, level);
            }
        }

        /// <summary>Asigna los nombres y costes fijos cuando aparece por primera vez el modelo.</summary>
        static void PrepareProductionRow(ProductionRow row)
        {
            row.name.key = row.entry.NameKey;
            row.name.Refresh();

            for (int offset = 0; offset < row.upgrades.Length; offset++)
            {
                int level = offset + 2;
                UpgradeRow upgrade = row.upgrades[offset];
                upgrade.description.key = "machine.level_name";
                upgrade.description.SetLocalizedArguments(row.entry.NameKey, level);
                upgrade.cost.SetArguments(row.entry.Money * level, row.entry.Ingenuity * level);
            }
        }

        /// <summary>Muestra las mejoras pendientes y habilita la siguiente si se puede pagar.</summary>
        void RefreshProductionUpgrades(ProductionRow machine, int currentLevel)
        {
            var research = session.ProductionResearch;

            for (int offset = 0; offset < machine.upgrades.Length; offset++)
            {
                UpgradeRow row = machine.upgrades[offset];
                int level = offset + 2;
                bool visible = level > currentLevel;

                row.button.interactable = visible && session.IsPlaying && research.CanUpgrade(machine.entry, level);
                SetVisible(row.button.gameObject, visible);
            }
        }

        static void SetVisible(GameObject row, bool visible)
        {
            if (row.activeSelf != visible)
                row.SetActive(visible);
        }

        static int AutomationUpgradeLevel(int slot) => slot % AutomationService.UpgradesPerMachine + 2;
    }
}
