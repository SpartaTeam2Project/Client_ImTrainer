using UnityEngine;

/// <summary>
/// 스폰된 적 한 마리의 추적, 접촉 피해, 체력을 담당한다.
/// </summary>
public class Enemy : MonoBehaviour
{
    private const float MOVE_SQR_EPSILON = 0.0001f;
    private const float CONTACT_RADIUS = 0.75f;
    private const float CONTACT_INTERVAL = 0.6f;

    private EnemyManager _owner;
    private EnemyView _view;
    private int _playerId;
    private float _health;
    private float _contactDamage;
    private float _moveSpeed;
    private float _nextContactTime;
    private int _waveIndex;

    public float Health => _health;

    public int WaveIndex => _waveIndex;

    public bool IsAlive => _health > 0f;

    #region Unity Methods

    private void Awake()
    {
        _view = GetComponent<EnemyView>();
    }

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
    public void Bind(EnemyManager owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// 스폰 직후 대상 플레이어, 체력, 접촉 피해, 이동 속도를 넣는다.
    /// </summary>
    public void Initialize(int playerId, int waveIndex, float maxHealth, float contactDamage, float moveSpeed)
    {
        _playerId = playerId;
        _waveIndex = waveIndex;
        _health = maxHealth;
        _contactDamage = contactDamage;
        _moveSpeed = moveSpeed;
        _nextContactTime = 0f;

        if (_view == null)
        {
            _view = GetComponent<EnemyView>();
        }

        if (_view != null)
        {
            _view.SetVisual(false, Vector2.down);
        }
    }

    /// <summary>
    /// 플레이어 위치로 이동하고, 닿아 있으면 피해를 준다. 플레이어가 죽으면 false.
    /// </summary>
    public bool Tick(Vector2 playerPosition)
    {
        if (!IsAlive)
        {
            return true;
        }

        MoveToward(playerPosition, Time.deltaTime);
        if (Vector2.Distance(transform.position, playerPosition) > CONTACT_RADIUS)
        {
            return true;
        }

        if (Time.time < _nextContactTime)
        {
            return true;
        }

        _nextContactTime = Time.time + CONTACT_INTERVAL;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return true;
        }

        playerManager.TakeDamage(_playerId, _contactDamage);
        return playerManager.IsAlive;
    }

    /// <summary>
    /// 체력을 깎는다. 체력이 0이면 소유 매니저에 사망을 알린다.
    /// </summary>
    public void ApplyDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
        {
            return;
        }

        _health = Mathf.Max(0f, _health - amount);
        if (_health > 0f)
        {
            return;
        }

        if (_owner != null)
        {
            _owner.NotifyDied(this);
        }
    }

    #endregion

    #region Private Methods

    private void MoveToward(Vector2 target, float deltaTime)
    {
        var current = (Vector2)transform.position;
        var next = Vector2.MoveTowards(current, target, _moveSpeed * deltaTime);
        transform.position = next;

        var movement = next - current;
        if (_view != null)
        {
            _view.SetVisual(movement.sqrMagnitude > MOVE_SQR_EPSILON, movement);
        }
    }

    #endregion
}
