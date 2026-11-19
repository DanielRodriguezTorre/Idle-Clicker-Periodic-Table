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
        /// El dinero disponible para comprar y pagar el mantenimiento, siempre en unidades enteras.
        /// </summary>
        public long money;

        /// <summary>
        /// Ciencia acumulada por el trabajo de los investigadores.
        /// </summary>
        public long science;

        /// <summary>
        /// Ingenio acumulado por el trabajo de los ingenieros.
        /// </summary>
        public long ingenuity;

        /// <summary>
        /// Investigadores contratados, cada uno puede producir una unidad de ciencia por segundo.
        /// </summary>
        public int researchers;

        /// <summary>
        /// Ingenieros contratados, cada uno puede producir una unidad de ingenio por segundo.
        /// </summary>
        public int engineers;

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
            money = Math.Max(0L, money);
            science = Math.Max(0L, science);
            ingenuity = Math.Max(0L, ingenuity);
            researchers = Math.Max(0, researchers);
            engineers = Math.Max(0, engineers);
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
    }
}
