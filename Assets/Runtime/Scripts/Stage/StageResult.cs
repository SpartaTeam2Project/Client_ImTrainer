using System;
using UnityEngine;

/// <summary>
/// 스테이지가 끝났을 때 모으는 결과. 전투 중에는 만들지 않는다.
/// 원본 수치만 담는다. 점수와 순위는 서버가 계산한다. 서버로 보낼 때는 StageResultMapper가 요청 DTO로 바꾼다.
/// </summary>
[Serializable]
public class StageResult
{
    [SerializeField] private string _runId;
    [SerializeField] private string _clearedAtUtc;
    [SerializeField] private int _playerId;
    [SerializeField] private string _stageId;
    [SerializeField] private string _stageName;
    [SerializeField] private string _playableId;
    [SerializeField] private bool _cleared;
    [SerializeField] private int _killCount;
    [SerializeField] private float _elapsedSeconds;
    [SerializeField] private int _reachedLevel;
    [SerializeField] private CurrencyAmount[] _currencies;
    [SerializeField] private CurrencyAmount[] _currenciesGained;
    [SerializeField] private StageStatValue[] _playerStats;
    [SerializeField] private StageStatValue[] _counters;
    [SerializeField] private StageUnitDamage[] _unitDamages;
    [SerializeField] private StagePartyMember[] _party;

    public StageResult(StageResultInfo info, CurrencyAmount[] currencies, StageStatsReport report)
    {
        _runId = info.RunId;
        _clearedAtUtc = info.ClearedAtUtc;
        _playerId = info.PlayerId;
        _stageId = info.StageId;
        _stageName = info.StageName;
        _playableId = info.PlayableId;
        _cleared = info.Cleared;
        _killCount = info.KillCount;
        _elapsedSeconds = info.ElapsedSeconds;
        _reachedLevel = info.ReachedLevel;
        _currencies = currencies ?? Array.Empty<CurrencyAmount>();
        _currenciesGained = report.CurrenciesGained ?? Array.Empty<CurrencyAmount>();
        _playerStats = report.PlayerStats ?? Array.Empty<StageStatValue>();
        _counters = report.Counters ?? Array.Empty<StageStatValue>();
        _unitDamages = report.UnitDamages ?? Array.Empty<StageUnitDamage>();
        _party = report.Party ?? Array.Empty<StagePartyMember>();
    }

    /// <summary>
    /// 판마다 새로 만드는 GUID. 서버가 중복 제출을 거르는 키다.
    /// </summary>
    public string RunId => _runId ?? string.Empty;

    /// <summary>
    /// 판이 끝난 시각. ISO-8601 UTC 문자열이다.
    /// </summary>
    public string ClearedAtUtc => _clearedAtUtc ?? string.Empty;

    public int PlayerId => _playerId;

    public string StageId => _stageId ?? string.Empty;

    public string StageName => _stageName ?? string.Empty;

    /// <summary>
    /// 고른 트레이너 데이터의 에셋 이름.
    /// </summary>
    public string PlayableId => _playableId ?? string.Empty;

    public bool Cleared => _cleared;

    public int KillCount => _killCount;

    public float ElapsedSeconds => _elapsedSeconds;

    public int ReachedLevel => _reachedLevel;

    /// <summary>
    /// 판이 끝났을 때 남은 스테이지 재화. 상점에서 쓴 만큼 빠져 있다.
    /// </summary>
    public CurrencyAmount[] Currencies => _currencies ?? Array.Empty<CurrencyAmount>();

    /// <summary>
    /// 이번 판에 들어온 재화 총량. 쓴 것도 포함한다.
    /// </summary>
    public CurrencyAmount[] CurrenciesGained => _currenciesGained ?? Array.Empty<CurrencyAmount>();

    public StageStatValue[] PlayerStats => _playerStats ?? Array.Empty<StageStatValue>();

    public StageStatValue[] Counters => _counters ?? Array.Empty<StageStatValue>();

    public StageUnitDamage[] UnitDamages => _unitDamages ?? Array.Empty<StageUnitDamage>();

    public StagePartyMember[] Party => _party ?? Array.Empty<StagePartyMember>();
}

/// <summary>
/// StageResult의 머리 정보. StageResultBuilder가 채운다.
/// </summary>
public struct StageResultInfo
{
    public string RunId;
    public string ClearedAtUtc;
    public int PlayerId;
    public string StageId;
    public string StageName;
    public string PlayableId;
    public bool Cleared;
    public int KillCount;
    public float ElapsedSeconds;
    public int ReachedLevel;
}
