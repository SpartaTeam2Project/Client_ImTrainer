using System.Collections.Generic;

/// <summary>
/// 결과 화면에 수치를 적는 방식.
/// </summary>
public enum StageStatFormat
{
    /// <summary>정수.</summary>
    Integer,
    /// <summary>소수 첫째 자리.</summary>
    Decimal,
    /// <summary>1.2를 120%로 적는다.</summary>
    Ratio,
    /// <summary>이미 퍼센트인 값. 20을 20%로 적는다.</summary>
    Percent,
    /// <summary>큰 수를 K, M으로 줄인다.</summary>
    Abbreviated
}

/// <summary>
/// 결과 화면 한 줄의 키, 이름, 표기, 아이콘.
/// </summary>
public readonly struct StageStatDefinition
{
    public readonly string Key;
    public readonly string Label;
    public readonly StageStatFormat Format;
    public readonly bool HasIcon;
    public readonly WeaponAbilityType IconAbility;

    public StageStatDefinition(string key, string label, StageStatFormat format)
    {
        Key = key;
        Label = label;
        Format = format;
        HasIcon = false;
        IconAbility = default;
    }

    public StageStatDefinition(string key, string label, StageStatFormat format, WeaponAbilityType iconAbility)
    {
        Key = key;
        Label = label;
        Format = format;
        HasIcon = true;
        IconAbility = iconAbility;
    }
}

/// <summary>
/// 결과 화면에 나오는 줄의 순서표. 줄을 늘리려면 여기에 한 줄을 더한다.
/// 상자처럼 새 카운터는 StageCounterAdded를 발행하고 DataCells에 한 줄을 더하면 된다.
/// </summary>
public static class StageStatDefinitions
{
    /// <summary>
    /// 왼쪽 플레이어 스탯. 아이콘은 같은 이름 패시브 능력 아이콘을 쓴다.
    /// </summary>
    public static readonly IReadOnlyList<StageStatDefinition> PlayerStats = new[]
    {
        new StageStatDefinition(StageStatKeys.MAX_HP, "최대 체력", StageStatFormat.Integer, WeaponAbilityType.MaxHP),
        new StageStatDefinition(StageStatKeys.HP_REGEN, "초당 체력 회복", StageStatFormat.Decimal, WeaponAbilityType.RestoreHP),
        new StageStatDefinition(StageStatKeys.DAMAGE_MULTIPLIER, "주는 피해", StageStatFormat.Ratio, WeaponAbilityType.Damage),
        new StageStatDefinition(StageStatKeys.COOLDOWN_MULTIPLIER, "쿨다운", StageStatFormat.Ratio, WeaponAbilityType.Cooldown),
        new StageStatDefinition(StageStatKeys.PROJECTILE_MULTIPLIER, "투사체 수", StageStatFormat.Ratio),
        new StageStatDefinition(StageStatKeys.PROJECTILE_SPEED_MULTIPLIER, "투사체 속도", StageStatFormat.Ratio, WeaponAbilityType.ProjectileSpeed),
        new StageStatDefinition(StageStatKeys.SIZE_MULTIPLIER, "크기", StageStatFormat.Ratio, WeaponAbilityType.Size),
        new StageStatDefinition(StageStatKeys.DURATION_MULTIPLIER, "지속 시간", StageStatFormat.Ratio, WeaponAbilityType.Duration),
        new StageStatDefinition(StageStatKeys.MOVE_SPEED, "이동 속도", StageStatFormat.Decimal, WeaponAbilityType.MoveSpeed),
        new StageStatDefinition(StageStatKeys.MAGNET_RADIUS, "자석 범위", StageStatFormat.Decimal, WeaponAbilityType.Magnet),
        new StageStatDefinition(StageStatKeys.XP_MULTIPLIER, "경험치 배율", StageStatFormat.Ratio, WeaponAbilityType.XP),
        new StageStatDefinition(StageStatKeys.DAMAGE_REDUCTION_PERCENT, "피해 감소", StageStatFormat.Percent, WeaponAbilityType.DamageReduction),
        new StageStatDefinition(StageStatKeys.RECEIVED_DAMAGE_MULTIPLIER, "받는 피해", StageStatFormat.Ratio),
    };

    /// <summary>
    /// 중앙 데이터 통계. 재화 칸은 이 표 뒤에 재화마다 붙는다.
    /// </summary>
    public static readonly IReadOnlyList<StageStatDefinition> DataCells = new[]
    {
        new StageStatDefinition(StageStatKeys.KILLS, "몬스터 처치", StageStatFormat.Integer),
        new StageStatDefinition(StageStatKeys.BOSS_KILLS, "보스 처치", StageStatFormat.Integer),
        new StageStatDefinition(StageStatKeys.REACHED_LEVEL, "도달 레벨", StageStatFormat.Integer),
        new StageStatDefinition(StageStatKeys.EXP_GAINED, "경험치 획득", StageStatFormat.Integer),
        new StageStatDefinition(StageStatKeys.DAMAGE_TAKEN, "받은 피해", StageStatFormat.Integer),
        new StageStatDefinition(StageStatKeys.HEALED, "회복량", StageStatFormat.Integer),
        new StageStatDefinition(StageStatKeys.SHOP_PURCHASES, "상점 구매", StageStatFormat.Integer),
        new StageStatDefinition(StageStatKeys.SYNTHESES, "합성", StageStatFormat.Integer),
        new StageStatDefinition(StageStatKeys.SELLS, "판매", StageStatFormat.Integer),
    };
}
