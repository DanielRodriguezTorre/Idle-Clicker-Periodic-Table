using UnityEngine;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>
    /// Los contadores de dinero, ciencia e ingenio del menú común se mantienen al día con la partida.
    /// </summary>
    public sealed class ResourceCounters : MonoBehaviour
    {
        /// <summary>
        /// Los tres textos del menú, ya preparados con sus claves de traducción.
        /// </summary>
        public LocalizedText money, science, ingenuity;
        /// <summary>
        /// El aviso del menú que aparece si falla el guardado.
        /// </summary>
        public GameObject saveWarning;
        private GameSession session;

        /// <summary>
        /// El menú escucha los cambios de la sesión y muestra sus números nada más activarse.
        /// </summary>
        private void OnEnable()
        {
            session = GameSession.Instance;

            if (session == null) 
                return;

            session.Changed += Refresh;
            Refresh();
        }

        /// <summary>
        /// Se deja de escuchar a la sesión cuando el menú ya no está activo.
        /// </summary>
        private void OnDisable()
        {
            if (session != null) 
                session.Changed -= Refresh;
        }

        /// <summary>
        /// Se actualizan los tres recursos y se comprueba si debe aparecer el aviso de guardado.
        /// </summary>
        private void Refresh()
        {
            money.SetArguments(session.State.money);
            science.SetArguments(session.State.science);
            ingenuity.SetArguments(session.State.ingenuity);
            saveWarning.SetActive(session.SaveFailed);
        }
    }
}
