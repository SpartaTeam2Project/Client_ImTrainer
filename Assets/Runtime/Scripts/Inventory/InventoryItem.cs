using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 가방, 장착, 상점이 같이 쓰는 칸. 그림과 가격 자리가 여기 있다.
/// </summary>
public class InventoryItem : MonoBehaviour
{
    private static readonly Color EMPTY_FRAME = new Color(0.28f, 0.3f, 0.34f, 1f);
    private static readonly Color EMPTY_BACKGROUND = new Color(0.12f, 0.13f, 0.16f, 1f);
    private static readonly Color FILLED_FRAME = new Color(0.86f, 0.72f, 0.34f, 1f);
    private static readonly Color FILLED_BACKGROUND = new Color(0.18f, 0.22f, 0.3f, 1f);

    [SerializeField] private Image _frame;
    [SerializeField] private Image[] _starImages = new Image[Item.STAR_MAX];
    [SerializeField] private Image _background;
    [SerializeField] private Image _icon;
    [SerializeField] private Image _select;
    [SerializeField] private GameObject _state;
    [SerializeField] private TextMeshProUGUI _starText;
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private GameObject _priceRoot;
    [SerializeField] private Image _priceFrame;
    [SerializeField] private Image _priceBackground;
    [SerializeField] private Image _priceIcon;
    [SerializeField] private TextMeshProUGUI _priceCount;
    [SerializeField] private Button _button;

    public Button Button => _button;

    private void Awake()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        ApplyFont(_starText);
        ApplyFont(_countText);
        ApplyFont(_priceCount);
        HidePrice();
        ShowEmpty();
    }

    /// <summary>
    /// 빈 칸으로 둔다. 테두리와 배경만 남긴다.
    /// </summary>
    public void ShowEmpty()
    {
        SetPokemonVisual(true);
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
        SetPokemonVisual(true);
        if (_icon != null)
        {
            _icon.sprite = portrait;
            _icon.enabled = portrait != null;
            _icon.preserveAspect = true;
            _icon.color = Color.white;
        }

        if (_state != null)
        {
            _state.SetActive(true);
        }

        if (_starText != null)
        {
            var label = string.IsNullOrEmpty(pokemonName) ? string.Empty : pokemonName;
            _starText.text = label + " " + star + "성";
        }

        if (_countText != null)
        {
            _countText.gameObject.SetActive(showCount);
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

    /// <summary>
    /// 상점 가격 자리를 켜고 몬스터볼 그림과 개수를 넣는다.
    /// </summary>
    public void ShowPrice(Sprite icon, int amount)
    {
        if (_priceRoot != null)
        {
            _priceRoot.SetActive(true);
        }

        if (_priceIcon != null)
        {
            _priceIcon.sprite = icon;
            _priceIcon.enabled = icon != null;
            _priceIcon.preserveAspect = true;
        }

        if (_priceCount != null)
        {
            _priceCount.text = amount.ToString();
        }
    }

    /// <summary>
    /// 가격 자리를 끈다.
    /// </summary>
    public void HidePrice()
    {
        if (_priceRoot != null)
        {
            _priceRoot.SetActive(false);
        }
    }

    /// <summary>
    /// 보유 수량만 가격 자리로 보여 준다.
    /// </summary>
    public void ShowBalance(Sprite icon, int amount)
    {
        ShowEmpty();
        SetPokemonVisual(false);
        ShowPrice(icon, amount);
        StretchPrice();
        if (_button != null)
        {
            _button.interactable = false;
        }
    }

    private void SetPokemonVisual(bool visible)
    {
        if (_frame != null)
        {
            _frame.gameObject.SetActive(visible);
        }

        if (_background != null)
        {
            _background.gameObject.SetActive(visible);
        }

        if (_icon != null)
        {
            _icon.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// 프레임 아래 별 이미지를 성 수만큼 켠다.
    /// </summary>
    private void ShowStars(int star)
    {
        if (_starImages == null)
        {
            return;
        }

        for (var i = 0; i < _starImages.Length; i++)
        {
            if (_starImages[i] == null)
            {
                continue;
            }

            _starImages[i].gameObject.SetActive(i < star);
        }
    }

    private void StretchPrice()
    {
        if (_priceRoot == null)
        {
            return;
        }

        var rect = _priceRoot.transform as RectTransform;
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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
