using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 칸이 아닌 곳에 놓을 자리. 가방 영역이면 장착 해제, 휴지통이면 판매한다.
/// focusSprite를 넣으면 마우스가 올라와 있는 동안(끌고 있을 때 포함) 그 그림으로 바꾼다.
/// </summary>
public class InventoryDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum ZoneKind
    {
        Bag,
        Trash
    }

    private const float HIGHLIGHT_SCALE = 1.08f;
    private const float HIGHLIGHT_DURATION = 0.12f;

    [SerializeField] private ZoneKind _kind;
    [SerializeField] private InventoryUi _owner;
    [SerializeField] private Graphic _highlight;
    [SerializeField] private Color _highlightColor = new Color32(220, 80, 80, 255);
    [SerializeField] private Image _image;
    [SerializeField] private Sprite _focusSprite;

    private Color _normalColor;
    private bool _hasNormalColor;
    private Tween _scaleTween;
    private Sprite _normalSprite;
    private PressFlash _press;

    public ZoneKind Kind => _kind;

    private void Awake()
    {
        if (_highlight != null)
        {
            _normalColor = _highlight.color;
            _hasNormalColor = true;
        }

        if (_image != null)
        {
            _normalSprite = _image.sprite;
        }

        _press = new PressFlash(_image, _focusSprite, gameObject);
    }

    private void OnDisable()
    {
        SetHighlight(false);
        SetFocus(false);
        _press?.Release();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetFocus(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetFocus(false);
    }

    /// <summary>
    /// 단축키로 이 자리 동작을 했을 때 잠깐 포커스 그림을 보여 준다.
    /// </summary>
    public void PlayPress()
    {
        _press?.Play();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (_owner != null)
        {
            _owner.DropOnZone(_kind);
        }
    }

    /// <summary>
    /// 끄는 동안 놓을 수 있다는 걸 보여 준다. 창이 열려 있으면 시간이 멈춰 있어서 시간 정지와 상관없이 돈다.
    /// </summary>
    public void SetHighlight(bool on)
    {
        if (_scaleTween != null)
        {
            _scaleTween.Kill();
            _scaleTween = null;
        }

        if (_highlight != null && _hasNormalColor)
        {
            _highlight.color = on ? _highlightColor : _normalColor;
        }

        if (!isActiveAndEnabled)
        {
            transform.localScale = Vector3.one;
            return;
        }

        _scaleTween = transform.DOScale(on ? HIGHLIGHT_SCALE : 1f, HIGHLIGHT_DURATION).SetUpdate(true).SetLink(gameObject);
    }

    private void SetFocus(bool on)
    {
        if (_image == null || _focusSprite == null)
        {
            return;
        }

        _image.sprite = on ? _focusSprite : _normalSprite;
    }
}
