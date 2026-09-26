using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일시정지 중 계속, 나가기, 설정 진입을 받는다.
/// </summary>
public class UIPauseWindow : MonoBehaviour
{
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _exitButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private RectTransform _abilitiesList;

    private GameController _gameController;

    private void Awake()
    {
        if (_abilitiesList == null)
        {
            Debug.LogError("일시정지 창에 능력 목록 영역이 없습니다.");
        }

        if (_resumeButton != null)
        {
            _resumeButton.onClick.AddListener(OnResumeButtonClicked);
        }

        if (_exitButton != null)
        {
            _exitButton.onClick.AddListener(OnExitButtonClicked);
        }

        if (_settingsButton != null)
        {
            _settingsButton.onClick.AddListener(OpenSettings);
        }
    }

    private void OnEnable()
    {
        CacheGameController();
    }

    private void OnResumeButtonClicked()
    {
        CacheGameController();
        if (_gameController == null || _gameController.ActiveStage == null)
        {
            return;
        }

        _gameController.ActiveStage.Resume();
    }

    private void OnExitButtonClicked()
    {
        ReturnToMenu();
    }

    /// <summary>
    /// 설정 창은 이후 작업에서 연다.
    /// </summary>
    public void OpenSettings()
    {
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

    private void CacheGameController()
    {
        if (_gameController != null || Managers.Instance == null)
        {
            return;
        }

        _gameController = Managers.Instance.GetComponent<GameController>();
    }
}
