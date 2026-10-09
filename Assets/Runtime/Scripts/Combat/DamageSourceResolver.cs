/// <summary>
/// 능력과 장착 칸으로 피해 출처를 만든다. 칸이 없는 능력은 포켓몬에 귀속하지 않는다.
/// </summary>
public static class DamageSourceResolver
{
    public static DamageSource Resolve(int playerId, WeaponAbilityType abilityType, ISlotOriginAbility origin)
    {
        var memberId = DamageSource.NO_MEMBER;
        if (origin != null
            && origin.HasOriginSlot
            && Managers.Instance != null
            && Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
        {
            memberId = itemManager.GetMemberId(playerId, (int)origin.OriginSlot);
        }

        return new DamageSource(playerId, memberId, abilityType);
    }
}
