using UnityEngine;

namespace IdleClickerPeriodicTable.Core
{
    /// <summary>
    /// La producción de cada familia se puede ajustar desde el Inspector, sin tocar el código.
    /// </summary>
    [CreateAssetMenu(fileName = "GameBalance", menuName = "Idle Clicker/Game Balance")]
    public sealed class GameBalance : ScriptableObject
    {
        /// <summary>
        /// Lo que gana por segundo una máquina de cada familia antes de aplicar mejoras.
        /// </summary>
        public double[] baseRates = { 1, 10, 100, 1000 };
    }
}
