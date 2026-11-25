using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace IdleClickerPeriodicTable.Core
{
    /// <summary>El progreso de cada descubrimiento, tener una entrada aquí significa que ya fue descubierto.</summary>
    [DataContract]
    public sealed class ProductionProgress
    {
        /// <summary>Indica si ya se pagó la investigación de este descubrimiento.</summary>
        [DataMember] public bool researched;

        /// <summary>Cuántas unidades se han fabricado de este modelo.</summary>
        [DataMember] public int count;

        /// <summary>El nivel que usan tanto las unidades existentes como las que se fabriquen después.</summary>
        [DataMember] public int level = 1;
    }

    /// <summary>Aquí se llevan los descubrimientos de los elementos, sus modelos y las mejoras de personal.</summary>
    public sealed class ProductionResearchService
    {
        /// <summary>Los modelos de producción, tratamiento y almacenamiento pueden llegar hasta el nivel 4.</summary>
        public const int MaxLevel = 4;
        readonly GameState state;
        readonly Dictionary<string, ProductionProgress> progress;

        /// <summary>Se retoma el progreso guardado o se crea si la partida es anterior a este catálogo.</summary>
        public ProductionResearchService(GameState state)
        {
            this.state = state;
            progress = state.productionProgress ??= new(StringComparer.Ordinal);
        }

        /// <summary>Se descubren las entradas cuando alcanzan la ciencia y los requisitos, gastar recursos no borra lo descubierto.</summary>
        public bool CheckDiscoveries()
        {
            bool changed = false;

            foreach (ProductionDefinition entry in ProductionCatalog.Entries)
            {
                if (progress.ContainsKey(entry.Id) || state.science < entry.Science || state.researchLevel < entry.AutomationRequired || (entry.Prerequisite.Length > 0 && !IsResearched(entry.Prerequisite)) || (entry.IsStaffUpgrade && !IsNextStaffUpgrade(entry)))
                    continue;

                progress.Add(entry.Id, new());
                changed = true;
            }

            return changed;
        }

        /// <summary>La entrada ya se puede mostrar al descubrirla, aunque todavía no se haya pagado la investigación.</summary>
        public bool IsDiscovered(string id) => progress.ContainsKey(id);

        /// <summary>Indica si ya se pagó la investigación.</summary>
        public bool IsResearched(string id) => progress.TryGetValue(id, out var item) && item.researched;

        /// <summary>Cuántas unidades tiene el modelo, incluidas las compradas después de mejorarlo.</summary>
        public int Count(string id) => progress.TryGetValue(id, out var item) ? item.count : 0;

        /// <summary>El nivel del modelo empieza en 1, incluso si todavía no tiene una entrada de progreso.</summary>
        public int Level(string id) => progress.TryGetValue(id, out var item) ? Math.Max(1, item.level) : 1;

        /// <summary>Solo se puede pagar una investigación ya descubierta y pendiente.</summary>
        public bool CanResearch(ProductionDefinition entry) => IsDiscovered(entry.Id) && !IsResearched(entry.Id) && state.science >= entry.Science;

        /// <summary>Se cobra la ciencia y la entrada queda investigada.</summary>
        public bool Research(ProductionDefinition entry)
        {
            if (!CanResearch(entry))
                return false;

            state.science -= entry.Science;
            progress[entry.Id].researched = true;
            return true;
        }

        /// <summary>Se puede fabricar si la entrada está investigada y alcanzan ambos recursos, las mejoras de personal van en orden.</summary>
        public bool CanBuild(ProductionDefinition entry)
        {
            if (!IsResearched(entry.Id))
                return false;

            if (entry.IsStaffUpgrade)
                return IsNextStaffUpgrade(entry) && CanPay(entry, 1);

            return entry.HasMachine && Count(entry.Id) < int.MaxValue && CanPay(entry, Level(entry.Id));
        }

        /// <summary>Se cobra la fabricación y se añade una máquina o se aplica el siguiente nivel de la profesión.</summary>
        public bool Build(ProductionDefinition entry)
        {
            if (!CanBuild(entry))
                return false;

            if (entry.IsStaffUpgrade)
            {
                Pay(entry, 1);
                state.ApplyStaffLevel(entry.Profession, entry.StaffLevel);
            }
            else
            {
                Pay(entry, Level(entry.Id));
                progress[entry.Id].count++;
            }

            return true;
        }

        /// <summary>La mejora de personal deja de aparecer en Ingeniería cuando ya está aplicada.</summary>
        public bool IsStaffUpgradeApplied(ProductionDefinition entry) => entry.IsStaffUpgrade && StaffLevel(entry.Profession) >= entry.StaffLevel;

        /// <summary>El nivel de la mejora tiene que ser el siguiente al que ya usa la profesión.</summary>
        bool IsNextStaffUpgrade(ProductionDefinition entry) => StaffLevel(entry.Profession) + 1 == entry.StaffLevel;

        int StaffLevel(StaffProfession profession) => profession == StaffProfession.Research ? state.ResearchStaffLevel : state.EngineeringStaffLevel;

        /// <summary>Las mejoras se compran en orden para cada modelo investigado.</summary>
        public bool CanUpgrade(ProductionDefinition entry, int level) => entry.HasMachine && IsResearched(entry.Id) && level <= MaxLevel && level == Level(entry.Id) + 1 && CanPay(entry, level);

        /// <summary>Se cobra la mejora una vez y el nuevo nivel se aplica a todo el modelo.</summary>
        public bool Upgrade(ProductionDefinition entry, int level)
        {
            if (!CanUpgrade(entry, level))
                return false;

            Pay(entry, level);
            progress[entry.Id].level = level;
            return true;
        }

        bool CanPay(ProductionDefinition entry, int level) => state.money >= entry.Money * level && state.ingenuity >= entry.Ingenuity * level;

        void Pay(ProductionDefinition entry, int level)
        {
            state.money -= entry.Money * level;
            state.ingenuity -= entry.Ingenuity * level;
        }
    }
}
