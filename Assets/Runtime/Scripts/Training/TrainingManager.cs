using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 플레이어별 트레이닝 레벨을 로컬에 남긴다.
/// </summary>
public class TrainingManager : BaseManager
{
    private const string PREFS_PREFIX = "training_";
    private const int UNTRAINED_LEVEL = -1;

    [SerializeField] private TrainingDatabase _database;

    private bool _missingDatabaseLogged;

    /// <summary>
    /// 트레이닝 매니저 초기화.
    /// </summary>
    public override UniTask InitializeAsync()
    {
        if (_database == null)
        {
            LogMissingDatabase();
        }

        return base.InitializeAsync();
    }

    /// <summary>
    /// 저장된 레벨. 한 번도 올리지 않았으면 -1.
    /// </summary>
    public int GetTrainingLevel(int playerId, TrainingType trainingType)
    {
        return PlayerPrefs.GetInt(GetKey(playerId, trainingType), UNTRAINED_LEVEL);
    }

    /// <summary>
    /// 그 플레이어의 트레이닝 레벨을 1 올린다.
    /// </summary>
    public void IncrementTrainingLevel(int playerId, TrainingType trainingType)
    {
        var level = GetTrainingLevel(playerId, trainingType);
        PlayerPrefs.SetInt(GetKey(playerId, trainingType), level + 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 한 번이라도 올렸으면 true.
    /// </summary>
    public bool IsTrainingAcquired(int playerId, TrainingType trainingType)
    {
        return GetTrainingLevel(playerId, trainingType) != UNTRAINED_LEVEL;
    }

    /// <summary>
    /// 현재 레벨의 효과 값. 없거나 아직 올리지 않았으면 0.
    /// </summary>
    public float GetTrainingValue(int playerId, TrainingType trainingType)
    {
        if (_database == null)
        {
            LogMissingDatabase();
            return 0f;
        }

        var data = _database.GetTraining(trainingType);
        var level = GetTrainingLevel(playerId, trainingType);
        if (data == null || level < 0)
        {
            return 0f;
        }

        var trainingLevel = data.GetLevel(level);
        return trainingLevel == null ? 0f : trainingLevel.Value;
    }

    /// <summary>
    /// 카테고리 Lv. 그 카테고리 트레이닝들에서 올린 레벨 수를 더한다.
    /// </summary>
    public int GetCategoryLevel(int playerId, TrainingCategory category)
    {
        if (_database == null)
        {
            LogMissingDatabase();
            return 0;
        }

        var total = 0;
        for (var i = 0; i < _database.Count; i++)
        {
            var data = _database.GetTraining(i);
            if (data == null || data.Category != category)
            {
                continue;
            }

            total += GetTrainingLevel(playerId, data.TrainingType) + 1;
        }

        return total;
    }

    /// <summary>
    /// 해금 조건을 채웠으면 true.
    /// </summary>
    public bool IsUnlocked(int playerId, TrainingData data)
    {
        if (data == null)
        {
            return false;
        }

        return data.RequiredLevel <= 0 || GetCategoryLevel(playerId, data.RequiredCategory) >= data.RequiredLevel;
    }

    /// <summary>
    /// 지금까지 산 레벨들의 포켓달러 비용 합.
    /// </summary>
    public int GetSpentCost(int playerId)
    {
        if (_database == null)
        {
            LogMissingDatabase();
            return 0;
        }

        var total = 0;
        for (var i = 0; i < _database.Count; i++)
        {
            var data = _database.GetTraining(i);
            if (data == null)
            {
                continue;
            }

            var level = GetTrainingLevel(playerId, data.TrainingType);
            for (var id = 0; id <= level; id++)
            {
                var trainingLevel = data.GetLevel(id);
                if (trainingLevel != null)
                {
                    total += trainingLevel.Cost;
                }
            }
        }

        return total;
    }

    /// <summary>
    /// 그 플레이어의 트레이닝 레벨을 모두 지운다. 돌려줄 포켓달러 합을 반환한다.
    /// </summary>
    public int ResetAll(int playerId)
    {
        if (_database == null)
        {
            LogMissingDatabase();
            return 0;
        }

        var refund = GetSpentCost(playerId);
        for (var i = 0; i < _database.Count; i++)
        {
            var data = _database.GetTraining(i);
            if (data != null)
            {
                PlayerPrefs.DeleteKey(GetKey(playerId, data.TrainingType));
            }
        }

        PlayerPrefs.Save();
        return refund;
    }

    private static string GetKey(int playerId, TrainingType trainingType)
    {
        return PREFS_PREFIX + playerId + "_" + (int)trainingType;
    }

    private void LogMissingDatabase()
    {
        if (_missingDatabaseLogged)
        {
            return;
        }

        _missingDatabaseLogged = true;
        Debug.LogError("TrainingManager에 TrainingDatabase가 없습니다.");
    }
}
