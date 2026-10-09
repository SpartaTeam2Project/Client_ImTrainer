using System.Collections.Generic;

/// <summary>
/// 같은 종, 같은 성 세 마리 합성. 재료 수집과 결과 배치만 하고 기록과 발행은 ItemManager가 한다.
/// </summary>
internal static class ItemSynthesis
{
    /// <summary>
    /// 가방과 장착 칸을 합쳐 합성한다.
    /// 재료는 놓은 장착 칸, 끈 장착 칸, 가방, 나머지 장착 칸 순서로 쓴다.
    /// 장착 칸이 재료로 쓰이면 결과는 처음 쓰인 장착 칸에 남고, 아니면 가방에 들어간다.
    /// usedSlots에는 재료로 쓴 장착 칸이 담긴다.
    /// </summary>
    public static bool TrySynthesize(RunInventory run, ItemCatalog catalog, int uid, int upgradeLevel, int targetSlot, int sourceSlot,
        List<int> usedSlots, out Item result, out string message)
    {
        result = null;
        usedSlots.Clear();
        var source = catalog.TryGetItem(uid);
        if (run == null || source == null)
        {
            message = "합성할 포켓몬이 없습니다.";
            return false;
        }

        if (run.Bag.GetItemNumber(uid, upgradeLevel) + run.Equipment.GetItemNumber(uid, upgradeLevel) < ItemManager.SYNTHESIS_COUNT)
        {
            message = "같은 성 세 마리가 필요합니다.";
            return false;
        }

        if (!TryCreateResult(catalog, source, upgradeLevel, out result, out message))
        {
            return false;
        }

        var bagSnapshot = run.Bag.Capture();
        run.TakeEquippedMaterial(targetSlot, uid, upgradeLevel, usedSlots);
        run.TakeEquippedMaterial(sourceSlot, uid, upgradeLevel, usedSlots);
        var remaining = ItemManager.SYNTHESIS_COUNT - usedSlots.Count;
        remaining -= run.Bag.RemoveItem(uid, upgradeLevel, remaining);
        for (var i = 0; i < run.Equipment.Stacks.Count && remaining > 0; i++)
        {
            if (run.TakeEquippedMaterial(i, uid, upgradeLevel, usedSlots))
            {
                remaining--;
            }
        }

        if (usedSlots.Count > 0)
        {
            run.Equipment.Stacks[usedSlots[0]].SetItem(result, 1);
            // 결과 칸은 개체 번호를 이어 써서 진화해도 같은 포켓몬으로 집계된다.
            run.Members.ReleaseMaterials(usedSlots);
        }
        else
        {
            var leftover = run.Bag.AddItem(result, 1);
            if (!leftover.Empty)
            {
                run.Bag.Restore(bagSnapshot);
                message = "가방이 가득 찼습니다.";
                return false;
            }
        }

        message = result.name + " " + result.upgradeLevel + "성이 되었습니다.";
        return true;
    }

    /// <summary>
    /// 합성 결과를 만든다. 3성 미만은 같은 종 한 성 위, 3성은 다음 진화 1성이다.
    /// </summary>
    private static bool TryCreateResult(ItemCatalog catalog, Item source, int upgradeLevel, out Item result, out string message)
    {
        result = null;
        message = string.Empty;
        if (upgradeLevel < Item.STAR_MAX)
        {
            result = catalog.CreateItem(source.uid, upgradeLevel + 1);
        }
        else if (source.evolutionUid >= 0)
        {
            result = catalog.CreateItem(source.evolutionUid, Item.STAR_MIN);
        }
        else
        {
            message = "다음 포켓몬이 없습니다.";
            return false;
        }

        if (result == null)
        {
            message = "합성 결과를 만들지 못했습니다.";
            return false;
        }

        return true;
    }
}
