using UnityEngine;

/// <summary>
/// 스폰된 플레이어 한 명의 체력, 이동, 시선을 담당한다.
/// </summary>
public class Player : MonoBehaviour
{
    private const float MOVE_SQR_EPSILON = 0.0001f;

    private PlayerManager _owner;
    private PlayerView _view;
    private Vector2 _lookDirection = Vector2.right;

    public float Speed { get; private set; }

    public Vector2 LookDirection => _lookDirection;

    public float CurrentHealth { get; private set; }

    public float MaxHealth { get; private set; }

    public bool IsAlive { get; private set; }

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
        var maxHealth = stats != null ? stats.MaxHealth : 1f;
        var speed = stats != null ? stats.MoveSpeed : 0f;
        MaxHealth = Mathf.Max(1f, maxHealth);
        CurrentHealth = MaxHealth;
        Speed = Mathf.Max(0f, speed);
        _lookDirection = Vector2.right;
        IsAlive = true;

        _view = GetComponent<PlayerView>();
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

        var delta = movement * Speed * Time.deltaTime;
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
        if (CurrentHealth > 0f)
        {
            return;
        }

        IsAlive = false;
        if (_view != null)
        {
            _view.SetVisual(false, _lookDirection);
        }

        if (_owner != null)
        {
            _owner.NotifyDied(this);
        }
    }

    #endregion
}
