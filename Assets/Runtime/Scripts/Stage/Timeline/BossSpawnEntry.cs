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
    RisingVolley = 1
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
    [SerializeField] private float _moveSpeed = 1.2f;
    [SerializeField] private EnemyAttackKind _attackKind = EnemyAttackKind.Contact;
    [SerializeField] private BossSkillKind _skill = BossSkillKind.None;
    [Tooltip("상승 연사 쿨타임. 나타난 뒤 이 시간이 지나야 첫 스킬을 쓰고, 시전이 끝난 뒤에도 이 시간을 기다린다.")]
    [SerializeField, Min(0.05f)] private float _skillCooldown = 4f;
    [Tooltip("보스 스킬 탄 그림. 플레이어 무기 탄과는 따로다.")]
    [SerializeField] private Sprite _projectileSprite;
    [SerializeField, Min(0.5f)] private float _attackRange = 4f;
    [SerializeField, Min(0.05f)] private float _attackInterval = 1.4f;
    [SerializeField, Min(0.01f)] private float _projectileSpeed = 6f;
    [SerializeField, Min(0f)] private float _hitRadius = 0.75f;
    [SerializeField, Min(0.01f)] private float _attackDistance = 0.45f;
    [SerializeField] private bool _overrideScale;
    [SerializeField, Min(0.01f)] private float _scale = MonsterVisualData.DEFAULT_SCALE;

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
        _moveSpeed = source._moveSpeed;
        _attackKind = source._attackKind;
        _skill = party.Skill;
        _skillCooldown = source._skillCooldown;
        _projectileSprite = party.ProjectileSprite != null ? party.ProjectileSprite : source._projectileSprite;
        _attackRange = source._attackRange;
        _attackInterval = source._attackInterval;
        _projectileSpeed = source._projectileSpeed;
        _hitRadius = source._hitRadius;
        _attackDistance = source._attackDistance;
        _overrideScale = source._overrideScale;
        _scale = source._scale;
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
            MoveSpeed = _moveSpeed,
            AttackKind = _attackKind,
            Skill = _skill,
            SkillCooldown = _skillCooldown,
            ProjectileSprite = _projectileSprite,
            AttackRange = _attackRange,
            AttackInterval = _attackInterval,
            ProjectileSpeed = _projectileSpeed,
            HitRadius = _hitRadius,
            AttackDistance = _attackDistance,
            OverrideScale = _overrideScale,
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
    [SerializeField] private float _moveSpeed = 1.2f;
    [SerializeField] private EnemyAttackKind _attackKind = EnemyAttackKind.Contact;
    [Tooltip("상승 연사 쿨타임. 나타난 뒤 이 시간이 지나야 첫 스킬을 쓰고, 시전이 끝난 뒤에도 이 시간을 기다린다.")]
    [SerializeField, Min(0.05f)] private float _skillCooldown = 4f;
    [SerializeField, Min(0.5f)] private float _attackRange = 4f;
    [SerializeField, Min(0.05f)] private float _attackInterval = 1.4f;
    [SerializeField, Min(0.01f)] private float _projectileSpeed = 6f;
    [SerializeField, Min(0f)] private float _hitRadius = 0.75f;
    [SerializeField, Min(0.01f)] private float _attackDistance = 0.45f;
    [SerializeField] private bool _overrideScale;
    [SerializeField, Min(0.01f)] private float _scale = MonsterVisualData.DEFAULT_SCALE;

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
}
