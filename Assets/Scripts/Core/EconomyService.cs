using System.Numerics;

namespace IdleClickerPeriodicTable.Core
{
    /// <summary>Aquí se hacen las cuentas de los clics y las máquinas, sin depender de la interfaz.</summary>
    public sealed class EconomyService
    {
        /// <summary>La partida donde se suma lo ganado con clics y producción.</summary>
        readonly GameState state;

        /// <summary>Los valores de producción configurados en el Inspector.</summary>
        readonly GameBalance balance;

        /// <summary>Se conectan las cuentas a los datos de la partida y al balance.</summary>
        /// <param name="state">La partida ya creada o cargada.</param>
        /// <param name="balance">Lo que produce cada familia sin mejoras.</param>
        public EconomyService(GameState state, GameBalance balance)
        {
            this.state = state;
            this.balance = balance;
        }

        /// <summary>Cada clic da lo mismo que todas las máquinas en un segundo, con un mínimo de una unidad.</summary>
        public BigInteger ClickValue => BigInteger.Max(BigInteger.One, IncomePerSecond);

        /// <summary>Lo que ganan todas las máquinas juntas en un segundo de juego.</summary>
        public BigInteger IncomePerSecond
        {
            get
            {
                BigInteger total = BigInteger.Zero;

                for (int index = 0; index < 4; index++)
                    total += GetMachineIncome(index);

                return total;
            }
        }

        /// <summary>Se calcula lo que produce una familia entera.</summary>
        /// <param name="index">La familia que se quiere consultar, de cero a tres.</param>
        /// <returns>Lo que gana por segundo, o cero si la producción base falta o no es válida.</returns>
        public BigInteger GetMachineIncome(int index)
        {
            // Una configuración incompleta no debería meter ingresos raros en la partida.
            if (balance.baseRates == null || index >= balance.baseRates.Length)
                return 0;

            long rate = balance.baseRates[index];

            if (rate < 0)
                return 0;

            return (BigInteger)rate * state.machineLevels[index] * state.machines[index];
        }

        /// <summary>Se suma al dinero lo que vale este clic.</summary>
        public void Click()
        {
            state.money += ClickValue;
        }

        /// <summary>Se suma un segundo de producción, el reloj lo lleva la sesión, no este servicio.</summary>
        public void Tick()
        {
            state.money += IncomePerSecond;
        }
    }
}
