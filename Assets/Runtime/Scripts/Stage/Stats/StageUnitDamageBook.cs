using System.Collections.Generic;

/// <summary>
/// 피해 통계의 개체별 기록. 장착 칸이 바뀌면 개체의 파티 구간을 열고 닫는다.
/// 포켓몬에 귀속되지 않은 피해는 능력 종류별 한 줄로 모은다.
/// </summary>
internal sealed class StageUnitDamageBook
{
    private readonly int[] _slotMembers = new int[WeaponSlots.MAX_COUNT];
    private readonly Dictionary<int, UnitDamageRecord> _members = new Dictionary<int, UnitDamageRecord>();
    private readonly Dictionary<WeaponAbilityType, UnitDamageRecord> _unattributed = new Dictionary<WeaponAbilityType, UnitDamageRecord>();
    private readonly List<UnitDamageRecord> _order = new List<UnitDamageRecord>();

    public void HandleEquipmentChanged(float time, int slot, int memberId, int uid, int upgradeLevel, WeaponAbilityType abilityType)
    {
        if (slot < 0 || slot >= _slotMembers.Length)
        {
            return;
        }

        var previous = _slotMembers[slot];
        _slotMembers[slot] = memberId;
        if (memberId != DamageSource.NO_MEMBER)
        {
            var record = GetOrCreateMember(memberId, abilityType);
            record.SetUnit(uid, upgradeLevel, abilityType);
            record.Presence.Open(time);
        }

        // 칸끼리 바꿀 때는 다른 칸에 아직 있으므로 닫지 않는다.
        if (previous != DamageSource.NO_MEMBER && previous != memberId && !IsInParty(previous)
            && _members.TryGetValue(previous, out var left))
        {
            left.Presence.Close(time);
        }
    }

    public void AddDamage(float time, int memberId, WeaponAbilityType abilityType, float amount)
    {
        var record = memberId != DamageSource.NO_MEMBER
            ? GetOrCreateMember(memberId, abilityType)
            : GetOrCreateUnattributed(time, abilityType);
        record.AddDamage(time, amount);
    }

    /// <summary>
    /// 지금까지의 값을 처음 들어온 순서로 내보낸다.
    /// </summary>
    public StageUnitDamage[] Export(float now)
    {
        var entries = new StageUnitDamage[_order.Count];
        for (var i = 0; i < _order.Count; i++)
        {
            entries[i] = _order[i].ToEntry(now);
        }

        return entries;
    }

    private bool IsInParty(int memberId)
    {
        for (var i = 0; i < _slotMembers.Length; i++)
        {
            if (_slotMembers[i] == memberId)
            {
                return true;
            }
        }

        return false;
    }

    private UnitDamageRecord GetOrCreateMember(int memberId, WeaponAbilityType abilityType)
    {
        if (_members.TryGetValue(memberId, out var record))
        {
            return record;
        }

        record = new UnitDamageRecord(memberId, abilityType);
        _members[memberId] = record;
        _order.Add(record);
        return record;
    }

    private UnitDamageRecord GetOrCreateUnattributed(float time, WeaponAbilityType abilityType)
    {
        if (_unattributed.TryGetValue(abilityType, out var record))
        {
            return record;
        }

        // 칸 없는 능력은 처음 피해를 준 때부터 끝까지 있던 것으로 본다.
        record = new UnitDamageRecord(DamageSource.NO_MEMBER, abilityType);
        record.Presence.Open(time);
        _unattributed[abilityType] = record;
        _order.Add(record);
        return record;
    }
}
