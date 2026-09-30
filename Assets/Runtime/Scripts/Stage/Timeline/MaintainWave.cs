using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 살아 있는 수를 클립이 켜진 동안 유지한다.
/// </summary>
public class MaintainWave : MonsterWaveAsset
{
    [Header("Maintain")]
    [SerializeField, Min(1)] private int _enemiesCount = 1;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<MaintainWaveBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.Configure(Profile, _enemiesCount, CircularSpawn);
        return playable;
    }
}

/// <summary>
/// 죽은 수만큼 빈자리를 다시 낸다.
/// </summary>
public class MaintainWaveBehaviour : PlayableBehaviour
{
    private MonsterWaveProfile _profile;
    private int _keepAlive;
    private bool _circularSpawn;
    private int _alive;
    private bool _ready;

    /// <summary>
    /// 클립 에셋이 만든 재생 값.
    /// </summary>
    public void Configure(MonsterWaveProfile profile, int enemiesCount, bool circularSpawn)
    {
        _profile = profile;
        _keepAlive = Mathf.Max(1, enemiesCount);
        _circularSpawn = circularSpawn;
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (_ready)
        {
            return;
        }

        _ready = true;
        _alive = 0;
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (!_ready || _alive >= _keepAlive)
        {
            return;
        }

        var spawned = MonsterWavePlayback.Spawn(_profile, _keepAlive - _alive, _circularSpawn, HandleDied);
        _alive += spawned;
    }

    private void HandleDied(Enemy enemy)
    {
        _alive = Mathf.Max(0, _alive - 1);
    }
}
