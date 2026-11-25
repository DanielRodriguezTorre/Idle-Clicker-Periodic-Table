using System;
using IdleClickerPeriodicTable.Core;
using UnityEngine;
using UnityEngine.UI;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>La fabricación y las mejoras comparten las filas que ya están preparadas en la escena.</summary>
    public sealed class EngineeringListView : MonoBehaviour
    {
        /// <summary>El botón y los textos de cada mejora, ya preparada en la lista.</summary>
        [Serializable]
        sealed class UpgradeRow
        {
            public Button button;
            public LocalizedText description, cost;
        }

        /// <summary>Cada fila representa un modelo con sus tres mejoras o una mejora de personal de un solo uso.</summary>
        [Serializable]
        sealed class ProductionRow
        {
            /// <summary>El identificador enlaza la fila con el catálogo y el progreso guardado.</summary>
            public string id;
            public Button button;
            public LocalizedText name, description, cost, count;
            public UpgradeRow[] upgrades;

            [NonSerialized] public ProductionDefinition entry;
            [NonSerialized] public int shownLevel;
            [NonSerialized] public int shownCount;
        }

        const int MachineCount = 4;
        const int UpgradeCount = MachineCount * AutomationService.UpgradesPerMachine;
        const int ProductionUpgradeCount = ProductionResearchService.MaxLevel - 1;

        /// <summary>Las filas y los botones de los cuatro automatizadores económicos.</summary>
        public GameObject[] rows;
        public Button[] buttons;

        /// <summary>Los textos con el coste, la producción y la cantidad de cada familia de automatizadores.</summary>
        public LocalizedText[] costs, descriptions, counts;

        /// <summary>Las mejoras de automatizadores van agrupadas por familia y nivel.</summary>
        [SerializeField] UpgradeRow[] upgrades;

        /// <summary>Los modelos de los cuatro elementos y las mejoras de personal comparten la misma lista.</summary>
        [SerializeField] ProductionRow[] productionRows;

        GameSession session;

        /// <summary>Se prepara la lista y se escuchan los cambios de la sesión mientras la pantalla está activa.</summary>
        void OnEnable()
        {
            session = GameSession.Instance;
            PrepareAutomationUpgrades();

            foreach (ProductionRow row in productionRows)
                row.entry = ProductionCatalog.Find(row.id);

            session.Changed += Refresh;
            Refresh();
        }

        /// <summary>Se deja de escuchar a la sesión al salir de la pantalla.</summary>
        void OnDisable()
        {
            if (session != null)
                session.Changed -= Refresh;
        }

        /// <summary>Se compra una unidad del automatizador que corresponde al botón.</summary>
        public void Buy(int index)
        {
            if (session != null && index >= 0 && index < MachineCount)
                session.BuyAutomationMachine(index);
        }

        /// <summary>Se compra la mejora de automatización que corresponde al botón.</summary>
        public void BuyUpgrade(int slot)
        {
            if (session != null && slot >= 0 && slot < UpgradeCount)
                session.BuyAutomationUpgrade(slot / AutomationService.UpgradesPerMachine, AutomationUpgradeLevel(slot));
        }

        /// <summary>Se compra una unidad del modelo de producción que corresponde al botón.</summary>
        public void BuyProduction(int index)
        {
            if (session != null && index >= 0 && index < productionRows.Length && productionRows[index].entry is ProductionDefinition entry)
                session.BuildProductionMachine(entry);
        }

        /// <summary>Se compra el nivel que corresponde a una de las tres mejoras de producción de la fila.</summary>
        public void BuyProductionUpgrade(int slot)
        {
            int index = slot / ProductionUpgradeCount;

            if (session != null && slot >= 0 && index < productionRows.Length && productionRows[index].entry is ProductionDefinition entry)
                session.UpgradeProductionMachine(entry, slot % ProductionUpgradeCount + 2);
        }

        /// <summary>Los textos de cada mejora de automatización muestran su producción y su precio.</summary>
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

        /// <summary>Se actualizan las filas de automatizadores y producción dentro de la misma lista.</summary>
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

        /// <summary>Aparecen las mejoras pendientes de las familias de automatizadores ya investigadas.</summary>
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

        /// <summary>Los modelos investigados muestran su cantidad y nivel, las mejoras de personal desaparecen al aplicarlas.</summary>
        void RefreshProduction()
        {
            var research = session.ProductionResearch;

            foreach (ProductionRow row in productionRows)
            {
                var entry = row.entry;
                bool visible = entry != null && research.IsResearched(entry.Id) && !research.IsStaffUpgradeApplied(entry);

                if (!visible)
                {
                    SetVisible(row.button.gameObject, false);

                    foreach (UpgradeRow upgrade in row.upgrades)
                        SetVisible(upgrade.button.gameObject, false);

                    continue;
                }

                if (entry.IsStaffUpgrade)
                {
                    RefreshStaffUpgrade(row);
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

        /// <summary>La mejora de personal aparece hasta que se fabrica, con su coste fijo y sin contador de unidades.</summary>
        void RefreshStaffUpgrade(ProductionRow row)
        {
            if (!row.button.gameObject.activeSelf)
            {
                StaffUpgradeText.Apply(row.entry, row.name, row.description);
                row.cost.SetArguments(row.entry.Money, row.entry.Ingenuity);
                SetVisible(row.count.gameObject, false);
                SetVisible(row.button.gameObject, true);
            }

            row.button.interactable = session.IsPlaying && session.ProductionResearch.CanBuild(row.entry);
        }

        /// <summary>Los nombres y los costes fijos se preparan cuando el modelo aparece por primera vez.</summary>
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

        /// <summary>Aparecen las mejoras pendientes, solo se puede comprar la siguiente si alcanzan los recursos.</summary>
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
