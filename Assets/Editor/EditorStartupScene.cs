using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Inicio se abre al arrancar el editor, sin molestar a las recompilaciones ni al trabajo sin guardar.
/// </summary>
[InitializeOnLoad]
internal static class EditorStartupScene
{
    /// <summary>
    /// La ruta de la escena de inicio que ya está guardada en el proyecto.
    /// </summary>
    private const string ScenePath = "Assets/Scenes/Inicio.unity";

    /// <summary>
    /// La marca que recuerda si ya se intentó abrir Inicio en esta sesión del editor.
    /// </summary>
    private const string SessionKey = "IdleClickerPeriodicTable.EditorStartupScene.Opened";

    /// <summary>
    /// La apertura se deja para cuando el editor termine de importar los recursos.
    /// </summary>
    static EditorStartupScene()
    {
        if (Application.isBatchMode || SessionState.GetBool(SessionKey, false))
            return;

        EditorApplication.delayCall += OpenStartupScene;
    }

    /// <summary>
    /// Se abre Inicio una sola vez, sin cerrar escenas recuperadas ni cambios sin guardar.
    /// </summary>
    private static void OpenStartupScene()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += OpenStartupScene;
            return;
        }

        // El intento cuenta aunque luego no se abra Inicio, así no se insiste tras una recompilación.
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
