using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Abre Inicio al arrancar el editor sin interferir con recompilaciones ni trabajo sin guardar.
/// </summary>
[InitializeOnLoad]
internal static class EditorStartupScene
{
    /// <summary>Ruta de la escena inicial ya diseñada y serializada.</summary>
    private const string ScenePath = "Assets/Scenes/Inicio.unity";

    /// <summary>Identifica el intento de apertura durante esta sesión del editor.</summary>
    private const string SessionKey = "IdleClickerPeriodicTable.EditorStartupScene.Opened";

    /// <summary>
    /// Difiere la apertura hasta que el editor termine de importar los assets.
    /// </summary>
    static EditorStartupScene()
    {
        if (Application.isBatchMode || SessionState.GetBool(SessionKey, false))
            return;

        EditorApplication.delayCall += OpenStartupScene;
    }

    /// <summary>
    /// Abre la escena una sola vez y conserva las escenas recuperadas o modificadas.
    /// </summary>
    private static void OpenStartupScene()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += OpenStartupScene;
            return;
        }

        SessionState.SetBool(SessionKey, true);

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        for (int index = 0; index < SceneManager.sceneCount; index++)
            if (SceneManager.GetSceneAt(index).isDirty)
                return;

        if (SceneManager.GetActiveScene().path == ScenePath)
            return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            Debug.LogError("No se encuentra la escena inicial: " + ScenePath);
            return;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }
}
