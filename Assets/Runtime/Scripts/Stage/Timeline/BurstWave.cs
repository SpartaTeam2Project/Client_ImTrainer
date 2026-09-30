using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 클립 길이를 나눠 각 시점에 무리를 한 번에 낸다.
/// </summary>
public class BurstWave : MonsterWaveAsset
{
    [Header("Burst")]
    [SerializeField, Min(1)] private int _enemiesCount = 1;
    [SerializeField, Min(1)] private int _burstCount = 1;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<BurstWaveBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.Configure(Profile, _enemiesCount, _burstCount, CircularSpawn);
        return playable;
    }
}

/// <summary>
/// Burst 클립의 예정 시각에 스폰을 호출한다.
/// </summary>
public class BurstWaveBehaviour : PlayableBehaviour
{
    private MonsterWaveProfile _profile;
    private int _enemiesCount;
    private int _burstCount;
    private bool _circularSpawn;
    private float[] _times;
    private bool[] _fired;
    private bool _ready;

    /// <summary>
    /// 클립 에셋이 만든 재생 값.
    /// </summary>
    public void Configure(MonsterWaveProfile profile, int enemiesCount, int burstCount, bool circularSpawn)
    {
        _profile = profile;
        _enemiesCount = Mathf.Max(1, enemiesCount);
        _burstCount = Mathf.Max(1, burstCount);
        _circularSpawn = circularSpawn;
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
            _times[i] = _burstCount <= 0 ? 0f : duration / _burstCount * i;
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
            MonsterWavePlayback.Spawn(_profile, _enemiesCount, _circularSpawn, null);
        }
    }
}
