using UnityEngine;
using UnityEngine.Timeline;

/// <summary>
/// 배경을 이어 붙이는 방식.
/// </summary>
public enum StageType
{
    Endless = 0,
    VerticalEndless = 1,
    HorizontalEndless = 2,
    Rect = 3
}

/// <summary>
/// 웨이브 몬스터의 공격. 근거리는 접촉이고, 원거리는 사거리에서 투사체를 던진다.
/// </summary>
public enum EnemyAttackKind
{
    [InspectorName("근거리")]
    Contact = 0,
    [InspectorName("원거리")]
    Projectile = 1
}

/// <summary>
/// 스테이지의 제한 시간과 적 배율, 스폰 타임라인.
/// </summary>
[CreateAssetMenu(fileName = "StageData", menuName = "Stage/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("진행")]
    [SerializeField] private bool _endsOnTime = true;
    [SerializeField] private float _clearTimeSeconds = 45f;

    [Header("맵")]
    [SerializeField] private StageType _stageType = StageType.Endless;
    [SerializeField] private StageFieldData _fieldData;
    [Tooltip("켜면 필드 데이터의 PropChances에 따라 배경 칸에 장식을 깐다.")]
    [SerializeField] private bool _spawnProp;

    [Header("스폰")]
    [SerializeField] private TimelineAsset _timeline;
    [SerializeField] private float _enemyHpMultiplier = 1f;
    [SerializeField] private float _enemyDamageMultiplier = 1f;

    public bool EndsOnTime => _endsOnTime;

    public float ClearTimeSeconds => _clearTimeSeconds;

    public float EnemyHpMultiplier => _enemyHpMultiplier;

    public float EnemyDamageMultiplier => _enemyDamageMultiplier;

    public StageType StageType => _stageType;

    public StageFieldData FieldData => _fieldData;

    public bool SpawnProp => _spawnProp;

    public TimelineAsset Timeline => _timeline;
}
