/// <summary>
/// 포켓몬 개체 하나의 종, 성, 누적 피해, 파티 구간, 최근 피해.
/// </summary>
internal sealed class UnitDamageRecord
{
    public readonly int MemberId;
    public readonly PresenceTimeline Presence = new PresenceTimeline();

    private readonly RecentDamageBuffer _recent = new RecentDamageBuffer();
    private int _uid = ItemCatalog.EMPTY_UID;
    private int _upgradeLevel;
    private WeaponAbilityType _abilityType;
    private float _total;

    public UnitDamageRecord(int memberId, WeaponAbilityType abilityType)
    {
        MemberId = memberId;
        _abilityType = abilityType;
    }

    /// <summary>
    /// 진화와 성 올리기 뒤의 종과 성으로 바꾼다.
    /// </summary>
    public void SetUnit(int uid, int upgradeLevel, WeaponAbilityType abilityType)
    {
        _uid = uid;
        _upgradeLevel = upgradeLevel;
        _abilityType = abilityType;
    }

    public void AddDamage(float time, float amount)
    {
        _total += amount;
        _recent.Add(time, amount);
    }

    public StageUnitDamage ToEntry(float now)
    {
        var windowStart = RecentDamageBuffer.WindowStart(now);
        return new StageUnitDamage(
            MemberId,
            _uid,
            _upgradeLevel,
            _abilityType,
            _total,
            Presence.Total(now),
            _recent.Sum(now),
            Presence.Overlap(windowStart, now, now),
            Presence.IsOpen);
    }
}
