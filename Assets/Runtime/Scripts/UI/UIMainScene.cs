using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 타이틀 씬에서 게임 시작을 요청한다.
/// </summary>
public class UIMainScene : MonoBehaviour
{
    /// <summary>
    /// 게임 씬으로 전환한다.
    /// </summary>
    public void StartGame()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<SceneLoadManager>(out var sceneLoadManager))
        {
            Debug.LogError("SceneLoadManager를 찾을 수 없습니다.");
            return;
        }

        sceneLoadManager.LoadGameAsync().Forget();
    }
}
