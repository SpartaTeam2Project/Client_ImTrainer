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
    [SerializeField] private string _description;
    [SerializeField] private bool _showInUi = true;
    [SerializeField] private TrainingValueDisplay _valueDisplay;
    [SerializeField] private List<TrainingLevel> _levels = new List<TrainingLevel>();

    [Header("스킬트리 배치")]
    [SerializeField] private TrainingCategory _category;
    [SerializeField, Min(0)] private int _row;
    [SerializeField, Range(0, 2)] private int _column;

    [Header("해금 조건")]
    [SerializeField] private TrainingCategory _requiredCategory;
    [SerializeField, Min(0)] private int _requiredLevel;

    public TrainingType TrainingType => _trainingType;

    /// <summary>
    /// 창에서 이 칸이 놓이는 열.
    /// </summary>
    public TrainingCategory Category => _category;

    /// <summary>
    /// 열 안의 행. 0이 맨 위.
    /// </summary>
    public int Row => _row;

    /// <summary>
    /// 열 안의 칸. 0이 맨 왼쪽.
    /// </summary>
    public int Column => _column;

    /// <summary>
    /// 해금 기준이 되는 카테고리.
    /// </summary>
    public TrainingCategory RequiredCategory => _requiredCategory;

    /// <summary>
    /// 해금에 필요한 카테고리 Lv. 0이면 처음부터 열려 있다.
    /// </summary>
    public int RequiredLevel => _requiredLevel;

    public Sprite Icon => _icon;

    public string Title => _title ?? string.Empty;

    public string Description => _description ?? string.Empty;

    /// <summary>
    /// 창에 그릴지. false면 데이터베이스에 있어도 칸을 만들지 않는다.
    /// </summary>
    public bool ShowInUi => _showInUi;

    public TrainingValueDisplay ValueDisplay => _valueDisplay;

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
