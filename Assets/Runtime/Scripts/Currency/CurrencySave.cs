/// <summary>
/// 플레이어 하나의 재화 수량.
/// </summary>
public class CurrencySave
{
    private int _amount;

    public CurrencySave(int amount)
    {
        _amount = amount < 0 ? 0 : amount;
    }

    public int Amount => _amount;

    /// <summary>
    /// 수량을 더한다. 0 이하는 무시한다.
    /// </summary>
    public void Deposit(int depositedAmount)
    {
        if (depositedAmount <= 0)
        {
            return;
        }

        _amount += depositedAmount;
    }

    /// <summary>
    /// 요구 수량 이상을 가지고 있으면 true.
    /// </summary>
    public bool CanAfford(int requiredAmount)
    {
        return requiredAmount >= 0 && _amount >= requiredAmount;
    }

    /// <summary>
    /// 수량이 충분하면 빼고 true를 반환한다.
    /// </summary>
    public bool TryWithdraw(int withdrawnAmount)
    {
        if (withdrawnAmount <= 0 || !CanAfford(withdrawnAmount))
        {
            return false;
        }

        _amount -= withdrawnAmount;
        return true;
    }

    /// <summary>
    /// 수량을 0으로 만든다.
    /// </summary>
    public void Clear()
    {
        _amount = 0;
    }
}
