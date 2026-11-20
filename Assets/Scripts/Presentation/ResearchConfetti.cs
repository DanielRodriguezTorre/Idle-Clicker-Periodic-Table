using UnityEngine;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>El confeti se anima con las piezas que ya están guardadas en la escena.</summary>
    public sealed class ResearchConfetti : MonoBehaviour
    {
        /// <summary>Piezas de confeti creadas en el editor, una por entrada.</summary>
        public RectTransform[] pieces;

        /// <summary>El grupo del contenedor desvanece todas las piezas a la vez.</summary>
        [SerializeField] CanvasGroup group;

        /// <summary>Duración de cada celebración en segundos reales.</summary>
        [Min(0.1f)] public float duration = 2;

        /// <summary>Velocidad inicial de dispersión en unidades locales por segundo.</summary>
        [Min(0)] public float speed = 180;

        /// <summary>Aceleración vertical descendente en unidades locales por segundo al cuadrado.</summary>
        [Min(0)] public float gravity = 160;

        /// <summary>Velocidad máxima de giro de las piezas en grados por segundo.</summary>
        [Min(0)] public float rotationSpeed = 180;

        /// <summary>La posición y el giro de cada pieza, junto con su movimiento durante la celebración.</summary>
        struct PieceMotion
        {
            public Vector2 start, velocity;
            public Quaternion rotation;
            public float spin;
        }

        PieceMotion[] motion;
        float remaining;

        /// <summary>Se guardan la posición y el giro originales de las piezas para reiniciar cada celebración.</summary>
        void Awake()
        {
            motion = new PieceMotion[pieces.Length];

            for (int index = 0; index < pieces.Length; index++)
                motion[index] = new() { start = pieces[index].anchoredPosition, rotation = pieces[index].localRotation };
        }

        /// <summary>Las piezas se ocultan al terminar el efecto o al desactivar la pantalla.</summary>
        void OnDisable()
        {
            remaining = 0;
            group.alpha = 0;
        }

        /// <summary>Se reutilizan todas las piezas para celebrar una investigación completada.</summary>
        public void Play()
        {
            if (!gameObject.activeInHierarchy)
                return;

            for (int index = 0; index < pieces.Length; index++)
            {
                ref PieceMotion piece = ref motion[index];
                pieces[index].anchoredPosition = piece.start + Random.insideUnitCircle * 25;
                pieces[index].localRotation = piece.rotation;
                piece.velocity = new(Random.Range(-speed, speed), Random.Range(speed * 0.65f, speed * 1.4f));
                piece.spin = Random.Range(-rotationSpeed, rotationSpeed);
            }

            remaining = duration;
            group.alpha = 1;
            enabled = true;
        }

        /// <summary>Mientras dura la celebración, las piezas se mueven, giran y desaparecen poco a poco.</summary>
        void Update()
        {
            float delta = Time.unscaledDeltaTime;
            remaining -= delta;

            if (remaining <= 0)
            {
                enabled = false;
                return;
            }

            float fall = gravity * delta;

            for (int index = 0; index < pieces.Length; index++)
            {
                ref PieceMotion piece = ref motion[index];
                piece.velocity.y -= fall;
                pieces[index].anchoredPosition += piece.velocity * delta;
                pieces[index].Rotate(0, 0, piece.spin * delta, Space.Self);
            }

            group.alpha = Mathf.Clamp01(remaining / Mathf.Min(0.5f, duration));
        }
    }
}
