using UnityEngine;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>El aviso de descubrimiento aparece unos segundos, su interfaz ya está en cada escena.</summary>
    public sealed class DiscoveryToast : MonoBehaviour
    {
        /// <summary>El grupo que permite mostrar y desvanecer el aviso.</summary>
        public CanvasGroup group;

        /// <summary>Segundos visibles, incluidos los últimos 0,5 segundos de desvanecimiento.</summary>
        [Min(0.1f)] public float duration = 5;

        GameSession session;
        float remaining;

        /// <summary>Se escuchan los descubrimientos nuevos y se recoge cualquier aviso pendiente al cambiar de pantalla.</summary>
        void OnEnable()
        {
            session = GameSession.Instance;
            remaining = 0;
            group.alpha = 0;
            group.blocksRaycasts = false;
            group.interactable = false;
            session.NewScientificDiscoveries += OnDiscovery;

            if (session.TakeDiscoveryNotice())
                Show();
        }

        /// <summary>El aviso no conserva referencias a la sesión después de abandonar la pantalla.</summary>
        void OnDisable()
        {
            if (session != null)
                session.NewScientificDiscoveries -= OnDiscovery;

            remaining = 0;

            if (group != null)
                group.alpha = 0;
        }

        /// <summary>Se recoge el aviso una sola vez y se muestra en la pantalla activa.</summary>
        void OnDiscovery()
        {
            if (session.TakeDiscoveryNotice())
                Show();
        }

        /// <summary>Se reinicia el tiempo del aviso, sin crear ni sustituir su interfaz.</summary>
        public void Show()
        {
            if (group == null)
                return;

            remaining = Mathf.Max(0.1f, duration);
            group.alpha = 1;
        }

        /// <summary>El aviso se desvanece al terminar su tiempo, sin impedir que se use la pantalla.</summary>
        void Update()
        {
            if (remaining <= 0)
                return;

            remaining = Mathf.Max(0, remaining - Time.unscaledDeltaTime);
            group.alpha = Mathf.Clamp01(remaining / Mathf.Min(0.5f, Mathf.Max(0.1f, duration)));
        }
    }
}
