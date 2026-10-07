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
    [Tooltip("상점에서 이 재화로 살 때 볼이 열리는 중간 그림. 없으면 열림 연출을 건너뛴다.")]
    [SerializeField] private Sprite _openingIcon;
    [Tooltip("상점에서 이 재화로 살 때 볼이 다 열린 그림. 없으면 열림 연출을 건너뛴다.")]
    [SerializeField] private Sprite _openIcon;
    [SerializeField] private bool _keepAfterStage;

    public string Id => _id;

    public string Name => _name;

    public Sprite Icon => _icon;

    public Sprite OpeningIcon => _openingIcon;

    public Sprite OpenIcon => _openIcon;

    /// <summary>
    /// true면 스테이지에서 모은 수량을 판이 끝날 때 메타 잔액에 더한다.
    /// </summary>
    public bool KeepAfterStage => _keepAfterStage;
}
