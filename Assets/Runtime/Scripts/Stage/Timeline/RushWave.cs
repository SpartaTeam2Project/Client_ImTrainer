using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 막대 구간 동안 묶음을 여러 번 카메라 밖에서 돌진시킨다.
/// </summary>
public class RushWave : MonsterWaveAsset
{
    private const float MIN_CLUSTER_RADIUS = 0.05f;
    private const float MIN_RUSH_SPEED = 0.05f;

    [Header("Rush")]
    [SerializeField, Min(1)] private int _enemiesCount = 8;
    [SerializeField, Min(1)] private int _burstCount = 1;
    [SerializeField, Min(MIN_CLUSTER_RADIUS)] private float _clusterRadius = 0.8f;
    [SerializeField, Min(MIN_RUSH_SPEED)] private float _rushSpeed = 6f;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<RushWaveBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.Configure(Profile, _enemiesCount, _burstCount, _clusterRadius, _rushSpeed);
        return playable;
    }
}

/// <summary>
/// Rush 클립의 예정 시각에 돌진 묶음을 호출한다.
/// </summary>
public class RushWaveBehaviour : PlayableBehaviour
{
    private MonsterWaveProfile _profile;
    private int _enemiesCount;
    private int _burstCount;
    private float _clusterRadius;
    private float _rushSpeed;
    private float[] _times;
    private bool[] _fired;
    private bool _ready;

    /// <summary>
    /// 클립 에셋이 만든 재생 값.
    /// </summary>
    public void Configure(MonsterWaveProfile profile, int enemiesCount, int burstCount, float clusterRadius, float rushSpeed)
    {
        _profile = profile;
        _enemiesCount = Mathf.Max(1, enemiesCount);
        _burstCount = Mathf.Max(1, burstCount);
        _clusterRadius = clusterRadius;
        _rushSpeed = rushSpeed;
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (_ready)
        {
            return;
        }

        _ready = true;
        var duration = (float)playable.GetDuration();
        _times = new float[_burstCount];
        _fired = new bool[_burstCount];
        for (var i = 0; i < _burstCount; i++)
        {
            _times[i] = duration / _burstCount * i;
        }
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (!_ready || _times == null)
        {
            return;
        }

        var time = (float)playable.GetTime();
        for (var i = 0; i < _times.Length; i++)
        {
            if (_fired[i] || time < _times[i])
            {
                continue;
            }

            _fired[i] = true;
            MonsterWavePlayback.SpawnRush(_profile, _enemiesCount, _clusterRadius, _rushSpeed);
        }
    }
}
