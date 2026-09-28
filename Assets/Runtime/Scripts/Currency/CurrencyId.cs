using System;
using UnityEngine;

/// <summary>
/// 인스펙터에 넣는 재화 id.
/// </summary>
[Serializable]
public class CurrencyId
{
    [SerializeField] private string _value;

    public string Value => _value;

    public CurrencyId()
    {
    }

    public CurrencyId(string value)
    {
        _value = value;
    }

    public static implicit operator string(CurrencyId currencyId)
    {
        return currencyId == null ? string.Empty : currencyId._value;
    }

    public static implicit operator CurrencyId(string currencyId)
    {
        return new CurrencyId(currencyId);
    }
}
