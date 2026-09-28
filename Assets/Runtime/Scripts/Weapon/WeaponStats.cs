using System;
using UnityEngine;

/// <summary>
/// 무기 레벨 1의 자리와 발사 수치.
/// </summary>
[Serializable]
public class WeaponStats
{
    private const float MIN_COOLDOWN = 0.05f;

    [SerializeField] private float _followDistance = 1.15f;
    [SerializeField] private float _cooldown = 1f;
    [SerializeField] private float _damage = 8f;
    [SerializeField] private int _projectileCount = 1;
    [SerializeField] private float _attackRange = 7f;
    [SerializeField] private float _projectileSpeed = 9f;
    [SerializeField] private float _hitRadius = 0.45f;

    public float FollowDistance => Mathf.Max(0f, _followDistance);

    public float Cooldown => Mathf.Max(MIN_COOLDOWN, _cooldown);

    public float Damage => Mathf.Max(0f, _damage);

    public int ProjectileCount => Mathf.Max(1, _projectileCount);

    public float AttackRange => Mathf.Max(0f, _attackRange);

    public float ProjectileSpeed => Mathf.Max(0f, _projectileSpeed);

    public float HitRadius => Mathf.Max(0f, _hitRadius);

    /// <summary>
    /// 고른 수치만 더한다. 쿨타임은 적힌 만큼 짧아진다.
    /// </summary>
    public void AddStat(WeaponStatKind stat, float amount)
    {
        switch (stat)
        {
            case WeaponStatKind.AttackRange:
                _attackRange += amount;
                break;
            case WeaponStatKind.Cooldown:
                _cooldown -= Mathf.Abs(amount);
                break;
            case WeaponStatKind.Width:
                _hitRadius += amount;
                break;
            case WeaponStatKind.Damage:
                _damage += amount;
                break;
        }
    }
}
