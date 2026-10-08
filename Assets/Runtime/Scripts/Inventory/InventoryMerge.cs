/// <summary>
/// 출발 칸 포켓몬을 다른 칸에 겹쳐 합성하는 판정. 끌어 놓기와 키보드 합성이 같이 쓴다.
/// </summary>
internal static class InventoryMerge
{
    /// <summary>
    /// 칸에 출발 칸과 같은 종, 같은 성 포켓몬이 있으면 true.
    /// </summary>
    public static bool IsSame(InventoryHolder holder, int index, InventorySlotRef source)
    {
        return holder != null && index >= 0 && index < holder.Stacks.Count && holder.Stacks[index].isSameItem(source.Uid, source.Star);
    }

    /// <summary>
    /// 가방은 바뀔 때마다 정렬돼서 기억한 칸 번호 대신 종과 성으로 다시 찾는다. 없으면 -1이다.
    /// </summary>
    public static int ResolveBagIndex(InventoryHolder bag, InventorySlotRef source)
    {
        if (bag == null)
        {
            return -1;
        }

        if (IsSame(bag, source.Index, source))
        {
            return source.Index;
        }

        return bag.FindIndex(source.Uid, source.Star);
    }

    /// <summary>
    /// 대상 칸에 놓으면 합성하는지 본다. 같은 종, 같은 성이어야 한다.
    /// 대상이 출발 칸 자신이면 allowSelf일 때만 true이고, 그때는 남은 재료를 가방과 장착에서 모은다.
    /// </summary>
    public static bool IsTarget(InventoryHolder bag, InventoryHolder equipment, InventorySlotRef source,
        InventorySlotDrag.SlotKind targetKind, int targetIndex, bool allowSelf)
    {
        if (source.IsEmpty || targetKind == InventorySlotDrag.SlotKind.None)
        {
            return false;
        }

        var holder = targetKind == InventorySlotDrag.SlotKind.Bag ? bag : equipment;
        if (!IsSame(holder, targetIndex, source))
        {
            return false;
        }

        return allowSelf || !IsSelf(bag, source, targetKind, targetIndex);
    }

    /// <summary>
    /// 대상 칸으로 합성한다. 장착 칸이 재료로 쓰이면 결과는 그 칸에 남는다. IsTarget으로 먼저 확인한다.
    /// </summary>
    public static bool Merge(ItemManager itemManager, int playerId, InventorySlotRef source,
        InventorySlotDrag.SlotKind targetKind, int targetIndex)
    {
        var sourceSlot = source.Kind == InventorySlotDrag.SlotKind.Equipment ? source.Index : -1;
        var targetSlot = targetKind == InventorySlotDrag.SlotKind.Equipment && targetIndex != sourceSlot ? targetIndex : -1;
        return itemManager.TrySynthesize(playerId, source.Uid, source.Star, targetSlot, sourceSlot);
    }

    private static bool IsSelf(InventoryHolder bag, InventorySlotRef source, InventorySlotDrag.SlotKind targetKind, int targetIndex)
    {
        if (targetKind != source.Kind)
        {
            return false;
        }

        return targetKind == InventorySlotDrag.SlotKind.Bag
            ? targetIndex == ResolveBagIndex(bag, source)
            : targetIndex == source.Index;
    }
}
