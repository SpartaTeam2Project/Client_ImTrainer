using System;
using UnityEngine;

/// <summary>
/// 보스 클립만 쓰는 스킬. 없으면 웨이브와 같은 접촉·투사체를 쓴다.
/// </summary>
public enum BossSkillKind
{
    [InspectorName("없음")]
    None = 0,
    [InspectorName("상승 연사")]
    RisingVolley = 1,
    [InspectorName("돌진")]
    ChargeDash = 2,
    [InspectorName("내리찍기")]
    Slam = 3,
    [InspectorName("몸통박치기")]
    Lunge = 4,
    [InspectorName("원형 연타")]
    CircleVolley = 5,
    [InspectorName("니드킹 몸통박치기")]
    NidokingLunge = 6,
    [InspectorName("부채꼴 사격")]
    FanVolley = 7
}

/// <summary>
/// 보스 한 마리의 그림과 전투 수치. 웨이브 트랙과 같은 칸을 쓴다.
/// </summary>
[Serializable]
public class BossSpawnEntry
{
    [SerializeField] private MonsterVisualData _monster;
    [SerializeField] private int _id;
    [SerializeField] private float _maxHealth = 30f;
    [SerializeField] private float _contactDamage = 4f;
    [Tooltip("스킬이 플레이어에게 주는 피해.")]
    [SerializeField] private float _skillDamage = 4f;
    [SerializeField] private float _moveSpeed = 1.2f;
    [SerializeField] private EnemyAttackKind _attackKind = EnemyAttackKind.Contact;
    [SerializeField] private BossSkillKind _skill = BossSkillKind.None;
    [Tooltip("스킬 쿨타임. 나타난 뒤 이 시간이 지나야 첫 스킬을 쓰고, 시전이 끝난 뒤에도 이 시간을 기다린다.")]
    [SerializeField, Min(0.05f)] private float _skillCooldown = 4f;
    [Tooltip("보스 스킬 탄 그림. 플레이어 무기 탄과는 따로다.")]
    [SerializeField] private Sprite _projectileSprite;
    [SerializeField] private Sprite[] _chargeProjectileFrames = Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _flyProjectileFrames = Array.Empty<Sprite>();
    [SerializeField, Min(0.5f)] private float _attackRange = 4f;
    [SerializeField, Min(0.05f)] private float _attackInterval = 1.4f;
    [SerializeField, Min(0.01f)] private float _projectileSpeed = 6f;
    [SerializeField, Min(0f)] private float _hitRadius = 0.75f;
    [SerializeField, Min(0.01f)] private float _attackDistance = 0.45f;
    [HideInInspector]
    [SerializeField] private bool _overrideScale;
    [Tooltip("보스 몸 크기. 몬스터 SO의 크기는 쓰지 않는다.")]
    [SerializeField, Min(0.01f)] private float _scale = MonsterVisualData.DEFAULT_SCALE;
    [SerializeField, Min(0.05f)] private float _chargeSeconds = 0.9f;
    [SerializeField, Min(0.01f)] private float _dashSpeed = 3.6f;
    [Tooltip("돌진이 끝난 뒤 그 방향을 유지하는 시간.")]
    [SerializeField, Min(0f)] private float _dashRecoverSeconds = 0.5f;
    [SerializeField, Min(0.05f)] private float _slamChargeSeconds = 1.5f;
    [SerializeField, Min(0.1f)] private float _skillRange = 2.5f;
    [Tooltip("내리찍기 착지 뒤 스탬프 마지막 그림을 유지하는 시간.")]
    [SerializeField, Min(0f)] private float _slamRecoverSeconds = 0.5f;
    [Tooltip("이 거리 안에 플레이어가 있고 쿨타임이 끝났을 때만 몸통박치기를 한다.")]
    [SerializeField, Min(0.1f)] private float _lungeRange = 2.5f;
    [Tooltip("몸통박치기 뒤 Strike 마지막 그림을 유지하는 시간.")]
    [SerializeField, Min(0f)] private float _lungeRecoverSeconds = 0.5f;
    [Tooltip("몸통박치기로 플레이어에게 다가가는 속도.")]
    [SerializeField, Min(0.01f)] private float _lungeSpeed = 3f;
    [Tooltip("원형 연타가 공격 사이에 걸어가는 속도.")]
    [SerializeField, Min(0.01f)] private float _circleMoveSpeed = 3f;
    [Tooltip("원형 연타 원의 반경.")]
    [SerializeField, Min(0.1f)] private float _circleRange = 2.5f;
    [Tooltip("원형 연타의 공격과 공격 사이 걷는 시간.")]
    [SerializeField, Min(0f)] private float _circleGapSeconds = 0.75f;
    [Tooltip("이 거리 안에 플레이어가 있고 쿨타임이 끝났을 때만 부채꼴 사격을 한다.")]
    [SerializeField, Min(0.1f)] private float _fanCastRange = 4f;
    [Tooltip("부채꼴로 날리는 투사체 수.")]
    [SerializeField, Min(1)] private int _fanCount = 5;
    [Tooltip("투사체가 날아가는 거리.")]
    [SerializeField, Min(0.1f)] private float _fanDistance = 8f;

    public MonsterVisualData Monster => _monster;

    /// <summary>
    /// 타임라인 칸의 기본 수치로 빈 보스 칸을 만든다.
    /// </summary>
    public BossSpawnEntry()
    {
    }

    /// <summary>
    /// 파티 몬스터가 있으면 그 몬스터와 스킬을 쓰고, 체력 같은 수치는 이 칸을 유지한다.
    /// 파티가 비어 있으면 이 칸을 그대로 돌려준다.
    /// </summary>
    public BossSpawnEntry WithParty(BossPartyMember party)
    {
        if (party == null || party.Monster == null)
        {
            return this;
        }

        return new BossSpawnEntry(this, party);
    }

    private BossSpawnEntry(BossSpawnEntry source, BossPartyMember party)
    {
        _monster = party.Monster;
        _id = party.Monster.DexNumber;
        _maxHealth = source._maxHealth;
        _contactDamage = source._contactDamage;
        _skillDamage = source._skillDamage;
        _moveSpeed = source._moveSpeed;
        _attackKind = source._attackKind;
        _skill = party.Skill;
        _skillCooldown = source._skillCooldown;
        _projectileSprite = party.ProjectileSprite != null ? party.ProjectileSprite : source._projectileSprite;
        _chargeProjectileFrames = party.ChargeProjectileFrames;
        _flyProjectileFrames = party.FlyProjectileFrames;
        _attackRange = source._attackRange;
        _attackInterval = source._attackInterval;
        _projectileSpeed = source._projectileSpeed;
        _hitRadius = source._hitRadius;
        _attackDistance = source._attackDistance;
        _overrideScale = source._overrideScale;
        _scale = source._scale;
        _chargeSeconds = source._chargeSeconds;
        _dashSpeed = source._dashSpeed;
        _dashRecoverSeconds = source._dashRecoverSeconds;
        _slamChargeSeconds = source._slamChargeSeconds;
        _skillRange = source._skillRange;
        _slamRecoverSeconds = source._slamRecoverSeconds;
        _lungeRange = source._lungeRange;
        _lungeRecoverSeconds = source._lungeRecoverSeconds;
        _lungeSpeed = source._lungeSpeed;
        _circleMoveSpeed = source._circleMoveSpeed;
        _circleRange = source._circleRange;
        _circleGapSeconds = source._circleGapSeconds;
        _fanCastRange = source._fanCastRange;
        _fanCount = source._fanCount;
        _fanDistance = source._fanDistance;
    }

    /// <summary>
    /// 그림이 있으면 스폰 프로필을 만든다. 화면 밖 재배치는 끈다.
    /// </summary>
    public MonsterWaveProfile CreateProfile()
    {
        return new MonsterWaveProfile
        {
            Monster = _monster,
            Id = _id,
            MaxHealth = _maxHealth,
            ContactDamage = _contactDamage,
            SkillDamage = _skillDamage,
            MoveSpeed = _moveSpeed,
            AttackKind = _attackKind,
            Skill = _skill,
            SkillCooldown = _skillCooldown,
            ProjectileSprite = _projectileSprite,
            ChargeProjectileFrames = _chargeProjectileFrames,
            FlyProjectileFrames = _flyProjectileFrames,
            AttackRange = _attackRange,
            AttackInterval = _attackInterval,
            ProjectileSpeed = _projectileSpeed,
            HitRadius = _hitRadius,
            AttackDistance = _attackDistance,
            ChargeSeconds = _chargeSeconds,
            DashSpeed = _dashSpeed,
            DashRecoverSeconds = _dashRecoverSeconds,
            SlamChargeSeconds = _slamChargeSeconds,
            SkillRange = _skillRange,
            SlamRecoverSeconds = _slamRecoverSeconds,
            LungeRange = _lungeRange,
            LungeRecoverSeconds = _lungeRecoverSeconds,
            LungeSpeed = _lungeSpeed,
            CircleMoveSpeed = _circleMoveSpeed,
            CircleRange = _circleRange,
            CircleGapSeconds = _circleGapSeconds,
            FanCastRange = _fanCastRange,
            FanCount = _fanCount,
            FanDistance = _fanDistance,
            OverrideScale = true,
            Scale = _scale > 0f ? _scale : MonsterVisualData.DEFAULT_SCALE,
            DisableOffscreenTeleport = true
        };
    }

    /// <summary>
    /// 이 칸의 체력과 속도만 복사한다. 몬스터와 스킬은 포함하지 않는다.
    /// </summary>
    public BossFightStats CaptureStats()
    {
        return new BossFightStats(
            _maxHealth,
            _contactDamage,
            _moveSpeed,
            _attackKind,
            _skillCooldown,
            _attackRange,
            _attackInterval,
            _projectileSpeed,
            _hitRadius,
            _attackDistance,
            _overrideScale,
            _scale);
    }

    /// <summary>
    /// 런타임 복사본의 수치만 바꾼다. 클립에 저장된 칸에는 호출하지 않는다.
    /// </summary>
    public void ApplyStats(BossFightStats stats)
    {
        if (stats == null)
        {
            return;
        }

        _maxHealth = stats.MaxHealth;
        _contactDamage = stats.ContactDamage;
        _skillDamage = stats.SkillDamage;
        _moveSpeed = stats.MoveSpeed;
        _attackKind = stats.AttackKind;
        _skillCooldown = stats.SkillCooldown;
        _attackRange = stats.AttackRange;
        _attackInterval = stats.AttackInterval;
        _projectileSpeed = stats.ProjectileSpeed;
        _hitRadius = stats.HitRadius;
        _attackDistance = stats.AttackDistance;
        _overrideScale = stats.OverrideScale;
        _scale = stats.Scale;
        _chargeSeconds = stats.ChargeSeconds;
        _dashSpeed = stats.DashSpeed;
        _dashRecoverSeconds = stats.DashRecoverSeconds;
        _slamChargeSeconds = stats.SlamChargeSeconds;
        _skillRange = stats.SkillRange;
        _slamRecoverSeconds = stats.SlamRecoverSeconds;
        _lungeRange = stats.LungeRange;
        _lungeRecoverSeconds = stats.LungeRecoverSeconds;
        _lungeSpeed = stats.LungeSpeed;
        _circleMoveSpeed = stats.CircleMoveSpeed;
        _circleRange = stats.CircleRange;
        _circleGapSeconds = stats.CircleGapSeconds;
        _fanCastRange = stats.FanCastRange;
        _fanCount = stats.FanCount;
        _fanDistance = stats.FanDistance;
    }
}

/// <summary>
/// 타임라인에서 보스 한 마리의 체력과 속도를 조절하는 칸. 몬스터와 스킬은 보스 SO에 있다.
/// </summary>
[Serializable]
public class BossFightStats
{
    [SerializeField] private float _maxHealth = 30f;
    [SerializeField] private float _contactDamage = 4f;
    [Tooltip("스킬이 플레이어에게 주는 피해.")]
    [SerializeField] private float _skillDamage = 4f;
    [SerializeField] private float _moveSpeed = 1.2f;
    [SerializeField] private EnemyAttackKind _attackKind = EnemyAttackKind.Contact;
    [Tooltip("스킬 쿨타임. 나타난 뒤 이 시간이 지나야 첫 스킬을 쓰고, 시전이 끝난 뒤에도 이 시간을 기다린다.")]
    [SerializeField, Min(0.05f)] private float _skillCooldown = 4f;
    [SerializeField, Min(0.5f)] private float _attackRange = 4f;
    [SerializeField, Min(0.05f)] private float _attackInterval = 1.4f;
    [SerializeField, Min(0.01f)] private float _projectileSpeed = 6f;
    [SerializeField, Min(0f)] private float _hitRadius = 0.75f;
    [SerializeField, Min(0.01f)] private float _attackDistance = 0.45f;
    [HideInInspector]
    [SerializeField] private bool _overrideScale;
    [Tooltip("보스 몸 크기. 몬스터 SO의 크기는 쓰지 않는다.")]
    [SerializeField, Min(0.01f)] private float _scale = MonsterVisualData.DEFAULT_SCALE;
    [SerializeField, Min(0.05f)] private float _chargeSeconds = 0.9f;
    [SerializeField, Min(0.01f)] private float _dashSpeed = 3.6f;
    [Tooltip("돌진이 끝난 뒤 그 방향을 유지하는 시간.")]
    [SerializeField, Min(0f)] private float _dashRecoverSeconds = 0.5f;
    [SerializeField, Min(0.05f)] private float _slamChargeSeconds = 1.5f;
    [SerializeField, Min(0.1f)] private float _skillRange = 2.5f;
    [Tooltip("내리찍기 착지 뒤 스탬프 마지막 그림을 유지하는 시간.")]
    [SerializeField, Min(0f)] private float _slamRecoverSeconds = 0.5f;
    [Tooltip("이 거리 안에 플레이어가 있고 쿨타임이 끝났을 때만 몸통박치기를 한다.")]
    [SerializeField, Min(0.1f)] private float _lungeRange = 2.5f;
    [Tooltip("몸통박치기 뒤 Strike 마지막 그림을 유지하는 시간.")]
    [SerializeField, Min(0f)] private float _lungeRecoverSeconds = 0.5f;
    [Tooltip("몸통박치기로 플레이어에게 다가가는 속도.")]
    [SerializeField, Min(0.01f)] private float _lungeSpeed = 3f;
    [Tooltip("원형 연타가 공격 사이에 걸어가는 속도.")]
    [SerializeField, Min(0.01f)] private float _circleMoveSpeed = 3f;
    [Tooltip("원형 연타 원의 반경.")]
    [SerializeField, Min(0.1f)] private float _circleRange = 2.5f;
    [Tooltip("원형 연타의 공격과 공격 사이 걷는 시간.")]
    [SerializeField, Min(0f)] private float _circleGapSeconds = 0.75f;
    [Tooltip("이 거리 안에 플레이어가 있고 쿨타임이 끝났을 때만 부채꼴 사격을 한다.")]
    [SerializeField, Min(0.1f)] private float _fanCastRange = 4f;
    [Tooltip("부채꼴로 날리는 투사체 수.")]
    [SerializeField, Min(1)] private int _fanCount = 5;
    [Tooltip("투사체가 날아가는 거리.")]
    [SerializeField, Min(0.1f)] private float _fanDistance = 8f;

    public BossFightStats()
    {
    }

    public BossFightStats(
        float maxHealth,
        float contactDamage,
        float moveSpeed,
        EnemyAttackKind attackKind,
        float skillCooldown,
        float attackRange,
        float attackInterval,
        float projectileSpeed,
        float hitRadius,
        float attackDistance,
        bool overrideScale,
        float scale)
    {
        _maxHealth = maxHealth;
        _contactDamage = contactDamage;
        _skillDamage = 4f;
        _moveSpeed = moveSpeed;
        _attackKind = attackKind;
        _skillCooldown = skillCooldown;
        _attackRange = attackRange;
        _attackInterval = attackInterval;
        _projectileSpeed = projectileSpeed;
        _hitRadius = hitRadius;
        _attackDistance = attackDistance;
        _overrideScale = overrideScale;
        _scale = scale;
    }

    public float MaxHealth => _maxHealth;

    public float ContactDamage => _contactDamage;

    public float SkillDamage => _skillDamage;

    public float MoveSpeed => _moveSpeed;

    public EnemyAttackKind AttackKind => _attackKind;

    public float SkillCooldown => _skillCooldown;

    public float AttackRange => _attackRange;

    public float AttackInterval => _attackInterval;

    public float ProjectileSpeed => _projectileSpeed;

    public float HitRadius => _hitRadius;

    public float AttackDistance => _attackDistance;

    public bool OverrideScale => _overrideScale;

    public float Scale => _scale;

    public float ChargeSeconds => _chargeSeconds > 0f ? _chargeSeconds : 0.9f;

    public float DashSpeed => _dashSpeed > 0f ? _dashSpeed : 3.6f;

    public float DashRecoverSeconds => Mathf.Max(0f, _dashRecoverSeconds);

    public float SlamChargeSeconds => _slamChargeSeconds > 0f ? _slamChargeSeconds : 1.5f;

    public float SkillRange => _skillRange > 0f ? _skillRange : 2.5f;

    public float SlamRecoverSeconds => Mathf.Max(0f, _slamRecoverSeconds);

    public float LungeRange => _lungeRange > 0f ? _lungeRange : 2.5f;

    public float LungeRecoverSeconds => Mathf.Max(0f, _lungeRecoverSeconds);

    public float LungeSpeed => _lungeSpeed > 0f ? _lungeSpeed : 3f;

    public float CircleMoveSpeed => _circleMoveSpeed > 0f ? _circleMoveSpeed : 3f;

    public float CircleRange => _circleRange > 0f ? _circleRange : 2.5f;

    public float CircleGapSeconds => Mathf.Max(0f, _circleGapSeconds);

    public float FanCastRange => _fanCastRange > 0f ? _fanCastRange : 4f;

    public int FanCount => _fanCount > 0 ? _fanCount : 5;

    public float FanDistance => _fanDistance > 0f ? _fanDistance : 8f;
}
