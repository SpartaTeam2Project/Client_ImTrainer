using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 같은 종류 재화의 등급별 드롭 프리팹과 가중치. 드롭이 정해진 뒤 어느 등급을 떨어뜨릴지 고른다.
/// </summary>
[Serializable]
public class DropTierTable
{
    [Serializable]
    private class Entry
    {
        [SerializeField] private CoinDropBehavior _prefab;
        [SerializeField, Min(0f)] private float _weight = 1f;

        public CoinDropBehavior Prefab => _prefab;

        public float Weight => _prefab != null ? Mathf.Max(0f, _weight) : 0f;
    }

    [SerializeField] private List<Entry> _entries = new List<Entry>();

    /// <summary>
    /// 가중치에 따라 등급 하나를 뽑는다. 뽑을 등급이 없으면 null.
    /// </summary>
    public CoinDropBehavior Pick()
    {
        if (_entries == null || _entries.Count == 0)
        {
            return null;
        }

        var total = 0f;
        for (var i = 0; i < _entries.Count; i++)
        {
            total += _entries[i].Weight;
        }

        if (total <= 0f)
        {
            return null;
        }

        var roll = UnityEngine.Random.value * total;
        for (var i = 0; i < _entries.Count; i++)
        {
            var weight = _entries[i].Weight;
            if (weight <= 0f)
            {
                continue;
            }

            if (roll < weight)
            {
                return _entries[i].Prefab;
            }

            roll -= weight;
        }

        return null;
    }
}
