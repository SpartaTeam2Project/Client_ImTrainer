using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 몬스터 웨이브 클립의 공통 값. 트랙이 재생 직전에 프로필을 넣는다.
/// </summary>
public abstract class MonsterWaveAsset : PlayableAsset
{
    [SerializeField] private MonsterWaveOverride _waveOverride = new MonsterWaveOverride();
    [SerializeField] private bool _circularSpawn;

    private MonsterWaveProfile _profile;

    public MonsterWaveOverride WaveOverride => _waveOverride;

    public bool CircularSpawn => _circularSpawn;

    protected MonsterWaveProfile Profile => _profile;

    /// <summary>
    /// 트랙 수치와 클립 덮어쓰기를 합친 프로필을 재생에 쓴다.
    /// </summary>
    public void Bind(MonsterWaveProfile profile)
    {
        _profile = profile;
    }
}
