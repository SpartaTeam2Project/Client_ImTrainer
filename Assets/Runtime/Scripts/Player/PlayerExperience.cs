using UnityEngine;

/// <summary>
/// 플레이어의 레벨과 경험치를 담당한다.
/// </summary>
public class PlayerExperience : MonoBehaviour
{
    private const int STARTING_LEVEL = 1;
    private const int MAX_LEVEL_UPS_PER_GAIN = 20;
    private const float MIN_REQUIRED_XP = 1f;

    [Header("Experience")]
    [SerializeField] private float _baseRequiredXp = 5f;
    [SerializeField] private float _requiredGrowth = 1.25f;

    private PlayerStat _stat;

    public int Level { get; private set; } = STARTING_LEVEL;

    public float CurrentXp { get; private set; }

    public float RequiredXp { get; private set; } = MIN_REQUIRED_XP;

    #region Public Methods

    /// <summary>
    /// 프리팹에 적힌 경험치 곡선으로 1레벨부터 시작한다.
    /// </summary>
    public void Initialize(PlayerStat stat)
    {
        _stat = stat;
        SetProgress(STARTING_LEVEL, 0f, CalculateRequiredXp(STARTING_LEVEL));
    }

    /// <summary>
    /// 경험치를 더한다. 필요량을 채우면 레벨을 올리고, 오른 횟수를 돌려준다.
    /// </summary>
    public int AddExperience(float amount)
    {
        if (amount <= 0f)
        {
            return 0;
        }

        var xp = CurrentXp + amount * Mathf.Max(0f, _stat.xpMultiplier);
        Debug.Log("원래 경험치/증가후 경험치:" +amount+"/"+(xp-CurrentXp));
        var level = Level;
        var required = Mathf.Max(MIN_REQUIRED_XP, RequiredXp);
        var gained = 0;
        while (xp >= required && gained < MAX_LEVEL_UPS_PER_GAIN)
        {
            xp -= required;
            level++;
            required = CalculateRequiredXp(level);
            gained++;
        }

        SetProgress(level, xp, required);
        return gained;
    }

    #endregion

    #region Private Methods

    private float CalculateRequiredXp(int level)
    {
        var step = Mathf.Max(STARTING_LEVEL, level);
        var growth = Mathf.Max(1f, _requiredGrowth);
        var required = Mathf.Max(MIN_REQUIRED_XP, _baseRequiredXp) * Mathf.Pow(growth, step - 1);
        return Mathf.Max(MIN_REQUIRED_XP, required);
    }

    private void SetProgress(int level, float currentXp, float requiredXp)
    {
        Level = Mathf.Max(STARTING_LEVEL, level);
        CurrentXp = Mathf.Max(0f, currentXp);
        RequiredXp = Mathf.Max(MIN_REQUIRED_XP, requiredXp);
    }

    #endregion
}
