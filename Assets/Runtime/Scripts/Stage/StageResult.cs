using System;
using UnityEngine;

/// <summary>
/// 스테이지가 끝났을 때 모으는 결과. 전투 중에는 만들지 않는다.
/// </summary>
[Serializable]
public class StageResult
{
    [SerializeField] private int _killCount;
    [SerializeField] private float _elapsedSeconds;

    public StageResult(int killCount, float elapsedSeconds)
    {
        _killCount = killCount;
        _elapsedSeconds = elapsedSeconds;
    }

    public int KillCount => _killCount;

    public float ElapsedSeconds => _elapsedSeconds;
}
