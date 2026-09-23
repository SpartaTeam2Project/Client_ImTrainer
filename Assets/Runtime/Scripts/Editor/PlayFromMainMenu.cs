using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 플레이 모드 시작 씬을 메인 메뉴로 고정한다.
/// </summary>
[InitializeOnLoad]
public static class PlayFromMainMenu
{
    static PlayFromMainMenu()
    {
        EditorApplication.delayCall += ApplyStartScene;
    }

    private static void ApplyStartScene()
    {
        var sceneAsset = FindMainMenuScene();
        if (sceneAsset == null)
        {
            return;
        }

        EditorSceneManager.playModeStartScene = sceneAsset;
    }

    private static SceneAsset FindMainMenuScene()
    {
        var guids = AssetDatabase.FindAssets($"t:Scene {SceneNames.MAIN_MENU}");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) != SceneNames.MAIN_MENU)
            {
                continue;
            }

            return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }

        return null;
    }
}
