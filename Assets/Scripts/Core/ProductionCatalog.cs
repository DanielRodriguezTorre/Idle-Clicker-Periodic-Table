using System;
using System.Collections.Generic;

namespace IdleClickerPeriodicTable.Core
{
    /// <summary>Los datos de cada investigación y, si lo tiene, del modelo que permite fabricar.</summary>
    public sealed class ProductionDefinition
    {
        /// <summary>El identificador no cambia, sirve para guardar el progreso y enlazar los requisitos.</summary>
        public string Id { get; }

        /// <summary>El elemento al que pertenece esta investigación o máquina.</summary>
        public string Element { get; }

        /// <summary>Clave del nombre que se muestra en los listados.</summary>
        public string NameKey { get; }

        /// <summary>La investigación de producción que debe completarse antes, o un texto vacío si no necesita ninguna.</summary>
        public string Prerequisite { get; }

        /// <summary>El nivel de automatización que hace falta haber investigado.</summary>
        public int AutomationRequired { get; }

        /// <summary>La ciencia necesaria para descubrir la entrada, esa misma cantidad se gasta al investigarla.</summary>
        public long Science { get; }

        /// <summary>Lo que cuesta una unidad en ingenio antes de aplicar su nivel, es cero si no hay máquina.</summary>
        public long Ingenuity { get; }

        /// <summary>Lo que cuesta una unidad en dinero antes de aplicar su nivel, es cero si no hay máquina.</summary>
        public long Money { get; }

        /// <summary>Indica si esta investigación permite fabricar una máquina.</summary>
        public bool HasMachine => Money != 0;

        /// <summary>Se guardan los requisitos de la investigación y lo que costará fabricar su modelo.</summary>
        internal ProductionDefinition(string id, string element, string nameKey, string prerequisite, long science, long ingenuity = 0, long money = 0, int automationRequired = 0)
        {
            Id = id;
            Element = element;
            NameKey = nameKey;
            Prerequisite = prerequisite;
            Science = science;
            Ingenuity = ingenuity;
            Money = money;
            AutomationRequired = automationRequired;
        }
    }

    /// <summary>Las investigaciones y los modelos de los cuatro elementos, con sus costes y requisitos.</summary>
    public static class ProductionCatalog
    {
        /// <summary>Los elementos y sus modelos aparecen en el orden del catálogo.</summary>
        public static IReadOnlyList<ProductionDefinition> Entries { get; } = new ProductionDefinition[]
        {
            new("H", "H", "periodic_table.name.h", "", 1_000, automationRequired: 1),
            new("H1", "H", "production.H1", "H", 1_100, 400, 600),
            new("H2", "H", "production.H2", "H1", 1_400, 700, 1_000),
            new("H3", "H", "production.H3", "H2", 2_000, 1_300, 1_900),
            new("H4", "H", "production.H4", "H3", 2_300, 900, 1_200),
            new("H5", "H", "production.H5", "H4", 2_800, 1_300, 1_800),
            new("H.R1", "H", "production.R1", "H", 1_200, 450, 650),
            new("H.R2", "H", "production.R2", "H", 1_600, 800, 1_150),
            new("H.R3", "H", "production.R3", "H", 1_800, 850, 1_200),
            new("H.R4", "H", "production.R4", "H", 2_200, 1_050, 1_450),
            new("H.storage", "H", "production.H.storage", "H5", 3_200, 1_600, 2_200),

            new("O", "O", "periodic_table.name.o", "H", 6_000, automationRequired: 0),
            new("O1", "O", "production.O1", "O", 6_100, 1_500, 2_100),
            new("O2", "O", "production.O2", "O1", 6_250, 1_800, 2_500),
            new("O3", "O", "production.O3", "O2", 6_400, 2_200, 3_100),
            new("O4", "O", "production.O4", "O3", 6_500, 2_400, 3_200),
            new("O5", "O", "production.O5", "O4", 7_500, 3_300, 4_500),
            new("O6", "O", "production.O6", "O5", 9_000, 4_700, 6_300),
            new("O.R1", "O", "production.R1", "O", 6_200, 1_250, 1_750),
            new("O.R2", "O", "production.R2", "O", 6_600, 2_000, 2_800),
            new("O.R4", "O", "production.R4", "O", 6_800, 2_600, 3_700),
            new("O.R8", "O", "production.R8", "O", 8_500, 2_200, 3_000),
            new("O.storage", "O", "production.O.storage", "O6", 9_500, 5_000, 7_000),

            new("N", "N", "periodic_table.name.n", "O", 75_000, automationRequired: 2),
            new("N1", "N", "production.N1", "N", 80_000, 15_000, 22_000),
            new("N2", "N", "production.N2", "N1", 95_000, 27_000, 40_000),
            new("N3", "N", "production.N3", "N2", 115_000, 34_000, 50_000),
            new("N4", "N", "production.N4", "N3", 145_000, 45_000, 67_000),
            new("N5", "N", "production.N5", "N4", 190_000, 62_000, 92_000),
            new("N6", "N", "production.N6", "N5", 245_000, 80_000, 120_000),
            new("N7", "N", "production.N7", "N6", 300_000, 95_000, 145_000),
            new("N.R1", "N", "production.R1", "N", 82_000, 13_000, 18_000),
            new("N.R2", "N", "production.R2", "N", 88_000, 18_000, 26_000),
            new("N.R5", "N", "production.R5", "N", 90_000, 24_000, 35_000),
            new("N.R6", "N", "production.R6", "N", 105_000, 31_000, 45_000),
            new("N.storage", "N", "production.N.storage", "N7", 340_000, 110_000, 165_000),

            new("S", "S", "periodic_table.name.s", "N", 2_500_000, automationRequired: 3),
            new("S1", "S", "production.S1", "S", 2_800_000, 750_000, 1_100_000),
            new("S2", "S", "production.S2", "S1", 3_200_000, 950_000, 1_400_000),
            new("S3", "S", "production.S3", "S2", 3_800_000, 1_300_000, 1_950_000),
            new("S4", "S", "production.S4", "S3", 4_300_000, 1_550_000, 2_300_000),
            new("S5", "S", "production.S5", "S4", 5_000_000, 1_850_000, 2_700_000),
            new("S6", "S", "production.S6", "S5", 5_800_000, 2_250_000, 3_350_000),
            new("S7", "S", "production.S7", "S6", 6_500_000, 2_650_000, 3_900_000),
            new("S8", "S", "production.S8", "S7", 7_300_000, 3_100_000, 4_600_000),
            new("S.R1", "S", "production.R1", "S", 2_900_000, 650_000, 950_000),
            new("S.R2", "S", "production.R2", "S", 3_100_000, 900_000, 1_300_000),
            new("S.R5", "S", "production.R5", "S", 3_400_000, 1_100_000, 1_600_000),
            new("S.R7", "S", "production.R7", "S", 3_000_000, 850_000, 1_250_000),
            new("S.storage", "S", "production.S.storage", "S8", 8_000_000, 3_400_000, 5_000_000),
        };

        static readonly Dictionary<string, ProductionDefinition> ById = IndexEntries();

        /// <summary>Se busca la entrada de una fila por su identificador, sin depender de su posición en la lista.</summary>
        public static ProductionDefinition Find(string id) => id != null && ById.TryGetValue(id, out var entry) ? entry : null;

        static Dictionary<string, ProductionDefinition> IndexEntries()
        {
            var index = new Dictionary<string, ProductionDefinition>(Entries.Count, StringComparer.Ordinal);

            foreach (ProductionDefinition entry in Entries)
                index.Add(entry.Id, entry);

            return index;
        }
    }
}
