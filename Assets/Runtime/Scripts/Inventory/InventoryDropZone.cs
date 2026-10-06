using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 칸이 아닌 곳에 놓을 자리. 가방 영역이면 장착 해제, 휴지통이면 판매한다.
/// </summary>
public class InventoryDropZone : MonoBehaviour, IDropHandler
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

    private Color _normalColor;
    private bool _hasNormalColor;
    private Tween _scaleTween;

    public ZoneKind Kind => _kind;

    private void Awake()
    {
        if (_highlight != null)
        {
            _normalColor = _highlight.color;
            _hasNormalColor = true;
        }
    }

    private void OnDisable()
    {
        SetHighlight(false);
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
}
