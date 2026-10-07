using System.Collections.Generic;

/// <summary>
/// 한 플레이어의 판 가방, 장착 칸, 상점 칸.
/// </summary>
internal sealed class RunInventory
{
    public readonly InventoryHolder Bag;
    public readonly InventoryHolder Equipment;
    public readonly ShopOffer[] Offers;

    /// <summary>
    /// 이번 판 상점이 뽑는 종. 스테이지 세대로 거른 상점 후보다.
    /// </summary>
    public readonly List<int> ShopPool = new List<int>();

    private RunInventory(InventoryHolder bag, InventoryHolder equipment, ShopOffer[] offers)
    {
        Bag = bag;
        Equipment = equipment;
        Offers = offers;
    }

    public static RunInventory Create()
    {
        var bag = new InventoryHolder
        {
            Name = "Inventory",
            Type = InventoryHolder.HolderType.PlayerInventory,
            InventorySize = ItemManager.INVENTORY_SIZE
        };
        bag.EnsureSize();
        var equipment = new InventoryHolder
        {
            Name = "Equipment",
            Type = InventoryHolder.HolderType.PlayerEquipment,
            InventorySize = WeaponSlots.MAX_COUNT
        };
        equipment.EnsureSize();
        var offers = new ShopOffer[ItemManager.SHOP_OFFER_COUNT];
        for (var i = 0; i < offers.Length; i++)
        {
            offers[i] = new ShopOffer();
        }

        return new RunInventory(bag, equipment, offers);
    }

    /// <summary>
    /// 가방 칸에 포켓몬이 있으면 true.
    /// </summary>
    public bool HasBagItem(int bagIndex)
    {
        return bagIndex >= 0 && bagIndex < Bag.Stacks.Count && !Bag.Stacks[bagIndex].Empty;
    }

    public bool IsValidSlot(int slot)
    {
        return slot >= 0 && slot < Equipment.Stacks.Count;
    }

    /// <summary>
    /// 장착 칸에 포켓몬이 있으면 true.
    /// </summary>
    public bool HasEquipped(int slot)
    {
        return IsValidSlot(slot) && !Equipment.Stacks[slot].Empty;
    }

    /// <summary>
    /// 가방 칸 한 마리를 빈 장착 칸으로 옮긴다.
    /// </summary>
    public bool TryMoveToSlot(int bagIndex, int slot)
    {
        if (bagIndex < 0 || bagIndex >= Bag.Stacks.Count)
        {
            return false;
        }

        var stack = Bag.Stacks[bagIndex];
        if (stack.Empty || stack.Item == null)
        {
            return false;
        }

        if (!IsValidSlot(slot) || !Equipment.Stacks[slot].Empty)
        {
            return false;
        }

        var item = stack.Item.Copy();
        stack.AddNumber(-1);
        if (!Equipment.TrySetSlot(slot, item))
        {
            Bag.AddItem(item, 1);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 가방 칸을 비우고 들어 있던 포켓몬과 마릿수를 돌려준다.
    /// </summary>
    public bool TryTakeBag(int bagIndex, out Item item, out int number)
    {
        item = null;
        number = 0;
        if (!HasBagItem(bagIndex))
        {
            return false;
        }

        var stack = Bag.Stacks[bagIndex];
        item = stack.Item.Copy();
        number = stack.Number;
        stack.Delete();
        return true;
    }

    /// <summary>
    /// 장착 칸에서 뺄 포켓몬을 복사한다. 칸은 비우지 않는다. 마지막 한 마리는 뺄 수 없다.
    /// </summary>
    public bool TryTakeEquipped(int slot, out Item item, out string message)
    {
        item = null;
        message = string.Empty;
        if (!HasEquipped(slot))
        {
            message = "장착된 포켓몬이 없습니다.";
            return false;
        }

        if (Equipment.CountFilled() <= ItemManager.MIN_EQUIPPED)
        {
            message = "포켓몬은 한 마리 이상 장착해야 합니다.";
            return false;
        }

        item = Equipment.Stacks[slot].Item.Copy();
        return item != null;
    }

    /// <summary>
    /// 같은 종, 같은 성인 장착 칸을 합성 재료로 비운다. 이미 쓴 칸은 다시 쓰지 않는다.
    /// </summary>
    public bool TakeEquippedMaterial(int slot, int uid, int upgradeLevel, List<int> usedSlots)
    {
        if (!IsValidSlot(slot) || usedSlots.Count >= ItemManager.SYNTHESIS_COUNT || usedSlots.Contains(slot)
            || !Equipment.Stacks[slot].isSameItem(uid, upgradeLevel))
        {
            return false;
        }

        Equipment.Stacks[slot].Delete();
        usedSlots.Add(slot);
        return true;
    }
}
