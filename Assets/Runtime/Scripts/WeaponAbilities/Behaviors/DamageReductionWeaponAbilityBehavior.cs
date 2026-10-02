/// <summary>
/// 피해 감소를 플레이어에 적용한다.
/// </summary>
public class DamageReductionWeaponAbilityBehavior : WeaponAbilityBehavior<DamageReductionWeaponAbilityData, DamageReductionWeaponAbilityLevel>
{
    protected override void SetWeaponAbilityLevel(int levelId)
    {
        base.SetWeaponAbilityLevel(levelId);
        if (WeaponAbilityLevel != null && TryGetPlayer(out var player))
        {
            player.Stat.RecalculateDamageReduction(WeaponAbilityLevel.DamageReductionPercent);
        }
    }

    /// <summary>
    /// 감소를 없애고 치운다.
    /// </summary>
    public override void Clear()
    {
        if (TryGetPlayer(out var player))
        {
            player.Stat.RecalculateDamageReduction(0f);
        }

        base.Clear();
    }
}
