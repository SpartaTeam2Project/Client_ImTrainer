using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;

/// <summary>
/// 게임 씬에서 메인 메뉴 복귀를 요청한다.
/// </summary>
public class UIPauseWindow : MonoBehaviour
{
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _exitButton;


    private void Awake()
    {
        _resumeButton.onClick.AddListener(OnResumeButtonClicked);
        _exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    private void OnResumeButtonClicked()
    {
        gameObject.SetActive(false);
    }

    private void OnExitButtonClicked()
    {
        ReturnToMenu();
    }       
    
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

        sceneLoadManager.LoadTitleSceneAsync().Forget();
    }
}
