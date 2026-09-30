using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 클립이 켜진 동안 초당 마리 수만큼 계속 낸다.
/// </summary>
public class ContinuousWave : MonsterWaveAsset
{
    private const float MIN_SPAWN_PER_SECOND = 0.01f;

    [Header("Continuous")]
    [SerializeField, Min(MIN_SPAWN_PER_SECOND)] private float _continuousSpawnPerSecond = 1f;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<ContinuousWaveBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.Configure(Profile, _continuousSpawnPerSecond, CircularSpawn);
        return playable;
    }
}

/// <summary>
/// 놓친 간격만큼 스폰을 따라잡는다.
/// </summary>
public class ContinuousWaveBehaviour : PlayableBehaviour
{
    private const float MIN_SPAWN_PER_SECOND = 0.01f;

    private MonsterWaveProfile _profile;
    private float _spawnRate;
    private bool _circularSpawn;
    private float _lastSpawnTime;
    private bool _ready;

    /// <summary>
    /// 클립 에셋이 만든 재생 값.
    /// </summary>
    public void Configure(MonsterWaveProfile profile, float continuousSpawnPerSecond, bool circularSpawn)
    {
        _profile = profile;
        var perSecond = Mathf.Max(MIN_SPAWN_PER_SECOND, continuousSpawnPerSecond);
        _spawnRate = 1f / perSecond;
        _circularSpawn = circularSpawn;
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (_ready)
        {
            return;
        }

        _ready = true;
        _lastSpawnTime = -_spawnRate;
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (!_ready || _spawnRate <= 0f)
        {
            return;
        }

        var time = (float)playable.GetTime();
        var amount = 0;
        while (_lastSpawnTime + _spawnRate < time)
        {
            _lastSpawnTime += _spawnRate;
            amount++;
        }

        if (amount > 0 && _spawnRate > 0f)
        {
            MonsterWavePlayback.Spawn(_profile, amount, _circularSpawn, null);
        }
    }
}
