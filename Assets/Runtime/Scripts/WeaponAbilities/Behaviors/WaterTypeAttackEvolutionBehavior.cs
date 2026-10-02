using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 물 타입 진화. 더 긴 관통 빔을 반복해서 쏜다.
/// </summary>
public class WaterTypeAttackEvolutionBehavior : WeaponAbilityBehavior<WaterTypeAttackEvolutionData, WaterTypeAttackEvolutionLevel>, ISlotOriginAbility
{
    private const float MIN_MULTIPLIER = 0.01f;

    [SerializeField] private WaterTypeAttackBeamBehavior beam;

    private CancellationTokenSource _loop;
    private WeaponSlot _originSlot;

    public WeaponSlot OriginSlot => _originSlot;

    #region Unity Methods

    private void Awake()
    {
        CacheBeam();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 진화 물줄기가 나갈 장착 칸을 빔에 넘긴다.
    /// </summary>
    public void BindSlot(WeaponSlot slot)
    {
        _originSlot = slot;
        CacheBeam();
        if (beam != null)
        {
            beam.BindSlot(slot);
        }
    }

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

    protected override void SetWeaponAbilityLevel(int levelId)
    {
        base.SetWeaponAbilityLevel(levelId);
        StopLoop();
        _loop = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        LaunchLoopAsync(_loop.Token).Forget();
    }

    private void CacheBeam()
    {
        if (beam == null)
        {
            beam = GetComponentInChildren<WaterTypeAttackBeamBehavior>(true);
        }
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
            if (WeaponAbilityLevel == null || beam == null || !TryGetPlayer(out var player))
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

            var damage = WeaponAbilityLevel.Damage * player.Stat.damageMultiplier;
            var length = WeaponAbilityLevel.BeamLength * player.Stat.sizeMultiplier;
            var width = WeaponAbilityLevel.BeamWidth * player.Stat.sizeMultiplier;
            var duration = WeaponAbilityLevel.BeamDuration * Mathf.Max(MIN_MULTIPLIER, player.Stat.durationMultiplier);
            beam.Play(PlayerId, damage, length, width, duration, WeaponAbilityLevel.DamageInterval, ResolveAttackType());
            var shotCanceled = await WaitCombatSecondsAsync(duration, token);
            if (shotCanceled)
            {
                return;
            }

            var cooldown = WeaponAbilityLevel.WeaponAbilityCooldown * Mathf.Max(MIN_MULTIPLIER, player.Stat.cooldownMultiplier);
            var cooldownCanceled = await WaitCombatSecondsAsync(cooldown, token);
            if (cooldownCanceled)
            {
                return;
            }
        }
    }

    #endregion
}
