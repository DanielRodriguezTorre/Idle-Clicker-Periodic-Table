using System;

namespace IdleClickerPeriodicTable.Core
{
    /// <summary>Aquí se descubren e investigan los modelos en orden y se fabrican sus máquinas, sin depender de la interfaz.</summary>
    public sealed class AutomationService
    {
        /// <summary>La partida con sus recursos, descubrimientos, investigaciones y máquinas.</summary>
        readonly GameState state;

        /// <summary>Los costes y la producción de los cuatro modelos, ajustados en el proyecto.</summary>
        readonly GameBalance balance;

        /// <summary>Se conecta la automatización a la partida y se ponen en orden los datos de las cuatro familias.</summary>
        /// <param name="state">Los datos de la partida ya creada o cargada.</param>
        /// <param name="balance">Costes y producción de los modelos.</param>
        /// <exception cref="ArgumentNullException">Falta la configuración de automatizadores.</exception>
        public AutomationService(GameState state, GameBalance balance)
        {
            this.state = state;
            this.balance = balance ?? throw new ArgumentNullException(nameof(balance), "No se ha proporcionado la configuración de automatizadores.");
            state.Normalize();
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

        /// <summary>La ciencia necesaria para descubrir un modelo es también lo que cuesta investigarlo.</summary>
        /// <param name="index">Índice del modelo, de cero a tres.</param>
        /// <returns>Coste en ciencia, o cero si su configuración falta o no es positiva.</returns>
        /// <exception cref="ArgumentOutOfRangeException">El índice no corresponde a un modelo.</exception>
        public long ResearchCost(int index)
        {
            return ReadCost(balance.researchCosts, index);
        }

        /// <summary>Se consulta cuánto dinero y cuánto ingenio cuesta fabricar una unidad de un modelo.</summary>
        /// <param name="index">Índice del modelo, de cero a tres.</param>
        /// <returns>Coste de cada recurso, o cero si su configuración falta o no es positiva.</returns>
        /// <exception cref="ArgumentOutOfRangeException">El índice no corresponde a un modelo.</exception>
        public long MachineCost(int index)
        {
            return ReadCost(balance.machineCosts, index);
        }

        /// <summary>Lo que produce una máquina en un segundo, contando las mejoras de su familia.</summary>
        /// <param name="index">Índice del modelo, de cero a tres.</param>
        /// <returns>Ingreso unitario, sin multiplicar por la cantidad fabricada, o cero si falta la tasa.</returns>
        /// <exception cref="ArgumentOutOfRangeException">El índice no corresponde a un modelo.</exception>
        public long MachineRate(int index)
        {
            ValidateIndex(index);

            if (balance.baseRates == null || index >= balance.baseRates.Length)
                return 0;

            return ResourceMath.MultiplyCapped(balance.baseRates[index], state.machineLevels[index]);
        }

        /// <summary>Un coste que falta o no es válido no debe permitir comprar gratis ni ganar recursos al pagar.</summary>
        static long ReadCost(long[] costs, int index)
        {
            ValidateIndex(index);

            if (costs == null || index >= costs.Length || costs[index] <= 0)
                return 0;

            return costs[index];
        }

        /// <summary>El índice tiene que corresponder a una de las cuatro familias.</summary>
        static void ValidateIndex(int index)
        {
            if (index < 0 || index >= 4)
                throw new ArgumentOutOfRangeException(nameof(index), "El índice del automatizador debe estar entre 0 y 3.");
        }
    }
}
