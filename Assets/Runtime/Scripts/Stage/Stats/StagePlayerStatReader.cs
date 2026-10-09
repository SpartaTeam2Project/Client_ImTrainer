/// <summary>
/// 판이 끝난 시점의 플레이어 스탯을 결과 키로 읽는다. 스탯이 늘면 여기와 StageStatDefinitions에 한 줄씩 더한다.
/// </summary>
internal static class StagePlayerStatReader
{
    public static StageStatValue[] Read(Player player)
    {
        if (player == null || player.Stat == null || player.Health == null)
        {
            return System.Array.Empty<StageStatValue>();
        }

        var stat = player.Stat;
        return new[]
        {
            new StageStatValue(StageStatKeys.MAX_HP, player.Health.maxHealth),
            new StageStatValue(StageStatKeys.HP_REGEN, stat.hpRegen),
            new StageStatValue(StageStatKeys.DAMAGE_MULTIPLIER, stat.damageMultiplier),
            new StageStatValue(StageStatKeys.COOLDOWN_MULTIPLIER, stat.cooldownMultiplier),
            new StageStatValue(StageStatKeys.PROJECTILE_MULTIPLIER, stat.projectileMultiplier),
            new StageStatValue(StageStatKeys.PROJECTILE_SPEED_MULTIPLIER, stat.projectileSpeedMultiplier),
            new StageStatValue(StageStatKeys.SIZE_MULTIPLIER, stat.sizeMultiplier),
            new StageStatValue(StageStatKeys.DURATION_MULTIPLIER, stat.durationMultiplier),
            new StageStatValue(StageStatKeys.MOVE_SPEED, stat.moveSpeed * stat.moveSpeedMultiplier),
            new StageStatValue(StageStatKeys.MAGNET_RADIUS, stat.magnetRadius),
            new StageStatValue(StageStatKeys.XP_MULTIPLIER, stat.xpMultiplier),
            new StageStatValue(StageStatKeys.DAMAGE_REDUCTION_PERCENT, stat.damageReductionPercent),
            new StageStatValue(StageStatKeys.RECEIVED_DAMAGE_MULTIPLIER, stat.receivedDamageMultiplier),
        };
    }
}
