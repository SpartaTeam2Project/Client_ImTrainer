/// <summary>
/// 쿨다운 배율을 플레이어에 적용한다.
/// </summary>
public class CooldownWeaponAbilityBehavior : WeaponAbilityBehavior<CooldownWeaponAbilityData, CooldownWeaponAbilityLevel>
{
    protected override void SetWeaponAbilityLevel(int levelId)
    {
        base.SetWeaponAbilityLevel(levelId);
        if (WeaponAbilityLevel != null && TryGetPlayer(out var player))
        {
            player.RecalculateCooldownMultiplier(WeaponAbilityLevel.CooldownMultiplier);
        }
    }

    /// <summary>
    /// 배율을 기본값으로 되돌리고 치운다.
    /// </summary>
    public override void Clear()
    {
        if (TryGetPlayer(out var player))
        {
            player.RecalculateCooldownMultiplier(1f);
        }

        base.Clear();
    }
}
