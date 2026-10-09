using UnityEngine;

/// <summary>
/// 플레이어의 체력, 피해, 회복, 사망 판정을 담당한다.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float _maxHealth = 30f;

    private Player _owner;
    private PlayerStat _stat;
    private HealthbarBehavior _healthbar;

    public float CurrentHealth { get; private set; }

    public float maxHealth { get; private set; }

    public bool IsAlive { get; private set; }

    #region Public Methods

    /// <summary>
    /// 체력을 가득 채우고 체력바를 연결한다.
    /// </summary>
    public void Initialize(Player owner, PlayerStat stat)
    {
        _owner = owner;
        _stat = stat;
        maxHealth = Mathf.Max(1f, _maxHealth);
        CurrentHealth = maxHealth;
        IsAlive = true;

        _healthbar = GetComponentInChildren<HealthbarBehavior>(true);
        if (_healthbar != null)
        {
            _healthbar.SetAutoShowOnChanged(true);
            _healthbar.SetAutoHideWhenMax(true);
            _healthbar.Apply(CurrentHealth, maxHealth);
        }
    }

    /// <summary>
    /// 피해를 받는다. 체력이 0이면 Player에 사망을 알린다.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
        {
            return;
        }

        // 기존 Player/WeaponAbility 계열의 피해 감소 효과
        var reducedDamage =
            amount *
            (100f - Mathf.Clamp(_stat.damageReductionPercent, 0f, 100f))
            / 100f;

        // Upgrade에서 추가된 받는 피해 배율
        var finalDamage =
            reducedDamage *
            Mathf.Max(0f, _stat.receivedDamageMultiplier);

        Debug.Log(
            $"원래 피해: {amount} / " +
            $"기존 피해감소 적용: {reducedDamage} / " +
            $"최종 피해: {finalDamage}"
        );

        CurrentHealth = Mathf.Max(0f, CurrentHealth - finalDamage);

        if (_healthbar != null)
        {
            _healthbar.Apply(CurrentHealth, maxHealth);
        }

        if (finalDamage > 0f && _owner != null)
        {
            _owner.NotifyDamaged();
        }

        if (CurrentHealth > 0f)
        {
            return;
        }

        IsAlive = false;

        if (_owner != null)
        {
            _owner.NotifyDied();
        }
    }

    /// <summary>
    /// 최대 체력 비율만큼 회복한다. 실제로 오른 체력을 돌려준다.
    /// </summary>
    public float RestoreHp(float percent)
    {
        if (!IsAlive || percent <= 0f)
        {
            return 0f;
        }

        var previous = CurrentHealth;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + maxHealth * (percent / 100f));
        if (_healthbar != null)
        {
            _healthbar.Apply(CurrentHealth, maxHealth);
        }

        return CurrentHealth - previous;
    }

    /// <summary>
    /// 최대 체력 배율을 기본값에 곱하고, 현재 체력 비율은 유지한다.
    /// </summary>
    public void RecalculateMaxHp(float multiplier)
    {
        var previous = Mathf.Max(1f, maxHealth);
        var ratio = Mathf.Clamp01(CurrentHealth / previous);
        maxHealth = Mathf.Max(1f, _maxHealth) * Mathf.Max(0f, multiplier);
        CurrentHealth = maxHealth * ratio;
        if (_healthbar != null)
        {
            _healthbar.Apply(CurrentHealth, maxHealth);
        }
    }

    #endregion
}
