using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 가방, 장착 칸을 끌어서 다른 칸이나 휴지통에 놓게 한다. 빈 칸을 끌면 가방 스크롤로 넘긴다.
/// </summary>
public class InventorySlotDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public enum SlotKind
    {
        None,
        Bag,
        Equipment
    }

    private InventoryUi _owner;
    private SlotKind _kind;
    private int _index = -1;
    private ScrollRect _scroll;
    private bool _dragging;
    private bool _scrolling;

    /// <summary>
    /// 이 칸이 어느 목록의 몇 번째인지 정한다.
    /// </summary>
    public void Bind(InventoryUi owner, SlotKind kind, int index)
    {
        _owner = owner;
        _kind = kind;
        _index = index;
        _scroll = GetComponentInParent<ScrollRect>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragging = false;
        _scrolling = false;
        if (_owner != null && _kind != SlotKind.None && _owner.BeginDrag(_kind, _index, eventData))
        {
            _dragging = true;
            eventData.eligibleForClick = false;
            return;
        }

        if (_scroll != null)
        {
            _scrolling = true;
            _scroll.OnBeginDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragging)
        {
            _owner.MoveDrag(eventData);
            return;
        }

        if (_scrolling)
        {
            _scroll.OnDrag(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragging)
        {
            _dragging = false;
            _owner.EndDrag();
            return;
        }

        if (_scrolling)
        {
            _scrolling = false;
            _scroll.OnEndDrag(eventData);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (_owner == null || _kind == SlotKind.None)
        {
            return;
        }

        _owner.DropOnSlot(_kind, _index);
    }

    private void OnDisable()
    {
        if (_dragging && _owner != null)
        {
            _owner.EndDrag();
        }

        _dragging = false;
        _scrolling = false;
    }
}
