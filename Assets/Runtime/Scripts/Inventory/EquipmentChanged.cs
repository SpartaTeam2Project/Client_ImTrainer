/// <summary>
/// 장착 칸 하나가 바뀌었다. 비면 Uid는 -1이다.
/// </summary>
public struct EquipmentChanged
{
    public int PlayerId;
    public int Slot;
    public int Uid;
    public int UpgradeLevel;

    public EquipmentChanged(int playerId, int slot, int uid, int upgradeLevel)
    {
        PlayerId = playerId;
        Slot = slot;
        Uid = uid;
        UpgradeLevel = upgradeLevel;
    }
}

/// <summary>
/// 가방이나 장착 목록이 바뀌어 창을 다시 그린다.
/// </summary>
public struct InventoryChanged
{
    public int PlayerId;

    public InventoryChanged(int playerId)
    {
        PlayerId = playerId;
    }
}
