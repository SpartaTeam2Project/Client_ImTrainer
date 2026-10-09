using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 창에서 방향키로 장착 칸과 가방 칸 사이를 옮겨 다닌다.
/// 칸 배치를 따로 적지 않고 화면 위치로 누른 방향에 가장 가까운 칸을 고른다.
/// </summary>
public sealed class InventoryFocusNavigator
{
    // 진행 방향에서 옆으로 벗어난 거리를 이만큼 더 멀게 본다. 같은 줄, 같은 열 칸을 먼저 고른다.
    private const float SIDE_DISTANCE_WEIGHT = 2f;

    private readonly List<RectTransform> _bag = new List<RectTransform>();
    private readonly List<RectTransform> _equipment = new List<RectTransform>();
    private readonly StorageScrollFollow _scrollFollow = new StorageScrollFollow();
    private ScrollRect _bagScroll;

    /// <summary>
    /// 옮겨 다닐 칸을 정한다. 가방은 정렬돼서 빈 칸이 끝에 몰리므로 차 있는 칸 수만큼만 받는다.
    /// </summary>
    public void SetSlots(IReadOnlyList<InventoryItem> bagSlots, int bagCount, EquipmentUi equipmentUi, ScrollRect bagScroll)
    {
        _bagScroll = bagScroll;
        _bag.Clear();
        for (var i = 0; i < bagCount && i < bagSlots.Count; i++)
        {
            _bag.Add(bagSlots[i] != null ? (RectTransform)bagSlots[i].transform : null);
        }

        _equipment.Clear();
        for (var i = 0; equipmentUi != null && i < WeaponSlots.MAX_COUNT; i++)
        {
            var slot = equipmentUi.GetSlot(i);
            _equipment.Add(slot != null ? (RectTransform)slot.transform : null);
        }
    }

    /// <summary>
    /// 포커스 칸이 아직 옮겨 다닐 수 있는 칸이면 true.
    /// </summary>
    public bool Contains(InventorySlotDrag.SlotKind kind, int index)
    {
        return GetRect(kind, index) != null;
    }

    /// <summary>
    /// 처음 포커스할 칸. 가방 첫 칸, 가방이 비면 첫 장착 칸이다.
    /// </summary>
    public bool TryGetFirst(out InventorySlotDrag.SlotKind kind, out int index)
    {
        kind = _bag.Count > 0 ? InventorySlotDrag.SlotKind.Bag : InventorySlotDrag.SlotKind.Equipment;
        index = 0;
        return GetRect(kind, index) != null;
    }

    /// <summary>
    /// 지금 칸에서 누른 방향으로 가장 가까운 칸을 찾는다. 그쪽에 칸이 없으면 false다.
    /// </summary>
    public bool TryMove(InventorySlotDrag.SlotKind kind, int index, Vector2Int move,
        out InventorySlotDrag.SlotKind nextKind, out int nextIndex)
    {
        nextKind = kind;
        nextIndex = index;
        var from = GetRect(kind, index);
        if (from == null)
        {
            return TryGetFirst(out nextKind, out nextIndex);
        }

        var origin = Center(from);
        var direction = new Vector2(move.x, move.y).normalized;
        var bestScore = float.MaxValue;
        Pick(_bag, InventorySlotDrag.SlotKind.Bag, from, origin, direction, ref bestScore, ref nextKind, ref nextIndex);
        Pick(_equipment, InventorySlotDrag.SlotKind.Equipment, from, origin, direction, ref bestScore, ref nextKind, ref nextIndex);
        return bestScore < float.MaxValue;
    }

    /// <summary>
    /// 가방 칸이 스크롤 밖이면 보이게 옮긴다.
    /// </summary>
    public void ScrollTo(InventorySlotDrag.SlotKind kind, int index)
    {
        if (kind == InventorySlotDrag.SlotKind.Bag)
        {
            _scrollFollow.ScrollTo(_bagScroll, GetRect(kind, index));
        }
    }

    private static void Pick(List<RectTransform> rects, InventorySlotDrag.SlotKind kind, RectTransform from, Vector2 origin,
        Vector2 direction, ref float bestScore, ref InventorySlotDrag.SlotKind bestKind, ref int bestIndex)
    {
        for (var i = 0; i < rects.Count; i++)
        {
            var rect = rects[i];
            if (rect == null || rect == from)
            {
                continue;
            }

            // 칸 크기 기준으로 재야 캔버스 배율과 상관없이 같은 줄을 판정한다.
            var delta = (Center(rect) - origin) / Mathf.Max(1f, from.rect.width * from.lossyScale.x);
            var forward = Vector2.Dot(delta, direction);
            if (forward < 0.5f)
            {
                continue;
            }

            var side = Mathf.Abs(delta.x * direction.y - delta.y * direction.x);
            var score = forward + side * SIDE_DISTANCE_WEIGHT;
            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestKind = kind;
            bestIndex = i;
        }
    }

    private RectTransform GetRect(InventorySlotDrag.SlotKind kind, int index)
    {
        var rects = kind == InventorySlotDrag.SlotKind.Bag ? _bag
            : kind == InventorySlotDrag.SlotKind.Equipment ? _equipment
            : null;
        return rects != null && index >= 0 && index < rects.Count ? rects[index] : null;
    }

    private static Vector2 Center(RectTransform rect)
    {
        return rect.TransformPoint(rect.rect.center);
    }
}
