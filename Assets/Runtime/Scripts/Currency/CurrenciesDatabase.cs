using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임에서 쓰는 재화 목록.
/// </summary>
[CreateAssetMenu(fileName = "CurrenciesDatabase", menuName = "Currency/Currencies Database")]
public class CurrenciesDatabase : ScriptableObject
{
    [SerializeField] private List<CurrencyData> _currencies = new List<CurrencyData>();

    public int Count => _currencies == null ? 0 : _currencies.Count;

    /// <summary>
    /// 순서에 해당하는 재화. 범위를 벗어나면 null.
    /// </summary>
    public CurrencyData GetCurrency(int index)
    {
        if (_currencies == null || index < 0 || index >= _currencies.Count)
        {
            return null;
        }

        return _currencies[index];
    }

    /// <summary>
    /// id가 같은 재화. 없으면 null.
    /// </summary>
    public CurrencyData GetCurrency(string id)
    {
        if (_currencies == null || string.IsNullOrEmpty(id))
        {
            return null;
        }

        for (var i = 0; i < _currencies.Count; i++)
        {
            var currency = _currencies[i];
            if (currency != null && currency.Id == id)
            {
                return currency;
            }
        }

        return null;
    }
}
