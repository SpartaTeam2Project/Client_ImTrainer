using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 가방, 장착, 상점이 같이 쓰는 칸. 포켓몬 그림, 이름, 성, 개수만 그린다.
/// </summary>
public class InventoryItem : MonoBehaviour
{
    private static readonly Color EMPTY_FRAME = new Color32(100, 100, 100, 255);
    private static readonly Color EMPTY_BACKGROUND = new Color32(100, 100, 100, 255);
    private static readonly Color FILLED_FRAME = Color.white;
    private static readonly Color FILLED_BACKGROUND = Color.white;

    [SerializeField] private Image _frame;
    [SerializeField] private Image[] _starImages = new Image[Item.STAR_MAX];
    [SerializeField] private Image _background;
    [SerializeField] private Image _icon;
    [SerializeField] private Image _select;
    [SerializeField] private GameObject _state;
    [SerializeField] private TextMeshProUGUI _starText;
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private Button _button;

    private InventorySlotDrag _drag;

    public Button Button => _button;

    /// <summary>
    /// 끌기를 받는 컴포넌트. 프리팹에 없으면 붙여서 돌려준다.
    /// </summary>
    public InventorySlotDrag Drag
    {
        get
        {
            if (_drag == null && !TryGetComponent(out _drag))
            {
                _drag = gameObject.AddComponent<InventorySlotDrag>();
            }

            return _drag;
        }
    }

    private void Awake()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        ApplyFont(_starText);
        ApplyFont(_countText);
        ShowEmpty();
    }

    /// <summary>
    /// 빈 칸으로 둔다. 테두리와 배경만 남긴다.
    /// </summary>
    public void ShowEmpty()
    {
        if (_icon != null)
        {
            _icon.sprite = null;
            _icon.enabled = false;
        }

        if (_state != null)
        {
            _state.SetActive(false);
        }

        ShowStars(0);
        SetSelected(false);
        if (_frame != null)
        {
            _frame.color = EMPTY_FRAME;
        }

        if (_background != null)
        {
            _background.color = EMPTY_BACKGROUND;
        }
    }

    /// <summary>
    /// 포켓몬 칸을 채운다. 가방이면 개수도 적는다.
    /// </summary>
    public void ShowPokemon(Sprite portrait, string pokemonName, int star, int count, bool showCount, bool selected)
    {
        if (_icon != null)
        {
            _icon.sprite = portrait;
            _icon.enabled = portrait != null;
            _icon.preserveAspect = true;
            _icon.color = Color.white;
        }

        if (_starText != null)
        {
            var label = string.IsNullOrEmpty(pokemonName) ? string.Empty : pokemonName;
            _starText.text = label + " " + star + "성";
        }

        if (_countText != null)
        {
            _countText.text = "x" + count;
        }

        ShowStars(star);
        SetSelected(selected);
        if (_frame != null)
        {
            _frame.color = FILLED_FRAME;
        }

        if (_background != null)
        {
            _background.color = FILLED_BACKGROUND;
        }
    }

    /// <summary>
    /// 고른 칸 표시를 켜거나 끈다.
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (_select != null)
        {
            _select.enabled = selected;
        }
    }

    private void ShowStars(int star)
    {
        ShowStars(_starImages, star);
    }

    /// <summary>
    /// 별 이미지를 성 수만큼 켠다. 정보 창도 같이 쓴다.
    /// </summary>
    public static void ShowStars(Image[] starImages, int star)
    {
        if (starImages == null)
        {
            return;
        }

        for (var i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null)
            {
                continue;
            }

            starImages[i].gameObject.SetActive(i < star);
        }
    }

    private static void ApplyFont(TextMeshProUGUI text)
    {
        if (text == null || text.font != null || TMP_Settings.defaultFontAsset == null)
        {
            return;
        }

        text.font = TMP_Settings.defaultFontAsset;
    }
}
