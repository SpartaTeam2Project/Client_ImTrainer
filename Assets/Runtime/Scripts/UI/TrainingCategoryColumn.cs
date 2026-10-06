using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스킬트리의 한 열. 헤더, 카테고리 Lv, 칸 배치를 맡는다.
/// </summary>
public class TrainingCategoryColumn : MonoBehaviour
{
    private const string LEVEL_FORMAT = "Lv.{0}";

    [SerializeField] private TrainingCategory _category;
    [SerializeField] private string _title;
    [SerializeField] private Color _color = Color.white;
    [SerializeField] private TMP_Text _titleLabel;
    [SerializeField] private TMP_Text _levelLabel;
    [SerializeField] private Image _underline;
    [SerializeField] private RectTransform _slotsParent;
    [SerializeField] private Vector2 _cellSize = new Vector2(80f, 104f);
    [SerializeField] private Vector2 _spacing = new Vector2(14f, 10f);

    public TrainingCategory Category => _category;

    public string Title => _title ?? string.Empty;

    public Color Color => _color;

    private void Awake()
    {
        if (_titleLabel != null)
        {
            _titleLabel.text = Title;
            _titleLabel.color = _color;
        }

        if (_underline != null)
        {
            _underline.color = _color;
        }
    }

    /// <summary>
    /// 칸을 이 열의 (행, 칸) 위치에 놓는다. 빈자리가 있어도 위치는 그대로다.
    /// </summary>
    public void Place(TrainingSlotBehavior slot, int row, int column)
    {
        if (slot == null || _slotsParent == null)
        {
            return;
        }

        var rect = slot.Rect != null ? slot.Rect : (RectTransform)slot.transform;
        rect.SetParent(_slotsParent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = _cellSize;
        rect.anchoredPosition = new Vector2(
            column * (_cellSize.x + _spacing.x),
            -row * (_cellSize.y + _spacing.y));
    }

    /// <summary>
    /// 헤더 아래 카테고리 Lv를 바꾼다.
    /// </summary>
    public void SetLevel(int level)
    {
        if (_levelLabel != null)
        {
            _levelLabel.text = string.Format(LEVEL_FORMAT, level);
        }
    }
}
