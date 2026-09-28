using UnityEngine;

/// <summary>
/// 스폰된 플레이어 한 명의 체력, 이동, 시선, 레벨을 담당한다.
/// </summary>
public class Player : MonoBehaviour
{
    private const float MOVE_SQR_EPSILON = 0.0001f;
    private const int STARTING_LEVEL = 1;
    private const float STARTING_REQUIRED_XP = 5f;

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

    public float RequiredXp { get; private set; } = STARTING_REQUIRED_XP;

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
    /// 스탯으로 체력과 속도를 맞추고 살아 있는 상태로 둔다.
    /// </summary>
    public void Initialize(CharacterStats stats)
    {
        maxHealth = Mathf.Max(1f, stats != null ? stats.MaxHealth : 1f);
        CurrentHealth = maxHealth;
        moveSpeed = Mathf.Max(0f, stats != null ? stats.MoveSpeed : 0f);
        damageMultiplier = stats != null ? stats.DamageMultiplier : 1f;
        magnetRadius = stats != null ? stats.MagnetRadius : 0f;
        xpMultiplier = stats != null ? stats.XpMultiplier : 1f;
        hpRegen = stats != null ? stats.HpRegen : 0f;
        cooldownMultiplier = stats != null ? stats.CooldownMultiplier : 1f;
        projectileSpeedMultiplier = stats != null ? stats.ProjectileSpeedMultiplier : 1f;
        projectileMultiplier = stats != null ? stats.ProjectileMultiplier : 1f;
        invincibleSeconds = stats != null ? stats.InvincibleSeconds : 0f;
        _lookDirection = Vector2.right;
        IsAlive = true;
        SetProgress(STARTING_LEVEL, 0f, STARTING_REQUIRED_XP);

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
    /// 레벨과 경험치 게이지를 맞춘다.
    /// </summary>
    public void SetProgress(int level, float currentXp, float requiredXp)
    {
        Level = Mathf.Max(STARTING_LEVEL, level);
        CurrentXp = Mathf.Max(0f, currentXp);
        RequiredXp = Mathf.Max(1f, requiredXp);
    }

    #endregion
}
