using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 메인 메뉴, 로딩, 게임 씬 전환을 담당한다.
/// </summary>
public class SceneLoadManager : BaseManager
{
    private const float SCENE_ACTIVATION_PROGRESS = 0.9f;

    private bool _isLoading;

    /// <summary>
    /// 로딩 씬을 거친 뒤 게임 씬으로 바꾼다.
    /// </summary>
    public async UniTask LoadGameAsync()
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        var cancellationToken = this.GetCancellationTokenOnDestroy();

        try
        {
            Managers.Instance.ChangeState(GameState.Loading);
            await LoadSceneAsync(SceneNames.LOADING_SCREEN, LoadSceneMode.Additive, cancellationToken);
            await UnloadSceneAsync(SceneNames.MAIN_MENU, cancellationToken);
            await LoadSceneAsync(SceneNames.GAME, LoadSceneMode.Single, cancellationToken);

            if (Managers.Instance != null)
            {
                Managers.Instance.ChangeState(GameState.Playing);
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// 로딩 씬을 거친 뒤 메인 메뉴로 돌아간다.
    /// </summary>
    public async UniTask LoadMainMenuAsync()
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        var cancellationToken = this.GetCancellationTokenOnDestroy();

        try
        {
            Managers.Instance.ChangeState(GameState.Loading);
            await LoadSceneAsync(SceneNames.LOADING_SCREEN, LoadSceneMode.Additive, cancellationToken);
            await UnloadSceneAsync(SceneNames.GAME, cancellationToken);
            await LoadSceneAsync(SceneNames.MAIN_MENU, LoadSceneMode.Single, cancellationToken);

            if (Managers.Instance != null)
            {
                Managers.Instance.ChangeState(GameState.Menu);
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// 씬이 거의 준비될 때까지 활성화를 미룬 뒤 로드를 끝낸다.
    /// </summary>
    private static async UniTask LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, CancellationToken cancellationToken)
    {
        var operation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
        if (operation == null)
        {
            Debug.LogError($"씬을 로드할 수 없음: {sceneName}");
            return;
        }

        operation.allowSceneActivation = false;
        while (operation.progress < SCENE_ACTIVATION_PROGRESS)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        operation.allowSceneActivation = true;
        await operation.ToUniTask(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 올라와 있는 씬만 언로드한다. 활성화 플래그는 건드리지 않는다.
    /// </summary>
    private static async UniTask UnloadSceneAsync(string sceneName, CancellationToken cancellationToken)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded)
        {
            return;
        }

        var operation = SceneManager.UnloadSceneAsync(scene);
        if (operation == null)
        {
            Debug.LogError($"씬을 언로드할 수 없음: {sceneName}");
            return;
        }

        await operation.ToUniTask(cancellationToken: cancellationToken);
    }
}
