using System;

/// <summary>
/// 장착 칸을 바로 해제하거나 버린다. 빈 칸이면 아무 반응 없이 넘기고,
/// 디졸브로 판매 중인 장착 칸까지 빼고 마지막 한 마리면 경고와 error 소리를 낸다. InventoryUi가 만든다.
/// 판매는 InventorySellDirector.Sell이 맡는다.
/// </summary>
public sealed class InventoryEquipmentActions
{
    private const string UNEQUIP_SOUND = "inventory_unequip";
    private const string ERROR_SOUND = "error";

    private readonly InventorySellDirector _sellDirector;

    public InventoryEquipmentActions(InventorySellDirector sellDirector)
    {
        _sellDirector = sellDirector;
    }

    /// <summary>
    /// 장착 칸을 가방으로 되돌린다. 성공하면 해제 소리, 실패하면 error.
    /// </summary>
    public void Unequip(int slot)
    {
        Remove(slot, (itemManager, playerId) => itemManager.TryUnequip(playerId, slot), UNEQUIP_SOUND);
    }

    /// <summary>
    /// 장착 칸을 환급 없이 비운다. 성공하면 소리 없음, 실패하면 error.
    /// </summary>
    public void Discard(int slot)
    {
        Remove(slot, (itemManager, playerId) => itemManager.TryDiscardEquipped(playerId, slot), null);
    }

    private void Remove(int slot, Func<ItemManager, int, bool> remove, string successSound)
    {
        if (!TryGetContext(out var itemManager, out var playerId) || !HasEquipped(itemManager, playerId, slot))
        {
            return;
        }

        UiSound.PlayResult(_sellDirector.TryReserveEquipped() && remove(itemManager, playerId), successSound, ERROR_SOUND);
    }

    private static bool HasEquipped(ItemManager itemManager, int playerId, int slot)
    {
        var equipment = itemManager.GetEquipment(playerId);
        return equipment != null && slot >= 0 && slot < equipment.Stacks.Count && !equipment.Stacks[slot].Empty;
    }

    private static bool TryGetContext(out ItemManager itemManager, out int playerId)
    {
        itemManager = null;
        playerId = 0;
        if (Managers.Instance == null
            || !Managers.Instance.TryGetManager<ItemManager>(out itemManager)
            || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return false;
        }

        playerId = playerManager.LocalPlayerId;
        return true;
    }
}
