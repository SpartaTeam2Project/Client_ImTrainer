using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배경 타일, 변, 구석, 마진, 장식 확률.
/// </summary>
[CreateAssetMenu(fileName = "StageFieldData", menuName = "Stage/Stage Field Data")]
public class StageFieldData : ScriptableObject
{
    [SerializeField] private List<GameObject> _backgroundPrefabs = new List<GameObject>();

    [Header("Sides")]
    [SerializeField] private GameObject _topPrefab;
    [SerializeField] private GameObject _bottomPrefab;
    [SerializeField] private GameObject _leftPrefab;
    [SerializeField] private GameObject _rightPrefab;

    [Header("Corners")]
    [SerializeField] private GameObject _topRightPrefab;
    [SerializeField] private GameObject _topLeftPrefab;
    [SerializeField] private GameObject _bottomRightPrefab;
    [SerializeField] private GameObject _bottomLeftPrefab;

    [Header("Margins")]
    [SerializeField] private float _leftMargin;
    [SerializeField] private float _rightMargin;
    [SerializeField] private float _topMargin;
    [SerializeField] private float _bottomMargin;

    [Header("Prop")]
    [SerializeField] private List<StagePropData> _propChances = new List<StagePropData>();

    public GameObject TopPrefab => _topPrefab;

    public GameObject BottomPrefab => _bottomPrefab;

    public GameObject LeftPrefab => _leftPrefab;

    public GameObject RightPrefab => _rightPrefab;

    public GameObject TopRightPrefab => _topRightPrefab;

    public GameObject TopLeftPrefab => _topLeftPrefab;

    public GameObject BottomRightPrefab => _bottomRightPrefab;

    public GameObject BottomLeftPrefab => _bottomLeftPrefab;

    public float LeftMargin => _leftMargin;

    public float RightMargin => _rightMargin;

    public float TopMargin => _topMargin;

    public float BottomMargin => _bottomMargin;

    public IReadOnlyList<StagePropData> PropChances => _propChances;

    public bool HasBackground
    {
        get
        {
            if (_backgroundPrefabs == null)
            {
                return false;
            }

            for (var i = 0; i < _backgroundPrefabs.Count; i++)
            {
                if (_backgroundPrefabs[i] != null)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 비어 있지 않은 배경 프리팹만 모은다.
    /// </summary>
    public List<GameObject> GetBackgroundPrefabs()
    {
        var prefabs = new List<GameObject>();
        if (_backgroundPrefabs == null)
        {
            return prefabs;
        }

        for (var i = 0; i < _backgroundPrefabs.Count; i++)
        {
            if (_backgroundPrefabs[i] != null)
            {
                prefabs.Add(_backgroundPrefabs[i]);
            }
        }

        return prefabs;
    }
}

/// <summary>
/// 칸 하나에 깔 장식의 개수와 확률.
/// </summary>
[Serializable]
public class StagePropData
{
    [SerializeField] private GameObject _prefab;
    [SerializeField, Min(1)] private int _maxAmount = 1;
    [SerializeField, Range(0f, 100f)] private float _chance;

    public GameObject Prefab => _prefab;

    public int MaxAmount => _maxAmount;

    public float Chance => _chance;
}
