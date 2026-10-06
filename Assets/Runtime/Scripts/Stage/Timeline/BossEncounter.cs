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

    [SerializeField] private BossTrainerData _boss;
    [SerializeField] private BossFightStats[] _partyStats = System.Array.Empty<BossFightStats>();
    [SerializeField] private BossSpawnEntry _fixedBoss = new BossSpawnEntry();
    [SerializeField] private BossSpawnEntry[] _candidates = CreateCandidates();
    [SerializeField, Range(MIN_SPAWN_COUNT, MAX_SPAWN_COUNT)] private int _spawnCount = MIN_SPAWN_COUNT;
    [SerializeField] private BossFenceSettings _fence = new BossFenceSettings();
    [SerializeField] private BossVersusSettings _versus = new BossVersusSettings();

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<BossEncounterBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        ResolveBosses(out var fixedBoss, out var candidates);
        behaviour.Configure(fixedBoss, candidates, _spawnCount, CreateFenceSpec(), CreateEntranceCast());
        return playable;
    }

    private BossFenceSpec CreateFenceSpec()
    {
        var fence = _fence ?? new BossFenceSettings();
        return new BossFenceSpec
        {
            Width = fence.Width,
            Height = fence.Height,
            Left = fence.Left,
            Right = fence.Right,
            Edge = fence.Edge,
            LeftScale = fence.LeftScale,
            RightScale = fence.RightScale,
            BottomScale = fence.BottomScale,
            TopScale = fence.TopScale,
            Gap = fence.Gap
        };
    }

    private BossTrainerEntranceCast CreateEntranceCast()
    {
        return new BossTrainerEntranceCast
        {
            DownFrames = ResolveDownFrames(),
            SideFrame = ResolveSideFrame(),
            StandingFrame = ResolveStandingFrame(),
            Exclamation = _boss != null ? _boss.Exclamation : null,
            Versus = CreateVersusCast()
        };
    }

    private BossTrainerVersusCast CreateVersusCast()
    {
        var versus = _versus ?? new BossVersusSettings();
        return new BossTrainerVersusCast
        {
            View = versus.View,
            RedBar = versus.Bar,
            PlayerBack = ResolvePlayerBack(),
            RivalFrames = ResolveRivalFrames(),
            SlashFrames = versus.SlashFrames,
            Versus = versus.Mark
        };
    }

    /// <summary>
    /// 선택한 플레이어블의 등 사진. 없거나 비어 있으면 클립에 넣은 등 사진을 쓴다.
    /// </summary>
    private Sprite ResolvePlayerBack()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AccountManager>(out var account))
        {
            return (_versus ?? new BossVersusSettings()).PlayerBack;
        }

        var playable = account.ResolveSelectedPlayable();
        var versus = _versus ?? new BossVersusSettings();
        if (playable == null || playable.VersusBack == null)
        {
            return versus.PlayerBack;
        }

        return playable.VersusBack;
    }

    public int PartySlotCount
    {
        get
        {
            if (_boss == null || _boss.Party == null)
            {
                return 0;
            }

            return _boss.Party.Length;
        }
    }

    /// <summary>
    /// 파티 마리수와 스탯 칸 수를 맞춘다. 이미 맞으면 false.
    /// 처음 생길 때 첫 칸은 지금 고정 보스의 수치를 가져온다.
    /// </summary>
    public bool SyncPartyStats()
    {
        var count = PartySlotCount;
        if (count <= 0)
        {
            return false;
        }

        var previousLength = _partyStats == null ? 0 : _partyStats.Length;
        var changed = previousLength != count;
        if (!changed)
        {
            for (var i = 0; i < _partyStats.Length; i++)
            {
                if (_partyStats[i] == null)
                {
                    changed = true;
                    break;
                }
            }
        }

        if (!changed)
        {
            return false;
        }

        var next = new BossFightStats[count];
        for (var i = 0; i < count; i++)
        {
            if (i < previousLength && _partyStats[i] != null)
            {
                next[i] = _partyStats[i];
                continue;
            }

            next[i] = CreateDefaultStats(i, previousLength);
        }

        _partyStats = next;
        return true;
    }

    private void OnValidate()
    {
        SyncPartyStats();
    }

    /// <summary>
    /// 보스 SO 파티가 있으면 몬스터마다 같은 순번의 스탯을 쓴다.
    /// 파티가 비어 있으면 클립의 고정 보스와 후보를 그대로 쓴다.
    /// </summary>
    private void ResolveBosses(out BossSpawnEntry fixedBoss, out BossSpawnEntry[] candidates)
    {
        fixedBoss = _fixedBoss;
        candidates = _candidates;
        if (_boss == null || !_boss.HasMonster())
        {
            return;
        }

        var party = _boss.Party;
        var spawned = new System.Collections.Generic.List<BossSpawnEntry>();
        for (var i = 0; i < party.Length; i++)
        {
            var member = party[i];
            if (member == null || member.Monster == null)
            {
                continue;
            }

            var entry = new BossSpawnEntry().WithParty(member);
            entry.ApplyStats(StatsFor(i));
            spawned.Add(entry);
        }

        if (spawned.Count == 0)
        {
            return;
        }

        fixedBoss = spawned[0];
        candidates = new BossSpawnEntry[spawned.Count - 1];
        for (var i = 1; i < spawned.Count; i++)
        {
            candidates[i - 1] = spawned[i];
        }
    }

    private BossFightStats StatsFor(int index)
    {
        if (_partyStats != null && index >= 0 && index < _partyStats.Length && _partyStats[index] != null)
        {
            return _partyStats[index];
        }

        return CreateDefaultStats(index, 0);
    }

    private BossFightStats CreateDefaultStats(int index, int previousLength)
    {
        if (previousLength == 0 && index == 0 && _fixedBoss != null)
        {
            return _fixedBoss.CaptureStats();
        }

        if (previousLength == 0 && index > 0 && _candidates != null && index - 1 < _candidates.Length && _candidates[index - 1] != null)
        {
            return _candidates[index - 1].CaptureStats();
        }

        return new BossFightStats();
    }

    private Sprite[] ResolveDownFrames()
    {
        if (HasFrames(_boss != null ? _boss.WalkDown : null))
        {
            return _boss.WalkDown;
        }

        if (HasFrames(_boss != null ? _boss.IdleDown : null))
        {
            return _boss.IdleDown;
        }

        return System.Array.Empty<Sprite>();
    }

    private Sprite ResolveSideFrame()
    {
        var idleLeft = _boss != null ? _boss.IdleLeft : null;
        if (HasFrames(idleLeft))
        {
            return idleLeft[0];
        }

        return null;
    }

    private Sprite ResolveStandingFrame()
    {
        var idleDown = _boss != null ? _boss.IdleDown : null;
        if (HasFrames(idleDown))
        {
            return idleDown[0];
        }

        return null;
    }

    private Sprite[] ResolveRivalFrames()
    {
        if (HasFrames(_boss != null ? _boss.VersusFrames : null))
        {
            return _boss.VersusFrames;
        }

        return System.Array.Empty<Sprite>();
    }

    private static bool HasFrames(Sprite[] frames)
    {
        return frames != null && frames.Length > 0 && frames[0] != null;
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

/// <summary>
/// 타임라인에서 접어 두는 울타리 설정.
/// </summary>
[System.Serializable]
public class BossFenceSettings
{
    [Tooltip("울타리 바깥 가로. 플레이어와 보스가 벗어나지 못하는 너비다.")]
    [SerializeField, Min(4f)] private float _width = RectBossFence.WIDTH;
    [Tooltip("울타리 바깥 세로. 플레이어와 보스가 벗어나지 못하는 높이다.")]
    [SerializeField, Min(4f)] private float _height = RectBossFence.HEIGHT;
    [SerializeField] private Sprite _left;
    [Tooltip("왼쪽 기둥 배율.")]
    [SerializeField, Min(0.01f)] private float _leftScale = RectBossFence.DEFAULT_SCALE;
    [SerializeField] private Sprite _right;
    [Tooltip("오른쪽 기둥 배율.")]
    [SerializeField, Min(0.01f)] private float _rightScale = RectBossFence.DEFAULT_SCALE;
    [SerializeField] private Sprite _edge;
    [Tooltip("아래 판 배율.")]
    [SerializeField, Min(0.01f)] private float _bottomScale = RectBossFence.DEFAULT_SCALE;
    [Tooltip("위 판 배율.")]
    [SerializeField, Min(0.01f)] private float _topScale = RectBossFence.DEFAULT_SCALE;
    [Tooltip("나무 사이 빈 간격. 0이면 서로 맞닿는다.")]
    [SerializeField, Min(0f)] private float _gap;

    public float Width => _width;

    public float Height => _height;

    public Sprite Left => _left;

    public Sprite Right => _right;

    public Sprite Edge => _edge;

    public float LeftScale => _leftScale;

    public float RightScale => _rightScale;

    public float BottomScale => _bottomScale;

    public float TopScale => _topScale;

    public float Gap => _gap;
}

/// <summary>
/// 타임라인에서 접어 두는 VS 화면 설정. Versus Mark까지 한 칸이다.
/// </summary>
[System.Serializable]
public class BossVersusSettings
{
    [Tooltip("보스마다 같이 쓰는 VS 화면. 위치와 크기는 이 프리팹에서 고친다.")]
    [SerializeField] private BossTrainerVersusView _view;
    [SerializeField] private Sprite _bar;
    [SerializeField] private Sprite _playerBack;
    [SerializeField] private Sprite[] _slashFrames = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite _mark;

    public BossTrainerVersusView View => _view;

    public Sprite Bar => _bar;

    public Sprite PlayerBack => _playerBack;

    public Sprite[] SlashFrames => _slashFrames;

    public Sprite Mark => _mark;
}
