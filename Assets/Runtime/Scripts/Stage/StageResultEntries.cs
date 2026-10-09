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
/// 키 하나와 수치 하나. 플레이어 스탯과 판 카운터가 같이 쓴다. 키는 StageStatKeys다.
/// </summary>
[Serializable]
public struct StageStatValue
{
    [SerializeField] private string _key;
    [SerializeField] private float _value;

    public StageStatValue(string key, float value)
    {
        _key = key;
        _value = value;
    }

    public string Key => _key;

    public float Value => _value;
}

/// <summary>
/// 포켓몬 개체 하나가 이번 판에 준 피해와 파티에 있던 시간. DPS와 비율은 화면과 서버가 계산한다.
/// </summary>
[Serializable]
public struct StageUnitDamage
{
    [SerializeField] private int _memberId;
    [SerializeField] private int _uid;
    [SerializeField] private int _upgradeLevel;
    [SerializeField] private WeaponAbilityType _abilityType;
    [SerializeField] private float _totalDamage;
    [SerializeField] private float _secondsInParty;
    [SerializeField] private float _recentDamage;
    [SerializeField] private float _recentSeconds;
    [SerializeField] private bool _inPartyAtEnd;

    public StageUnitDamage(int memberId, int uid, int upgradeLevel, WeaponAbilityType abilityType, float totalDamage,
        float secondsInParty, float recentDamage, float recentSeconds, bool inPartyAtEnd)
    {
        _memberId = memberId;
        _uid = uid;
        _upgradeLevel = upgradeLevel;
        _abilityType = abilityType;
        _totalDamage = totalDamage;
        _secondsInParty = secondsInParty;
        _recentDamage = recentDamage;
        _recentSeconds = recentSeconds;
        _inPartyAtEnd = inPartyAtEnd;
    }

    /// <summary>
    /// 판 안의 개체 번호. 0이면 포켓몬에 귀속되지 않은 피해다.
    /// </summary>
    public int MemberId => _memberId;

    /// <summary>
    /// 마지막 종. 진화하면 진화한 종이다.
    /// </summary>
    public int Uid => _uid;

    public int UpgradeLevel => _upgradeLevel;

    public WeaponAbilityType AbilityType => _abilityType;

    public float TotalDamage => _totalDamage;

    public float SecondsInParty => _secondsInParty;

    /// <summary>
    /// 마지막 60초 동안 준 피해.
    /// </summary>
    public float RecentDamage => _recentDamage;

    /// <summary>
    /// 마지막 60초 중 파티에 있던 시간.
    /// </summary>
    public float RecentSeconds => _recentSeconds;

    public bool InPartyAtEnd => _inPartyAtEnd;
}

/// <summary>
/// 판이 끝났을 때 장착 칸 하나.
/// </summary>
[Serializable]
public struct StagePartyMember
{
    [SerializeField] private int _slot;
    [SerializeField] private int _memberId;
    [SerializeField] private int _uid;
    [SerializeField] private int _upgradeLevel;

    public StagePartyMember(int slot, int memberId, int uid, int upgradeLevel)
    {
        _slot = slot;
        _memberId = memberId;
        _uid = uid;
        _upgradeLevel = upgradeLevel;
    }

    public int Slot => _slot;

    public int MemberId => _memberId;

    public int Uid => _uid;

    public int UpgradeLevel => _upgradeLevel;
}
