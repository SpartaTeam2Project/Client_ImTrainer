using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 플레이 모드 시작 씬을 타이틀 씬으로 고정한다.
/// </summary>
[InitializeOnLoad]
public static class PlayFromTitleScene
{
    static PlayFromTitleScene()
    {
        EditorApplication.delayCall += ApplyStartScene;
    }

    private static void ApplyStartScene()
    {
        var sceneAsset = FindTitleScene();
        if (sceneAsset == null)
        {
            return;
        }

        EditorSceneManager.playModeStartScene = sceneAsset;
    }

    private static SceneAsset FindTitleScene()
    {
        var guids = AssetDatabase.FindAssets($"t:Scene {SceneNames.TITLE_SCENE}");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) != SceneNames.TITLE_SCENE)
            {
                continue;
            }

            return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }

        return null;
    }
}
