using System;
using UnityEngine;

/// <summary>
/// 재화 하나의 표시 정보와, 판이 끝난 뒤 메타 잔액에 남길지 여부.
/// </summary>
[Serializable]
public class CurrencyData
{
    [SerializeField] private string _id;
    [SerializeField] private string _name;
    [SerializeField] private Sprite _icon;
    [SerializeField] private bool _keepAfterStage;

    public string Id => _id;

    public string Name => _name;

    public Sprite Icon => _icon;

    /// <summary>
    /// true면 스테이지에서 모은 수량을 판이 끝날 때 메타 잔액에 더한다.
    /// </summary>
    public bool KeepAfterStage => _keepAfterStage;
}
