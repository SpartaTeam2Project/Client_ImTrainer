/// <summary>
/// 합성이 끝났다. 3성 합성이면 다음 포켓몬으로 진화한다. 결과가 가방에 들어가면 EquipmentSlot은 -1이다.
/// </summary>
public struct ItemSynthesized
{
    public int PlayerId;
    public int PreviousUid;
    public int Uid;
    public int UpgradeLevel;
    public bool Evolved;
    public int EquipmentSlot;

    public ItemSynthesized(int playerId, int previousUid, int uid, int upgradeLevel, bool evolved, int equipmentSlot)
    {
        PlayerId = playerId;
        PreviousUid = previousUid;
        Uid = uid;
        UpgradeLevel = upgradeLevel;
        Evolved = evolved;
        EquipmentSlot = equipmentSlot;
    }
}
