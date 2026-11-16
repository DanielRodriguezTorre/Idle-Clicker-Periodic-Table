using UnityEngine;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>
    /// Muestra las cuentas de la economía y anima los cuatro textos flotantes que ya trae la escena.
    /// </summary>
    public sealed class EconomyView : MonoBehaviour
    {
        /// <summary>
        /// El texto que muestra cuánto vale ahora un clic.
        /// </summary>
        public LocalizedText clickValue;
        /// <summary>
        /// Los textos con la cantidad y la producción de cada una de las cuatro familias.
        /// </summary>
        public LocalizedText[] machineCounts, machineRates;
        /// <summary>
        /// Un texto flotante por familia para mostrar lo que acaba de producir.
        /// </summary>
        public LocalizedText[] floatingTexts;
        // Estos grupos permiten desvanecer los textos sin desactivar sus objetos.
        public CanvasGroup[] floatingGroups;
        private readonly Vector2[] startingPositions = new Vector2[4];
        private readonly float[] remainingTimes = new float[4];
        private GameSession session;

        private void OnEnable()
        {
            session = GameSession.Instance;

            if (session == null) 
                return;
            
            // Se conserva el punto de salida de cada texto y se oculta hasta que haya producción.
            for (int index = 0; index < 4; index++)
            {
                startingPositions[index] = ((RectTransform)floatingTexts[index].transform).anchoredPosition;
                floatingGroups[index].alpha = 0;
            }

            session.Changed += Refresh;
            session.ProductionTick += ShowProduction;
            Refresh();
        }

        private void OnDisable()
        {
            if (session == null) 
                return;

            session.Changed -= Refresh;
            session.ProductionTick -= ShowProduction;
        }

        /// <summary>
        /// El clic se pasa a la sesión, que sabe cuánto dinero toca sumar.
        /// </summary>
        public void Click() => session.Click();

        /// <summary>
        /// Se muestran el valor del clic y las cuentas actuales de cada familia.
        /// </summary>
        private void Refresh()
        {
            clickValue.SetArguments(session.Economy.ClickValue);

            for (int index = 0; index < 4; index++)
            {
                machineCounts[index].SetArguments(session.State.machines[index]);
                machineRates[index].SetArguments(session.Economy.GetMachineIncome(index));
            }
        }

        /// <summary>
        /// Sale un texto con lo ganado por cada familia que haya producido dinero.
        /// </summary>
        private void ShowProduction()
        {
            for (int index = 0; index < 4; index++)
            {
                double income = session.Economy.GetMachineIncome(index);

                if (income <= 0) 
                    continue;

                // La animación vuelve a su punto de salida cada vez que llega producción.
                floatingTexts[index].SetArguments(income);
                remainingTimes[index] = 0.9f;
                floatingGroups[index].alpha = 1;
                ((RectTransform)floatingTexts[index].transform).anchoredPosition = startingPositions[index];
            }
        }

        /// <summary>
        /// Los textos van subiendo y desapareciendo poco a poco durante los 0,9 segundos de la animación.
        /// </summary>
        private void Update()
        {
            for (int index = 0; index < 4; index++)
            {
                if (remainingTimes[index] <= 0) 
                    continue;

                remainingTimes[index] = Mathf.Max(0, remainingTimes[index] - Time.deltaTime);
                float progress = 1 - remainingTimes[index] / 0.9f;
                floatingGroups[index].alpha = 1 - progress;
                ((RectTransform)floatingTexts[index].transform).anchoredPosition = startingPositions[index] + Vector2.up * (progress * 35);
            }
        }
    }
}
