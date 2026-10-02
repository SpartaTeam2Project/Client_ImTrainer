using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 트레이닝 한 칸. 다음 레벨 비용과 구매 버튼을 보여 준다.
/// </summary>
public class TrainingItemBehavior : MonoBehaviour, IPointerEnterHandler
{
    private const string MAX_LEVEL_LABEL = "최대";
    private const string LEVEL_LABEL_FORMAT = "레벨 {0}";
    private const string PERCENT_BONUS_FORMAT = "보너스 +{0}%";
    private const string PERCENT_REDUCTION_FORMAT = "필요 경험치 -{0}%";
    private const string COUNT_FORMAT = "+{0}";
    private const float HIDDEN_VALUE = 0f;
    private const float PERCENT_SCALE = 100f;

    private static readonly Color EMPTY_FRAME = new Color(0.28f, 0.3f, 0.34f, 1f);
    private static readonly Color EMPTY_BACKGROUND = new Color(0.12f, 0.13f, 0.16f, 1f);
    private static readonly Color FILLED_FRAME = new Color(0.86f, 0.72f, 0.34f, 1f);
    private static readonly Color FILLED_BACKGROUND = new Color(0.18f, 0.22f, 0.3f, 1f);

    [SerializeField] private RectTransform _rect;
    [SerializeField] private Image _iconBackground;
    [SerializeField] private Image _iconFrame;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _titleLabel;
    [SerializeField] private TMP_Text _descriptionLabel;
    [SerializeField] private TMP_Text _effectLabel;
    [SerializeField] private TMP_Text _levelLabel;
    [SerializeField] private Button _buyButton;
    [SerializeField] private Image _buyButtonImage;
    [SerializeField] private Sprite _enabledButtonSprite;
    [SerializeField] private Sprite _disabledButtonSprite;
    [SerializeField] private TMP_Text _costLabel;
    [SerializeField] private Image _costIcon;
    [SerializeField] private GameObject _maxLabel;
    [SerializeField] private GameObject _selected;

    private int _playerId;
    private int _levelId;
    private Action<TrainingItemBehavior> _onFocus;
    private Action<TrainingItemBehavior> _onPurchase;

    public RectTransform Rect => _rect;

    public TrainingData Data { get; private set; }

    public bool CanPurchase { get; private set; }

    private void Awake()
    {
        if (_rect == null)
        {
            _rect = transform as RectTransform;
        }

        DisableNavigation();
    }

    private void OnEnable()
    {
        if (_buyButton != null)
        {
            _buyButton.onClick.AddListener(HandlePurchase);
        }
    }

    private void OnDisable()
    {
        if (_buyButton != null)
        {
            _buyButton.onClick.RemoveListener(HandlePurchase);
        }
    }

    /// <summary>
    /// 데이터와 다음에 살 레벨 순서를 칸에 넣는다. 저장된 레벨이 -1이면 0이다.
    /// </summary>
    public void Init(TrainingData data, int levelId, int playerId, Action<TrainingItemBehavior> onFocus, Action<TrainingItemBehavior> onPurchase)
    {
        Data = data;
        _levelId = levelId;
        _playerId = playerId;
        _onFocus = onFocus;
        _onPurchase = onPurchase;
        Redraw();
    }

    /// <summary>
    /// 구매에 성공한 뒤 다음 레벨로 표시를 바꾼다.
    /// </summary>
    public void AdvanceLevel()
    {
        _levelId++;
        Redraw();
    }

    /// <summary>
    /// 잔액에 맞춰 구매 버튼을 다시 그린다.
    /// </summary>
    public void Redraw()
    {
        if (Data == null)
        {
            return;
        }

        if (_titleLabel != null)
        {
            _titleLabel.text = Data.Title;
        }

        if (_descriptionLabel != null)
        {
            _descriptionLabel.text = Data.Description;
        }

        RedrawIcon();
        RedrawEffect();

        if (_levelLabel != null)
        {
            _levelLabel.text = _levelId >= Data.LevelsCount
                ? MAX_LEVEL_LABEL
                : string.Format(LEVEL_LABEL_FORMAT, _levelId + 1);
        }

        RedrawButton();
    }

    private void RedrawIcon()
    {
        var hasIcon = Data.Icon != null;
        if (_iconImage != null)
        {
            _iconImage.sprite = Data.Icon;
            _iconImage.enabled = hasIcon;
            _iconImage.preserveAspect = true;
        }

        if (_iconBackground != null)
        {
            _iconBackground.enabled = true;
            _iconBackground.color = hasIcon ? FILLED_BACKGROUND : EMPTY_BACKGROUND;
        }

        if (_iconFrame != null)
        {
            _iconFrame.enabled = true;
            _iconFrame.color = hasIcon ? FILLED_FRAME : EMPTY_FRAME;
        }
    }

    private void RedrawEffect()
    {
        if (_effectLabel == null)
        {
            return;
        }

        var level = _levelId >= Data.LevelsCount ? null : Data.GetLevel(_levelId);
        var value = level == null ? HIDDEN_VALUE : level.Value;
        _effectLabel.text = value <= HIDDEN_VALUE ? string.Empty : FormatEffect(value);
    }

    private string FormatEffect(float value)
    {
        switch (Data.ValueDisplay)
        {
            case TrainingValueDisplay.PercentReduction:
                return string.Format(PERCENT_REDUCTION_FORMAT, ToPercent(value));
            case TrainingValueDisplay.Count:
                return string.Format(COUNT_FORMAT, Mathf.RoundToInt(value));
            default:
                return string.Format(PERCENT_BONUS_FORMAT, ToPercent(value));
        }
    }

    private static int ToPercent(float value)
    {
        return Mathf.RoundToInt(value * PERCENT_SCALE);
    }

    /// <summary>
    /// 방향키로 고른 칸을 표시한다.
    /// </summary>
    public void SetFocused(bool focused)
    {
        if (_selected != null)
        {
            _selected.SetActive(focused);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _onFocus?.Invoke(this);
    }

    private void RedrawButton()
    {
        var maxed = _levelId >= Data.LevelsCount;
        if (_costLabel != null)
        {
            _costLabel.gameObject.SetActive(!maxed);
        }

        if (_costIcon != null)
        {
            _costIcon.gameObject.SetActive(!maxed);
        }

        if (_maxLabel != null)
        {
            _maxLabel.SetActive(maxed);
        }

        if (maxed)
        {
            CanPurchase = false;
            SetButtonEnabled(false);
            return;
        }

        var level = Data.GetLevel(_levelId);
        var cost = level == null ? 0 : level.Cost;
        if (_costLabel != null)
        {
            _costLabel.text = cost.ToString();
        }

        if (_costIcon != null)
        {
            _costIcon.sprite = GetPocketDollarIcon();
            _costIcon.enabled = _costIcon.sprite != null;
        }

        CanPurchase = CanAfford(cost);
        SetButtonEnabled(CanPurchase);
    }

    private void SetButtonEnabled(bool enabled)
    {
        if (_buyButton != null)
        {
            _buyButton.interactable = enabled;
        }

        if (_buyButtonImage == null)
        {
            return;
        }

        var sprite = enabled ? _enabledButtonSprite : _disabledButtonSprite;
        if (sprite != null)
        {
            _buyButtonImage.sprite = sprite;
        }
    }

    private bool CanAfford(int cost)
    {
        if (cost <= 0 || Managers.Instance == null || !Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies))
        {
            return false;
        }

        var save = currencies.GetCurrency(_playerId, CurrenciesManager.POCKET_DOLLAR_ID, true);
        return save != null && save.CanAfford(cost);
    }

    private Sprite GetPocketDollarIcon()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies))
        {
            return null;
        }

        return currencies.GetIcon(CurrenciesManager.POCKET_DOLLAR_ID);
    }

    private void HandlePurchase()
    {
        _onPurchase?.Invoke(this);
    }

    private void DisableNavigation()
    {
        if (_buyButton == null)
        {
            return;
        }

        var navigation = _buyButton.navigation;
        navigation.mode = Navigation.Mode.None;
        _buyButton.navigation = navigation;
    }
}
