/// <summary>
/// 판 결과의 카운터와 플레이어 스탯 키. 서버로도 그대로 나가므로 이미 쓴 값은 바꾸지 않는다.
/// 새 키를 더하면 StageStatDefinitions와 Docs/stage-result-api.md에도 한 줄 추가한다.
/// </summary>
public static class StageStatKeys
{
    // 판 카운터
    public const string KILLS = "kills";
    public const string REACHED_LEVEL = "reachedLevel";
    public const string BOSS_KILLS = "bossKills";
    public const string EXP_GAINED = "expGained";
    public const string DAMAGE_TAKEN = "damageTaken";
    public const string HEALED = "healed";
    public const string SHOP_PURCHASES = "shopPurchases";
    public const string SYNTHESES = "syntheses";
    public const string SELLS = "sells";

    // 플레이어 최종 스탯
    public const string MAX_HP = "maxHp";
    public const string HP_REGEN = "hpRegen";
    public const string DAMAGE_MULTIPLIER = "damageMultiplier";
    public const string COOLDOWN_MULTIPLIER = "cooldownMultiplier";
    public const string PROJECTILE_MULTIPLIER = "projectileMultiplier";
    public const string PROJECTILE_SPEED_MULTIPLIER = "projectileSpeedMultiplier";
    public const string SIZE_MULTIPLIER = "sizeMultiplier";
    public const string DURATION_MULTIPLIER = "durationMultiplier";
    public const string MOVE_SPEED = "moveSpeed";
    public const string MAGNET_RADIUS = "magnetRadius";
    public const string XP_MULTIPLIER = "xpMultiplier";
    public const string DAMAGE_REDUCTION_PERCENT = "damageReductionPercent";
    public const string RECEIVED_DAMAGE_MULTIPLIER = "receivedDamageMultiplier";
}
