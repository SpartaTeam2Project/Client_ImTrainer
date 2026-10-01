/// <summary>
/// 공격력 배율을 플레이어에 적용한다.
/// </summary>
public class DamageWeaponAbilityBehavior : WeaponAbilityBehavior<DamageWeaponAbilityData, DamageWeaponAbilityLevel>
{
    protected override void SetWeaponAbilityLevel(int levelId)
    {
        base.SetWeaponAbilityLevel(levelId);
    }

    /// <summary>
    /// 배율을 기본값으로 되돌리고 치운다.
    /// </summary>
    public override void Clear()
    {
        base.Clear();
    }
}
