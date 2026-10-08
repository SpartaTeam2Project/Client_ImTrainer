using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 상점 볼에서 가방 칸으로 가는 광선 모양 하나. 좌표는 PurchaseBeam 기준 로컬 좌표다.
/// 날아가는 사이 가방이 다시 정렬돼 칸이 옮겨질 수 있어서 도착점은 그릴 때마다 to()로 다시 읽는다.
/// 돌려준 Sequence의 시간 설정과 정리는 PurchaseBeam이 맡는다.
/// </summary>
[Serializable]
public abstract class PurchaseBeamStyle
{
    /// <summary>
    /// pool에서 Image를 꺼내 광선을 그린다. 광선이 칸에 닿는 순간 onArrive를 부른다.
    /// </summary>
    public abstract Sequence Play(BeamImagePool pool, Vector2 from, Func<Vector2> to, Action onArrive);

    protected static Color WithAlpha(Color color, float alpha)
    {
        color.a *= alpha;
        return color;
    }

    /// <summary>
    /// from에서 to로 가는 방향의 왼쪽 수직 단위 벡터.
    /// </summary>
    protected static Vector2 Perpendicular(Vector2 from, Vector2 to)
    {
        var direction = to - from;
        return direction.sqrMagnitude > 0f ? new Vector2(-direction.y, direction.x).normalized : Vector2.up;
    }
}
