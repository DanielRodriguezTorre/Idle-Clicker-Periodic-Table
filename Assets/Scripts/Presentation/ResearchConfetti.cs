using UnityEngine;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>El confeti se anima con las piezas que ya están guardadas en la escena.</summary>
    public sealed class ResearchConfetti : MonoBehaviour
    {
        /// <summary>Piezas de confeti creadas en el editor, una por entrada.</summary>
        public RectTransform[] pieces;

        /// <summary>Los grupos que permiten desvanecer cada pieza, sin crear componentes durante la animación.</summary>
        public CanvasGroup[] groups;

        /// <summary>Duración de cada celebración en segundos reales.</summary>
        [Min(0.1f)] public float duration = 2;

        /// <summary>Velocidad inicial de dispersión en unidades locales por segundo.</summary>
        [Min(0)] public float speed = 180;

        /// <summary>Aceleración vertical descendente en unidades locales por segundo al cuadrado.</summary>
        [Min(0)] public float gravity = 160;

        /// <summary>Velocidad máxima de giro de las piezas en grados por segundo.</summary>
        [Min(0)] public float rotationSpeed = 180;

        Vector2[] startingPositions, velocities;
        Quaternion[] startingRotations;
        float[] spins;
        float remaining;

        /// <summary>Se guardan las posiciones originales y las piezas quedan ocultas hasta la siguiente celebración.</summary>
        void OnEnable()
        {
            if (!Initialize())
            {
                Debug.LogError("Faltan referencias de las piezas de confeti.", this);
                enabled = false;
                return;
            }

            Hide();
        }

        /// <summary>La animación se oculta al salir de la pantalla.</summary>
        void OnDisable() => Hide();

        /// <summary>Los datos del movimiento se preparan una sola vez, sin añadir objetos ni componentes.</summary>
        bool Initialize()
        {
            if (pieces == null || groups == null || pieces.Length == 0 || pieces.Length != groups.Length)
                return false;

            for (int index = 0; index < pieces.Length; index++)
                if (pieces[index] == null || groups[index] == null)
                    return false;

            if (startingPositions != null && startingPositions.Length == pieces.Length)
                return true;

            startingPositions = new Vector2[pieces.Length];
            startingRotations = new Quaternion[pieces.Length];
            velocities = new Vector2[pieces.Length];
            spins = new float[pieces.Length];

            for (int index = 0; index < pieces.Length; index++)
            {
                startingPositions[index] = pieces[index].anchoredPosition;
                startingRotations[index] = pieces[index].localRotation;
            }

            return true;
        }

        /// <summary>Se reutilizan todas las piezas para celebrar una investigación completada.</summary>
        public void Play()
        {
            if (!isActiveAndEnabled || !Initialize())
                return;

            remaining = Mathf.Max(0.1f, duration);
            float initialSpeed = Mathf.Max(0, speed);
            float maxSpin = Mathf.Max(0, rotationSpeed);

            for (int index = 0; index < pieces.Length; index++)
            {
                pieces[index].anchoredPosition = startingPositions[index] + Random.insideUnitCircle * 25;
                pieces[index].localRotation = startingRotations[index];
                velocities[index] = new(Random.Range(-initialSpeed, initialSpeed), Random.Range(initialSpeed * 0.65f, initialSpeed * 1.4f));
                spins[index] = Random.Range(-maxSpin, maxSpin);
                groups[index].alpha = 1;
            }
        }

        /// <summary>Mientras dura la celebración, las piezas se mueven, giran y desaparecen poco a poco.</summary>
        void Update()
        {
            if (remaining <= 0)
                return;

            float delta = Time.unscaledDeltaTime;
            remaining = Mathf.Max(0, remaining - delta);
            float alpha = Mathf.Clamp01(remaining / Mathf.Min(0.5f, Mathf.Max(0.1f, duration)));

            for (int index = 0; index < pieces.Length; index++)
            {
                velocities[index].y -= Mathf.Max(0, gravity) * delta;
                pieces[index].anchoredPosition += velocities[index] * delta;
                pieces[index].Rotate(0, 0, spins[index] * delta, Space.Self);
                groups[index].alpha = alpha;
            }
        }

        /// <summary>Se ocultan los grupos asignados y se detiene el reloj de la animación.</summary>
        void Hide()
        {
            remaining = 0;

            if (groups == null)
                return;

            for (int index = 0; index < groups.Length; index++)
                if (groups[index] != null)
                    groups[index].alpha = 0;
        }
    }
}
