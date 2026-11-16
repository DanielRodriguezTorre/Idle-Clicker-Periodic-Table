using System;

namespace IdleClickerPeriodicTable.Core
{
    /// <summary>
    /// Aquí se hacen las cuentas de los clics y las máquinas, sin depender de la interfaz.
    /// </summary>
    public sealed class EconomyService
    {
        /// <summary>
        /// La partida donde se suma lo ganado con clics y producción.
        /// </summary>
        private readonly GameState state;

        /// <summary>
        /// Los valores de producción configurados en el Inspector.
        /// </summary>
        private readonly GameBalance balance;

        /// <summary>
        /// Se conectan las cuentas a la partida y se revisan sus datos antes de empezar.
        /// </summary>
        /// <param name="state">La partida que se va a usar, sus valores se ponen en orden al recibirla.</param>
        /// <param name="balance">Lo que produce cada familia sin mejoras.</param>
        /// <exception cref="ArgumentNullException">Falta la partida o su configuración de producción.</exception>
        public EconomyService(GameState state, GameBalance balance)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state), "No se ha proporcionado el estado de la partida.");
            this.balance = balance ?? throw new ArgumentNullException(nameof(balance), "No se ha proporcionado la configuración de producción.");
            state.Normalize();
        }

        /// <summary>
        /// Lo que vale ahora cada clic, según la investigación desbloqueada.
        /// </summary>
        public double ClickValue
        {
            get
            {
                switch (state.researchLevel)
                {
                    case 2: return 10;
                    case 3: return 100;
                    case 4: return 1000;
                    // Sin investigar o con la primera investigación, el clic sigue valiendo uno.
                    default: return 1;
                }
            }
        }

        /// <summary>
        /// Lo que ganan todas las máquinas juntas en un segundo de juego.
        /// </summary>
        public double IncomePerSecond
        {
            get
            {
                double total = 0;

                for (int index = 0; index < 4; index++)
                    total += GetMachineIncome(index);

                return total;
            }
        }

        /// <summary>
        /// Se calcula lo que produce una familia entera.
        /// </summary>
        /// <param name="index">La familia que se quiere consultar, de cero a tres.</param>
        /// <returns>Lo que gana por segundo, o cero si la producción base falta o no es válida.</returns>
        /// <exception cref="ArgumentOutOfRangeException">El índice no corresponde a ninguna de las cuatro familias.</exception>
        public double GetMachineIncome(int index)
        {
            if (index < 0 || index >= 4)
                throw new ArgumentOutOfRangeException(nameof(index), "El índice de la familia de máquinas debe estar entre 0 y 3.");

            // Una configuración incompleta no debería meter ingresos raros en la partida.
            if (balance.baseRates == null || index >= balance.baseRates.Length)
                return 0;

            double rate = balance.baseRates[index];

            if (rate < 0 || double.IsNaN(rate) || double.IsInfinity(rate))
                return 0;

            return rate * state.machineLevels[index] * state.machines[index];
        }

        /// <summary>
        /// Se suma al dinero lo que vale este clic.
        /// </summary>
        public void Click()
        {
            state.money += ClickValue;
        }

        /// <summary>
        /// Se suma un segundo de producción, el reloj lo lleva la sesión, no este servicio.
        /// </summary>
        public void Tick()
        {
            state.money += IncomePerSecond;
        }
    }
}
