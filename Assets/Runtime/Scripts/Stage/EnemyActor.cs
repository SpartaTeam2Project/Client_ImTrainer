using UnityEngine;

/// <summary>
/// 자리표시 적. 추적과 접촉 판정에 필요한 수치만 가진다.
/// </summary>
public class EnemyActor : MonoBehaviour
{
    private float _health;
    private float _contactDamage;
    private float _moveSpeed;
    private float _nextContactTime;
    private int _waveIndex;

    public float Health => _health;

    public float ContactDamage => _contactDamage;

    public int WaveIndex => _waveIndex;

    public bool IsAlive => _health > 0f;

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
    }

    /// <summary>
    /// 플레이어 위치로 이동한다.
    /// </summary>
    public void MoveToward(Vector2 target, float deltaTime)
    {
        var next = Vector2.MoveTowards(transform.position, target, _moveSpeed * deltaTime);
        transform.position = next;
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
}
