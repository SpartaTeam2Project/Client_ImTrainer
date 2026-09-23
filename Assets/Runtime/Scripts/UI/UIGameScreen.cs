using TMPro;
using UnityEngine;

/// <summary>
/// 인게임 화면. 생존 시간, 킬, 체력과 승패 문구를 보여 준다.
/// </summary>
public class UIGameScreen : MonoBehaviour
{
    private const float LABEL_HEIGHT = 36f;
    private const float RESULT_HEIGHT = 140f;

    [SerializeField] private GameObject _menuButton;
    [SerializeField] private TMP_FontAsset _fontAsset;

    private TextMeshProUGUI _timerText;
    private TextMeshProUGUI _killText;
    private TextMeshProUGUI _healthText;
    private TextMeshProUGUI _pauseText;
    private TextMeshProUGUI _resultText;

    private void Awake()
    {
        _timerText = CreateLabel("Timer", new Vector2(0f, 1f), new Vector2(16f, -12f), LABEL_HEIGHT, TextAlignmentOptions.TopLeft);
        _killText = CreateLabel("Kills", new Vector2(0f, 1f), new Vector2(16f, -48f), LABEL_HEIGHT, TextAlignmentOptions.TopLeft);
        _healthText = CreateLabel("Health", new Vector2(0f, 1f), new Vector2(16f, -84f), LABEL_HEIGHT, TextAlignmentOptions.TopLeft);
        _pauseText = CreateLabel("Pause", new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), LABEL_HEIGHT, TextAlignmentOptions.Center);
        _resultText = CreateLabel("Result", new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), RESULT_HEIGHT, TextAlignmentOptions.Center);
        _pauseText.gameObject.SetActive(false);
        _resultText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (Managers.Instance == null)
        {
            return;
        }

        Managers.Instance.OnGameStateChanged += HandleGameStateChanged;
        ApplyState(Managers.Instance.CurrentState);
    }

    private void OnDisable()
    {
        if (Managers.Instance == null)
        {
            return;
        }

        Managers.Instance.OnGameStateChanged -= HandleGameStateChanged;
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

    private void HandleGameStateChanged(GameState previousState, GameState newState)
    {
        ApplyState(newState);
    }

    private void ApplyState(GameState state)
    {
        var showResult = state == GameState.Victory || state == GameState.Defeat;
        if (_menuButton != null)
        {
            _menuButton.SetActive(showResult);
        }

        if (_pauseText != null)
        {
            _pauseText.gameObject.SetActive(state == GameState.Paused);
            _pauseText.text = "일시정지";
        }

        if (_resultText != null)
        {
            _resultText.gameObject.SetActive(showResult);
        }

        if (showResult)
        {
            RefreshResult(state);
        }
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

    private void RefreshResult(GameState state)
    {
        if (_resultText == null)
        {
            return;
        }

        var title = state == GameState.Victory ? "승리" : "패배";
        var kills = 0;
        var elapsed = 0f;
        if (TryGetGameController(out var gameController) && gameController.ActiveStage != null && gameController.ActiveStage.LastResult != null)
        {
            kills = gameController.ActiveStage.LastResult.KillCount;
            elapsed = gameController.ActiveStage.LastResult.ElapsedSeconds;
        }

        _resultText.text = $"{title}\n처치 {kills}\n{FormatTime(elapsed)}";
    }

    private TextMeshProUGUI CreateLabel(string objectName, Vector2 anchor, Vector2 anchoredPosition, float height, TextAlignmentOptions alignment)
    {
        var labelObject = new GameObject(objectName, typeof(RectTransform));
        labelObject.transform.SetParent(transform, false);
        var rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(420f, height);

        var text = labelObject.AddComponent<TextMeshProUGUI>();
        text.font = _fontAsset;
        text.fontSize = 28f;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
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
