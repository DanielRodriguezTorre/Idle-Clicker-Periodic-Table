using System.Collections.Generic;
using IdleClickerPeriodicTable.Core;
using UnityEngine;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>El personal aparece como círculos dentro de un recinto, su movimiento es solo visual.</summary>
    public sealed class StaffRoomView : MonoBehaviour
    {
        /// <summary>Si está activado se muestran investigadores, si no, ingenieros.</summary>
        public bool researchers;

        /// <summary>El rectángulo fijo dentro del cual se mueve el centro de cada círculo.</summary>
        public RectTransform movementArea;

        /// <summary>El prefab de cada círculo, es la única parte de esta interfaz que se crea durante la partida.</summary>
        public RectTransform workerPrefab;

        /// <summary>La velocidad de los círculos en unidades locales del recinto por segundo.</summary>
        [Min(0)] public float speed = 65;

        /// <summary>Los textos con la cantidad, el mantenimiento y la pausa de esta profesión, junto al estado de pago común.</summary>
        public LocalizedText countLabel, upkeepLabel, statusLabel;

        /// <summary>Los datos de cada círculo y su recorrido se guardan para no calcularlos de nuevo en cada frame.</summary>
        struct Motion
        {
            /// <summary>El círculo creado desde el prefab de esta profesión.</summary>
            public RectTransform rect;
            /// <summary>El siguiente destino, en coordenadas locales del recinto.</summary>
            public Vector2 target;
        }

        readonly List<Motion> motions = new();
        GameSession session;
        GameState displayedState;
        Rect bounds;

        /// <summary>Indica si los círculos de esta profesión deben quedarse quietos.</summary>
        bool Paused => researchers ? session.State.researchersPaused : session.State.engineersPaused;

        /// <summary>Al activar la vista, se reutilizan sus círculos y se empieza a escuchar cambios de cantidad.</summary>
        void OnEnable()
        {
            session = GameSession.Instance;

            if (session == null)
            {
                Debug.LogError("El recinto de personal necesita una sesión de partida.", this);
                enabled = false;
                return;
            }

            bounds = movementArea.rect;
            session.Changed += Refresh;
            Refresh();
        }

        /// <summary>Se deja de escuchar a la sesión, los círculos quedan para cuando se active la vista otra vez.</summary>
        void OnDisable()
        {
            if (session != null)
                session.Changed -= Refresh;
        }

        /// <summary>Se ajusta la cantidad de círculos, solo se crean los que falten tras una contratación.</summary>
        void Refresh()
        {
            if (!ReferenceEquals(displayedState, session.State))
            {
                // Una partida nueva o cargada no debe conservar los círculos de la anterior.
                RemoveWorkers(0);
                displayedState = session.State;
            }

            int count = researchers ? session.State.researchers : session.State.engineers;
            RemoveWorkers(count);

            while (motions.Count < count)
                AddWorker();

            countLabel.SetArguments(count);
            upkeepLabel.SetArguments(researchers ? session.Staff.ResearcherUpkeep : session.Staff.EngineerUpkeep);
            string key;

            if (count == 0)
                key = "staff.none";
            else if (Paused)
                key = "staff.paused";
            else
                key = session.Staff.CanPayUpkeep ? "staff.working" : "staff.unfunded";

            if (statusLabel.key == key)
                return;

            statusLabel.key = key;
            statusLabel.Refresh();
        }

        /// <summary>Se crea un círculo desde el prefab y se guarda su próximo destino.</summary>
        void AddWorker()
        {
            RectTransform worker = Instantiate(workerPrefab, movementArea, false);
            worker.anchoredPosition = RandomPoint();
            motions.Add(new() { rect = worker, target = RandomPoint() });
        }

        /// <summary>Se quitan solo los círculos de esta vista que ya no hagan falta.</summary>
        void RemoveWorkers(int count)
        {
            for (int index = motions.Count - 1; index >= count; index--)
            {
                GameObject worker = motions[index].rect.gameObject;
                worker.SetActive(false);
                Destroy(worker);
                motions.RemoveAt(index);
            }
        }

        /// <summary>El centro de cada círculo se mueve en línea recta hasta su próximo destino.</summary>
        void Update()
        {
            if (Paused)
                return;

            float step = speed * Time.deltaTime;

            if (step <= 0)
                return;

            for (int index = 0; index < motions.Count; index++)
            {
                Motion motion = motions[index];
                Vector2 position = Vector2.MoveTowards(motion.rect.anchoredPosition, motion.target, step);
                motion.rect.anchoredPosition = position;

                if (position != motion.target)
                    continue;

                motion.target = RandomPoint();
                motions[index] = motion;
            }
        }

        /// <summary>Se elige un destino dentro del rectángulo del recinto.</summary>
        Vector2 RandomPoint()
        {
            return new(Random.Range(bounds.xMin, bounds.xMax), Random.Range(bounds.yMin, bounds.yMax));
        }
    }
}
