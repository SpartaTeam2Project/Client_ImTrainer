/// <summary>
/// 진화 계통의 단계 칸. 일반 진화는 최대 3단이고 메가진화와 거다이맥스는 따로 둔다.
/// </summary>
public enum EvolutionStage
{
    Basic = 0,
    Stage1 = 1,
    Stage2 = 2,
    Mega = 3,
    VMax = 4
}

/// <summary>
/// 한 진화 계통의 단계별 uid와 고른 종의 단계. 없는 칸은 -1이다.
/// 갈래 진화가 있으면 단계 칸에 들어가지 않은 나머지 갈래 종을 따로 담는다.
/// </summary>
public struct EvolutionLine
{
    public const int STAGE_COUNT = 5;
    public const int MAX_BRANCH = 8;
    public const int NONE = -1;

    private readonly int[] _uids;
    private readonly int[] _branches;

    /// <summary>
    /// 고른 종의 uid.
    /// </summary>
    public int SelectedUid { get; }

    /// <summary>
    /// 단계 칸에 들어가지 않은 갈래 종 수.
    /// </summary>
    public int BranchCount { get; private set; }

    public EvolutionLine(int selectedUid)
    {
        SelectedUid = selectedUid;
        BranchCount = 0;
        _uids = new int[STAGE_COUNT];
        _branches = new int[MAX_BRANCH];
        for (var i = 0; i < _uids.Length; i++)
        {
            _uids[i] = NONE;
        }
    }

    /// <summary>
    /// 단계 칸의 uid. 없으면 -1.
    /// </summary>
    public int Get(EvolutionStage stage)
    {
        var index = (int)stage;
        return _uids != null && index >= 0 && index < _uids.Length ? _uids[index] : NONE;
    }

    public void Set(EvolutionStage stage, int uid)
    {
        var index = (int)stage;
        if (_uids != null && index >= 0 && index < _uids.Length)
        {
            _uids[index] = uid < 0 ? NONE : uid;
        }
    }

    /// <summary>
    /// index번째 갈래 종의 uid. 없으면 -1.
    /// </summary>
    public int GetBranch(int index)
    {
        return _branches != null && index >= 0 && index < BranchCount ? _branches[index] : NONE;
    }

    /// <summary>
    /// 갈래 종을 하나 더한다. 칸이 차면 버린다.
    /// </summary>
    public void AddBranch(int uid)
    {
        if (_branches != null && uid >= 0 && BranchCount < _branches.Length)
        {
            _branches[BranchCount] = uid;
            BranchCount++;
        }
    }

    /// <summary>
    /// 고른 종이 들어간 칸이면 true.
    /// </summary>
    public bool IsSelected(EvolutionStage stage)
    {
        return SelectedUid >= 0 && Get(stage) == SelectedUid;
    }
}
