using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// 몬스터 한 종의 트랙. 클립은 이 수치로 스폰한다.
/// </summary>
[TrackClipType(typeof(BurstWave))]
[TrackClipType(typeof(MaintainWave))]
[TrackClipType(typeof(ContinuousWave))]
[TrackClipType(typeof(EncircleWave))]
[TrackClipType(typeof(RushWave))]
[TrackColor(0.35f, 0.7f, 0.35f)]
public class MonsterWaveTrack : TrackAsset
{
    [SerializeField] private MonsterVisualData _monster;
    [SerializeField] private int _id;
    [SerializeField] private float _maxHealth = 10f;
    [SerializeField] private float _contactDamage = 2f;
    [SerializeField] private float _moveSpeed = 1.5f;
    [SerializeField] private EnemyAttackKind _attackKind = EnemyAttackKind.Contact;
    [SerializeField, Min(0.5f)] private float _attackRange = 4f;
    [SerializeField, Min(0.05f)] private float _attackInterval = 1.4f;
    [SerializeField, Min(0.01f)] private float _projectileSpeed = 6f;
    [SerializeField, Min(0f)] private float _hitRadius = 0.75f;
    [SerializeField, Min(0.01f)] private float _attackDistance = 0.45f;
    [SerializeField] private bool _overrideScale;
    [SerializeField, Min(0.01f)] private float _scale = MonsterVisualData.DEFAULT_SCALE;

    /// <summary>
    /// 이 트랙이 스폰하는 몬스터 그림.
    /// </summary>
    public MonsterVisualData Monster => _monster;

    /// <summary>
    /// 클립 에셋에 트랙 프로필을 넣은 뒤 재생 그래프를 만든다.
    /// </summary>
    protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
    {
        if (clip.asset is MonsterWaveAsset wave)
        {
            wave.Bind(CreateProfile(wave));
        }

        return base.CreatePlayable(graph, gameObject, clip);
    }

    private MonsterWaveProfile CreateProfile(MonsterWaveAsset wave)
    {
        var profile = new MonsterWaveProfile
        {
            Monster = _monster,
            Id = _id,
            MaxHealth = _maxHealth,
            ContactDamage = _contactDamage,
            MoveSpeed = _moveSpeed,
            AttackKind = _attackKind,
            AttackRange = _attackRange,
            AttackInterval = _attackInterval,
            ProjectileSpeed = _projectileSpeed,
            HitRadius = _hitRadius,
            AttackDistance = _attackDistance,
            OverrideScale = _overrideScale,
            Scale = _scale > 0f ? _scale : MonsterVisualData.DEFAULT_SCALE,
            CircularSpawn = wave != null && wave.CircularSpawn
        };

        if (wave != null && wave.WaveOverride != null)
        {
            wave.WaveOverride.Apply(profile);
        }

        return profile;
    }
}
