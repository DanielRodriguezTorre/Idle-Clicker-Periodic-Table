using System;

namespace IdleClickerPeriodicTable.Core
{
    /// <summary>
    /// Los recursos y el progreso viajan con la partida, aunque cambie la pantalla.
    /// </summary>
    [Serializable]
    public sealed class GameState
    {
        /// <summary>
        /// Dinero disponible, incluidas las fracciones que producen las máquinas.
        /// </summary>
        public double money;

        /// <summary>
        /// La ciencia queda preparada para cuando se añada la investigación.
        /// </summary>
        public double science;

        /// <summary>
        /// El ingenio queda preparado para cuando se añada la ingeniería.
        /// </summary>
        public double ingenuity;

        /// <summary>
        /// Investigaciones desbloqueadas hasta ahora, entre cero y cuatro.
        /// </summary>
        public int researchLevel;

        /// <summary>
        /// Cantidad de máquinas disponibles en cada una de las cuatro familias.
        /// </summary>
        public int[] machines = new int[4];

        /// <summary>
        /// Lo que se multiplica la producción de cada familia, de uno a cinco.
        /// </summary>
        public int[] machineLevels = { 1, 1, 1, 1 };

        /// <summary>
        /// Se ponen los datos en orden por si el guardado trae valores fuera de rango o le faltan familias.
        /// </summary>
        public void Normalize()
        {
            money = NormalizeResource(money);
            science = NormalizeResource(science);
            ingenuity = NormalizeResource(ingenuity);
            researchLevel = Math.Max(0, Math.Min(4, researchLevel));

            // Aunque el guardado venga incompleto, tiene que quedar sitio para las cuatro familias.
            if (machines == null)
                machines = new int[4];
            else if (machines.Length != 4)
                Array.Resize(ref machines, 4);

            if (machineLevels == null)
                machineLevels = new int[4];
            else if (machineLevels.Length != 4)
                Array.Resize(ref machineLevels, 4);

            for (int index = 0; index < 4; index++)
            {
                machines[index] = Math.Max(0, machines[index]);
                machineLevels[index] = Math.Max(1, Math.Min(5, machineLevels[index]));
            }
        }

        /// <summary>
        /// Si un recurso viene en negativo, infinito o con un valor que no es un número, se deja a cero.
        /// </summary>
        private static double NormalizeResource(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) || value < 0 ? 0 : value;
        }
    }
}
