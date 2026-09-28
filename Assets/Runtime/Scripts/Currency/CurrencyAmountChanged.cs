/// <summary>
/// 플레이어의 재화 수량이 바뀌었다.
/// </summary>
public struct CurrencyAmountChanged
{
    public int PlayerId;
    public string CurrencyId;
    public int Amount;
    public bool IsMetaBalance;

    public CurrencyAmountChanged(int playerId, string currencyId, int amount, bool isMetaBalance)
    {
        PlayerId = playerId;
        CurrencyId = currencyId;
        Amount = amount;
        IsMetaBalance = isMetaBalance;
    }
}
