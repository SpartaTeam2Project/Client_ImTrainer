using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 가까운 적 방향으로 관통 빔을 반복해서 쏜다.
/// </summary>
public class WaterTypeAttackBehavior : AbilityBehavior<WaterTypeAttackData, WaterTypeAttackLevel>
{
    private const float MIN_MULTIPLIER = 0.01f;

    [SerializeField] private WaterTypeAttackBeamBehavior beam;

    private CancellationTokenSource _loop;

    #region Unity Methods

    private void Awake()
    {
        if (beam == null)
        {
            beam = GetComponentInChildren<WaterTypeAttackBeamBehavior>(true);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 발사 루프와 켜져 있던 빔을 멈춘다.
    /// </summary>
    public override void Clear()
    {
        StopLoop();
        if (beam != null)
        {
            beam.Stop();
        }

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
        if (_loop == null)
        {
            return;
        }

        _loop.Cancel();
        _loop.Dispose();
        _loop = null;
    }

    private async UniTaskVoid LaunchLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (AbilityLevel == null || beam == null || !TryGetPlayer(out var player))
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

            var damage = AbilityLevel.Damage * player.damageMultiplier;
            var length = AbilityLevel.BeamLength * player.sizeMultiplier;
            var width = AbilityLevel.BeamWidth * player.sizeMultiplier;
            var duration = AbilityLevel.BeamDuration * Mathf.Max(MIN_MULTIPLIER, player.durationMultiplier);
            beam.Play(PlayerId, damage, length, width, duration, AbilityLevel.DamageInterval, ResolveAttackType());
            var shotCanceled = await WaitCombatSecondsAsync(duration, token);
            if (shotCanceled)
            {
                return;
            }

            var cooldown = AbilityLevel.AbilityCooldown * Mathf.Max(MIN_MULTIPLIER, player.cooldownMultiplier);
            var cooldownCanceled = await WaitCombatSecondsAsync(cooldown, token);
            if (cooldownCanceled)
            {
                return;
            }
        }
    }

    #endregion
}
