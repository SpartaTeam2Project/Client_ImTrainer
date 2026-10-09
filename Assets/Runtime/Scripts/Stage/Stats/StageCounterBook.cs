using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 키별 누적값. 처음 들어온 순서로 내보낸다.
/// </summary>
internal sealed class StageCounterBook
{
    private readonly Dictionary<string, float> _values = new Dictionary<string, float>();
    private readonly List<string> _order = new List<string>();

    public void Add(string key, float amount)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (_values.TryGetValue(key, out var current))
        {
            _values[key] = current + amount;
            return;
        }

        _values[key] = amount;
        _order.Add(key);
    }

    public void Set(string key, float value)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (!_values.ContainsKey(key))
        {
            _order.Add(key);
        }

        _values[key] = value;
    }

    public StageStatValue[] Export()
    {
        var entries = new StageStatValue[_order.Count];
        for (var i = 0; i < _order.Count; i++)
        {
            entries[i] = new StageStatValue(_order[i], _values[_order[i]]);
        }

        return entries;
    }

    /// <summary>
    /// 재화 장부로 쓸 때 키를 재화 id로 보고 정수로 내보낸다.
    /// </summary>
    public CurrencyAmount[] ExportCurrencies()
    {
        var entries = new CurrencyAmount[_order.Count];
        for (var i = 0; i < _order.Count; i++)
        {
            entries[i] = new CurrencyAmount(_order[i], Mathf.RoundToInt(_values[_order[i]]));
        }

        return entries;
    }
}
