/// <summary>
/// 적에게 들어간 피해의 출처. 능력이 발사할 때 만들어 투사체가 들고 다닌다.
/// </summary>
[System.Serializable]
public struct DamageSource
{
    /// <summary>
    /// 포켓몬에 귀속되지 않은 피해의 개체 번호.
    /// </summary>
    public const int NO_MEMBER = 0;

    public int PlayerId;
    public int MemberId;
    public WeaponAbilityType AbilityType;

    public DamageSource(int playerId, int memberId, WeaponAbilityType abilityType)
    {
        PlayerId = playerId;
        MemberId = memberId;
        AbilityType = abilityType;
    }
}
