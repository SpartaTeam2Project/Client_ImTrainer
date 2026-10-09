/// <summary>
/// 포켓몬을 팔았다. 장착 칸에서 팔았으면 FromEquipment가 true다.
/// </summary>
[System.Serializable]
public struct ItemSold
{
    public int PlayerId;
    public int Uid;
    public int UpgradeLevel;
    public int Count;
    public bool FromEquipment;

    public ItemSold(int playerId, int uid, int upgradeLevel, int count, bool fromEquipment)
    {
        PlayerId = playerId;
        Uid = uid;
        UpgradeLevel = upgradeLevel;
        Count = count;
        FromEquipment = fromEquipment;
    }
}
