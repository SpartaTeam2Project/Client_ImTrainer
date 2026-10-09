using UnityEngine;

/// <summary>
/// 타임라인 트랙이 클립에 넘기는 몬스터 한 종의 전투 수치.
/// </summary>
public sealed class MonsterWaveProfile
{
    public MonsterVisualData Monster { get; set; }

    public int Id { get; set; }

    public float MaxHealth { get; set; }

    public float ContactDamage { get; set; }

    public float SkillDamage { get; set; }

    public float MoveSpeed { get; set; }

    public EnemyAttackKind AttackKind { get; set; }

    public BossSkillKind Skill { get; set; }

    public float SkillCooldown { get; set; }

    public Sprite ProjectileSprite { get; set; }

    public Sprite[] ChargeProjectileFrames { get; set; }

    public Sprite[] FlyProjectileFrames { get; set; }

    public Sprite[] BeamFrames { get; set; }

    public Vector2[] BeamMouthOffsets { get; set; }

    public float AttackRange { get; set; }

    public float AttackInterval { get; set; }

    public float ProjectileSpeed { get; set; }

    public float HitRadius { get; set; }

    public float AttackDistance { get; set; }

    public float ChargeSeconds { get; set; }

    public float DashSpeed { get; set; }

    public float DashRecoverSeconds { get; set; }

    public float SlamChargeSeconds { get; set; }

    public float SkillRange { get; set; }

    public float SlamRecoverSeconds { get; set; }

    public float LungeRange { get; set; }

    public float LungeRecoverSeconds { get; set; }

    public float LungeSpeed { get; set; }

    public float CircleMoveSpeed { get; set; }

    public float CircleRange { get; set; }

    public float CircleGapSeconds { get; set; }

    public float FanCastRange { get; set; }

    public int FanCount { get; set; }

    public float FanDistance { get; set; }

    public float BeamCastRange { get; set; }

    public float BeamWidth { get; set; }

    public float BeamLength { get; set; }

    public float BeamSeconds { get; set; }

    public float BeamHitInterval { get; set; }

    public float BeamTurnSpeed { get; set; }

    public float PoolRange { get; set; }

    public float PoolSpreadSpeed { get; set; }

    public float PoolHitsPerSecond { get; set; }

    public float PoolSlowPercent { get; set; }

    public float PoolCastSeconds { get; set; }

    public float PoolSeconds { get; set; }

    public Sprite[] PoolBurstFrames { get; set; }

    public Sprite[] PoolLingerFrames { get; set; }

    public int SpoonCount { get; set; }

    public float SpoonChargeSeconds { get; set; }

    public float SpoonSpeed { get; set; }

    public float SpoonFlySeconds { get; set; }

    public Sprite[] SpoonFrames { get; set; }

    public bool OverrideScale { get; set; }

    public float Scale { get; set; }

    public bool CircularSpawn { get; set; }

    public bool DisableOffscreenTeleport { get; set; }
}
