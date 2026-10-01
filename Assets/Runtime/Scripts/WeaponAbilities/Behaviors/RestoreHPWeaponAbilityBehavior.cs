using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 간격마다 최대 체력 비율만큼 회복한다.
/// </summary>
public class RestoreHPWeaponAbilityBehavior : WeaponAbilityBehavior<RestoreHPWeaponAbilityData, RestoreHPWeaponAbilityLevel>
{
    private const float MIN_COOLDOWN_MULTIPLIER = 0.1f;

    private CancellationTokenSource _regen;

    /// <summary>
    /// 회복 루프를 시작한다.
    /// </summary>
    public override void Init(int playerId, WeaponAbilityData data, int level)
    {
        base.Init(playerId, data, level);
        _regen = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        RegenAsync(_regen.Token).Forget();
    }

    /// <summary>
    /// 회복 루프를 멈추고 치운다.
    /// </summary>
    public override void Clear()
    {
        if (_regen != null)
        {
            _regen.Cancel();
            _regen.Dispose();
            _regen = null;
        }

        base.Clear();
    }

    private async UniTaskVoid RegenAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (WeaponAbilityLevel == null || !TryGetPlayer(out var player))
            {
                await UniTask.Yield(token);
                continue;
            }

            player.RestoreHp(WeaponAbilityLevel.RestoredHPPercent);
            var wait = WeaponAbilityLevel.Cooldown * Mathf.Max(MIN_COOLDOWN_MULTIPLIER, player.cooldownMultiplier);
            await UniTask.Delay(TimeSpan.FromSeconds(wait), cancellationToken: token);
        }
    }
}
