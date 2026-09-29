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
    [SerializeField] private float _invincibleSeconds;

    [Header("Experience")]
    [SerializeField] private float _baseRequiredXp = 5f;
    [SerializeField] private float _requiredGrowth = 1.25f;

    private PlayerManager _owner;
    private PlayerView _view;
    private HealthbarBehavior _healthbar;
    private Vector2 _lookDirection = Vector2.right;

    public float moveSpeed { get; private set; }

    public float damageMultiplier { get; private set; } = 1f;

    public float magnetRadius { get; private set; }

    public float xpMultiplier { get; private set; } = 1f;

    public float hpRegen { get; private set; }

    public float cooldownMultiplier { get; private set; } = 1f;

    public float projectileSpeedMultiplier { get; private set; } = 1f;

    public float projectileMultiplier { get; private set; } = 1f;

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
        damageMultiplier = Mathf.Max(0f, _damageMultiplier);
        magnetRadius = Mathf.Max(0f, _magnetRadius);
        xpMultiplier = Mathf.Max(0f, _xpMultiplier);
        hpRegen = _hpRegen;
        cooldownMultiplier = Mathf.Max(0f, _cooldownMultiplier);
        projectileSpeedMultiplier = Mathf.Max(0f, _projectileSpeedMultiplier);
        projectileMultiplier = Mathf.Max(0f, _projectileMultiplier);
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

        var delta = movement * moveSpeed * Time.deltaTime;
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

    /// <summary>
    /// 피해를 받는다. 체력이 0이면 소유 매니저에 사망을 알린다.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
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

        var xp = CurrentXp + amount;
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
