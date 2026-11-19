using System.Collections.Generic;
using IdleClickerPeriodicTable.Core;
using UnityEngine;

namespace IdleClickerPeriodicTable.Presentation
{
    /// <summary>
    /// El personal aparece como círculos dentro de un recinto, su movimiento es solo visual.
    /// </summary>
    public sealed class StaffRoomView : MonoBehaviour
    {
        /// <summary>
        /// Si está activado se muestran investigadores, si no, ingenieros.
        /// </summary>
        public bool researchers;

        /// <summary>
        /// El recinto bajo el menú, los círculos deben quedar enteros dentro de sus límites.
        /// </summary>
        public RectTransform movementArea;

        /// <summary>
        /// El prefab de cada círculo, es la única parte de esta interfaz que se crea durante la partida.
        /// </summary>
        public RectTransform workerPrefab;

        /// <summary>
        /// La velocidad de los círculos en unidades locales del recinto por segundo.
        /// </summary>
        [Min(0)] public float speed = 65;

        /// <summary>
        /// El espacio extra que queda entre los círculos y el borde del recinto.
        /// </summary>
        [Min(0)] public float padding = 8;

        /// <summary>
        /// Los textos con la cantidad, el mantenimiento de esta profesión y el estado de toda la plantilla.
        /// </summary>
        public LocalizedText countLabel, upkeepLabel, statusLabel;

        /// <summary>
        /// Los datos de cada círculo y su recorrido se guardan para no calcularlos de nuevo en cada frame.
        /// </summary>
        private struct Motion
        {
            /// <summary>
            /// El círculo creado desde el prefab de esta profesión.
            /// </summary>
            public RectTransform rect;
            /// <summary>
            /// El siguiente destino, en coordenadas locales del recinto.
            /// </summary>
            public Vector2 target;
            /// <summary>
            /// La zona por la que puede moverse su centro, se recalcula si cambia el tamaño del recinto.
            /// </summary>
            public Rect bounds;
        }

        private readonly List<Motion> motions = new List<Motion>();
        private GameSession session;
        private GameState displayedState;
        private Rect areaRect;

        /// <summary>
        /// Al activar la vista, se reutilizan sus círculos y se empieza a escuchar cambios de cantidad.
        /// </summary>
        private void OnEnable()
        {
            session = GameSession.Instance;
            if (session == null || movementArea == null || workerPrefab == null || countLabel == null ||
                upkeepLabel == null || statusLabel == null)
            {
                Debug.LogError("Faltan referencias del recinto de personal.", this);
                enabled = false;
                return;
            }

            areaRect = movementArea.rect;
            session.Changed += Refresh;
            Refresh();
            UpdateBounds();
        }

        /// <summary>
        /// Se deja de escuchar a la sesión, los círculos quedan para cuando se active la vista otra vez.
        /// </summary>
        private void OnDisable()
        {
            if (session != null)
                session.Changed -= Refresh;
        }

        /// <summary>
        /// Se ajusta la cantidad de círculos, solo se crean los que falten tras una contratación.
        /// </summary>
        private void Refresh()
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
            if (session.State.researchers == 0 && session.State.engineers == 0)
                key = "staff.none";
            else
                key = session.Staff.CanPayUpkeep ? "staff.working" : "staff.unfunded";

            if (statusLabel.key == key)
                return;

            statusLabel.key = key;
            statusLabel.Refresh();
        }

        /// <summary>
        /// Se crea un círculo desde el prefab y se guarda su próximo destino.
        /// </summary>
        private void AddWorker()
        {
            RectTransform worker = Instantiate(workerPrefab, movementArea, false);
            worker.anchorMin = worker.anchorMax = movementArea.pivot;
            Rect bounds = AllowedBounds(worker);
            worker.anchoredPosition = RandomPoint(bounds);
            worker.gameObject.SetActive(true);
            motions.Add(new Motion { rect = worker, target = RandomPoint(bounds), bounds = bounds });
        }

        /// <summary>
        /// Se quitan solo los círculos de esta vista que ya no hagan falta.
        /// </summary>
        private void RemoveWorkers(int count)
        {
            for (int index = motions.Count - 1; index >= count; index--)
            {
                if (motions[index].rect != null)
                {
                    motions[index].rect.gameObject.SetActive(false);
                    Destroy(motions[index].rect.gameObject);
                }
                motions.RemoveAt(index);
            }
        }

        /// <summary>
        /// Los círculos se mueven por el recinto y sus límites se ajustan si cambia el tamaño.
        /// </summary>
        private void Update()
        {
            Rect currentRect = movementArea.rect;
            if (!areaRect.Equals(currentRect))
            {
                areaRect = currentRect;
                UpdateBounds();
            }

            float step = Mathf.Max(0, speed) * Time.deltaTime;
            if (step <= 0)
                return;

            for (int index = 0; index < motions.Count; index++)
            {
                Motion motion = motions[index];
                Vector2 position = motion.rect.anchoredPosition;
                if ((position - motion.target).sqrMagnitude <= 0.25f)
                    motion.target = RandomPoint(motion.bounds);
                motion.rect.anchoredPosition = Clamp(Vector2.MoveTowards(position, motion.target, step), motion.bounds);
                // Motion es una estructura, hay que guardar la copia para conservar el cambio de destino.
                motions[index] = motion;
            }
        }

        /// <summary>
        /// Los destinos y las posiciones se ajustan al nuevo tamaño para que los círculos no sobresalgan.
        /// </summary>
        private void UpdateBounds()
        {
            for (int index = 0; index < motions.Count; index++)
            {
                Motion motion = motions[index];
                motion.bounds = AllowedBounds(motion.rect);
                motion.target = Clamp(motion.target, motion.bounds);
                motion.rect.anchoredPosition = Clamp(motion.rect.anchoredPosition, motion.bounds);
                motions[index] = motion;
            }
        }

        /// <summary>
        /// Se deja sitio para el tamaño del círculo, su pivote y el margen, si no cabe en un eje, se centra.
        /// </summary>
        private Rect AllowedBounds(RectTransform worker)
        {
            Rect area = movementArea.rect;
            Vector2 size = worker.rect.size;
            Vector3 scale = worker.localScale;
            size.x *= Mathf.Abs(scale.x);
            size.y *= Mathf.Abs(scale.y);
            float margin = Mathf.Max(0, padding);
            float minX = area.xMin + size.x * worker.pivot.x + margin;
            float maxX = area.xMax - size.x * (1 - worker.pivot.x) - margin;
            float minY = area.yMin + size.y * worker.pivot.y + margin;
            float maxY = area.yMax - size.y * (1 - worker.pivot.y) - margin;
            if (minX > maxX)
                minX = maxX = area.center.x;

            if (minY > maxY)
                minY = maxY = area.center.y;

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        /// <summary>
        /// Se elige un destino dentro de la zona disponible para el centro del círculo.
        /// </summary>
        private static Vector2 RandomPoint(Rect bounds)
        {
            return new Vector2(Random.Range(bounds.xMin, bounds.xMax), Random.Range(bounds.yMin, bounds.yMax));
        }

        /// <summary>
        /// El punto se deja dentro de los límites del recinto.
        /// </summary>
        private static Vector2 Clamp(Vector2 point, Rect bounds)
        {
            return new Vector2(Mathf.Clamp(point.x, bounds.xMin, bounds.xMax), Mathf.Clamp(point.y, bounds.yMin, bounds.yMax));
        }
    }
}
