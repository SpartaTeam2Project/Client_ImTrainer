/// <summary>
/// 상점에서 포켓몬 한 마리를 샀다.
/// </summary>
[System.Serializable]
public struct ItemPurchased
{
    public int PlayerId;
    public int Uid;
    public int UpgradeLevel;
    public string CurrencyId;
    public int Price;

    public ItemPurchased(int playerId, int uid, int upgradeLevel, string currencyId, int price)
    {
        PlayerId = playerId;
        Uid = uid;
        UpgradeLevel = upgradeLevel;
        CurrencyId = currencyId;
        Price = price;
    }
}
