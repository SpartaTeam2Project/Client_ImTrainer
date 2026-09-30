using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 클립이 시작될 때 플레이어 주변 원 위에 한 번 둘러싼다.
/// </summary>
public class EncircleWave : MonsterWaveAsset
{
    private const float MIN_RADIUS = 0.5f;
    private const float MIN_APPROACH_SPEED = 0.05f;

    [Header("Encircle")]
    [SerializeField, Min(1)] private int _enemiesCount = 8;
    [SerializeField, Min(MIN_RADIUS)] private float _radius = 3f;
    [SerializeField, Min(MIN_APPROACH_SPEED)] private float _approachSpeed = 0.6f;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<EncircleWaveBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.Configure(Profile, _enemiesCount, _radius, _approachSpeed);
        return playable;
    }
}

/// <summary>
/// Encircle 클립 시작 프레임에 원형 배치를 한 번 호출한다.
/// </summary>
public class EncircleWaveBehaviour : PlayableBehaviour
{
    private MonsterWaveProfile _profile;
    private int _enemiesCount;
    private float _radius;
    private float _approachSpeed;
    private bool _fired;

    /// <summary>
    /// 클립 에셋이 만든 재생 값.
    /// </summary>
    public void Configure(MonsterWaveProfile profile, int enemiesCount, float radius, float approachSpeed)
    {
        _profile = profile;
        _enemiesCount = Mathf.Max(1, enemiesCount);
        _radius = radius;
        _approachSpeed = approachSpeed;
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (_fired)
        {
            return;
        }

        _fired = true;
        MonsterWavePlayback.SpawnRing(_profile, _enemiesCount, _radius, _approachSpeed);
    }
}
