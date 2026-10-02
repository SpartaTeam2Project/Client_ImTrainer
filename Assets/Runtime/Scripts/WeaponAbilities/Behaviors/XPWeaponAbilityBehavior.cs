/// <summary>
/// 경험치 배율을 플레이어에 적용한다.
/// </summary>
public class XPWeaponAbilityBehavior : WeaponAbilityBehavior<XPWeaponAbilityData, XPWeaponAbilityLevel>
{
    protected override void SetWeaponAbilityLevel(int levelId)
    {
        base.SetWeaponAbilityLevel(levelId);
        if (WeaponAbilityLevel != null && TryGetPlayer(out var player))
        {
            player.Stat.RecalculateXpMultiplier(WeaponAbilityLevel.XPMultiplier);
        }
    }

    /// <summary>
    /// 배율을 기본값으로 되돌리고 치운다.
    /// </summary>
    public override void Clear()
    {
        if (TryGetPlayer(out var player))
        {
            player.Stat.RecalculateXpMultiplier(1f);
        }

        base.Clear();
    }
}
