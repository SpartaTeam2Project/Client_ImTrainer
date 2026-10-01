using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가장 가까운 적이 사거리 안에 있으면, 총알을 플레이어 주위에 띄워 일정 시간 돌린다.
/// </summary>
public class OrbitWeapon : Weapon
{
    private const float ANGLE_SQR_EPSILON = 0.0001f;
    private const float DEFAULT_ORBIT_RADIUS = 1.8f;
    private const float DEFAULT_ORBIT_DURATION = 3f;
    private const float DEFAULT_HIT_INTERVAL = 0.5f;
    private const float FULL_CIRCLE_DEGREES = 360f;

    [Header("Orbit")]
    [SerializeField] private OrbitProjectile _projectilePrefab;
    [SerializeField] private float _orbitRadius = DEFAULT_ORBIT_RADIUS;
    [SerializeField] private float _orbitDuration = DEFAULT_ORBIT_DURATION;
    [SerializeField] private float _hitInterval = DEFAULT_HIT_INTERVAL;

    private readonly List<OrbitProjectile> _projectiles = new List<OrbitProjectile>();
    private Transform _projectileRoot;

    #region Protected Methods

    /// <summary>
    /// 발수만큼 균등한 각도로 궤도에 올린다. 이미 도는 중이면 수명만 다시 채운다.
    /// </summary>
    protected override void LaunchAttack(Vector2 origin, Vector2 direction)
    {
        if (HasActiveOrbit())
        {
            RefreshActiveOrbit();
            return;
        }

        if (!TryGetPlayerPosition(out var playerPosition))
        {
            return;
        }

        var fromPlayer = origin - playerPosition;
        var startDegrees = fromPlayer.sqrMagnitude > ANGLE_SQR_EPSILON
            ? Mathf.Atan2(fromPlayer.y, fromPlayer.x) * Mathf.Rad2Deg
            : Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        var count = Stats.ProjectileCount;
        var step = FULL_CIRCLE_DEGREES / count;
        for (var i = 0; i < count; i++)
        {
            LaunchOne(origin, AngleToDirection(startDegrees + step * i));
        }
    }

    /// <summary>
    /// 방향각 자리에 궤도 총알 하나를 켠다. projectileSpeed는 초당 회전 각도다.
    /// </summary>
    protected override void LaunchOne(Vector2 origin, Vector2 direction)
    {
        var angleDegrees = direction.sqrMagnitude > ANGLE_SQR_EPSILON
            ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg
            : 0f;
        var projectile = GetProjectile();
        projectile.ApplySprite(Visual != null ? Visual.ProjectileSprite : null);
        projectile.Launch(
            PlayerId,
            angleDegrees,
            _orbitRadius,
            Stats.ProjectileSpeed,
            _orbitDuration,
            Stats.HitRadius,
            _hitInterval,
            Stats.Damage,
            AttackType);
    }

    /// <summary>
    /// 도는 총알을 진행한다.
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
    /// 돌고 있던 총알을 끈다.
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
    /// 궤도 총알 뿌리를 지운다.
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
    /// 총알을 무기 밖 뿌리에 둔다.
    /// </summary>
    protected override void OnInitialized()
    {
        EnsureProjectileRoot();
    }

    #endregion

    #region Private Methods

    private bool HasActiveOrbit()
    {
        for (var i = 0; i < _projectiles.Count; i++)
        {
            var projectile = _projectiles[i];
            if (projectile != null && projectile.IsActive)
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshActiveOrbit()
    {
        for (var i = 0; i < _projectiles.Count; i++)
        {
            var projectile = _projectiles[i];
            if (projectile == null || !projectile.IsActive)
            {
                continue;
            }

            projectile.Refresh(_orbitDuration, Stats.ProjectileSpeed, _orbitRadius, Stats.HitRadius, Stats.Damage);
        }
    }

    private bool TryGetPlayerPosition(out Vector2 playerPosition)
    {
        playerPosition = Vector2.zero;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager(out PlayerManager playerManager))
        {
            return false;
        }

        if (playerManager.LocalPlayerId != PlayerId || !playerManager.IsAlive || playerManager.PlayerTransform == null)
        {
            return false;
        }

        playerPosition = playerManager.PlayerTransform.position;
        return true;
    }

    private OrbitProjectile GetProjectile()
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

    private OrbitProjectile CreateProjectile()
    {
        if (_projectilePrefab == null)
        {
            return OrbitProjectile.Create(_projectileRoot);
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

        var rootObject = new GameObject("OrbitProjectiles");
        _projectileRoot = rootObject.transform;
    }

    private static Vector2 AngleToDirection(float degrees)
    {
        var radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    #endregion
}
