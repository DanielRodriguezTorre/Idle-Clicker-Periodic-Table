using UnityEngine;

namespace IdleClickerPeriodicTable.Core
{
    /// <summary>La producción de las máquinas y los costes del personal se ajustan desde el Inspector, sin tocar el código.</summary>
    [CreateAssetMenu(fileName = "GameBalance", menuName = "Idle Clicker/Game Balance")]
    public sealed class GameBalance : ScriptableObject
    {
        /// <summary>Lo que gana por segundo una máquina de cada familia antes de aplicar mejoras.</summary>
        public long[] baseRates = { 1, 10, 100, 1000 };

        /// <summary>La ciencia necesaria para descubrir cada modelo, esa misma cantidad se gasta al investigarlo.</summary>
        public long[] researchCosts = { 100, 10000, 1000000, 100000000 };

        /// <summary>Lo que cuesta fabricar una unidad de cada modelo, se paga esa cantidad tanto en dinero como en ingenio.</summary>
        public long[] machineCosts = { 100, 10000, 1000000, 100000000 };

        /// <summary>Lo que cuesta contratar al primer empleado de cada profesión.</summary>
        public long staffInitialHireCost = 10;

        /// <summary>Cuánto sube el precio por cada empleado de la misma profesión, un 15 significa un 15 % más.</summary>
        public int staffHireIncreasePercent = 15;

        /// <summary>Lo que cuesta mantener al primer empleado de cada profesión durante un segundo.</summary>
        public long staffInitialUpkeep = 1;

        /// <summary>Cuánto sube el mantenimiento de cada nuevo empleado de la misma profesión, en porcentaje entero.</summary>
        public int staffUpkeepIncreasePercent = 15;
    }
}
