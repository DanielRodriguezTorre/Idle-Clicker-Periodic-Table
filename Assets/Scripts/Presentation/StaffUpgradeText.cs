using IdleClickerPeriodicTable.Core;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>La mejora muestra el mismo nivel y sus efectos tanto en Investigación como en Ingeniería.</summary>
    static class StaffUpgradeText
    {
        /// <summary>El título y la descripción de la fila muestran el nivel y el efecto de la mejora.</summary>
        public static void Apply(ProductionDefinition entry, LocalizedText title, LocalizedText description)
        {
            title.key = "machine.level_name";
            title.SetLocalizedArguments(entry.NameKey, entry.StaffLevel);
            description.key = entry.Profession == StaffProfession.Research
                ? "staff.upgrade.research_effect" : "staff.upgrade.engineering_effect";
            description.SetArguments(entry.StaffLevel);
        }
    }
}
