using UnityEngine;

/// <summary>
/// 보스 펜스가 이동과 장식 제거에 제공하는 경계.
/// </summary>
public interface IStageFence
{
    /// <summary>
    /// 좌표를 펜스 안으로 되돌린다.
    /// </summary>
    Vector2 ClampPosition(Vector2 position);

    /// <summary>
    /// 점이 펜스 안이면 true.
    /// </summary>
    bool Contains(Vector2 position);
}
