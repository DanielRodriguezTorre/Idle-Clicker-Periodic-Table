using UnityEngine;
using UnityEngine.UI;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>El selector refleja si una profesión está trabajando o en pausa.</summary>
    public sealed class StaffWorkToggle : MonoBehaviour
    {
        /// <summary>Si está activado controla a los investigadores, si no, a los ingenieros.</summary>
        public bool researchers;

        /// <summary>El selector que ya trae la escena, conectado a SetWorking.</summary>
        public Toggle toggle;

        /// <summary>El texto que indica si esta profesión está trabajando o en pausa.</summary>
        public LocalizedText label;

        GameSession session;

        /// <summary>Al activar el control se muestra el estado de la sesión y se escuchan sus cambios.</summary>
        void OnEnable()
        {
            session = GameSession.Instance;

            if (session == null || toggle == null || label == null)
            {
                Debug.LogError("Faltan referencias del control de trabajo del personal.", this);
                enabled = false;
                return;
            }

            session.Changed += Refresh;
            Refresh();
        }

        /// <summary>Se deja de escuchar a la sesión al desactivar el control o cambiar de pantalla.</summary>
        void OnDisable()
        {
            if (session != null)
                session.Changed -= Refresh;
        }

        /// <summary>Se pide activar o pausar esta profesión, sin cambiar la cantidad de empleados.</summary>
        /// <param name="working">Si el personal de esta profesión debe consumir y producir.</param>
        public void SetWorking(bool working)
        {
            if (session == null)
                return;

            if (researchers)
                session.SetResearchersWorking(working);
            else
                session.SetEngineersWorking(working);
        }

        /// <summary>Se actualiza la casilla sin volver a enviar la orden de activar o pausar al personal.</summary>
        void Refresh()
        {
            bool paused = researchers ? session.State.researchersPaused : session.State.engineersPaused;
            toggle.SetIsOnWithoutNotify(!paused);
            toggle.interactable = session.IsPlaying;
            string key;

            if (researchers)
                key = paused ? "staff.research_paused" : "staff.research_running";
            else
                key = paused ? "staff.engineering_paused" : "staff.engineering_running";

            if (label.key == key)
                return;

            label.key = key;
            label.Refresh();
        }
    }
}
