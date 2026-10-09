/// <summary>
/// 판 결과의 키-값 배열에서 값을 찾는다.
/// </summary>
public static class StageStatLookup
{
    public static bool TryGet(StageStatValue[] values, string key, out float value)
    {
        value = 0f;
        if (values == null)
        {
            return false;
        }

        for (var i = 0; i < values.Length; i++)
        {
            if (values[i].Key == key)
            {
                value = values[i].Value;
                return true;
            }
        }

        return false;
    }

    public static int GetAmount(CurrencyAmount[] amounts, string currencyId)
    {
        if (amounts == null)
        {
            return 0;
        }

        for (var i = 0; i < amounts.Length; i++)
        {
            if (amounts[i].CurrencyId == currencyId)
            {
                return amounts[i].Amount;
            }
        }

        return 0;
    }
}
