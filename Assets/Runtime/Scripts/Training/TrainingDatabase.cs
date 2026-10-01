using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 트레이닝 목록.
/// </summary>
[CreateAssetMenu(fileName = "TrainingDatabase", menuName = "Training/Training Database")]
public class TrainingDatabase : ScriptableObject
{
    [SerializeField] private List<TrainingData> _trainings = new List<TrainingData>();

    public int Count => _trainings == null ? 0 : _trainings.Count;

    /// <summary>
    /// 순서에 해당하는 트레이닝. 범위를 벗어나면 null.
    /// </summary>
    public TrainingData GetTraining(int index)
    {
        if (_trainings == null || index < 0 || index >= _trainings.Count)
        {
            return null;
        }

        return _trainings[index];
    }

    /// <summary>
    /// 종류가 같은 트레이닝. 없으면 null.
    /// </summary>
    public TrainingData GetTraining(TrainingType trainingType)
    {
        if (_trainings == null)
        {
            return null;
        }

        for (var i = 0; i < _trainings.Count; i++)
        {
            var training = _trainings[i];
            if (training != null && training.TrainingType == trainingType)
            {
                return training;
            }
        }

        return null;
    }
}
