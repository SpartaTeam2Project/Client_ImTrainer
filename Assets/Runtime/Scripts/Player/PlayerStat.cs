using System;
using UnityEngine;

/// <summary>
/// 플레이어의 이동 속도와 각종 배율을 담당한다.
/// </summary>
public class PlayerStat : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float _moveSpeed = 4f;
    [SerializeField] private float _damageMultiplier = 1f;
    [SerializeField] private float _magnetRadius = 1f;
    [SerializeField] private float _xpMultiplier = 1f;
    [SerializeField] private float _hpRegen;
    [SerializeField] private float _cooldownMultiplier = 1f;
    [SerializeField] private float _projectileSpeedMultiplier = 1f;
    [SerializeField] private float _projectileMultiplier = 1f;
    [SerializeField] private float _sizeMultiplier = 1f;
    [SerializeField] private float _durationMultiplier = 1f;
    [SerializeField] private float _invincibleSeconds;

    public float moveSpeed { get; private set; }

    // 아래 네 배율은 Player의 UpgradeAPI가 직접 누적한다.
    public float moveSpeedMultiplier { get; set; } = 1f;

    /// <summary>
    /// 장판처럼 잠시 느려질 때 곱하는 배율. 훈련 배율과는 따로다. 1이면 느려지지 않는다.
    /// </summary>
    public float moveSlowMultiplier { get; private set; } = 1f;
    public float receivedDamageMultiplier { get; set; } = 1f;

    public float damageMultiplier { get; set; } = 1f;

    public float magnetRadius { get; private set; }

    public float xpMultiplier { get; set; } = 1f;

    public float hpRegen { get; private set; }

    public float cooldownMultiplier { get; private set; } = 1f;

    public float projectileSpeedMultiplier { get; private set; } = 1f;

    public float projectileMultiplier { get; private set; } = 1f;

    public float sizeMultiplier { get; private set; } = 1f;

    public float durationMultiplier { get; private set; } = 1f;

    public float damageReductionPercent { get; private set; }

    public float invincibleSeconds { get; private set; }

    #region Public Methods

    /// <summary>
    /// 프리팹에 적힌 스탯으로 되돌린다.
    /// </summary>
    public void Initialize()
    {
        moveSpeed = Mathf.Max(0f, _moveSpeed);
        moveSpeedMultiplier = 1f;
        moveSlowMultiplier = 1f;
        receivedDamageMultiplier = 1f;
        damageMultiplier = Mathf.Max(0f, _damageMultiplier);
        magnetRadius = Mathf.Max(0f, _magnetRadius);
        xpMultiplier = Mathf.Max(0f, _xpMultiplier);
        hpRegen = _hpRegen;
        cooldownMultiplier = Mathf.Max(0f, _cooldownMultiplier);
        projectileSpeedMultiplier = Mathf.Max(0f, _projectileSpeedMultiplier);
        projectileMultiplier = Mathf.Max(0f, _projectileMultiplier);
        sizeMultiplier = Mathf.Max(0f, _sizeMultiplier);
        durationMultiplier = Mathf.Max(0f, _durationMultiplier);
        damageReductionPercent = 0f;
        invincibleSeconds = Mathf.Max(0f, _invincibleSeconds);
    }

    // player stat changer
    #region StatChangeAPI
    /// <summary>
    /// Upgrade에 의한 이동속도 배율을 합연산으로 누적한다.
    /// input:0.2f = +20%
    /// </summary>
    /// <param name="value"></param>
    public void AddMoveSpeedMultiplier(float value)
    {
        moveSpeedMultiplier = Mathf.Max(0f, moveSpeedMultiplier + value);
    }

    /// <summary>
    /// 장판 둔화 배율을 넣는다. 1이면 원래 속도, 0.6이면 40% 느리다.
    /// </summary>
    public void SetMoveSlow(float multiplier)
    {
        moveSlowMultiplier = Mathf.Clamp01(multiplier);
    }

    /// <summary>
    /// Upgrade에 의한 경험치 배율을 합연산으로 누적한다.
    /// input:0.2f = +20%
    /// </summary>
    /// <param name="value"></param>
    public void AddXpMultiplier(float value)
    {
        xpMultiplier = Mathf.Max(0f, xpMultiplier + value);
    }

    /// <summary>
    /// Upgrade에 의한 받는 피해 배율을 합연산으로 누적한다.
    /// input:-0.1f = -10% (받는피해 10%감소)
    /// </summary>
    /// <param name="value"></param>
    public void AddReceivedDamageMultiplier(float value)
    {
        receivedDamageMultiplier = Mathf.Max(0f, receivedDamageMultiplier + value);
    }
    /// <summary>
    /// Upgrade에 의한 주는 피해 배율을 합연산으로 누적한다.
    /// input:0.2f = +20%
    /// </summary>
    /// <param name="value"></param>
    public void AddDamageMultiplier(float value)
    {
        damageMultiplier = Mathf.Max(0f, damageMultiplier + value);
    }
    #endregion

    #region LegacyStatChageAPI
    [Obsolete]
    /// <summary>
    /// replaced: AddMoveSpeedMultiplier
    /// </summary>
    public void RecalculateMoveSpeed(float multiplier)
    {
        moveSpeed = Mathf.Max(0f, _moveSpeed) * Mathf.Max(0f, multiplier);
    }
    [Obsolete]
    /// <summary>
    /// replaced: AddXpMultiplier
    /// </summary>
    public void RecalculateXpMultiplier(float multiplier)
    {
        xpMultiplier = Mathf.Max(0f, _xpMultiplier) * Mathf.Max(0f, multiplier);
    }
    [Obsolete]
    /// <summary>
    /// 쿨다운 배율을 기본값에 곱한다.
    /// </summary>
    public void RecalculateCooldownMultiplier(float multiplier)
    {
        cooldownMultiplier = Mathf.Max(0f, _cooldownMultiplier) * Mathf.Max(0f, multiplier);
    }
    [Obsolete]
    /// <summary>
    /// 자석 반경 배율을 기본값에 곱한다.
    /// </summary>
    public void RecalculateMagnetRadius(float multiplier)
    {
        magnetRadius = Mathf.Max(0f, _magnetRadius) * Mathf.Max(0f, multiplier);
    }
    [Obsolete]
    /// <summary>
    /// 투사체 속도 배율을 기본값에 곱한다.
    /// </summary>
    public void RecalculateProjectileSpeedMultiplier(float multiplier)
    {
        projectileSpeedMultiplier = Mathf.Max(0f, _projectileSpeedMultiplier) * Mathf.Max(0f, multiplier);
    }
    [Obsolete]
    /// <summary>
    /// 능력 크기 배율을 기본값에 곱한다.
    /// </summary>
    public void RecalculateSizeMultiplier(float multiplier)
    {
        sizeMultiplier = Mathf.Max(0f, _sizeMultiplier) * Mathf.Max(0f, multiplier);
    }
    [Obsolete]
    /// <summary>
    /// 지속 시간 배율을 기본값에 곱한다.
    /// </summary>
    public void RecalculateDurationMultiplier(float multiplier)
    {
        durationMultiplier = Mathf.Max(0f, _durationMultiplier) * Mathf.Max(0f, multiplier);
    }
    [Obsolete]
    /// <summary>
    /// replaced: AddReceivedDamageMultiplier
    /// </summary>
    public void RecalculateDamageReduction(float percent)
    {
        damageReductionPercent = Mathf.Clamp(percent, 0f, 100f);
    }
    #endregion
    #endregion

}
