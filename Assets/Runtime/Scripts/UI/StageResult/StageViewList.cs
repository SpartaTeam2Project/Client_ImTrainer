using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 같은 행 프리팹을 필요한 개수만큼 만들어 두고 남는 행은 끈다.
/// </summary>
public sealed class StageViewList<T> where T : Component
{
    private readonly List<T> _views = new List<T>();

    /// <summary>
    /// count개를 켜고 돌려준다. 모자라면 root 아래에 만든다.
    /// </summary>
    public IReadOnlyList<T> Ensure(T prefab, Transform root, int count)
    {
        while (_views.Count < count && prefab != null && root != null)
        {
            _views.Add(Object.Instantiate(prefab, root));
        }

        for (var i = 0; i < _views.Count; i++)
        {
            _views[i].gameObject.SetActive(i < count);
        }

        return _views;
    }
}
