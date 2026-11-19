using System;
using IdleClickerPeriodicTable.Core;
using IdleClickerPeriodicTable.Presentation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Los controles de contratación, los recintos y los prefabs del personal quedan preparados antes de jugar.
/// </summary>
public static class StaffSceneAuthoring
{
    /// <summary>
    /// Se añade el personal a las escenas que ya existen, sin perder su interfaz ni sus referencias.
    /// </summary>
    [MenuItem("Tools/Idle Clicker/Preparar personal")]
    public static void Apply()
    {
        SceneAuthoring.LoadUIResources();
        RectTransform researcher = WorkerPrefab("CirculoInvestigador", new Color(0.2f, 0.47f, 0.85f), 0);
        RectTransform engineer = WorkerPrefab("CirculoIngeniero", new Color(0.8f, 0.39f, 0.12f), 2);
        PrepareEconomy();
        PrepareRoom("Investigacion", true, researcher);
        PrepareRoom("Ingenieria", false, engineer);
        EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath<GameBalance>("Assets/Settings/GameBalance.asset"));
        AssetDatabase.SaveAssets();
        Validate();
        SceneAuthoring.Verify();
        EditorSceneManager.OpenScene("Assets/Scenes/Inicio.unity");
    }

    /// <summary>
    /// Si el prefab ya existe se reutiliza, si no, se guarda un círculo con su imagen y sus componentes.
    /// </summary>
    private static RectTransform WorkerPrefab(string name, Color color, int paletteIndex)
    {
        string path = "Assets/Prefabs/" + name + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (existing != null) 
            return (RectTransform)existing.transform;

        RectTransform root = SceneAuthoring.Rect(name, null, Vector2.zero, new Vector2(36, 36));
        Image image = root.gameObject.AddComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        if (image.sprite == null) 
            throw new InvalidOperationException("No se encuentra el sprite circular incorporado.");

        image.raycastTarget = false;
        SceneAuthoring.SetColor(image, color);
        image.GetComponent<AccessibleGraphic>().paletteIndex = paletteIndex;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
        UnityEngine.Object.DestroyImmediate(root.gameObject);
        return (RectTransform)prefab.transform;
    }

    /// <summary>
    /// La parte inferior de Economía queda reservada para las dos profesiones.
    /// </summary>
    private static void PrepareEconomy()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Economia.unity");
        Canvas canvas = FindCanvas(scene);

        // Si el personal ya está preparado, no se vuelven a crear controles ni a mover los existentes.
        if (canvas.GetComponent<StaffPurchaseView>() != null) 
            return;

        EconomyView economy = canvas.GetComponent<EconomyView>();
        RectTransform click = (RectTransform)canvas.transform.Find("AreaDeClic");
        click.anchoredPosition = new Vector2(0, 35);
        click.sizeDelta = new Vector2(1800, 590);
        RectTransform instruction = (RectTransform)click.Find("Texto");
        instruction.anchoredPosition = new Vector2(0, 200);
        instruction.sizeDelta = new Vector2(1700, 90);
        economy.clickValue.target.rectTransform.anchoredPosition = new Vector2(0, 120);
        economy.clickValue.target.rectTransform.sizeDelta = new Vector2(1500, 60);

        for (int index = 0; index < 4; index++)
        {
            RectTransform card = (RectTransform)click.Find("Maquina" + (index + 1));
            card.anchoredPosition = new Vector2(card.anchoredPosition.x, -100);
            card.sizeDelta = new Vector2(410, 280);
            Place((RectTransform)card.Find("Nombre"), 90, 70);
            RectTransform square = (RectTransform)card.Find("Imagen");
            square.anchoredPosition = Vector2.zero;
            square.sizeDelta = new Vector2(70, 70);
            Place(economy.machineCounts[index].target.rectTransform, -70, 45);
            Place(economy.machineRates[index].target.rectTransform, -120, 60);
            Place(economy.floatingTexts[index].target.rectTransform, 45, 50);
        }

        StaffPurchaseView view = canvas.gameObject.AddComponent<StaffPurchaseView>();
        view.researcherCount = SceneAuthoring.Label("CantidadInvestigadores", canvas.transform, "staff.researchers", -450, -320, 840, 50, 24);
        view.engineerCount = SceneAuthoring.Label("CantidadIngenieros", canvas.transform, "staff.engineers", 450, -320, 840, 50, 24);
        view.researcherPrice = SceneAuthoring.Label("PrecioInvestigador", canvas.transform, "staff.next_price", -450, -370, 840, 50, 23);
        view.engineerPrice = SceneAuthoring.Label("PrecioIngeniero", canvas.transform, "staff.next_price", 450, -370, 840, 50, 23);
        view.hireResearcherButton = SceneAuthoring.Button("ContratarInvestigador", canvas.transform, "staff.hire_researcher", -450, -435, 680, 72, 24);
        view.hireEngineerButton = SceneAuthoring.Button("ContratarIngeniero", canvas.transform, "staff.hire_engineer", 450, -435, 680, 72, 24);
        UnityEventTools.AddPersistentListener(view.hireResearcherButton.onClick, view.HireResearcher);
        UnityEventTools.AddPersistentListener(view.hireEngineerButton.onClick, view.HireEngineer);
        view.upkeepLabel = SceneAuthoring.Label("MantenimientoPersonal", canvas.transform, "staff.upkeep", -450, -510, 870, 46, 20);
        view.statusLabel = SceneAuthoring.Label("EstadoPersonal", canvas.transform, "staff.none", 450, -510, 870, 46, 20);
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// Se prepara un recinto separado del menú, con su propio Canvas para el movimiento.
    /// </summary>
    private static void PrepareRoom(string name, bool researchers, RectTransform prefab)
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
        Canvas canvas = FindCanvas(scene);

        if (canvas.GetComponent<StaffRoomView>() != null) 
            return;

        Transform pending = canvas.transform.Find("Pendiente");

        if (pending != null) 
            UnityEngine.Object.DestroyImmediate(pending.gameObject);

        RectTransform title = (RectTransform)canvas.transform.Find("Titulo");
        Place(title, 280, 75);
        RectTransform frame = SceneAuthoring.Rect("RecintoPersonal", canvas.transform, new Vector2(0, -160), new Vector2(1800, 650));
        Image image = frame.gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        SceneAuthoring.SetColor(image, new Color(0.84f, 0.86f, 0.89f));
        RectTransform area = SceneAuthoring.Rect("MovimientoPersonal", frame, Vector2.zero, frame.sizeDelta);
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = area.offsetMax = Vector2.zero;
        area.gameObject.AddComponent<Canvas>();
        area.gameObject.AddComponent<RectMask2D>();
        StaffRoomView view = canvas.gameObject.AddComponent<StaffRoomView>();
        view.researchers = researchers;
        view.movementArea = area;
        view.workerPrefab = prefab;
        view.countLabel = SceneAuthoring.Label("CantidadPersonal", canvas.transform, researchers ? "staff.researchers" : "staff.engineers", -630, 210, 600, 60, 24);
        view.upkeepLabel = SceneAuthoring.Label("MantenimientoPersonal", canvas.transform, "staff.room_upkeep", 0, 210, 600, 60, 22);
        view.statusLabel = SceneAuthoring.Label("EstadoPersonal", canvas.transform, "staff.none", 630, 210, 600, 60, 22);
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// Solo se cambian la posición vertical y la altura de un control que ya existe.
    /// </summary>
    private static void Place(RectTransform rect, float y, float height)
    {
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
    }

    /// <summary>
    /// Se busca el Canvas en la raíz de la escena mientras se prepara desde el editor.
    /// </summary>
    private static Canvas FindCanvas(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.TryGetComponent(out Canvas canvas)) 
                return canvas;

        throw new InvalidOperationException("Falta Canvas en " + scene.name);
    }

    /// <summary>
    /// Se comprueban las referencias y las cuentas sin tocar partidas guardadas ni crear proyectos de pruebas.
    /// </summary>
    public static void Validate()
    {
        GameBalance balance = ScriptableObject.CreateInstance<GameBalance>();
        GameState state = new GameState { money = 100 };
        StaffService staff = new StaffService(state, balance);
        Require(staff.ResearcherCost == 10 && staff.EngineerCost == 10, "Precio inicial");
        Require(staff.HireResearcher() && state.money == 90 && staff.ResearcherCost == 12 && staff.EngineerCost == 10, "Contratación independiente");
        Require(staff.HireEngineer() && state.money == 80, "Contratación de ingeniero");
        staff.Tick();
        Require(state.money == 78 && state.science == 1 && state.ingenuity == 1, "Mantenimiento y conocimiento entero");
        state.money = 1;
        staff.Tick();
        Require(state.money == 1 && state.science == 1 && state.ingenuity == 1 && !staff.HireResearcher(), "Fondos insuficientes");
        state.money = 100;
        staff.HireResearcher();
        Require(staff.ResearcherUpkeep == 3 && staff.ResearcherCost == 14, "Mantenimiento creciente entero");
        state.researchers = 3;
        state.engineers = 0;
        state.science = 0;
        state.money = 5;
        staff.Tick();
        Require(state.money == 0 && state.science == 3, "Pago entero justo sin deuda");
        state.money = 4;
        staff.Tick();
        Require(state.money == 4 && state.science == 3, "Una unidad insuficiente impide trabajar");
        GameState recovered = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(new GameState { money = 5000000000L, science = long.MaxValue }));
        Require(recovered.money == 5000000000L && recovered.science == long.MaxValue, "Guardado exacto de enteros de 64 bits");
        UnityEngine.Object.DestroyImmediate(balance);

        foreach (string sceneName in new[] { "Economia", "Investigacion", "Ingenieria" })
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity");
            Canvas canvas = FindCanvas(scene);

            if (sceneName == "Economia")
            {
                StaffPurchaseView view = canvas.GetComponent<StaffPurchaseView>();
                Require(view != null && view.hireResearcherButton != null && view.hireEngineerButton != null && view.researcherPrice != null && view.engineerPrice != null, "Controles de contratación");
            }
            else
            {
                StaffRoomView view = canvas.GetComponent<StaffRoomView>();
                Require(view != null && view.movementArea != null && view.workerPrefab != null && AssetDatabase.Contains(view.workerPrefab), "Referencias del recinto");
                Require(view.movementArea.GetComponent<Canvas>() != null && view.movementArea.GetComponent<RectMask2D>() != null, "Canvas y límites del recinto");
            }
        }

        Debug.Log("Personal validado: contratación, mantenimiento, falta de fondos y referencias de las tres escenas.");
    }

    /// <summary>
    /// La validación se detiene si alguna cuenta o referencia de la escena no cuadra.
    /// </summary>
    private static void Require(bool condition, string description)
    {
        if (!condition) 
            throw new InvalidOperationException("Validación de personal: " + description);
    }
}
