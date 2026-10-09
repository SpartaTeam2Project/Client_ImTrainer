/// <summary>
/// 적 체력이 피해로 줄었다. Amount는 실제로 깎인 체력이라 넘친 피해는 빠진다.
/// </summary>
[System.Serializable]
public struct EnemyDamaged
{
    public int PlayerId;
    public int MemberId;
    public WeaponAbilityType AbilityType;
    public float Amount;
    public bool Killed;

    public EnemyDamaged(DamageSource source, float amount, bool killed)
    {
        PlayerId = source.PlayerId;
        MemberId = source.MemberId;
        AbilityType = source.AbilityType;
        Amount = amount;
        Killed = killed;
    }
}
