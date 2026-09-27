using System;
using UnityEngine;

/// <summary>
/// 레벨업 증강 선택창. 창을 연 뒤 선택이 끝나면 onChosen을 호출한다.
/// 경험치 매니저에 이 컴포넌트가 없으면 전투를 멈추지 않고 바로 이어 간다.
/// </summary>
public abstract class LevelUpChoice : MonoBehaviour
{
    /// <summary>
    /// 증강 선택창을 연다. 전투를 멈추려면 GameState.LevelUp으로 바꾼 뒤, 선택이 끝나면 onChosen을 호출한다.
    /// </summary>
    public abstract void Open(int playerId, int level, Action onChosen);
}
