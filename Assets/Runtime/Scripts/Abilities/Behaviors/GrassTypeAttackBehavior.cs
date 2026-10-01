using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 플레이어 주위를 일정 시간 도는 별을 소환한다.
/// </summary>
public class GrassTypeAttackBehavior : AbilityBehavior<GrassTypeAttackData, GrassTypeAttackLevel>
{
    private const float MIN_MULTIPLIER = 0.01f;
    private const float RADIUS_BLEND_SECONDS = 0.5f;
    private const float MIN_DELAY = 0.5f;

    [SerializeField] private GameObject starPrefab;

    private StageObjectPool<GrassTypeAttackProjectileBehavior> _pool;
    private readonly List<GrassTypeAttackProjectileBehavior> _stars = new List<GrassTypeAttackProjectileBehavior>();
    private CancellationTokenSource _loop;
    private float _angle;
    private float _radiusMultiplier;
    private float _radiusTarget;

    #region Unity Methods

    private void Awake()
    {
        _pool = new StageObjectPool<GrassTypeAttackProjectileBehavior>(starPrefab, transform);
    }

    private void LateUpdate()
    {
        if (AbilityLevel == null || !TryGetPlayer(out var player))
        {
            return;
        }

        if (AbilityManager.IsCombatPaused())
        {
            return;
        }

        transform.position = player.transform.position;
        var blendSpeed = 1f / RADIUS_BLEND_SECONDS;
        _radiusMultiplier = Mathf.MoveTowards(_radiusMultiplier, _radiusTarget, blendSpeed * Time.deltaTime);
        _angle += AbilityLevel.AngularSpeed * Mathf.Max(MIN_MULTIPLIER, player.projectileSpeedMultiplier) * Time.deltaTime;
        PlaceStars(player.sizeMultiplier);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 소환 루프와 돌고 있던 별을 치운다.
    /// </summary>
    public override void Clear()
    {
        StopLoop();
        _pool?.DestroyAll();
        base.Clear();
    }

    #endregion

    #region Private Methods

    protected override void SetAbilityLevel(int levelId)
    {
        base.SetAbilityLevel(levelId);
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

        ClearStars();
    }

    private async UniTaskVoid LaunchLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (AbilityLevel == null || !TryGetPlayer(out var player))
            {
                var waitCanceled = await UniTask.Yield(token).SuppressCancellationThrow();
                if (waitCanceled)
                {
                    return;
                }

                continue;
            }

            if (AbilityManager.IsCombatPaused())
            {
                var pausedCanceled = await UniTask.Yield(token).SuppressCancellationThrow();
                if (pausedCanceled)
                {
                    return;
                }

                continue;
            }

            SpawnStars(player);
            _radiusTarget = 1f;
            var lifetime = AbilityLevel.ProjectileLifetime * Mathf.Max(MIN_MULTIPLIER, player.durationMultiplier);
            var shown = await WaitCombatSecondsAsync(Mathf.Max(0f, lifetime - RADIUS_BLEND_SECONDS), token);
            if (shown)
            {
                return;
            }

            HideStars();
            _radiusTarget = 0f;
            var hideCanceled = await WaitCombatSecondsAsync(RADIUS_BLEND_SECONDS, token);
            if (hideCanceled)
            {
                return;
            }

            ClearStars();
            var cooldown = AbilityLevel.AbilityCooldown * Mathf.Max(MIN_MULTIPLIER, player.cooldownMultiplier);
            var delay = cooldown - lifetime;
            if (delay < MIN_DELAY)
            {
                delay = MIN_DELAY;
            }

            var delayCanceled = await WaitCombatSecondsAsync(delay, token);
            if (delayCanceled)
            {
                return;
            }
        }
    }

    private void SpawnStars(Player player)
    {
        ClearStars();
        if (_pool == null || AbilityLevel == null)
        {
            return;
        }

        var count = Mathf.Max(0, AbilityLevel.ProjectilesCount);
        var attackType = ResolveAttackType();
        for (var i = 0; i < count; i++)
        {
            var star = _pool.Get();
            if (star == null)
            {
                continue;
            }

            star.Show(PlayerId, AbilityLevel.Damage, attackType, player.sizeMultiplier);
            _stars.Add(star);
        }
    }

    private void HideStars()
    {
        for (var i = 0; i < _stars.Count; i++)
        {
            if (_stars[i] != null)
            {
                _stars[i].Hide();
            }
        }
    }

    private void ClearStars()
    {
        for (var i = 0; i < _stars.Count; i++)
        {
            if (_stars[i] != null)
            {
                _stars[i].Clear();
            }
        }

        _stars.Clear();
        _radiusMultiplier = 0f;
        _radiusTarget = 0f;
    }

    private void PlaceStars(float sizeMultiplier)
    {
        var count = _stars.Count;
        if (count == 0 || AbilityLevel == null)
        {
            return;
        }

        var distance = AbilityLevel.Radius * _radiusMultiplier * sizeMultiplier;
        for (var i = 0; i < count; i++)
        {
            if (_stars[i] == null)
            {
                continue;
            }

            var projectileAngle = 360f / count * i + _angle;
            _stars[i].transform.localPosition = Quaternion.Euler(0f, 0f, projectileAngle) * Vector3.up * distance;
        }
    }

    #endregion
}
