using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스킬트리 오른쪽 상세 패널. 고른 칸의 설명, 다음 레벨 비용, 해금 조건을 보여 준다.
/// </summary>
public class TrainingDetailPanel : MonoBehaviour
{
    private const string LEVEL_FORMAT = "({0}/{1})";
    private const string NEXT_EFFECT_FORMAT = "다음 레벨: {0}";
    private const string PERCENT_BONUS_FORMAT = "보너스 +{0}%";
    private const string PERCENT_REDUCTION_FORMAT = "필요 경험치 -{0}%";
    private const string COUNT_FORMAT = "+{0}";
    private const float HIDDEN_VALUE = 0f;
    private const float PERCENT_SCALE = 100f;
    private const float BACKGROUND_SCALE = 0.3f;

    private static readonly Color AFFORD_COLOR = new Color(0.55f, 1f, 0.55f, 1f);
    private static readonly Color CANNOT_AFFORD_COLOR = new Color(1f, 0.4f, 0.4f, 1f);

    [SerializeField] private TMP_Text _titleLabel;
    [SerializeField] private TMP_Text _levelLabel;
    [SerializeField] private Image _iconBackground;
    [SerializeField] private Image _iconFrame;
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _descriptionLabel;
    [SerializeField] private TMP_Text _effectLabel;
    [SerializeField] private GameObject _costRoot;
    [SerializeField] private Image _costIcon;
    [SerializeField] private TMP_Text _costLabel;
    [SerializeField] private GameObject _maxLabel;
    [SerializeField] private TMP_Text _lockLabel;

    /// <summary>
    /// 칸 내용을 패널에 그린다. lockText는 잠겼을 때 띄울 문구다.
    /// </summary>
    public void Show(TrainingSlotBehavior slot, string lockText, bool canAfford, Sprite costIcon)
    {
        if (slot == null || slot.Data == null)
        {
            return;
        }

        var data = slot.Data;
        SetText(_titleLabel, data.Title);
        SetText(_levelLabel, string.Format(LEVEL_FORMAT, Mathf.Min(slot.Level, data.LevelsCount), data.LevelsCount));
        SetText(_descriptionLabel, data.Description);

        if (_icon != null)
        {
            _icon.sprite = data.Icon;
            _icon.enabled = data.Icon != null;
            _icon.preserveAspect = true;
        }

        if (_iconFrame != null)
        {
            _iconFrame.color = slot.Color;
        }

        if (_iconBackground != null)
        {
            var color = slot.Color;
            _iconBackground.color = new Color(color.r * BACKGROUND_SCALE, color.g * BACKGROUND_SCALE, color.b * BACKGROUND_SCALE, 1f);
        }

        var nextLevel = slot.IsMaxed ? null : data.GetLevel(slot.Level);
        RedrawEffect(data, nextLevel);
        RedrawCost(slot, nextLevel, canAfford, costIcon);

        if (_lockLabel != null)
        {
            _lockLabel.gameObject.SetActive(!slot.Unlocked);
            _lockLabel.text = lockText ?? string.Empty;
        }
    }

    private void RedrawEffect(TrainingData data, TrainingLevel nextLevel)
    {
        if (_effectLabel == null)
        {
            return;
        }

        var value = nextLevel == null ? HIDDEN_VALUE : nextLevel.Value;
        _effectLabel.text = value <= HIDDEN_VALUE
            ? string.Empty
            : string.Format(NEXT_EFFECT_FORMAT, FormatEffect(data.ValueDisplay, value));
    }

    private void RedrawCost(TrainingSlotBehavior slot, TrainingLevel nextLevel, bool canAfford, Sprite costIcon)
    {
        var maxed = slot.IsMaxed;
        if (_maxLabel != null)
        {
            _maxLabel.SetActive(maxed);
        }

        if (_costRoot != null)
        {
            _costRoot.SetActive(!maxed && nextLevel != null);
        }

        if (maxed || nextLevel == null)
        {
            return;
        }

        if (_costLabel != null)
        {
            _costLabel.text = nextLevel.Cost.ToString();
            _costLabel.color = canAfford ? AFFORD_COLOR : CANNOT_AFFORD_COLOR;
        }

        if (_costIcon != null)
        {
            _costIcon.sprite = costIcon;
            _costIcon.enabled = costIcon != null;
        }
    }

    private static string FormatEffect(TrainingValueDisplay display, float value)
    {
        switch (display)
        {
            case TrainingValueDisplay.PercentReduction:
                return string.Format(PERCENT_REDUCTION_FORMAT, Mathf.RoundToInt(value * PERCENT_SCALE));
            case TrainingValueDisplay.Count:
                return string.Format(COUNT_FORMAT, Mathf.RoundToInt(value));
            default:
                return string.Format(PERCENT_BONUS_FORMAT, Mathf.RoundToInt(value * PERCENT_SCALE));
        }
    }

    private static void SetText(TMP_Text label, string text)
    {
        if (label != null)
        {
            label.text = text;
        }
    }
}
