using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 보스전을 한 번 시작하는 클립. 고정 보스와 후보, 등장 수를 가진다.
/// </summary>
public class BossEncounter : PlayableAsset
{
    private const int CANDIDATE_COUNT = 5;
    private const int MIN_SPAWN_COUNT = 1;
    private const int MAX_SPAWN_COUNT = 6;

    [SerializeField] private BossSpawnEntry _fixedBoss = new BossSpawnEntry();
    [SerializeField] private BossSpawnEntry[] _candidates = CreateCandidates();
    [SerializeField, Range(MIN_SPAWN_COUNT, MAX_SPAWN_COUNT)] private int _spawnCount = MIN_SPAWN_COUNT;

    [Header("Fence")]
    [Tooltip("울타리 바깥 가로. 플레이어와 보스가 벗어나지 못하는 너비다.")]
    [SerializeField, Min(4f)] private float _fenceWidth = RectBossFence.WIDTH;
    [Tooltip("울타리 바깥 세로. 플레이어와 보스가 벗어나지 못하는 높이다.")]
    [SerializeField, Min(4f)] private float _fenceHeight = RectBossFence.HEIGHT;
    [SerializeField] private Sprite _fenceLeft;
    [Tooltip("왼쪽 기둥 배율.")]
    [SerializeField, Min(0.01f)] private float _fenceLeftScale = RectBossFence.DEFAULT_SCALE;
    [SerializeField] private Sprite _fenceRight;
    [Tooltip("오른쪽 기둥 배율.")]
    [SerializeField, Min(0.01f)] private float _fenceRightScale = RectBossFence.DEFAULT_SCALE;
    [SerializeField] private Sprite _fenceEdge;
    [Tooltip("아래 판 배율.")]
    [SerializeField, Min(0.01f)] private float _fenceBottomScale = RectBossFence.DEFAULT_SCALE;
    [Tooltip("위 판 배율.")]
    [SerializeField, Min(0.01f)] private float _fenceTopScale = RectBossFence.DEFAULT_SCALE;
    [Tooltip("나무 사이 빈 간격. 0이면 서로 맞닿는다.")]
    [SerializeField, Min(0f)] private float _fenceGap;

    [Header("Entrance")]
    [SerializeField] private Sprite[] _trainerDownFrames;
    [SerializeField] private Sprite _trainerSideFrame;
    [SerializeField] private Sprite _exclamation;

    [Header("Versus")]
    [Tooltip("보스마다 같이 쓰는 VS 화면. 위치와 크기는 이 프리팹에서 고친다.")]
    [SerializeField] private BossTrainerVersusView _versusView;
    [SerializeField] private Sprite _versusBar;
    [SerializeField] private Sprite _playerBack;
    [SerializeField] private Sprite[] _rivalFrames;
    [SerializeField] private Sprite[] _slashFrames;
    [SerializeField] private Sprite _versusMark;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<BossEncounterBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.Configure(_fixedBoss, _candidates, _spawnCount, CreateFenceSpec(), CreateEntranceCast());
        return playable;
    }

    private BossFenceSpec CreateFenceSpec()
    {
        return new BossFenceSpec
        {
            Width = _fenceWidth,
            Height = _fenceHeight,
            Left = _fenceLeft,
            Right = _fenceRight,
            Edge = _fenceEdge,
            LeftScale = _fenceLeftScale,
            RightScale = _fenceRightScale,
            BottomScale = _fenceBottomScale,
            TopScale = _fenceTopScale,
            Gap = _fenceGap
        };
    }

    private BossTrainerEntranceCast CreateEntranceCast()
    {
        return new BossTrainerEntranceCast
        {
            DownFrames = _trainerDownFrames,
            SideFrame = _trainerSideFrame,
            Exclamation = _exclamation,
            Versus = CreateVersusCast()
        };
    }

    private BossTrainerVersusCast CreateVersusCast()
    {
        return new BossTrainerVersusCast
        {
            View = _versusView,
            RedBar = _versusBar,
            PlayerBack = _playerBack,
            RivalFrames = _rivalFrames,
            SlashFrames = _slashFrames,
            Versus = _versusMark
        };
    }

    private static BossSpawnEntry[] CreateCandidates()
    {
        var candidates = new BossSpawnEntry[CANDIDATE_COUNT];
        for (var i = 0; i < candidates.Length; i++)
        {
            candidates[i] = new BossSpawnEntry();
        }

        return candidates;
    }
}

/// <summary>
/// 재생이 시작되면 보스전을 한 번만 연다.
/// </summary>
public class BossEncounterBehaviour : PlayableBehaviour
{
    private BossSpawnEntry _fixedBoss;
    private BossSpawnEntry[] _candidates;
    private int _spawnCount;
    private BossFenceSpec _fence;
    private BossTrainerEntranceCast _entrance;
    private bool _started;

    /// <summary>
    /// 클립 에셋이 만든 보스 구성과 울타리 크기, 배율.
    /// </summary>
    public void Configure(
        BossSpawnEntry fixedBoss,
        BossSpawnEntry[] candidates,
        int spawnCount,
        BossFenceSpec fence,
        BossTrainerEntranceCast entrance)
    {
        _fixedBoss = fixedBoss;
        _candidates = candidates;
        _spawnCount = spawnCount;
        _fence = fence;
        _entrance = entrance;
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (_started || !Application.isPlaying || info.evaluationType != FrameData.EvaluationType.Playback)
        {
            return;
        }

        _started = true;
        BossArenaPlayback.Begin(_fixedBoss, _candidates, _spawnCount, _fence, _entrance);
    }
}
