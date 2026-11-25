using System.Numerics;

namespace IdleClickerPeriodicTable.Core
{
    /// <summary>Los precios y el mantenimiento se calculan con enteros exactos, los pagos no llevan decimales.</summary>
    public sealed class StaffService
    {
        /// <summary>La partida con sus recursos y el personal contratado.</summary>
        readonly GameState state;

        /// <summary>Los importes iniciales y cuánto crecen, ajustables desde el Inspector.</summary>
        readonly GameBalance balance;

        /// <summary>La cantidad de empleados que se usó para las cuentas guardadas.</summary>
        int cachedResearchers = -1, cachedEngineers = -1;

        /// <summary>Los ajustes anteriores, así solo se repiten las cuentas cuando hay algún cambio.</summary>
        long cachedHireBase, cachedUpkeepBase;
        int cachedHirePercent, cachedUpkeepPercent;

        /// <summary>Las cuentas de contratación y mantenimiento se reutilizan hasta que cambien las cantidades o los ajustes.</summary>
        BigInteger researcherCost, engineerCost, researcherUpkeep, engineerUpkeep;

        /// <summary>Se conecta la plantilla a los datos de la partida y al balance.</summary>
        /// <param name="state">La partida ya creada o cargada sobre la que se harán las cuentas.</param>
        /// <param name="balance">Los ajustes de contratación y mantenimiento.</param>
        public StaffService(GameState state, GameBalance balance)
        {
            this.state = state;
            this.balance = balance;
        }

        /// <summary>Lo que cuesta el siguiente investigador, se redondea hacia arriba solo al final de la cuenta.</summary>
        public BigInteger ResearcherCost { get { RefreshCache(); return researcherCost; } }

        /// <summary>Lo que cuesta el siguiente ingeniero, la cantidad de investigadores no cambia su precio.</summary>
        public BigInteger EngineerCost { get { RefreshCache(); return engineerCost; } }

        /// <summary>Lo que cuesta mantener a los investigadores con su nivel actual, es cero mientras estén pausados.</summary>
        public BigInteger ResearcherUpkeep { get { RefreshCache(); return state.researchersPaused ? BigInteger.Zero : researcherUpkeep * state.ResearchStaffLevel; } }

        /// <summary>Lo que cuesta mantener a los ingenieros con su nivel actual, es cero mientras estén pausados.</summary>
        public BigInteger EngineerUpkeep { get { RefreshCache(); return state.engineersPaused ? BigInteger.Zero : engineerUpkeep * state.EngineeringStaffLevel; } }

        /// <summary>Lo que cuesta mantener durante un segundo a las profesiones que no estén pausadas.</summary>
        public BigInteger TotalUpkeep { get { return ResearcherUpkeep + EngineerUpkeep; } }

        /// <summary>Indica si hay empleados en alguna profesión que no esté pausada.</summary>
        public bool HasActiveStaff => (!state.researchersPaused && state.researchers > 0) || (!state.engineersPaused && state.engineers > 0);

        /// <summary>El dinero tiene que alcanzar para todo el mantenimiento, sin márgenes ni deuda.</summary>
        public bool CanPayUpkeep => state.money >= TotalUpkeep;

        /// <summary>Se puede contratar un investigador si alcanza el dinero y cabe otro en el contador.</summary>
        public bool CanHireResearcher => state.researchers < int.MaxValue && state.money >= ResearcherCost;

        /// <summary>Se puede contratar un ingeniero si alcanza el dinero y cabe otro en el contador.</summary>
        public bool CanHireEngineer => state.engineers < int.MaxValue && state.money >= EngineerCost;

        /// <summary>Se cobra una contratación y se añade un investigador, solo si se puede pagar.</summary>
        /// <returns>Indica si la contratación se ha completado.</returns>
        public bool HireResearcher()
        {
            if (!CanHireResearcher)
                return false;

            state.money -= researcherCost;
            state.researchers++;
            return true;
        }

        /// <summary>Se cobra una contratación y se añade un ingeniero, solo si se puede pagar.</summary>
        /// <returns>Indica si la contratación se ha completado.</returns>
        public bool HireEngineer()
        {
            if (!CanHireEngineer)
                return false;

            state.money -= engineerCost;
            state.engineers++;
            return true;
        }

        /// <summary>Solo cobran y producen las profesiones activas, si no alcanza para todas ellas, ninguna trabaja.</summary>
        public void Tick()
        {
            BigInteger upkeep = TotalUpkeep;

            if (state.money < upkeep)
                return;

            state.money -= upkeep;

            if (!state.researchersPaused)
                state.science += (BigInteger)state.researchers * state.ResearchStaffLevel;

            if (!state.engineersPaused)
                state.ingenuity += (BigInteger)state.engineers * state.EngineeringStaffLevel;
        }

        /// <summary>Se guardan las cuentas base si cambia el personal o sus ajustes, el nivel se multiplica al consultar el mantenimiento.</summary>
        void RefreshCache()
        {
            long hireBase = balance.staffInitialHireCost > 0 ? balance.staffInitialHireCost : 10;
            long upkeepBase = balance.staffInitialUpkeep > 0 ? balance.staffInitialUpkeep : 1;
            int hirePercent = balance.staffHireIncreasePercent >= 0 ? balance.staffHireIncreasePercent : 15;
            int upkeepPercent = balance.staffUpkeepIncreasePercent >= 0 ? balance.staffUpkeepIncreasePercent : 15;
            bool changed = hireBase != cachedHireBase || upkeepBase != cachedUpkeepBase || hirePercent != cachedHirePercent || upkeepPercent != cachedUpkeepPercent;

            if (changed || cachedResearchers != state.researchers)
            {
                cachedResearchers = state.researchers;
                researcherCost = CalculateCost(cachedResearchers, hireBase, hirePercent);
                researcherUpkeep = CalculateUpkeep(cachedResearchers, upkeepBase, upkeepPercent);
            }

            if (changed || cachedEngineers != state.engineers)
            {
                cachedEngineers = state.engineers;
                engineerCost = CalculateCost(cachedEngineers, hireBase, hirePercent);
                engineerUpkeep = CalculateUpkeep(cachedEngineers, upkeepBase, upkeepPercent);
            }

            cachedHireBase = hireBase;
            cachedUpkeepBase = upkeepBase;
            cachedHirePercent = hirePercent;
            cachedUpkeepPercent = upkeepPercent;
        }

        /// <summary>La fracción se conserva exacta hasta calcular el precio, los redondeos de otras contrataciones no se arrastran.</summary>
        static BigInteger CalculateCost(int count, long initial, int percent)
        {
            if (percent == 0)
                return initial;

            BigInteger scale = BigInteger.Pow(100, count);
            BigInteger growth = BigInteger.Pow(100L + percent, count);
            return CeilingDivide(initial * growth, scale);
        }

        /// <summary>Se suma el mantenimiento base y se redondea el total hacia arriba, después se multiplicará por el nivel de la profesión.</summary>
        static BigInteger CalculateUpkeep(int count, long initial, int percent)
        {
            if (percent == 0)
                return (BigInteger)count * initial;

            BigInteger scale = BigInteger.Pow(100, count);
            BigInteger growth = BigInteger.Pow(100L + percent, count);

            // La suma geométrica es inicial × (factor^cantidad − 1) / (factor − 1).
            return CeilingDivide((BigInteger)initial * 100 * (growth - scale), percent * scale);
        }

        /// <summary>Se divide y, si queda resto, se redondea hacia arriba sin usar decimales.</summary>
        static BigInteger CeilingDivide(BigInteger numerator, BigInteger denominator) => (numerator + denominator - 1) / denominator;
    }
}
