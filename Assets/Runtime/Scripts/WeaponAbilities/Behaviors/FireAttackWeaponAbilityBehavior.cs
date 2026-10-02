using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 가까운 적에게 파이어볼을 연사한다.
/// </summary>
public class FireAttackWeaponAbilityBehavior : WeaponAbilityBehavior<FireAttackWeaponAbilityData, FireAttackWeaponAbilityLevel>
{
    private const float CLOSEST_RANGE = 80f;
    private const float MIN_MULTIPLIER = 0.01f;

    [SerializeField] private GameObject fireballPrefab;

    private StageObjectPool<FireballProjectileBehavior> _pool;
    private readonly List<FireballProjectileBehavior> _alive = new List<FireballProjectileBehavior>();
    private CancellationTokenSource _loop;

    #region Unity Methods

    private void Awake()
    {
        _pool = new StageObjectPool<FireballProjectileBehavior>(fireballPrefab, transform);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 수명이 끝난 파이어볼을 발사 목록에서 뺀다.
    /// </summary>
    public void NotifyFireballFinished(FireballProjectileBehavior fireball)
    {
        _alive.Remove(fireball);
    }

    /// <summary>
    /// 발사 루프와 날아가던 파이어볼을 멈추고 치운다.
    /// </summary>
    public override void Clear()
    {
        StopLoop();
        _pool?.DestroyAll();
        base.Clear();
    }

    #endregion

    #region Private Methods

    protected override void SetWeaponAbilityLevel(int levelId)
    {
        base.SetWeaponAbilityLevel(levelId);
        StopLoop();
        _loop = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        LaunchLoopAsync(_loop.Token).Forget();
    }

    private void StopLoop()
    {
        if (_loop != null)
        {
            _loop.Cancel();
            _loop.Dispose();
            _loop = null;
        }

        for (var i = 0; i < _alive.Count; i++)
        {
            if (_alive[i] != null)
            {
                _alive[i].Clear();
            }
        }

        _alive.Clear();
    }

    private async UniTaskVoid LaunchLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (WeaponAbilityLevel == null || !TryGetPlayer(out var player))
            {
                var waitCanceled = await UniTask.Yield(token).SuppressCancellationThrow();
                if (waitCanceled)
                {
                    return;
                }

                continue;
            }

            if (WeaponAbilityManager.IsCombatPaused())
            {
                var pausedCanceled = await UniTask.Yield(token).SuppressCancellationThrow();
                if (pausedCanceled)
                {
                    return;
                }

                continue;
            }

            var count = Mathf.Max(0, WeaponAbilityLevel.ProjectilesCount);
            for (var i = 0; i < count; i++)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (WeaponAbilityManager.IsCombatPaused())
                {
                    break;
                }

                LaunchOne(player);
                if (WeaponAbilityLevel.TimeBetweenFireballs <= 0f)
                {
                    continue;
                }

                var betweenCanceled = await WaitSecondsAsync(WeaponAbilityLevel.TimeBetweenFireballs, token);
                if (betweenCanceled)
                {
                    return;
                }
            }

            var cooldown = WeaponAbilityLevel.WeaponAbilityCooldown * Mathf.Max(MIN_MULTIPLIER, player.Stat.cooldownMultiplier);
            cooldown -= WeaponAbilityLevel.TimeBetweenFireballs * count;
            var cooldownCanceled = await WaitSecondsAsync(Mathf.Max(0f, cooldown), token);
            if (cooldownCanceled)
            {
                return;
            }
        }
    }

    private async UniTask<bool> WaitSecondsAsync(float seconds, CancellationToken token)
    {
        var elapsed = 0f;
        while (elapsed < seconds)
        {
            if (token.IsCancellationRequested)
            {
                return true;
            }

            if (!WeaponAbilityManager.IsCombatPaused())
            {
                elapsed += Time.deltaTime;
            }

            var frameCanceled = await UniTask.Yield(token).SuppressCancellationThrow();
            if (frameCanceled)
            {
                return true;
            }
        }

        return false;
    }

    private void LaunchOne(Player player)
    {
        if (_pool == null || player == null || WeaponAbilityLevel == null)
        {
            return;
        }

        var fireball = _pool.Get();
        if (fireball == null)
        {
            return;
        }

        var origin = player.transform.position;
        var direction = ResolveDirection(origin);
        if (Managers.Instance != null && Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            playerManager.FaceShot(PlayerId, direction);
        }

        fireball.transform.SetPositionAndRotation(origin, Quaternion.FromToRotation(Vector2.up, direction));
        fireball.Launch(
            this,
            ResolveAttackType(),
            WeaponAbilityLevel.Damage * player.Stat.damageMultiplier,
            WeaponAbilityLevel.Speed,
            WeaponAbilityLevel.FireballLifetime,
            WeaponAbilityLevel.ProjectileSize,
            WeaponAbilityLevel.ExplosionRadius,
            player.Stat.sizeMultiplier,
            player.Stat.durationMultiplier,
            Mathf.Max(MIN_MULTIPLIER, player.Stat.projectileSpeedMultiplier));
        _alive.Add(fireball);
    }

    private Vector2 ResolveDirection(Vector3 origin)
    {
        if (Managers.Instance != null
            && Managers.Instance.TryGetManager<EnemyManager>(out var enemyManager)
            && enemyManager.TryGetClosest(origin, CLOSEST_RANGE, out var enemy))
        {
            var offset = (Vector2)enemy.transform.position - (Vector2)origin;
            if (offset.sqrMagnitude > 0.0001f)
            {
                return offset.normalized;
            }
        }

        var random = UnityEngine.Random.insideUnitCircle;
        if (random.sqrMagnitude <= 0.0001f)
        {
            return Vector2.up;
        }

        return random.normalized;
    }

    private MonsterType ResolveAttackType()
    {
        if (Managers.Instance != null && Managers.Instance.TryGetManager<AccountManager>(out var account))
        {
            var monster = account.ResolveRunMonster();
            if (monster != null)
            {
                return monster.PrimaryType;
            }
        }

        return MonsterType.Normal;
    }

    #endregion
}
