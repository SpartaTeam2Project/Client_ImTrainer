using UnityEngine;

/// <summary>
/// 플레이어 오브젝트가 파괴될 때 매니저 참조를 끊는다.
/// </summary>
public class PlayerActor : MonoBehaviour
{
    private PlayerManager _owner;

    #region Unity Methods

    private void OnDestroy()
    {
        if (_owner == null)
        {
            return;
        }

        _owner.NotifyActorDestroyed(this);
        _owner = null;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 소유 매니저를 연결한다.
    /// </summary>
    public void Bind(PlayerManager owner)
    {
        _owner = owner;
    }

    #endregion
}
