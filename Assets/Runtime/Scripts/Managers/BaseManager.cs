using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 모든 매니저의 기본 클래스
/// </summary>
public abstract class BaseManager : MonoBehaviour
{
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// 매니저 초기화. 하위 클래스에서 재정의한다.
    /// </summary>
    public virtual UniTask InitializeAsync()
    {
        IsInitialized = true;
        return UniTask.CompletedTask;
    }

    /// <summary>
    /// 매니저 정리. 필요하면 하위 클래스에서 재정의한다.
    /// </summary>
    public virtual void Cleanup()
    {
        IsInitialized = false;
    }

    /// <summary>
    /// 등록된 다른 매니저를 가져온다.
    /// </summary>
    protected T GetManager<T>() where T : BaseManager
    {
        return Managers.Instance.GetManager<T>();
    }
}
