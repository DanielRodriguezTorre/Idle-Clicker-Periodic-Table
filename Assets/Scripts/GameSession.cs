using System;
using IdleClickerPeriodicTable.Core;
using IdleClickerPeriodicTable.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleClickerPeriodicTable
{
    /// <summary>
    /// La partida y su reloj se conservan al cambiar de pantalla, pero la interfaz no.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameSession : MonoBehaviour
    {
        /// <summary>
        /// La misma sesión para todas las pantallas.
        /// </summary>
        public static GameSession Instance { get; private set; }

        /// <summary>
        /// Los valores de producción y los costes del personal que se pueden ajustar desde el proyecto.
        /// </summary>
        public GameBalance balance;

        /// <summary>
        /// Cada cuántos segundos reales se guarda automáticamente mientras hay una partida en marcha.
        /// </summary>
        [Min(1)] public float autosaveSeconds = 30;

        /// <summary>
        /// Los datos que luego van al archivo de la partida.
        /// </summary>
        public GameState State { get; private set; }

        /// <summary>
        /// Quien hace las cuentas de los clics y la producción de esta partida.
        /// </summary>
        public EconomyService Economy { get; private set; }

        /// <summary>
        /// Aquí se llevan las cuentas de contratación, mantenimiento y producción del personal.
        /// </summary>
        public StaffService Staff { get; private set; }

        /// <summary>
        /// Indica si hay una partida en marcha.
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// Si el último guardado falló, la interfaz puede avisar sin parar la partida.
        /// </summary>
        public bool SaveFailed { get; private set; }

        /// <summary>
        /// Se avisa cuando cambian los datos, así los contadores no se refrescan en cada frame.
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// Se avisa de cada segundo de producción para que salgan los textos flotantes.
        /// </summary>
        public event Action ProductionTick;

        private double secondTimer;
        private float saveTimer;

        // La referencia se limpia al entrar en Play, incluso si Unity no recarga el dominio.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSessionReference() => Instance = null;

        private void Awake()
        {
            // Cada escena trae una sesión en su prefab, pero solo debe quedar la primera.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            GamePreferences.Load();
            State = new GameState();

            // Esto permite probar una pantalla directamente desde el editor sin pasar por Inicio.
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName != "Inicio")
            {
                if (GameSaveStore.TryLoad(out GameState saved)) 
                    State = saved;

                IsPlaying = sceneName != "Configuracion";
            }

            Economy = new EconomyService(State, balance);
            Staff = new StaffService(State, balance);
        }

        private void Update()
        {
            if (!IsPlaying) 
                return;

            secondTimer += Time.deltaTime;

            // Si un frame tarda más de un segundo, se cobran todos los segundos completos pendientes.
            while (secondTimer >= 1)
            {
                secondTimer -= 1;
                // Primero entra el dinero de las máquinas, después se comprueba si alcanza para el personal.
                Economy.Tick();
                Staff.Tick();
                Changed?.Invoke();
                ProductionTick?.Invoke();
            }

            // El guardado lleva su propio reloj: cambiar la velocidad del juego no cambia este intervalo.
            saveTimer += Time.unscaledDeltaTime;

            if (saveTimer < autosaveSeconds) 
                return;

            saveTimer = 0;
            Save();
        }

        /// <summary>
        /// Se empieza de cero y se comprueba que el guardado funciona antes de entrar en la partida.
        /// </summary>
        public bool NewGame()
        {
            State = new GameState();
            Economy = new EconomyService(State, balance);
            Staff = new StaffService(State, balance);
            secondTimer = 0;
            saveTimer = 0;
            IsPlaying = true;

            if (Save()) 
                return true;

            // Si el guardado falla, la partida nueva no debe seguir corriendo en el menú.
            IsPlaying = false;
            return false;
        }

        /// <summary>
        /// Se retoma lo guardado tal cual, el tiempo con el juego cerrado no da producción extra.
        /// </summary>
        public bool ContinueGame()
        {
            if (!GameSaveStore.TryLoad(out GameState saved)) 
                return false;

            State = saved;
            Economy = new EconomyService(State, balance);
            Staff = new StaffService(State, balance);
            secondTimer = 0;
            saveTimer = 0;
            IsPlaying = true;
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Se cobra el clic y se avisa a las pantallas para que actualicen sus números.
        /// </summary>
        public void Click()
        {
            if (!IsPlaying) 
                return;

            Economy.Click();
            Changed?.Invoke();
        }

        /// <summary>
        /// Se contrata un investigador si alcanza el dinero y se avisa a las pantallas del cambio.
        /// </summary>
        public void HireResearcher()
        {
            if (IsPlaying && Staff.HireResearcher()) 
                Changed?.Invoke();
        }

        /// <summary>
        /// Se contrata un ingeniero si alcanza el dinero y se avisa a las pantallas del cambio.
        /// </summary>
        public void HireEngineer()
        {
            if (IsPlaying && Staff.HireEngineer()) 
                Changed?.Invoke();
        }

        /// <summary>
        /// Solo se guarda si hay una partida en marcha, en el menú no hay nada que guardar.
        /// </summary>
        public bool Save()
        {
            if (!IsPlaying) 
                return true;

            SaveFailed = !GameSaveStore.TrySave(State);
            Changed?.Invoke();
            return !SaveFailed;
        }

        /// <summary>
        /// Antes de volver al inicio se guarda y se para la partida, si falla, no se cambia de pantalla.
        /// </summary>
        public bool ReturnToTitle()
        {
            if (!Save()) 
                return false;

            IsPlaying = false;
            return true;
        }

        private void OnApplicationQuit() => Save();

        private void OnApplicationPause(bool paused)
        {
            // La partida también se guarda cuando la aplicación entra en pausa.
            if (paused) 
                Save();
        }

        private void OnDestroy()
        {
            if (Instance == this) 
                Instance = null;
        }
    }
}
