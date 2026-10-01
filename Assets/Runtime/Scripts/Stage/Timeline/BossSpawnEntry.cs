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
}
