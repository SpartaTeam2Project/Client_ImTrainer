using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 화면. 생존 시간, 킬, 레벨, 체력, 경험치, 이번 판 몬스터볼과 포켓달러를 보여 준다.
/// </summary>
public class UIGameScene : MonoBehaviour
{
    [SerializeField] private Button _pauseButton;
    [SerializeField] private GameObject _pauseWindow;
    [SerializeField] private GameObject _combatHud;
    [SerializeField] private BackgroundTintUI _backgroundTint;
    [SerializeField] private StageCompleteScreen _stageCompleteScreen;
    [SerializeField] private StageFailedScreen _stageFailedScreen;
    [SerializeField] private TextMeshProUGUI _killText;
    [SerializeField] private TextMeshProUGUI _monsterBallText;
    [SerializeField] private TextMeshProUGUI _pocketDollarText;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private RectMask2D _experienceMask;
    [SerializeField] private TextMeshProUGUI _hpText;
    [SerializeField] private Image _hpFill;
    [SerializeField] private Sprite _hpHigh;
    [SerializeField] private Sprite _hpMedium;
    [SerializeField] private Sprite _hpLow;

    private const float HP_MEDIUM_THRESHOLD = 0.5f;
    private const float HP_LOW_THRESHOLD = 0.25f;
    private const int HP_BAR_PIXELS = 48;
    private const float HP_TWEEN_DURATION = 0.3f;
    private const float EXP_TWEEN_DURATION = 0.4f;

    private GameController _gameController;
    private int _shownHp = -1;
    private int _shownMaxHp = -1;
    private bool _hpReady;
    private float _hpTarget;
    private float _hpMax;
    private float _hpShown;
    private Tweener _hpTween;
    private bool _expReady;
    private int _expLevel;
    private float _expTarget;
    private int _expShownLevel;
    private float _expShown;
    private Sequence _expTween;

    private void Awake()
    {
        if (_pauseButton != null)
        {
            _pauseButton.onClick.AddListener(OnPauseButtonClicked);
        }
    }

    private void OnEnable()
    {
        CacheGameController();
        var eventManager = Managers.Instance.GetManager<EventManager>();
        eventManager.Subscribe<GameStateChanged>(HandleGameStateChanged);
        eventManager.Subscribe<CurrencyAmountChanged>(HandleCurrencyChanged);
        var state = Managers.Instance.CurrentState;
        ApplyPauseWindow(state, state != GameState.Paused);
        ApplyResultScreens(state);
        RefreshHealth(true);
        RefreshExperience(true);
        RefreshCurrencies();
    }

    private void Start()
    {
        CacheGameController();
    }

    private void OnDisable()
    {
        if (Managers.Instance != null && Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            eventManager.Unsubscribe<GameStateChanged>(HandleGameStateChanged);
            eventManager.Unsubscribe<CurrencyAmountChanged>(HandleCurrencyChanged);
        }

        KillTween(ref _hpTween);
        KillTween(ref _expTween);
        _hpReady = false;
        _expReady = false;
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
        ApplyResultScreens(changed.Next);
    }

    private void ApplyResultScreens(GameState state)
    {
        var showResult = state == GameState.Victory || state == GameState.Defeat;
        if (_combatHud != null)
        {
            _combatHud.SetActive(!showResult);
        }

        if (state == GameState.Victory)
        {
            if (_stageFailedScreen != null)
            {
                _stageFailedScreen.Hide();
            }

            if (_stageCompleteScreen != null)
            {
                CacheGameController();
                var stage = _gameController != null ? _gameController.ActiveStage : null;
                // 장착 포켓몬의 성공 포즈가 끝난 뒤에 결과창을 연다.
                _stageCompleteScreen.Show(stage != null ? stage.LastResult : null,
                    stage != null ? stage.ResultRevealDelay : 0f);
            }

            return;
        }

        if (state == GameState.Defeat)
        {
            if (_stageCompleteScreen != null)
            {
                _stageCompleteScreen.Hide();
            }

            if (_stageFailedScreen != null)
            {
                _stageFailedScreen.Show();
            }

            return;
        }

        if (_stageCompleteScreen != null)
        {
            _stageCompleteScreen.Hide();
        }

        if (_stageFailedScreen != null)
        {
            _stageFailedScreen.Hide();
        }
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

        var hideResultTint = state == GameState.Victory || state == GameState.Defeat;
        _backgroundTint.Hide(hideInstantly || hideResultTint);
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

        if (_timerText != null)
        {
            _timerText.text = FormatTime(elapsed);
        }

        if (_killText != null)
        {
            _killText.text = $"처치 {kills}";
        }

        RefreshHealth();
        RefreshExperience();
    }

    private void HandleCurrencyChanged(CurrencyAmountChanged changed)
    {
        if (changed.IsMetaBalance || !IsLocalPlayer(changed.PlayerId))
        {
            return;
        }

        if (changed.CurrencyId == CurrenciesManager.MONSTER_BALL_ID)
        {
            SetCurrencyText(_monsterBallText, changed.Amount);
            return;
        }

        if (changed.CurrencyId == CurrenciesManager.POCKET_DOLLAR_ID)
        {
            SetCurrencyText(_pocketDollarText, changed.Amount);
        }
    }

    private void RefreshCurrencies()
    {
        var monsterBalls = 0;
        var pocketDollars = 0;
        if (Managers.Instance != null
            && Managers.Instance.TryGetManager<PlayerManager>(out var playerManager)
            && Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies))
        {
            monsterBalls = ReadStageAmount(currencies, playerManager.LocalPlayerId, CurrenciesManager.MONSTER_BALL_ID);
            pocketDollars = ReadStageAmount(currencies, playerManager.LocalPlayerId, CurrenciesManager.POCKET_DOLLAR_ID);
        }

        SetCurrencyText(_monsterBallText, monsterBalls);
        SetCurrencyText(_pocketDollarText, pocketDollars);
    }

    private static int ReadStageAmount(CurrenciesManager currencies, int playerId, string currencyId)
    {
        var save = currencies.GetCurrency(playerId, currencyId, false);
        return save == null ? 0 : save.Amount;
    }

    private static bool IsLocalPlayer(int playerId)
    {
        return Managers.Instance != null
            && Managers.Instance.TryGetManager<PlayerManager>(out var playerManager)
            && playerId == playerManager.LocalPlayerId;
    }

    private static void SetCurrencyText(TextMeshProUGUI label, int amount)
    {
        if (label != null)
        {
            label.text = amount.ToString();
        }
    }

    private void RefreshHealth(bool instant = false)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return;
        }

        var current = playerManager.CurrentHealth;
        var max = playerManager.MaxHealth;
        instant |= !_hpReady;
        if (!instant && current == _hpTarget && max == _hpMax)
        {
            return;
        }

        _hpTarget = current;
        _hpMax = max;
        KillTween(ref _hpTween);

        if (instant)
        {
            // 플레이어가 아직 없으면 최대 체력이 0이라, 실제 값이 들어올 때도 바로 적용한다.
            _hpReady = max > 0f;
            ApplyHealth(current);
            return;
        }

        _hpTween = DOTween.To(() => _hpShown, ApplyHealth, current, HP_TWEEN_DURATION)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    private void ApplyHealth(float shown)
    {
        _hpShown = shown;

        var hp = Mathf.CeilToInt(shown);
        var maxHp = Mathf.CeilToInt(_hpMax);
        if (_hpText != null && (hp != _shownHp || maxHp != _shownMaxHp))
        {
            _shownHp = hp;
            _shownMaxHp = maxHp;
            _hpText.text = $"{hp}/{maxHp}";
        }

        if (_hpFill == null)
        {
            return;
        }

        var ratio = _hpMax <= 0f ? 0f : Mathf.Clamp01(shown / _hpMax);
        // 스프라이트 픽셀 단위로 끊어 바 끝이 픽셀 중간에서 잘리지 않게 한다.
        _hpFill.fillAmount = Mathf.Ceil(ratio * HP_BAR_PIXELS) / HP_BAR_PIXELS;

        var sprite = ratio > HP_MEDIUM_THRESHOLD ? _hpHigh : ratio > HP_LOW_THRESHOLD ? _hpMedium : _hpLow;
        if (sprite != null && _hpFill.sprite != sprite)
        {
            _hpFill.sprite = sprite;
        }
    }

    private void RefreshExperience(bool instant = false)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return;
        }

        var level = playerManager.Level;
        var required = playerManager.RequiredXp;
        var progress = required <= 0f ? 0f : Mathf.Clamp01(playerManager.CurrentXp / required);
        instant |= !_expReady;
        if (!instant && level == _expLevel && progress == _expTarget)
        {
            return;
        }

        _expLevel = level;
        _expTarget = progress;
        KillTween(ref _expTween);

        if (instant)
        {
            _expReady = true;
            ApplyExperienceLevel(level);
            ApplyExperience(progress);
            return;
        }

        _expTween = DOTween.Sequence().SetUpdate(true);
        if (level != _expShownLevel)
        {
            // 레벨이 오르면 바를 끝까지 채운 뒤 비우고 새 레벨 진행도까지 다시 채운다.
            if (level > _expShownLevel)
            {
                _expTween.Append(DOTween.To(() => _expShown, ApplyExperience, 1f, EXP_TWEEN_DURATION).SetEase(Ease.OutQuad));
            }

            _expTween.AppendCallback(() =>
            {
                ApplyExperienceLevel(level);
                ApplyExperience(0f);
            });
        }

        _expTween.Append(DOTween.To(() => _expShown, ApplyExperience, progress, EXP_TWEEN_DURATION).SetEase(Ease.OutQuad));
    }

    private void ApplyExperienceLevel(int level)
    {
        _expShownLevel = level;
        if (_levelText != null)
        {
            _levelText.text = $"{level}";
        }
    }

    private void ApplyExperience(float shown)
    {
        _expShown = shown;
        if (_experienceMask == null)
        {
            return;
        }

        var width = _experienceMask.rectTransform.rect.width;
        if (width <= 0f)
        {
            return;
        }

        var padding = _experienceMask.padding;
        padding.z = width * (1f - shown);
        _experienceMask.padding = padding;
    }

    private static void KillTween<T>(ref T tween) where T : Tween
    {
        if (tween == null)
        {
            return;
        }

        tween.Kill();
        tween = null;
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
