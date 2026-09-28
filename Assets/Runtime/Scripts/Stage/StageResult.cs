using System;
using UnityEngine;

/// <summary>
/// 한 판에서 모은 재화 수량.
/// </summary>
[Serializable]
public struct CurrencyAmount
{
    [SerializeField] private string _currencyId;
    [SerializeField] private int _amount;

    public CurrencyAmount(string currencyId, int amount)
    {
        _currencyId = currencyId;
        _amount = amount;
    }

    public string CurrencyId => _currencyId;

    public int Amount => _amount;
}

/// <summary>
/// 스테이지가 끝났을 때 모으는 결과. 전투 중에는 만들지 않는다.
/// </summary>
[Serializable]
public class StageResult
{
    [SerializeField] private int _playerId;
    [SerializeField] private int _killCount;
    [SerializeField] private float _elapsedSeconds;
    [SerializeField] private CurrencyAmount[] _currencies;

    public StageResult(int playerId, int killCount, float elapsedSeconds, CurrencyAmount[] currencies)
    {
        _playerId = playerId;
        _killCount = killCount;
        _elapsedSeconds = elapsedSeconds;
        _currencies = currencies ?? System.Array.Empty<CurrencyAmount>();
    }

    public int PlayerId => _playerId;

    public int KillCount => _killCount;

    public float ElapsedSeconds => _elapsedSeconds;

    public CurrencyAmount[] Currencies => _currencies ?? System.Array.Empty<CurrencyAmount>();
}
