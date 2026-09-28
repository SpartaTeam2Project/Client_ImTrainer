using System;
using UnityEngine;

/// <summary>
/// 한 판에 스폰할 플레이어의 트레이너 스탯.
/// </summary>
[Serializable]
public class CharacterStats
{
    [SerializeField] private float _maxHealth = 30f;
    [SerializeField] private float _moveSpeed = 4f;
    [SerializeField] private float _damageMultiplier = 1f;
    [SerializeField] private float _magnetRadius = 2.25f;
    [SerializeField] private float _xpMultiplier = 1f;
    [SerializeField] private float _hpRegen;
    [SerializeField] private float _cooldownMultiplier = 1f;
    [SerializeField] private float _projectileSpeedMultiplier = 1f;
    [SerializeField] private float _projectileMultiplier = 1f;
    [SerializeField] private float _invincibleSeconds;

    public float MaxHealth => _maxHealth;

    public float MoveSpeed => _moveSpeed;

    public float DamageMultiplier => Mathf.Max(0f, _damageMultiplier);

    public float MagnetRadius => Mathf.Max(0f, _magnetRadius);

    public float XpMultiplier => Mathf.Max(0f, _xpMultiplier);

    public float HpRegen => _hpRegen;

    public float CooldownMultiplier => Mathf.Max(0f, _cooldownMultiplier);

    public float ProjectileSpeedMultiplier => Mathf.Max(0f, _projectileSpeedMultiplier);

    public float ProjectileMultiplier => Mathf.Max(0f, _projectileMultiplier);

    public float InvincibleSeconds => Mathf.Max(0f, _invincibleSeconds);
}
