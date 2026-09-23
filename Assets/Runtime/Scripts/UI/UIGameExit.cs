using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 게임 씬에서 메인 메뉴 복귀를 요청한다.
/// </summary>
public class UIGameExit : MonoBehaviour
{
    /// <summary>
    /// 메인 메뉴 씬으로 돌아간다.
    /// </summary>
    public void ReturnToMenu()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<SceneLoadManager>(out var sceneLoadManager))
        {
            Debug.LogError("SceneLoadManager를 찾을 수 없습니다.");
            return;
        }

        sceneLoadManager.LoadMainMenuAsync().Forget();
    }
}
