/// <summary>
/// 이동 속도 배율을 플레이어에 적용한다.
/// </summary>
public class SportShoesWeaponAbilityBehavior : WeaponAbilityBehavior<MoveSpeedWeaponAbilityData, MoveSpeedWeaponAbilityLevel>
{
    protected override void SetWeaponAbilityLevel(int levelId)
    {
        base.SetWeaponAbilityLevel(levelId);
        if (WeaponAbilityLevel != null && TryGetPlayer(out var player))
        {
            player.RecalculateMoveSpeed(WeaponAbilityLevel.SpeedMultiplier);
        }
    }

    /// <summary>
    /// 배율을 기본값으로 되돌리고 치운다.
    /// </summary>
    public override void Clear()
    {
        if (TryGetPlayer(out var player))
        {
            player.RecalculateMoveSpeed(1f);
        }

        base.Clear();
    }
}
