using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 화면. 생존 시간, 킬, 체력과 승패 문구를 보여 준다.
/// </summary>
public class UIGameScene : MonoBehaviour
{
    [SerializeField] private Button _pauseButton;
    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private TextMeshProUGUI _killText;
    [SerializeField] private TextMeshProUGUI _timerText;

    private void Awake()
    {
        _pauseButton.onClick.AddListener(OnPauseButtonClicked);
    }

    private void OnEnable()
    {
        if (Managers.Instance == null)
        {
            return;
        }
    }

    private void OnDisable()
    {
        if (Managers.Instance == null)
        {
            return;
        }
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
        //_pauseWindow.SetActive(true);
    }


    private void RefreshCombat()
    {
        var elapsed = 0f;
        if (TryGetGameController(out var gameController))
        {
            elapsed = gameController.ElapsedSeconds;
        }

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

        if (_healthText != null)
        {
            _healthText.text = $"체력 {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }
    }



    private static bool TryGetGameController(out GameController gameController)
    {
        gameController = null;
        if (Managers.Instance == null)
        {
            return false;
        }

        gameController = Managers.Instance.GetComponent<GameController>();
        return gameController != null;
    }

    private static string FormatTime(float elapsedSeconds)
    {
        var total = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));
        var minutes = total / 60;
        var seconds = total % 60;
        return $"{minutes:00}:{seconds:00}";
    }
}
