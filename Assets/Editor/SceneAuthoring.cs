using System;
using System.IO;
using IdleClickerPeriodicTable;
using IdleClickerPeriodicTable.Core;
using IdleClickerPeriodicTable.Presentation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Las pantallas se montan desde el editor y la interfaz queda guardada en escenas y prefabs.
/// </summary>
public static class SceneAuthoring
{
    private const string SceneFolder = "Assets/Scenes/";
    private const string PrefabFolder = "Assets/Prefabs/";
    private static readonly string[] Scenes = { "Inicio", "Economia", "TablaPeriodica", "Investigacion", "Ingenieria", "Produccion", "Configuracion" };
    private static readonly string[] Tabs = { "Investigacion", "TablaPeriodica", "Ingenieria", "Economia", "Produccion" };
    private static readonly string[] TabKeys = { "nav.research", "nav.table", "nav.engineering", "nav.economy", "nav.production" };
    private static readonly Color Ink = new Color(0.08f, 0.09f, 0.12f);
    private static readonly Color[] TabColors =
    {
        new Color(0.71f, 0.83f, 1), new Color(0.75f, 0.9f, 0.77f),
        new Color(1, 0.85f, 0.65f), new Color(1, 0.95f, 0.62f), new Color(0.88f, 0.77f, 1)
    };
    private static Font font;
    private static Sprite buttonSprite;
    private static GameObject sessionPrefab;
    private static GameObject menuPrefab;
    private static bool resumeCreation;

    /// <summary>
    /// Se retoma una creación que se quedó a medias, permitiendo guardar otra vez en las mismas rutas.
    /// </summary>
    public static void ResumeCreate()
    {
        resumeCreation = true;
        Create();
    }

    /// <summary>
    /// Se crean las pantallas iniciales sin pisar escenas existentes, salvo al retomar una creación interrumpida.
    /// </summary>
    [MenuItem("Tools/Idle Clicker/Crear pantallas iniciales")]
    public static void Create()
    {
        // Las rutas se comprueban antes de crear nada para no llevarse por delante una escena ya diseñada.
        foreach (string scene in Scenes)
            if (!resumeCreation && File.Exists(SceneFolder + scene + ".unity"))
                throw new InvalidOperationException("La escena ya existe: " + scene + ". No se sobrescribirá.");

        Directory.CreateDirectory(PrefabFolder);
        Directory.CreateDirectory("Assets/Settings");
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        CreateSessionPrefab();
        CreateMenuPrefab();
        CreateStartScene();
        CreateEconomyScene();

        for (int index = 1; index < Tabs.Length; index++)
            if (Tabs[index] != "Economia") 
                CreatePendingScene(Tabs[index], TabKeys[index]);

        CreatePendingScene("Investigacion", "nav.research");
        CreateSettingsScene();
        EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[Scenes.Length];

        for (int index = 0; index < Scenes.Length; index++)
            buildScenes[index] = new EditorBuildSettingsScene(SceneFolder + Scenes[index] + ".unity", true);

        EditorBuildSettings.scenes = buildScenes;
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(SceneFolder + "Inicio.unity");
        Verify();
        Debug.Log("Pantallas creadas y guardadas. Toda la interfaz está serializada.");
    }
    
    /// <summary>
    /// Se prepara el prefab que llevará la sesión de partida en cada escena.
    /// </summary>
    private static void CreateSessionPrefab()
    {
        GameBalance balance = AssetDatabase.LoadAssetAtPath<GameBalance>("Assets/Settings/GameBalance.asset");

        // Si ya hay un balance ajustado, se reutiliza en vez de volver a los valores por defecto.
        if (balance == null)
        {
            balance = ScriptableObject.CreateInstance<GameBalance>();
            AssetDatabase.CreateAsset(balance, "Assets/Settings/GameBalance.asset");
        }

        GameObject root = new GameObject("SesionDePartida");
        GameSession session = root.AddComponent<GameSession>();
        session.balance = balance;
        sessionPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "SesionDePartida.prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }
    
    /// <summary>
    /// Se monta una sola vez el menú de pestañas y recursos que comparten las pantallas.
    /// </summary>
    private static void CreateMenuPrefab()
    {
        RectTransform root = Rect("MenuComun", null, Vector2.zero, new Vector2(1920, 190));
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 1);
        root.pivot = new Vector2(0.5f, 1);
        root.anchoredPosition = Vector2.zero;

        for (int index = 0; index < Tabs.Length; index++)
        {
            Button button = Button("Pestana" + Tabs[index], root, TabKeys[index], -805 + index * 270, -46, 254, 80, 21);
            SetColor(button.image, TabColors[index]);
            button.image.GetComponent<AccessibleGraphic>().paletteIndex = index;
            Navigate(button, Tabs[index]);
        }

        Button settings = Button("Configuracion", root, "nav.settings", 565, -46, 240, 68, 21);
        Navigate(settings, "Configuracion");
        Button home = Button("Inicio", root, "nav.home", 810, -46, 230, 68, 21);
        Navigate(home, "Inicio");
        ResourceCounters counters = root.gameObject.AddComponent<ResourceCounters>();
        counters.money = Label("Dinero", root, "resource.money", -600, -128, 590, 60, 26);
        counters.science = Label("Ciencia", root, "resource.science", 0, -128, 590, 60, 26);
        counters.ingenuity = Label("Ingenio", root, "resource.ingenuity", 600, -128, 590, 60, 26);
        LocalizedText warning = Label("AvisoGuardado", root, "save.error", 0, -177, 1700, 32, 20);
        counters.saveWarning = warning.gameObject;
        warning.gameObject.SetActive(false);
        AlignMenuChildren(root);
        menuPrefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabFolder + "MenuComun.prefab");
        UnityEngine.Object.DestroyImmediate(root.gameObject);
    }

    /// <summary>
    /// Se corrigen los anclajes del prefab del menú sin tener que reconstruirlo entero.
    /// </summary>
    public static void FixMenuAlignment()
    {
        string path = PrefabFolder + "MenuComun.prefab";
        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        AlignMenuChildren((RectTransform)contents.transform);
        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
        AssetDatabase.SaveAssets();
    }
    
    /// <summary>
    /// Los controles se anclan arriba del menú, conservando la posición que ya tenían.
    /// </summary>
    private static void AlignMenuChildren(RectTransform menu)
    {
        foreach (RectTransform child in menu)
        {
            Vector2 position = child.anchoredPosition;
            child.anchorMin = child.anchorMax = new Vector2(0.5f, 1);
            child.anchoredPosition = position;

            // Los botones y sus etiquetas necesitan una altura uniforme para quedar alineados.
            if (child.TryGetComponent(out Button button))
            {
                child.sizeDelta = new Vector2(child.sizeDelta.x, 80);
                RectTransform label = button.GetComponentInChildren<LocalizedText>(true).target.rectTransform;
                label.sizeDelta = new Vector2(label.sizeDelta.x, 68);
            }
        }
    }
    
    /// <summary>
    /// Se crea una escena vacía con cámara, sesión y lienzo, el menú común es opcional.
    /// </summary>
    private static RectTransform BeginScene(string name, bool withMenu)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = name;
        CreateCamera();
        PrefabUtility.InstantiatePrefab(sessionPrefab, scene);
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform root = (RectTransform)canvas.transform;
        RectTransform background = Rect("Fondo", root, Vector2.zero, new Vector2(1920, 1080));
        Image backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.raycastTarget = false;
        SetColor(backgroundImage, new Color(0.93f, 0.94f, 0.96f));
        CreateEventSystem();

        // Las pantallas de juego llevan el menú común, Inicio y Configuración van por su cuenta.
        if (withMenu)
        {
            GameObject menu = (GameObject)PrefabUtility.InstantiatePrefab(menuPrefab, scene);
            menu.transform.SetParent(root, false);
        }

        return root;
    }

    /// <summary>
    /// Se prepara una cámara ortográfica para la primera pantalla desde el editor.
    /// </summary>
    private static void CreateCamera()
    {
        GameObject root = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData));
        root.tag = "MainCamera";
        root.transform.position = new Vector3(0, 0, -10);
        Camera camera = root.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5;
        camera.targetDisplay = 0;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.93f, 0.94f, 0.96f);
        camera.allowHDR = false;
        camera.allowMSAA = false;
        root.GetComponent<UniversalAdditionalCameraData>().renderShadows = false;
    }

    /// <summary>
    /// Se añade una cámara a las escenas que no tengan, sin volver a montar su interfaz.
    /// </summary>
    public static void RepairCameras()
    {
        foreach (string name in Scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(SceneFolder + name + ".unity");
            bool hasCamera = false;

            // También cuentan las cámaras inactivas para no crear otra si ya hay una guardada.
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<Camera>(true) != null) 
                    hasCamera = true;

            if (hasCamera) 
                continue;

            CreateCamera();
            SaveScene(name);
        }

        Verify();
    }

    /// <summary>
    /// El EventSystem queda conectado a las acciones de UI para que los botones respondan.
    /// </summary>
    private static void CreateEventSystem()
    {
        const string actionsPath = "Assets/InputSystem_Actions.inputactions";
        GameObject root = new GameObject("EventSystem");
        // Se activa al final, cuando el módulo ya tiene sus acciones asignadas.
        root.SetActive(false);
        root.AddComponent<EventSystem>();
        InputSystemUIInputModule module = root.AddComponent<InputSystemUIInputModule>();
        SerializedObject serialized = new SerializedObject(module);
        serialized.FindProperty("m_ActionsAsset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>(actionsPath);
        // Cada acción y su campo del módulo tienen que ocupar la misma posición en estas dos listas.
        string[] actions = { "Point", "Navigate", "Submit", "Cancel", "Click", "RightClick", "MiddleClick", "ScrollWheel", "TrackedDevicePosition", "TrackedDeviceOrientation" };
        string[] fields = { "m_PointAction", "m_MoveAction", "m_SubmitAction", "m_CancelAction", "m_LeftClickAction", "m_RightClickAction", "m_MiddleClickAction", "m_ScrollWheelAction", "m_TrackedDevicePositionAction", "m_TrackedDeviceOrientationAction" };
        UnityEngine.Object[] references = AssetDatabase.LoadAllAssetsAtPath(actionsPath);
        
        for (int index = 0; index < actions.Length; index++)
        {
            foreach (UnityEngine.Object reference in references)
            {
                InputActionReference action = reference as InputActionReference;

                if (action == null || action.action == null || action.action.actionMap.name != "UI" || action.action.name != actions[index]) 
                    continue;

                serialized.FindProperty(fields[index]).objectReferenceValue = action;
                break;
            }
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        root.SetActive(true);
    }
    
    /// <summary>
    /// Se monta el inicio con sus botones, los mensajes de error y la confirmación de partida nueva.
    /// </summary>
    private static void CreateStartScene()
    {
        RectTransform root = BeginScene("Inicio", false);
        StartMenu controller = root.gameObject.AddComponent<StartMenu>();
        Label("Titulo", root, "game.title", 0, 330, 1700, 140, 48);
        controller.continueButton = Button("Continuar", root, "menu.continue", 0, 130, 660, 82, 30);
        UnityEventTools.AddPersistentListener(controller.continueButton.onClick, controller.ContinueGame);
        Button newGame = Button("NuevaPartida", root, "menu.new", 0, 15, 660, 82, 30);
        UnityEventTools.AddPersistentListener(newGame.onClick, controller.NewGame);
        Button settings = Button("Configuracion", root, "nav.settings", 0, -100, 660, 82, 30);
        Navigate(settings, "Configuracion");
        Button quit = Button("Salir", root, "menu.quit", 0, -215, 660, 82, 30);
        UnityEventTools.AddPersistentListener(quit.onClick, controller.Quit);
        controller.errorMessage = Label("Error", root, "save.error", 0, -400, 1700, 140, 24);
        controller.errorMessage.gameObject.SetActive(false);
        RectTransform overlay = Rect("ConfirmacionNuevaPartida", root, Vector2.zero, new Vector2(1920, 1080));
        Image dim = overlay.gameObject.AddComponent<Image>();
        SetColor(dim, new Color(0, 0, 0, 0.65f));
        RectTransform panel = Rect("Panel", overlay, Vector2.zero, new Vector2(1320, 480));
        Image panelImage = panel.gameObject.AddComponent<Image>();
        SetColor(panelImage, new Color(0.96f, 0.96f, 0.96f));
        Label("Aviso", panel, "menu.confirm_new", 0, 75, 1180, 200, 29);
        Button confirm = Button("Confirmar", panel, "common.confirm", -300, -135, 500, 90, 27);
        Button cancel = Button("Cancelar", panel, "common.cancel", 300, -135, 500, 90, 27);
        UnityEventTools.AddPersistentListener(confirm.onClick, controller.ConfirmNewGame);
        UnityEventTools.AddPersistentListener(cancel.onClick, controller.CancelNewGame);
        controller.confirmationPanel = overlay.gameObject;
        overlay.gameObject.SetActive(false);
        SaveScene("Inicio");
    }

    /// <summary>
    /// Se monta Economía con su zona de clic, las cuatro familias y los textos de producción.
    /// </summary>
    private static void CreateEconomyScene()
    {
        RectTransform root = BeginScene("Economia", true);
        EconomyView view = root.gameObject.AddComponent<EconomyView>();
        Button clickArea = Button("AreaDeClic", root, "economy.click", 0, -80, 1800, 800, 35);
        UnityEventTools.AddPersistentListener(clickArea.onClick, view.Click);
        RectTransform instruction = (RectTransform)clickArea.transform.GetChild(0);
        instruction.anchoredPosition = new Vector2(0, 245);
        instruction.sizeDelta = new Vector2(1700, 130);
        view.clickValue = Label("ValorDelClic", clickArea.transform, "economy.click_value", 0, 145, 1500, 80, 29);
        view.machineCounts = new LocalizedText[4];
        view.machineRates = new LocalizedText[4];
        view.floatingTexts = new LocalizedText[4];
        view.floatingGroups = new CanvasGroup[4];

        for (int index = 0; index < 4; index++)
        {
            float x = -645 + index * 430;
            RectTransform card = Rect("Maquina" + (index + 1), clickArea.transform, new Vector2(x, -150), new Vector2(410, 420));
            Label("Nombre", card, "machine." + (index + 1), 0, 160, 400, 90, 23);
            RectTransform square = Rect("Imagen", card, new Vector2(0, 30), new Vector2(110, 110));
            Image image = square.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            SetColor(image, TabColors[index]);
            image.GetComponent<AccessibleGraphic>().paletteIndex = index;
            view.machineCounts[index] = Label("Cantidad", card, "economy.machine_count", 0, -70, 400, 65, 23);
            view.machineRates[index] = Label("Produccion", card, "economy.machine_rate", 0, -145, 400, 100, 22);
            LocalizedText floating = Label("IngresoFlotante", card, "economy.gain", 0, 80, 400, 65, 30);
            view.floatingTexts[index] = floating;
            CanvasGroup group = floating.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0;
            // El texto flotante no debe quedarse con los clics que van a la zona de debajo.
            group.blocksRaycasts = false;
            group.interactable = false;
            view.floatingGroups[index] = group;
        }

        SaveScene("Economia");
    }
    
    /// <summary>
    /// La pantalla queda con un título provisional mientras esa parte del juego sigue pendiente.
    /// </summary>
    private static void CreatePendingScene(string name, string titleKey)
    {
        RectTransform root = BeginScene(name, true);
        Label("Titulo", root, titleKey, 0, 100, 1500, 130, 45);
        Label("Pendiente", root, "common.pending", 0, -100, 1500, 110, 30);
        SaveScene(name);
    }
    
    /// <summary>
    /// Se montan las opciones de idioma, tamaño de texto y paleta, junto con el botón para volver.
    /// </summary>
    private static void CreateSettingsScene()
    {
        RectTransform root = BeginScene("Configuracion", false);
        SettingsView view = root.gameObject.AddComponent<SettingsView>();
        Label("Titulo", root, "settings.title", 0, 430, 1700, 100, 44);
        Label("Idioma", root, "settings.language", 0, 310, 1700, 70, 29);
        string[] languages = { "es", "en", "de", "da", "fr", "it" };
        view.languageOutlines = new Outline[6];

        for (int index = 0; index < 6; index++)
        {
            Button option = Button("Idioma" + languages[index], root, "language." + languages[index], -750 + index * 300, 225, 280, 80, 25);
            UnityEventTools.AddIntPersistentListener(option.onClick, view.SetLanguage, index);
            view.languageOutlines[index] = AddOutline(option);
        }

        Label("TamanoTexto", root, "settings.text_size", 0, 95, 1700, 70, 29);
        string[] sizes = { "size.small", "size.normal", "size.large" };
        view.sizeOutlines = new Outline[3];

        for (int index = 0; index < 3; index++)
        {
            Button option = Button("Tamano" + index, root, sizes[index], -500 + index * 500, 10, 460, 80, 26);
            UnityEventTools.AddIntPersistentListener(option.onClick, view.SetTextSize, index);
            view.sizeOutlines[index] = AddOutline(option);
        }

        Label("ModoColor", root, "settings.color_mode", 0, -120, 1700, 70, 29);
        string[] modes = { "color.normal", "color.protanopia", "color.deuteranopia", "color.tritanopia" };
        view.colorOutlines = new Outline[4];

        for (int index = 0; index < 4; index++)
        {
            Button option = Button("Color" + index, root, modes[index], -660 + index * 440, -205, 410, 85, 24);
            UnityEventTools.AddIntPersistentListener(option.onClick, view.SetColorMode, index);
            view.colorOutlines[index] = AddOutline(option);
        }

        Button back = Button("Volver", root, "common.back", 0, -410, 580, 90, 29);
        SceneNavigation navigation = back.gameObject.AddComponent<SceneNavigation>();
        UnityEventTools.AddPersistentListener(back.onClick, navigation.Back);
        SaveScene("Configuracion");
    }
    
    /// <summary>
    /// Se crea un elemento de UI centrado con el padre, la posición y el tamaño indicados.
    /// </summary>
    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject item = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)item.transform;

        if (parent != null) 
            rect.SetParent(parent, false);

        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
    
    /// <summary>
    /// Se crea una etiqueta conectada a su traducción y a los ajustes de tamaño.
    /// </summary>
    private static LocalizedText Label(string name, Transform parent, string key, float x, float y, float width, float height, int fontSize)
    {
        RectTransform rect = Rect(name, parent, new Vector2(x, y), new Vector2(width, height));
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        // Las etiquetas solo muestran texto, los clics los recoge el botón que tengan debajo.
        text.raycastTarget = false;
        text.text = key;
        SetColor(text, Ink);
        LocalizedText localized = rect.gameObject.AddComponent<LocalizedText>();
        localized.target = text;
        localized.key = key;
        localized.baseFontSize = fontSize;
        return localized;
    }

    /// <summary>
    /// Se crea un botón con su imagen y etiqueta traducible, la acción se conecta después.
    /// </summary>
    private static Button Button(string name, Transform parent, string key, float x, float y, float width, float height, int fontSize)
    {
        RectTransform rect = Rect(name, parent, new Vector2(x, y), new Vector2(width, height));
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = buttonSprite;
        image.type = Image.Type.Sliced;
        SetColor(image, new Color(0.84f, 0.86f, 0.89f));
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f);
        button.colors = colors;
        Label("Texto", rect, key, 0, 0, width - 24, height - 12, fontSize);
        return button;
    }
    
    /// <summary>
    /// Se asigna el color original y el gráfico queda preparado para cambiar de paleta.
    /// </summary>
    private static void SetColor(Graphic graphic, Color color)
    {
        graphic.color = color;
        AccessibleGraphic accessible = graphic.GetComponent<AccessibleGraphic>();

        if (accessible == null) 
            accessible = graphic.gameObject.AddComponent<AccessibleGraphic>();
        
        accessible.target = graphic;
        accessible.normalColor = color;
    }
    
    /// <summary>
    /// Se añade el borde para marcar un botón como seleccionado, de entrada va apagado.
    /// </summary>
    private static Outline AddOutline(Button button)
    {
        Outline outline = button.gameObject.AddComponent<Outline>();
        outline.effectColor = Ink;
        outline.effectDistance = new Vector2(4, -4);
        outline.enabled = false;
        return outline;
    }
    
    /// <summary>
    /// El botón queda conectado a su pantalla de destino y lleva un borde para marcar la pestaña activa.
    /// </summary>
    private static void Navigate(Button button, string destination)
    {
        SceneNavigation navigation = button.gameObject.AddComponent<SceneNavigation>();
        navigation.sceneName = destination;
        navigation.activeOutline = AddOutline(button);
        // La conexión debe quedar guardada en la escena o el prefab, no solo durante esta ejecución.
        UnityEventTools.AddPersistentListener(button.onClick, navigation.Open);
    }
    
    /// <summary>
    /// Se guarda la escena abierta en su ruta, si no se puede, se lanza una excepción.
    /// </summary>
    private static void SaveScene(string name)
    {
        if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), SceneFolder + name + ".unity"))
            throw new IOException("No se pudo guardar la escena " + name);
    }

    /// <summary>
    /// Se comprueba que no falten cámaras, scripts, traducciones ni acciones de botones, sin recrear la interfaz.
    /// </summary>
    [MenuItem("Tools/Idle Clicker/Validar pantallas")]
    public static void Verify()
    {
        int labels = 0;

        foreach (string name in Scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(SceneFolder + name + ".unity");
            int activeCameras = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                    if (camera.isActiveAndEnabled && camera.targetDisplay == 0 && camera.targetTexture == null)
                        activeCameras++;

                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                        throw new InvalidOperationException("Script perdido en " + name + "/" + child.name);

                foreach (LocalizedText label in root.GetComponentsInChildren<LocalizedText>(true))
                {
                    if (label.target == null || GamePreferences.Translate(label.key) == label.key)
                        throw new InvalidOperationException("Texto sin referencia o traducción: " + name + "/" + label.key);

                    labels++;
                }

                foreach (AccessibleGraphic graphic in root.GetComponentsInChildren<AccessibleGraphic>(true))
                    if (graphic.target == null)
                        throw new InvalidOperationException("Color sin referencia: " + name);

                foreach (Button button in root.GetComponentsInChildren<Button>(true))
                    if (button.onClick.GetPersistentEventCount() == 0)
                        throw new InvalidOperationException("Botón sin acción: " + name + "/" + button.name);
            }

            if (activeCameras != 1)
                throw new InvalidOperationException("La escena debe tener una cámara activa para la pantalla 1: " + name);
        }

        EditorSceneManager.OpenScene(SceneFolder + "Inicio.unity");
        Debug.Log("Validación correcta: " + Scenes.Length + " escenas y " + labels + " textos localizados.");
    }

    /// <summary>
    /// Se genera una compilación de desarrollo para Windows en una carpeta aparte del proyecto Unity.
    /// </summary>
    public static void BuildWindows()
    {
        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Build/Windows"));
        Directory.CreateDirectory(directory);
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = Array.ConvertAll(Scenes, scene => SceneFolder + scene + ".unity"),
            locationPathName = Path.Combine(directory, "IdleClickerPeriodicTable.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        };
        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new InvalidOperationException("La compilación Windows ha fallado.");
    }
}
