using System;
using UnityEngine;

/// <summary>
/// 시간 구간에 적을 내는 방식. 몬스터 담당이 구간 데이터로 고른다.
/// </summary>
public enum WaveKind
{
    Burst = 0,
    Maintain = 1,
    Continuous = 2
}

/// <summary>
/// 한 시간 구간의 스폰 수와 적 수치.
/// </summary>
[Serializable]
public class WaveSpawn
{
    [SerializeField] private WaveKind _kind = WaveKind.Continuous;
    [SerializeField] private float _startSeconds;
    [SerializeField] private float _endSeconds = 60f;
    [SerializeField] private int _count = 1;
    [SerializeField] private float _spawnInterval = 1.5f;
    [SerializeField] private float _maxHealth = 10f;
    [SerializeField] private float _contactDamage = 2f;
    [SerializeField] private float _moveSpeed = 1.5f;

    public WaveKind Kind => _kind;

    public float StartSeconds => _startSeconds;

    public float EndSeconds => _endSeconds;

    public int Count => _count;

    public float SpawnInterval => _spawnInterval;

    public float MaxHealth => _maxHealth;

    public float ContactDamage => _contactDamage;

    public float MoveSpeed => _moveSpeed;
}

/// <summary>
/// 스테이지의 제한 시간과 적 배율, 웨이브 목록.
/// </summary>
[CreateAssetMenu(fileName = "StageData", menuName = "Stage/Stage Data")]
public class StageData : ScriptableObject
{
    [SerializeField] private bool _endsOnTime = true;
    [SerializeField] private float _clearTimeSeconds = 45f;
    [SerializeField] private float _enemyHpMultiplier = 1f;
    [SerializeField] private float _enemyDamageMultiplier = 1f;
    [SerializeField] private WaveSpawn[] _waves = Array.Empty<WaveSpawn>();

    public bool EndsOnTime => _endsOnTime;

    public float ClearTimeSeconds => _clearTimeSeconds;

    public float EnemyHpMultiplier => _enemyHpMultiplier;

    public float EnemyDamageMultiplier => _enemyDamageMultiplier;

    public WaveSpawn[] Waves => _waves;
}
