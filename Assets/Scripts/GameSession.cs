using System;
using IdleClickerPeriodicTable.Core;
using IdleClickerPeriodicTable.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleClickerPeriodicTable
{
    /// <summary>La partida y su reloj se conservan al cambiar de pantalla, pero la interfaz no.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameSession : MonoBehaviour
    {
        /// <summary>La misma sesión para todas las pantallas.</summary>
        public static GameSession Instance { get; private set; }

        /// <summary>Los valores de producción y los costes del personal que se pueden ajustar desde el proyecto.</summary>
        public GameBalance balance;

        /// <summary>Cada cuántos segundos reales se guarda automáticamente mientras hay una partida en marcha.</summary>
        [Min(1)] public float autosaveSeconds = 30;

        /// <summary>Los datos que luego van al archivo de la partida.</summary>
        public GameState State { get; private set; }

        /// <summary>Quien hace las cuentas de los clics y la producción de esta partida.</summary>
        public EconomyService Economy { get; private set; }

        /// <summary>Aquí se llevan las cuentas de contratación, mantenimiento y producción del personal.</summary>
        public StaffService Staff { get; private set; }

        /// <summary>Aquí se llevan los descubrimientos, la investigación y la fabricación de automatizadores.</summary>
        public AutomationService Automation { get; private set; }

        /// <summary>Indica si hay una partida en marcha.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>Si el último guardado falló, la interfaz puede avisar sin parar la partida.</summary>
        public bool SaveFailed { get; private set; }

        /// <summary>Se avisa cuando cambian los datos, así los contadores no se refrescan en cada frame.</summary>
        public event Action Changed;

        /// <summary>Se avisa de cada segundo de producción para que salgan los textos flotantes.</summary>
        public event Action ProductionTick;

        /// <summary>Se avisa cuando aparece al menos un descubrimiento científico nuevo.</summary>
        public event Action NewScientificDiscoveries;

        double secondTimer;
        float saveTimer;
        bool discoveryNoticePending;

        // La referencia se limpia al entrar en Play, incluso si Unity no recarga el dominio.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSessionReference() => Instance = null;

        void Awake()
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
            State = new();

            // Esto permite probar una pantalla directamente desde el editor sin pasar por Inicio.
            string sceneName = SceneManager.GetActiveScene().name;

            if (sceneName != "Inicio")
            {
                if (GameSaveStore.TryLoad(out GameState saved)) 
                {
                    State = saved;
                    GamePreferences.LoadStaffWork(State);
                }

                IsPlaying = sceneName != "Configuracion";
            }

            Economy = new(State, balance);
            Staff = new(State, balance);
            Automation = new(State, balance);
            discoveryNoticePending = Automation.CheckDiscoveries();
        }

        void Update()
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
                bool discovered = Automation.CheckDiscoveries();

                if (discovered)
                    discoveryNoticePending = true;

                Changed?.Invoke();

                if (discovered)
                    NewScientificDiscoveries?.Invoke();

                ProductionTick?.Invoke();
            }

            // El guardado lleva su propio reloj: cambiar la velocidad del juego no cambia este intervalo.
            saveTimer += Time.unscaledDeltaTime;

            if (saveTimer < autosaveSeconds) 
                return;

            saveTimer = 0;
            Save();
        }

        /// <summary>Se empieza de cero y se comprueba que el guardado funciona antes de entrar en la partida.</summary>
        public bool NewGame()
        {
            State = new();
            Economy = new(State, balance);
            Staff = new(State, balance);
            Automation = new(State, balance);
            discoveryNoticePending = false;
            secondTimer = 0;
            saveTimer = 0;
            IsPlaying = true;

            if (Save()) 
                return true;

            // Si el guardado falla, la partida nueva no debe seguir corriendo en el menú.
            IsPlaying = false;
            return false;
        }

        /// <summary>Se retoma lo guardado tal cual, el tiempo con el juego cerrado no da producción extra.</summary>
        public bool ContinueGame()
        {
            if (!GameSaveStore.TryLoad(out GameState saved)) 
                return false;

            State = saved;
            GamePreferences.LoadStaffWork(State);
            Economy = new(State, balance);
            Staff = new(State, balance);
            Automation = new(State, balance);
            discoveryNoticePending = Automation.CheckDiscoveries();
            secondTimer = 0;
            saveTimer = 0;
            IsPlaying = true;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Se cobra el clic y se avisa a las pantallas para que actualicen sus números.</summary>
        public void Click()
        {
            if (!IsPlaying) 
                return;

            Economy.Click();
            Changed?.Invoke();
        }

        /// <summary>Se contrata un investigador si alcanza el dinero y se avisa a las pantallas del cambio.</summary>
        public void HireResearcher()
        {
            if (IsPlaying && Staff.HireResearcher()) 
                Changed?.Invoke();
        }

        /// <summary>Se contrata un ingeniero si alcanza el dinero y se avisa a las pantallas del cambio.</summary>
        public void HireEngineer()
        {
            if (IsPlaying && Staff.HireEngineer()) 
                Changed?.Invoke();
        }

        /// <summary>Se pausa o reanuda a los investigadores sin cambiar su cantidad ni su precio de contratación.</summary>
        /// <param name="working">Si es verdadero, los investigadores pueden cobrar y producir.</param>
        public void SetResearchersWorking(bool working)
        {
            if (!IsPlaying || State.researchersPaused == !working)
                return;

            State.researchersPaused = !working;
            Changed?.Invoke();
        }

        /// <summary>Se pausa o reanuda a los ingenieros sin afectar a los investigadores.</summary>
        /// <param name="working">Si es verdadero, los ingenieros pueden cobrar y producir.</param>
        public void SetEngineersWorking(bool working)
        {
            if (!IsPlaying || State.engineersPaused == !working)
                return;

            State.engineersPaused = !working;
            Changed?.Invoke();
        }

        /// <summary>Se investiga un automatizador disponible y se actualizan las pantallas si se ha podido pagar.</summary>
        public bool ResearchAutomation(int index)
        {
            if (!IsPlaying || !Automation.Research(index))
                return false;

            Changed?.Invoke();
            return true;
        }

        /// <summary>Se compra una máquina ya investigada, pagando dinero e ingenio.</summary>
        public bool BuyAutomationMachine(int index)
        {
            if (!IsPlaying || !Automation.BuyMachine(index))
                return false;

            Changed?.Invoke();
            return true;
        }

        /// <summary>El aviso pendiente se entrega una sola vez a la pantalla activa, aunque haya cambiado la escena.</summary>
        public bool TakeDiscoveryNotice()
        {
            bool pending = discoveryNoticePending;
            discoveryNoticePending = false;
            return pending;
        }

        /// <summary>Solo se guarda si hay una partida en marcha, en el menú no hay nada que guardar.</summary>
        public bool Save()
        {
            if (!IsPlaying) 
                return true;

            SaveFailed = !GameSaveStore.TrySave(State);

            if (!SaveFailed)
                GamePreferences.SaveStaffWork(State);

            Changed?.Invoke();
            return !SaveFailed;
        }

        /// <summary>Antes de volver al inicio se guarda y se para la partida, si falla, no se cambia de pantalla.</summary>
        public bool ReturnToTitle()
        {
            if (!Save()) 
                return false;

            IsPlaying = false;
            return true;
        }

        void OnApplicationQuit() => Save();

        void OnApplicationPause(bool paused)
        {
            // La partida también se guarda cuando la aplicación entra en pausa.
            if (paused) 
                Save();
        }

        void OnDestroy()
        {
            if (Instance == this) 
                Instance = null;
        }
    }
}
