using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PlayModeStartSceneSetup
{
    static PlayModeStartSceneSetup()
    {
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MainMenu.unity");
        if (sceneAsset != null && EditorSceneManager.playModeStartScene != sceneAsset)
        {
            EditorSceneManager.playModeStartScene = sceneAsset;
        }
    }
}
