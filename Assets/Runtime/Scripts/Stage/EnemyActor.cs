using UnityEngine;

/// <summary>
/// 적 추적과 접촉 판정에 필요한 수치를 가진다.
/// </summary>
public class EnemyActor : MonoBehaviour
{
    private const float MOVE_SQR_EPSILON = 0.0001f;

    private EnemyView _view;
    private float _health;
    private float _contactDamage;
    private float _moveSpeed;
    private float _nextContactTime;
    private int _waveIndex;

    public float Health => _health;

    public float ContactDamage => _contactDamage;

    public int WaveIndex => _waveIndex;

    public bool IsAlive => _health > 0f;

    #region Unity Methods

    private void Awake()
    {
        _view = GetComponent<EnemyView>();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 스폰 직후 체력, 접촉 피해, 이동 속도를 넣는다.
    /// </summary>
    public void Initialize(int waveIndex, float maxHealth, float contactDamage, float moveSpeed)
    {
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
    /// 플레이어 위치로 이동한다.
    /// </summary>
    public void MoveToward(Vector2 target, float deltaTime)
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

    /// <summary>
    /// 접촉 간격이 지났으면 시각을 갱신하고 true.
    /// </summary>
    public bool TryStampContact(float time, float interval)
    {
        if (time < _nextContactTime)
        {
            return false;
        }

        _nextContactTime = time + interval;
        return true;
    }

    /// <summary>
    /// 체력을 깎는다.
    /// </summary>
    public void ApplyDamage(float amount)
    {
        _health -= amount;
    }

    #endregion
}
