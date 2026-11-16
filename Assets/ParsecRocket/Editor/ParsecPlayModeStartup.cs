using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ParsecPlayModeStartup
{
    private const string MenuPath = "Assets/ParsecRocket/Scenes/MenuScene.unity";

    static ParsecPlayModeStartup()
    {
        EditorApplication.delayCall += Configure;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void Configure()
    {
        var menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuPath);
        if (menu != null)
            EditorSceneManager.playModeStartScene = menu;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            Configure();
        if (state != PlayModeStateChange.EnteredPlayMode) return;

        // Apply only at Play entry, never when navigating between levels.
        if (SceneManager.GetActiveScene().path != MenuPath)
            SceneManager.LoadScene(MenuPath);
        if (!Application.isBatchMode)
        {
            var gameView = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameView != null) EditorWindow.GetWindow(gameView).Focus();
        }
        Debug.Log("Parsec: Play started at the main menu.");
    }
}
