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

    public float MoveSpeed { get; set; }

    public EnemyAttackKind AttackKind { get; set; }

    public BossSkillKind Skill { get; set; }

    public float SkillCooldown { get; set; }

    public Sprite ProjectileSprite { get; set; }

    public float AttackRange { get; set; }

    public float AttackInterval { get; set; }

    public float ProjectileSpeed { get; set; }

    public float HitRadius { get; set; }

    public float AttackDistance { get; set; }

    public bool OverrideScale { get; set; }

    public float Scale { get; set; }

    public bool CircularSpawn { get; set; }

    public bool DisableOffscreenTeleport { get; set; }
}
