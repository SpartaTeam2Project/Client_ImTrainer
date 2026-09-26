using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 프리팹 하나를 끄고 다시 쓰는 풀.
/// </summary>
public class StageObjectPool<T> where T : Component
{
    private readonly GameObject _prefab;
    private readonly Transform _parent;
    private readonly List<T> _instances = new List<T>();
    private bool _missingComponent;

    public StageObjectPool(GameObject prefab, Transform parent)
    {
        _prefab = prefab;
        _parent = parent;
    }

    /// <summary>
    /// 꺼져 있는 객체를 켜거나 새로 만든다.
    /// </summary>
    public T Get()
    {
        for (var i = 0; i < _instances.Count; i++)
        {
            var instance = _instances[i];
            if (instance == null || instance.gameObject.activeSelf)
            {
                continue;
            }

            instance.gameObject.SetActive(true);
            return instance;
        }

        if (_prefab == null || _missingComponent)
        {
            return null;
        }

        var createdObject = Object.Instantiate(_prefab, _parent);
        var created = createdObject.GetComponent<T>();
        if (created == null)
        {
            _missingComponent = true;
            Debug.LogError($"{_prefab.name}에 {typeof(T).Name}이 없습니다.");
            Object.Destroy(createdObject);
            return null;
        }

        _instances.Add(created);
        return created;
    }

    /// <summary>
    /// 만들어 둔 객체를 없앤다.
    /// </summary>
    public void DestroyAll()
    {
        for (var i = 0; i < _instances.Count; i++)
        {
            if (_instances[i] != null)
            {
                Object.Destroy(_instances[i].gameObject);
            }
        }

        _instances.Clear();
    }
}
