/// <summary>
/// 한 번 최대 체력 비율만큼 회복한다.
/// </summary>
public class HealEndgameWeaponAbilityBehavior : WeaponAbilityBehavior<HealEndgameWeaponAbilityData, HealEndgameWeaponAbilityLevel>
{
    protected override void SetWeaponAbilityLevel(int levelId)
    {
        base.SetWeaponAbilityLevel(levelId);
        if (WeaponAbilityLevel != null && TryGetPlayer(out var player))
        {
            player.Health.RestoreHp(WeaponAbilityLevel.HealPersentage);
        }
    }
}
