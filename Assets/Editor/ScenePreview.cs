using System.IO;
using IdleClickerPeriodicTable.Presentation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Se sacan imágenes de las pantallas desde el editor, sin abrir ventanas ni guardar cambios en las escenas.
/// </summary>
public static class ScenePreview
{
    /// <summary>
    /// Se generan capturas con varios idiomas y tamaños para ver si el texto sigue cabiendo bien.
    /// </summary>
    public static void Render()
    {
        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Previews"));
        Directory.CreateDirectory(directory);
        int language = GamePreferences.LanguageIndex;
        int size = GamePreferences.TextSizeIndex;
        int mode = GamePreferences.ColorMode;
        RenderPipelineAsset pipeline = GraphicsSettings.defaultRenderPipeline;
        RenderPipelineAsset qualityPipeline = QualitySettings.renderPipeline;

        try
        {
            // Estas capturas necesitan el renderizado clásico para llamar a Camera.Render directamente.
            GraphicsSettings.defaultRenderPipeline = null;
            QualitySettings.renderPipeline = null;
            GamePreferences.SetLanguage(0);
            GamePreferences.SetTextSize(1);
            GamePreferences.SetColorMode(0);
            RenderScene("Inicio", directory, "inicio");
            RenderScene("Economia", directory, "economia");
            RenderScene("Configuracion", directory, "configuracion");

            // Con texto grande se notan antes los problemas de espacio, por eso se prueba en todos los idiomas.
            for (int index = 0; index < 6; index++)
            {
                GamePreferences.SetLanguage(index);
                GamePreferences.SetTextSize(2);
                GamePreferences.SetColorMode(index % 4);
                RenderScene("Economia", directory, "economia-grande-" + index);
                RenderScene("Configuracion", directory, "configuracion-grande-" + index);
            }

            GamePreferences.SetLanguage(2);
            RenderScene("Economia", directory, "economia-importes-altos", true);
            RenderScene("Inicio", directory, "inicio-confirmacion", false, true);
        }
        finally
        {
            // Las opciones y el renderizado vuelven a su estado anterior, incluso si alguna captura falla.
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = qualityPipeline;
            GamePreferences.SetLanguage(language);
            GamePreferences.SetTextSize(size);
            GamePreferences.SetColorMode(mode);
            EditorSceneManager.OpenScene("Assets/Scenes/Inicio.unity");
        }
    }

    /// <summary>
    /// Se abre una escena, se rellenan sus textos con datos de prueba y se guarda una captura en PNG.
    /// </summary>
    /// <param name="sceneName">La pantalla de la que se quiere sacar la captura.</param>
    /// <param name="directory">La carpeta donde se guarda la imagen.</param>
    /// <param name="filename">El nombre de la imagen, sin la extensión PNG.</param>
    /// <param name="largeValues">Si está activado, se usan importes grandes para comprobar que caben.</param>
    /// <param name="showConfirmation">Si está activado, aparece el aviso de partida nueva en Inicio.</param>
    /// <exception cref="System.InvalidOperationException">Falta el lienzo o una cámara lista para capturar.</exception>
    private static void RenderScene(string sceneName, string directory, string filename, bool largeValues = false, bool showConfirmation = false)
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity");
        Canvas canvas = null;
        Camera camera = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.TryGetComponent(out Canvas found)) 
                canvas = found;

            if (root.TryGetComponent(out Camera sceneCamera)) 
                camera = sceneCamera;

            // Los textos ocultos también se preparan, por si la captura necesita mostrar algún panel.
            foreach (LocalizedText label in root.GetComponentsInChildren<LocalizedText>(true))
                label.SetArguments(largeValues ? 5000000000d : 0d);

            foreach (AccessibleGraphic graphic in root.GetComponentsInChildren<AccessibleGraphic>(true))
                graphic.Refresh();

            foreach (SceneNavigation navigation in root.GetComponentsInChildren<SceneNavigation>(true))
                if (navigation.activeOutline != null) 
                    navigation.activeOutline.enabled = navigation.sceneName == sceneName;

            if (root.TryGetComponent(out EconomyView economy)) 
                economy.clickValue.SetArguments(1d);

            if (showConfirmation && root.TryGetComponent(out StartMenu start)) 
                start.confirmationPanel.SetActive(true);

            if (root.TryGetComponent(out SettingsView settings))
            {
                // Fuera de Play, la captura tiene que marcar las opciones sin depender de OnEnable.
                for (int index = 0; index < settings.languageOutlines.Length; index++)
                    settings.languageOutlines[index].enabled = index == GamePreferences.LanguageIndex;

                for (int index = 0; index < settings.sizeOutlines.Length; index++)
                    settings.sizeOutlines[index].enabled = index == GamePreferences.TextSizeIndex;

                for (int index = 0; index < settings.colorOutlines.Length; index++)
                    settings.colorOutlines[index].enabled = index == GamePreferences.ColorMode;
            }
        }

        if (canvas == null) 
            throw new System.InvalidOperationException("Falta Canvas en " + sceneName);

        if (camera == null || !camera.isActiveAndEnabled || camera.targetDisplay != 0 || camera.targetTexture != null)
            throw new System.InvalidOperationException("Falta una cámara activa para la pantalla 1 en " + sceneName);

        camera.transform.position = new Vector3(0, 0, -10);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.white;
        camera.orthographic = true;
        camera.orthographicSize = 540;
        // La UI se dibuja en una textura de 1920x1080 para no depender del tamaño de la ventana del editor.
        RenderTexture texture = new RenderTexture(1920, 1080, 24);
        camera.targetTexture = texture;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 10;
        Canvas.ForceUpdateCanvases();
        camera.Render();
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = texture;
        Texture2D image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        image.Apply();
        File.WriteAllBytes(Path.Combine(directory, filename + ".png"), image.EncodeToPNG());
        // Se restaura la textura activa y se liberan los recursos temporales de la captura.
        RenderTexture.active = previous;
        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(texture);
    }
}
