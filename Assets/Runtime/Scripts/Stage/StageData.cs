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
/// 상점에 나올 포켓몬 세대. 인스펙터의 Everything이 전체 세대다.
/// </summary>
[System.Flags]
public enum ShopGeneration
{
    [InspectorName("1세대")]
    Gen1 = 1 << 0,
    [InspectorName("2세대")]
    Gen2 = 1 << 1,
    [InspectorName("3세대")]
    Gen3 = 1 << 2,
    [InspectorName("4세대")]
    Gen4 = 1 << 3,
    [InspectorName("5세대")]
    Gen5 = 1 << 4,
    [InspectorName("6세대")]
    Gen6 = 1 << 5,
    [InspectorName("7세대")]
    Gen7 = 1 << 6,
    [InspectorName("8세대")]
    Gen8 = 1 << 7,
    [InspectorName("9세대")]
    Gen9 = 1 << 8
}

public static class ShopGenerationMask
{
    /// <summary>
    /// 인스펙터의 Everything과 같은 값. 모든 비트가 켜져 있다.
    /// </summary>
    public const ShopGeneration ALL = (ShopGeneration)~0;

    public const int MIN_GENERATION = 1;
    public const int MAX_GENERATION = 9;

    /// <summary>
    /// 세대가 마스크에 들어 있으면 true. 1~9세대 밖이면 false.
    /// </summary>
    public static bool Contains(ShopGeneration mask, int generation)
    {
        if (generation < MIN_GENERATION || generation > MAX_GENERATION)
        {
            return false;
        }

        return ((int)mask & (1 << (generation - MIN_GENERATION))) != 0;
    }
}

/// <summary>
/// 스테이지의 제한 시간과 적 배율, 스폰 타임라인, 상점 세대.
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

    [Header("상점")]
    [Tooltip("상점에 나올 포켓몬 세대. Everything이면 모든 세대가 나온다.")]
    [SerializeField] private ShopGeneration _shopGenerations = ShopGenerationMask.ALL;

    public bool EndsOnTime => _endsOnTime;

    public float ClearTimeSeconds => _clearTimeSeconds;

    public float EnemyHpMultiplier => _enemyHpMultiplier;

    public float EnemyDamageMultiplier => _enemyDamageMultiplier;

    public StageType StageType => _stageType;

    public StageFieldData FieldData => _fieldData;

    public bool SpawnProp => _spawnProp;

    public TimelineAsset Timeline => _timeline;

    public ShopGeneration ShopGenerations => _shopGenerations;
}
