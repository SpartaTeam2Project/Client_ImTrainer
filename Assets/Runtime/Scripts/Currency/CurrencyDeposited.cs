/// <summary>
/// 스테이지 재화가 들어왔다. Amount는 이번에 들어온 양이다.
/// </summary>
[System.Serializable]
public struct CurrencyDeposited
{
    public int PlayerId;
    public string CurrencyId;
    public int Amount;

    public CurrencyDeposited(int playerId, string currencyId, int amount)
    {
        PlayerId = playerId;
        CurrencyId = currencyId;
        Amount = amount;
    }
}
