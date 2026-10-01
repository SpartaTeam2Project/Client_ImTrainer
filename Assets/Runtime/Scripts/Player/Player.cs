using UnityEngine;

/// <summary>
/// 스폰된 플레이어 한 명의 스탯, 경험치, 체력, 이동, 시선을 담당한다.
/// </summary>
public class Player : MonoBehaviour
{
    private const float MOVE_SQR_EPSILON = 0.0001f;
    private const int STARTING_LEVEL = 1;
    private const int MAX_LEVEL_UPS_PER_GAIN = 20;
    private const float MIN_REQUIRED_XP = 1f;

    [Header("Stats")]
    [SerializeField] private float _maxHealth = 30f;
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

    [Header("Experience")]
    [SerializeField] private float _baseRequiredXp = 5f;
    [SerializeField] private float _requiredGrowth = 1.25f;

    private PlayerManager _owner;
    private PlayerView _view;
    private HealthbarBehavior _healthbar;
    private Vector2 _lookDirection = Vector2.right;

    public float moveSpeed { get; private set; }

    public float moveSpeedMultiplier { get; private set; } = 1f;
    public float receivedDamageMultiplier { get; private set; } = 1f;

    public float damageMultiplier { get; private set; } = 1f;

    public float magnetRadius { get; private set; }

    public float xpMultiplier { get; private set; } = 1f;

    public float hpRegen { get; private set; }

    public float cooldownMultiplier { get; private set; } = 1f;

    public float projectileSpeedMultiplier { get; private set; } = 1f;

    public float projectileMultiplier { get; private set; } = 1f;

    public float sizeMultiplier { get; private set; } = 1f;

    public float durationMultiplier { get; private set; } = 1f;

    public float damageReductionPercent { get; private set; }

    public float invincibleSeconds { get; private set; }

    public Vector2 LookDirection => _lookDirection;

    public float CurrentHealth { get; private set; }

    public float maxHealth { get; private set; }

    public bool IsAlive { get; private set; }

    public int Level { get; private set; } = STARTING_LEVEL;

    public float CurrentXp { get; private set; }

    public float RequiredXp { get; private set; } = MIN_REQUIRED_XP;

    #region Unity Methods

    private void OnDestroy()
    {
        if (_owner == null)
        {
            return;
        }

        _owner.NotifyActorDestroyed(this);
        _owner = null;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 소유 매니저를 연결한다.
    /// </summary>
    public void Bind(PlayerManager owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// 프리팹에 적힌 스탯과 경험치 곡선으로 한 판을 시작한다.
    /// </summary>
    public void Initialize()
    {
        maxHealth = Mathf.Max(1f, _maxHealth);
        CurrentHealth = maxHealth;
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
        _lookDirection = Vector2.right;
        IsAlive = true;
        SetProgress(STARTING_LEVEL, 0f, CalculateRequiredXp(STARTING_LEVEL));

        _view = GetComponent<PlayerView>();
        if (_view != null)
        {
            _view.SetVisual(false, _lookDirection);
        }

        _healthbar = GetComponentInChildren<HealthbarBehavior>(true);
        if (_healthbar != null)
        {
            _healthbar.SetAutoShowOnChanged(true);
            _healthbar.SetAutoHideWhenMax(true);
            _healthbar.Apply(CurrentHealth, maxHealth);
        }
    }

    /// <summary>
    /// 선택한 플레이어블의 애니메이션 스프라이트로 모습을 바꾼다.
    /// </summary>
    public void ApplyPlayable(PlayableCharacterData data)
    {
        if (data == null)
        {
            return;
        }

        if (_view == null)
        {
            _view = GetComponent<PlayerView>();
        }

        if (_view == null)
        {
            return;
        }

        _view.ApplyPlayable(data);
    }

    /// <summary>
    /// 서 있는 채로 시선을 바꾼다.
    /// </summary>
    public void Face(Vector2 direction)
    {
        if (!IsAlive || direction.sqrMagnitude <= MOVE_SQR_EPSILON)
        {
            return;
        }

        _lookDirection = direction.normalized;
        if (_view != null)
        {
            _view.SetVisual(false, _lookDirection);
        }
    }

    /// <summary>
    /// 전달받은 이동량으로 움직인다. 입력 출처는 모른다.
    /// </summary>
    public void Move(Vector2 movement)
    {
        if (!IsAlive)
        {
            return;
        }

        var isMoving = movement.sqrMagnitude > MOVE_SQR_EPSILON;
        if (isMoving)
        {
            _lookDirection = movement.normalized;
        }

        var delta = movement * moveSpeed * moveSpeedMultiplier *Time.deltaTime;
        var next = (Vector2)transform.position + delta;
        if (Managers.Instance != null && Managers.Instance.TryGetManager<StageFieldManager>(out var fieldManager))
        {
            next = fieldManager.ValidatePosition(next);
        }

        transform.position = next;
        if (_view != null)
        {
            _view.SetVisual(isMoving, _lookDirection);
        }
    }

    //upgrade 관련 API입니다.(for client)
    #region UpgradeAPI
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
        receivedDamageMultiplier= Mathf.Max(0f, receivedDamageMultiplier + value);
    }
    /// <summary>
    /// Upgrade에 의한 주는 피해 배율을 합연산으로 누적한다.
    /// input:0.2f = +20%
    /// </summary>
    /// <param name="value"></param>
    public void AddDamageMultiplier(float value)
    {
        damageMultiplier= Mathf.Max(0f, damageMultiplier + value);
    }
    #endregion

    /// <summary>
    /// 피해를 받는다. 체력이 0이면 소유 매니저에 사망을 알린다.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
        {
            return;
        }
        var multipliedDamage = amount * Mathf.Max(0f, receivedDamageMultiplier);
        Debug.Log("받은피해/원래피해:"+multipliedDamage+"/"+amount);


        CurrentHealth = Mathf.Max(0f, CurrentHealth - multipliedDamage);
        if (_healthbar != null)
        {
            _healthbar.Apply(CurrentHealth, maxHealth);
        }

        if (CurrentHealth > 0f)
        {
            return;
        }

        IsAlive = false;
        if (_view != null)
        {
            _view.SetVisual(false, _lookDirection);
            _view.ShowDeathLight();
        }

        if (_owner != null)
        {
            _owner.NotifyDied(this);
        }
    }

    /// <summary>
    /// 경험치를 더한다. 필요량을 채우면 레벨을 올리고, 오른 횟수를 돌려준다.
    /// </summary>
    public int AddExperience(float amount)
    {
        if (amount <= 0f)
        {
            return 0;
        }

        var xp = CurrentXp + amount * Mathf.Max(0f, xpMultiplier);
        Debug.Log("원래 경험치/증가후 경험치:" +amount+"/"+(xp-CurrentXp));
        var level = Level;
        var required = Mathf.Max(MIN_REQUIRED_XP, RequiredXp);
        var gained = 0;
        while (xp >= required && gained < MAX_LEVEL_UPS_PER_GAIN)
        {
            xp -= required;
            level++;
            required = CalculateRequiredXp(level);
            gained++;
        }

        SetProgress(level, xp, required);
        return gained;
    }

    /// <summary>
    /// 최대 체력 비율만큼 회복한다.
    /// </summary>
    public void RestoreHp(float percent)
    {
        if (!IsAlive || percent <= 0f)
        {
            return;
        }

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + maxHealth * (percent / 100f));
        if (_healthbar != null)
        {
            _healthbar.Apply(CurrentHealth, maxHealth);
        }
    }


    /// <summary>
    /// 이동 속도 배율을 기본값에 곱한다.
    /// </summary>
    public void RecalculateMoveSpeed(float multiplier)
    {
        moveSpeed = Mathf.Max(0f, _moveSpeed) * Mathf.Max(0f, multiplier);
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

    #region Private Methods

    private float CalculateRequiredXp(int level)
    {
        var step = Mathf.Max(STARTING_LEVEL, level);
        var growth = Mathf.Max(1f, _requiredGrowth);
        var required = Mathf.Max(MIN_REQUIRED_XP, _baseRequiredXp) * Mathf.Pow(growth, step - 1);
        return Mathf.Max(MIN_REQUIRED_XP, required);
    }

    private void SetProgress(int level, float currentXp, float requiredXp)
    {
        Level = Mathf.Max(STARTING_LEVEL, level);
        CurrentXp = Mathf.Max(0f, currentXp);
        RequiredXp = Mathf.Max(MIN_REQUIRED_XP, requiredXp);
    }

    #endregion
}
