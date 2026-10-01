using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 트레이닝 하나. 레벨마다 효과 값과 포켓달러 비용을 가진다.
/// </summary>
[CreateAssetMenu(fileName = "Training", menuName = "Training/Training")]
public class TrainingData : ScriptableObject
{
    [SerializeField] private TrainingType _trainingType;
    [SerializeField] private Sprite _icon;
    [SerializeField] private string _title;
    [SerializeField] private List<TrainingLevel> _levels = new List<TrainingLevel>();

    public TrainingType TrainingType => _trainingType;

    public Sprite Icon => _icon;

    public string Title => _title ?? string.Empty;

    public int LevelsCount => _levels == null ? 0 : _levels.Count;

    /// <summary>
    /// 레벨 순서에 해당하는 수치. 범위를 벗어나면 null.
    /// </summary>
    public TrainingLevel GetLevel(int id)
    {
        if (_levels == null || id < 0 || id >= _levels.Count)
        {
            return null;
        }

        return _levels[id];
    }
}

/// <summary>
/// 트레이닝 한 단계의 효과와 포켓달러 비용.
/// </summary>
[Serializable]
public class TrainingLevel
{
    [SerializeField] private float _value;
    [SerializeField] private int _cost;

    public float Value => _value;

    public int Cost => _cost;
}
