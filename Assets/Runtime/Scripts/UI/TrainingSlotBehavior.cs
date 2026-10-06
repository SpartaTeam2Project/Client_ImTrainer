using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 스킬트리 한 칸. 아이콘과 산 레벨(x/N), 잠금 상태를 보여 준다.
/// </summary>
public class TrainingSlotBehavior : MonoBehaviour, IPointerEnterHandler
{
    private const string LEVEL_FORMAT = "{0}/{1}";
    private const float UNLOCKED_BACKGROUND_SCALE = 0.3f;
    private const float LOCKED_COLOR_SCALE = 0.45f;
    private const float LOCKED_BACKGROUND_SCALE = 0.15f;

    private static readonly Color MAXED_LABEL_COLOR = new Color(1f, 0.85f, 0.35f, 1f);
    private static readonly Color UNLOCKED_LABEL_COLOR = Color.white;
    private static readonly Color LOCKED_LABEL_COLOR = new Color(0.55f, 0.55f, 0.6f, 1f);

    [SerializeField] private RectTransform _rect;
    [SerializeField] private Button _button;
    [SerializeField] private Image _background;
    [SerializeField] private Image _frame;
    [SerializeField] private Image _icon;
    [SerializeField] private GameObject _lockOverlay;
    [SerializeField] private TMP_Text _levelLabel;
    [SerializeField] private GameObject _selected;

    private Action<TrainingSlotBehavior> _onFocus;
    private Action<TrainingSlotBehavior> _onSubmit;

    public RectTransform Rect => _rect;

    public TrainingData Data { get; private set; }

    /// <summary>
    /// 이 칸이 속한 열의 색.
    /// </summary>
    public Color Color { get; private set; }

    /// <summary>
    /// 지금까지 산 레벨 수. 0이면 아직 사지 않았다.
    /// </summary>
    public int Level { get; private set; }

    public bool Unlocked { get; private set; }

    public bool IsMaxed => Data != null && Level >= Data.LevelsCount;

    private void Awake()
    {
        if (_rect == null)
        {
            _rect = transform as RectTransform;
        }

        if (_button != null)
        {
            var navigation = _button.navigation;
            navigation.mode = Navigation.Mode.None;
            _button.navigation = navigation;
        }
    }

    private void OnEnable()
    {
        if (_button != null)
        {
            _button.onClick.AddListener(HandleClick);
        }
    }

    private void OnDisable()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
        }
    }

    /// <summary>
    /// 데이터와 열 색, 콜백을 넣는다.
    /// </summary>
    public void Init(TrainingData data, Color color, Action<TrainingSlotBehavior> onFocus, Action<TrainingSlotBehavior> onSubmit)
    {
        Data = data;
        Color = color;
        _onFocus = onFocus;
        _onSubmit = onSubmit;

        if (_icon != null)
        {
            _icon.sprite = data == null ? null : data.Icon;
            _icon.enabled = _icon.sprite != null;
            _icon.preserveAspect = true;
        }
    }

    /// <summary>
    /// 산 레벨 수와 해금 여부로 칸을 다시 그린다.
    /// </summary>
    public void Redraw(int level, bool unlocked)
    {
        Level = level;
        Unlocked = unlocked;
        if (Data == null)
        {
            return;
        }

        var frameColor = unlocked ? Color : Scale(Color, LOCKED_COLOR_SCALE);
        if (_frame != null)
        {
            _frame.color = frameColor;
        }

        if (_background != null)
        {
            _background.color = Scale(Color, unlocked ? UNLOCKED_BACKGROUND_SCALE : LOCKED_BACKGROUND_SCALE);
        }

        if (_icon != null)
        {
            _icon.color = unlocked ? Color.white : new Color(1f, 1f, 1f, LOCKED_COLOR_SCALE);
        }

        if (_lockOverlay != null)
        {
            _lockOverlay.SetActive(!unlocked);
        }

        if (_levelLabel != null)
        {
            _levelLabel.text = string.Format(LEVEL_FORMAT, Mathf.Min(level, Data.LevelsCount), Data.LevelsCount);
            _levelLabel.color = !unlocked ? LOCKED_LABEL_COLOR : IsMaxed ? MAXED_LABEL_COLOR : UNLOCKED_LABEL_COLOR;
        }
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

    private void HandleClick()
    {
        _onFocus?.Invoke(this);
        _onSubmit?.Invoke(this);
    }

    private static Color Scale(Color color, float scale)
    {
        return new Color(color.r * scale, color.g * scale, color.b * scale, 1f);
    }
}
