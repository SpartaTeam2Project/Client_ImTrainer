using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가장 가까운 적에게 직선 투사체를 쏜다. 맞은 적 한 명에서 사라진다.
/// </summary>
public class StraightWeapon : Weapon
{
    [Header("Projectile")]
    [SerializeField] private WeaponProjectile _projectilePrefab;

    private readonly List<WeaponProjectile> _projectiles = new List<WeaponProjectile>();
    private Transform _projectileRoot;

    #region Protected Methods

    /// <summary>
    /// 조준 방향으로 직선 투사체 하나를 낸다.
    /// </summary>
    protected override void LaunchOne(Vector2 origin, Vector2 direction)
    {
        var projectile = GetProjectile();
        projectile.ApplySprite(Visual != null ? Visual.ProjectileSprite : null);
        projectile.Launch(origin, direction, Stats.ProjectileSpeed, Stats.AttackRange, Stats.HitRadius, Stats.Damage, AttackType);
    }

    /// <summary>
    /// 날아가는 투사체를 진행한다.
    /// </summary>
    protected override void TickShots(float deltaTime)
    {
        for (var i = 0; i < _projectiles.Count; i++)
        {
            var projectile = _projectiles[i];
            if (projectile != null && projectile.IsActive)
            {
                projectile.Tick(deltaTime);
            }
        }
    }

    /// <summary>
    /// 날아가던 투사체를 끈다.
    /// </summary>
    protected override void DismissActiveShots()
    {
        for (var i = 0; i < _projectiles.Count; i++)
        {
            var projectile = _projectiles[i];
            if (projectile != null && projectile.IsActive)
            {
                projectile.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 투사체 뿌리를 지운다.
    /// </summary>
    protected override void ClearShots()
    {
        if (_projectileRoot != null)
        {
            Destroy(_projectileRoot.gameObject);
            _projectileRoot = null;
        }

        _projectiles.Clear();
    }

    /// <summary>
    /// 투사체를 무기 밖 뿌리에 둔다.
    /// </summary>
    protected override void OnInitialized()
    {
        EnsureProjectileRoot();
    }

    #endregion

    #region Private Methods

    private WeaponProjectile GetProjectile()
    {
        for (var i = 0; i < _projectiles.Count; i++)
        {
            var projectile = _projectiles[i];
            if (projectile != null && !projectile.IsActive)
            {
                return projectile;
            }
        }

        EnsureProjectileRoot();
        var created = CreateProjectile();
        _projectiles.Add(created);
        return created;
    }

    private WeaponProjectile CreateProjectile()
    {
        if (_projectilePrefab == null)
        {
            return WeaponProjectile.Create(_projectileRoot);
        }

        var projectile = Instantiate(_projectilePrefab, _projectileRoot);
        projectile.gameObject.SetActive(false);
        return projectile;
    }

    private void EnsureProjectileRoot()
    {
        if (_projectileRoot != null)
        {
            return;
        }

        var rootObject = new GameObject("WeaponProjectiles");
        _projectileRoot = rootObject.transform;
    }

    #endregion
}
