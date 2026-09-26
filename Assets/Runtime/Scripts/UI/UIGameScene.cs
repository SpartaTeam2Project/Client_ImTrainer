using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 화면. 생존 시간, 킬, 체력과 승패 문구를 보여 준다.
/// </summary>
public class UIGameScene : MonoBehaviour
{
    [SerializeField] private Button _pauseButton;
    [SerializeField] private GameObject _pauseWindow;
    [SerializeField] private BackgroundTintUI _backgroundTint;
    [SerializeField] private TextMeshProUGUI _killText;
    [SerializeField] private TextMeshProUGUI _timerText;

    private GameController _gameController;

    private void Awake()
    {
        _pauseButton.onClick.AddListener(OnPauseButtonClicked);
    }

    private void OnEnable()
    {
        CacheGameController();
        Managers.Instance.GetManager<EventManager>().Subscribe<GameStateChanged>(HandleGameStateChanged);
        var state = Managers.Instance.CurrentState;
        ApplyPauseWindow(state, state != GameState.Paused);
    }

    private void Start()
    {
        CacheGameController();
    }

    private void OnDisable()
    {
        Managers.Instance.GetManager<EventManager>().Unsubscribe<GameStateChanged>(HandleGameStateChanged);
        _gameController = null;
    }

    private void Update()
    {
        if (Managers.Instance == null)
        {
            return;
        }

        var state = Managers.Instance.CurrentState;
        if (state != GameState.Playing && state != GameState.Paused)
        {
            return;
        }

        RefreshCombat();
    }

    private void OnPauseButtonClicked()
    {
        if (Managers.Instance == null || Managers.Instance.CurrentState != GameState.Playing)
        {
            return;
        }

        CacheGameController();
        if (_gameController == null || _gameController.ActiveStage == null)
        {
            return;
        }

        _gameController.ActiveStage.Pause();
    }

    private void HandleGameStateChanged(GameStateChanged changed)
    {
        ApplyPauseWindow(changed.Next, changed.Previous != GameState.Paused);
    }

    private void ApplyPauseWindow(GameState state, bool hideInstantly)
    {
        var paused = state == GameState.Paused;
        if (_pauseWindow != null)
        {
            _pauseWindow.SetActive(paused);
        }

        if (_backgroundTint == null)
        {
            return;
        }

        if (paused)
        {
            _backgroundTint.Show();
            return;
        }

        _backgroundTint.Hide(hideInstantly);
    }


    private void RefreshCombat()
    {
        CacheGameController();
        var elapsed = _gameController != null ? _gameController.ElapsedSeconds : 0f;

        var kills = 0;
        if (Managers.Instance.TryGetManager<EnemyManager>(out var enemyManager))
        {
            kills = enemyManager.KillCount;
        }

        var current = 0f;
        var max = 0f;
        if (Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            current = playerManager.CurrentHealth;
            max = playerManager.MaxHealth;
        }

        if (_timerText != null)
        {
            _timerText.text = FormatTime(elapsed);
        }

        if (_killText != null)
        {
            _killText.text = $"처치 {kills}";
        }

    }



    private void CacheGameController()
    {
        if (_gameController != null || Managers.Instance == null)
        {
            return;
        }

        _gameController = Managers.Instance.GetComponent<GameController>();
    }

    private static string FormatTime(float elapsedSeconds)
    {
        var total = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));
        var minutes = total / 60;
        var seconds = total % 60;
        return $"{minutes:00}:{seconds:00}";
    }
}
