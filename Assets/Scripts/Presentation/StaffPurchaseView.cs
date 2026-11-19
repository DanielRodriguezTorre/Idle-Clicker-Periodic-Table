using UnityEngine;
using UnityEngine.UI;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>
    /// La contratación y sus costes se muestran con los controles que ya trae la escena.
    /// </summary>
    public sealed class StaffPurchaseView : MonoBehaviour
    {
        /// <summary>
        /// Los botones para contratar personal desde Economía.
        /// </summary>
        public Button hireResearcherButton, hireEngineerButton;

        /// <summary>
        /// Los textos con la cantidad de empleados de cada profesión.
        /// </summary>
        public LocalizedText researcherCount, engineerCount;

        /// <summary>
        /// Los textos con el precio de la siguiente contratación.
        /// </summary>
        public LocalizedText researcherPrice, engineerPrice;

        /// <summary>
        /// Los textos con el mantenimiento total y si hay dinero para pagarlo.
        /// </summary>
        public LocalizedText upkeepLabel, statusLabel;

        private GameSession session;

        /// <summary>
        /// Los controles se actualizan con los cambios de la sesión mientras la vista está activa.
        /// </summary>
        private void OnEnable()
        {
            session = GameSession.Instance;
            if (session == null || hireResearcherButton == null || hireEngineerButton == null ||
                researcherCount == null || engineerCount == null || researcherPrice == null ||
                engineerPrice == null || upkeepLabel == null || statusLabel == null)
            {
                Debug.LogError("Faltan referencias de contratación de personal.", this);
                enabled = false;
                return;
            }

            session.Changed += Refresh;
            Refresh();
        }

        /// <summary>
        /// Al salir de la pantalla, se deja de escuchar los cambios de la sesión.
        /// </summary>
        private void OnDisable()
        {
            if (session != null)
                session.Changed -= Refresh;
        }

        /// <summary>
        /// Se pide un investigador, la sesión comprueba el dinero y cobra su precio.
        /// </summary>
        public void HireResearcher()
        {
            if (session != null)
                session.HireResearcher();
        }

        /// <summary>
        /// Se pide un ingeniero con las mismas reglas de contratación que usa el resto de la partida.
        /// </summary>
        public void HireEngineer()
        {
            if (session != null)
                session.HireEngineer();
        }

        /// <summary>
        /// Los precios y botones se actualizan al abrir la vista o cuando cambian los datos de la partida.
        /// </summary>
        private void Refresh()
        {
            researcherCount.SetArguments(session.State.researchers);
            engineerCount.SetArguments(session.State.engineers);
            researcherPrice.SetArguments(session.Staff.ResearcherCost);
            engineerPrice.SetArguments(session.Staff.EngineerCost);
            upkeepLabel.SetArguments(session.Staff.TotalUpkeep);
            hireResearcherButton.interactable = session.IsPlaying && session.Staff.CanHireResearcher;
            hireEngineerButton.interactable = session.IsPlaying && session.Staff.CanHireEngineer;

            string key;
            if (session.State.researchers == 0 && session.State.engineers == 0)
                key = "staff.none";
            else
                key = session.Staff.CanPayUpkeep ? "staff.working" : "staff.unfunded";

            if (statusLabel.key == key)
                return;

            statusLabel.key = key;
            statusLabel.Refresh();
        }
    }
}
