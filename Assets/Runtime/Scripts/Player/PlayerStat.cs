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

    /// <summary>
    /// 이동 속도 배율을 기본값에 곱한다.
    /// </summary>
    public void RecalculateMoveSpeed(float multiplier)
    {
        moveSpeed = Mathf.Max(0f, _moveSpeed) * Mathf.Max(0f, multiplier);
    }

    /// <summary>
    /// 경험치 배율을 기본값에 곱한다.
    /// </summary>
    public void RecalculateXpMultiplier(float multiplier)
    {
        xpMultiplier = Mathf.Max(0f, _xpMultiplier) * Mathf.Max(0f, multiplier);
    }

    /// <summary>
    /// 쿨다운 배율을 기본값에 곱한다.
    /// </summary>
    public void RecalculateCooldownMultiplier(float multiplier)
    {
        cooldownMultiplier = Mathf.Max(0f, _cooldownMultiplier) * Mathf.Max(0f, multiplier);
    }

    /// <summary>
    /// 자석 반경 배율을 기본값에 곱한다.
    /// </summary>
    public void RecalculateMagnetRadius(float multiplier)
    {
        magnetRadius = Mathf.Max(0f, _magnetRadius) * Mathf.Max(0f, multiplier);
    }

    /// <summary>
    /// 투사체 속도 배율을 기본값에 곱한다.
    /// </summary>
    public void RecalculateProjectileSpeedMultiplier(float multiplier)
    {
        projectileSpeedMultiplier = Mathf.Max(0f, _projectileSpeedMultiplier) * Mathf.Max(0f, multiplier);
    }

    /// <summary>
    /// 능력 크기 배율을 기본값에 곱한다.
    /// </summary>
    public void RecalculateSizeMultiplier(float multiplier)
    {
        sizeMultiplier = Mathf.Max(0f, _sizeMultiplier) * Mathf.Max(0f, multiplier);
    }

    /// <summary>
    /// 지속 시간 배율을 기본값에 곱한다.
    /// </summary>
    public void RecalculateDurationMultiplier(float multiplier)
    {
        durationMultiplier = Mathf.Max(0f, _durationMultiplier) * Mathf.Max(0f, multiplier);
    }

    /// <summary>
    /// 받는 피해를 줄이는 퍼센트를 정한다. 0이면 그대로, 100이면 피해가 없다.
    /// </summary>
    public void RecalculateDamageReduction(float percent)
    {
        damageReductionPercent = Mathf.Clamp(percent, 0f, 100f);
    }

    #endregion
}
