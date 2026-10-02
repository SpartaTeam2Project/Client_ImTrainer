/// <summary>
/// 자석 반경 배율을 플레이어에 적용한다.
/// </summary>
public class MagnetWeaponAbilityBehavior : WeaponAbilityBehavior<MagnetWeaponAbilityData, MagnetWeaponAbilityLevel>
{
    protected override void SetWeaponAbilityLevel(int levelId)
    {
        base.SetWeaponAbilityLevel(levelId);
        if (WeaponAbilityLevel != null && TryGetPlayer(out var player))
        {
            player.Stat.RecalculateMagnetRadius(WeaponAbilityLevel.RadiusMultiplier);
        }
    }

    /// <summary>
    /// 배율을 기본값으로 되돌리고 치운다.
    /// </summary>
    public override void Clear()
    {
        if (TryGetPlayer(out var player))
        {
            player.Stat.RecalculateMagnetRadius(1f);
        }

        base.Clear();
    }
}
