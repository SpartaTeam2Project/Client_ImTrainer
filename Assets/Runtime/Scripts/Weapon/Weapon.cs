using UnityEngine;

/// <summary>
/// 매니저가 프리팹으로 만들어 시계 칸에 붙이는 무기.
/// </summary>
public abstract class Weapon : MonoBehaviour
{
    /// <summary>
    /// 소유 매니저를 연결한다.
    /// </summary>
    public abstract void Bind(AbilityManager owner);

    /// <summary>
    /// 이번 무기에 쓸 그림 에셋을 넣는다. 8방향과 총알은 이 에셋만 재생한다.
    /// </summary>
    public virtual void ApplyVisual(WeaponVisualData visual)
    {
    }

    /// <summary>
    /// 플레이어 식별자와 시계 칸을 넣고 공격을 시작한다.
    /// </summary>
    public abstract void Initialize(int playerId, WeaponSlot slot);

    /// <summary>
    /// 플레이어를 따라가고 공격한다.
    /// </summary>
    public abstract void Tick(float deltaTime);
}
