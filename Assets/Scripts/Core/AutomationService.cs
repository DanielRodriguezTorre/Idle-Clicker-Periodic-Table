using System;
using System.Numerics;

namespace IdleClickerPeriodicTable.Core
{
    /// <summary>Aquí se llevan la investigación, la fabricación y las mejoras de cada familia de automatizadores.</summary>
    public sealed class AutomationService
    {
        /// <summary>Cada familia tiene cuatro mejoras que se compran en orden, después del nivel base 1.</summary>
        public const int UpgradesPerMachine = 4;

        /// <summary>La partida con sus recursos, descubrimientos, investigaciones y máquinas.</summary>
        readonly GameState state;

        /// <summary>Los costes y la producción de los cuatro modelos, ajustados en el proyecto.</summary>
        readonly GameBalance balance;

        /// <summary>Se conecta la automatización a los datos de la partida y al balance.</summary>
        /// <param name="state">Los datos de la partida ya creada o cargada.</param>
        /// <param name="balance">Costes y producción de los modelos.</param>
        public AutomationService(GameState state, GameBalance balance)
        {
            this.state = state;
            this.balance = balance;
        }

        /// <summary>Se descubren los modelos cuando alcanza la ciencia disponible, los descubrimientos no se pierden después.</summary>
        /// <returns>Indica si se ha descubierto al menos un modelo nuevo.</returns>
        public bool CheckDiscoveries()
        {
            int previousLevel = state.discoveryLevel;

            while (state.discoveryLevel < 4)
            {
                long threshold = ResearchCost(state.discoveryLevel);

                if (threshold <= 0 || state.science < threshold) 
                    break;

                state.discoveryLevel++;
            }

            return state.discoveryLevel != previousLevel;
        }

        /// <summary>Solo se puede investigar el siguiente modelo, si ya está descubierto y alcanza la ciencia.</summary>
        /// <param name="index">Índice del modelo, de cero a tres.</param>
        /// <returns>Indica si se puede investigar ahora, si el índice no existe devuelve falso.</returns>
        public bool CanResearch(int index)
        {
            if (index < 0 || index >= 4 || index != state.researchLevel || index >= state.discoveryLevel)
                return false;

            long cost = ResearchCost(index);
            return cost > 0 && state.science >= cost;
        }

        /// <summary>Se gasta la ciencia una sola vez para desbloquear el modelo, esto todavía no fabrica máquinas.</summary>
        /// <param name="index">Índice del siguiente modelo que se quiere investigar.</param>
        /// <returns>Indica si la investigación se ha completado.</returns>
        public bool Research(int index)
        {
            if (!CanResearch(index))
                return false;

            state.science -= ResearchCost(index);
            state.researchLevel++;
            return true;
        }

        /// <summary>Para fabricar hace falta investigar el modelo, pagar ambos recursos y tener sitio en el contador.</summary>
        /// <param name="index">Índice del modelo, de cero a tres.</param>
        /// <returns>Indica si se puede fabricar otra unidad, si el índice no existe devuelve falso.</returns>
        public bool CanBuyMachine(int index)
        {
            if (index < 0 || index >= 4 || index >= state.researchLevel || state.machines[index] == int.MaxValue)
                return false;

            long cost = MachineCost(index);
            return cost > 0 && state.money >= cost && state.ingenuity >= cost;
        }

        /// <summary>Se pagan el dinero y el ingenio de una unidad, solo aumenta la cantidad de ese modelo.</summary>
        /// <param name="index">Índice del automatizador que se quiere fabricar.</param>
        /// <returns>Indica si la fabricación se ha completado.</returns>
        public bool BuyMachine(int index)
        {
            if (!CanBuyMachine(index))
                return false;

            long cost = MachineCost(index);
            state.money -= cost;
            state.ingenuity -= cost;
            state.machines[index]++;
            return true;
        }

        /// <summary>Solo se puede comprar la siguiente mejora de una familia investigada, si alcanzan el dinero y el ingenio.</summary>
        /// <param name="index">Familia del automatizador, de cero a tres.</param>
        /// <param name="level">Nivel de producción que se quiere comprar, de dos a cinco.</param>
        /// <returns>Si se puede comprar la mejora en el estado actual de la partida.</returns>
        public bool CanBuyUpgrade(int index, int level)
        {
            if (index < 0 || index >= 4 || index >= state.researchLevel || level != state.machineLevels[index] + 1)
                return false;

            long cost = UpgradeCost(index, level);
            return cost > 0 && state.money >= cost && state.ingenuity >= cost;
        }

        /// <summary>Se cobra la mejora una sola vez y se aumenta la producción de todas las máquinas de esa familia.</summary>
        /// <param name="index">Familia del automatizador, de cero a tres.</param>
        /// <param name="level">Nivel que se quiere aplicar.</param>
        /// <returns>Si se ha aplicado y cobrado la mejora.</returns>
        public bool BuyUpgrade(int index, int level)
        {
            if (!CanBuyUpgrade(index, level))
                return false;

            long cost = UpgradeCost(index, level);
            state.money -= cost;
            state.ingenuity -= cost;
            state.machineLevels[index] = level;
            return true;
        }

        /// <summary>Lo que cuesta la mejora tanto en dinero como en ingenio, o cero si ese nivel no está disponible.</summary>
        /// <param name="index">Familia del automatizador, de cero a tres.</param>
        /// <param name="level">Nivel de producción que se quiere comprar, de dos a cinco.</param>
        public long UpgradeCost(int index, int level)
        {
            if (level < 2 || level > UpgradesPerMachine + 1)
                return 0;

            // Cada familia ocupa cuatro posiciones, el nivel 2 es la primera mejora de su grupo.
            return ReadCost(balance.upgradeCosts, index * UpgradesPerMachine + level - 2);
        }

        /// <summary>La ciencia necesaria para descubrir un modelo es también lo que cuesta investigarlo.</summary>
        /// <param name="index">Índice del modelo, de cero a tres.</param>
        /// <returns>Coste en ciencia, o cero si su configuración falta o no es positiva.</returns>
        public long ResearchCost(int index)
        {
            return ReadCost(balance.researchCosts, index);
        }

        /// <summary>Se consulta cuánto dinero y cuánto ingenio cuesta fabricar una unidad de un modelo.</summary>
        /// <param name="index">Índice del modelo, de cero a tres.</param>
        /// <returns>Coste de cada recurso, o cero si su configuración falta o no es positiva.</returns>
        public long MachineCost(int index)
        {
            return ReadCost(balance.machineCosts, index);
        }

        /// <summary>Lo que produce una máquina en un segundo, contando las mejoras de su familia.</summary>
        /// <param name="index">Índice del modelo, de cero a tres.</param>
        /// <returns>Ingreso unitario, sin multiplicar por la cantidad fabricada, o cero si falta la tasa.</returns>
        public BigInteger MachineRate(int index)
        {
            return MachineRate(index, state.machineLevels[index]);
        }

        /// <summary>Se calcula lo que produciría una máquina a ese nivel para mostrar el resultado de la mejora.</summary>
        /// <param name="index">Familia del automatizador, de cero a tres.</param>
        /// <param name="level">Multiplicador que se aplica a su producción base.</param>
        /// <returns>Ingreso unitario por segundo, o cero si falta la producción base.</returns>
        public BigInteger MachineRate(int index, int level)
        {
            if (level <= 0 || balance.baseRates == null || index >= balance.baseRates.Length)
                return 0;

            return (BigInteger)Math.Max(0L, balance.baseRates[index]) * level;
        }

        /// <summary>Un coste que falta o no es válido no debe permitir comprar gratis ni ganar recursos al pagar.</summary>
        static long ReadCost(long[] costs, int index)
        {
            if (costs == null || index >= costs.Length || costs[index] <= 0)
                return 0;

            return costs[index];
        }
    }
}
