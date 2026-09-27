using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prefab별 객체 생성, 재사용, 반환을 담당한다. Enemy 상태와 Spawn 조건은 관리하지 않는다.
/// 흐름: Get(prefab, position) => 재사용 또는 최초 생성 => 활성화 => 호출자가 초기화.
/// 반환: 호출자가 상태 정리 => Return => 비활성 보관. Scene 종료 시 DestroyAll로 정리한다.
/// </summary>
public sealed class CombatObjectPool
{
    private readonly Dictionary<GameObject, Stack<GameObject>> _available = new Dictionary<GameObject, Stack<GameObject>>();
    private readonly Dictionary<GameObject, GameObject> _prefabs = new Dictionary<GameObject, GameObject>();
    private readonly HashSet<GameObject> _inUse = new HashSet<GameObject>();
    private readonly Transform _root;
    private bool _disposed;

    public Transform Root => _root;

    public CombatObjectPool(Transform stageRoot)
    {
        _root = new GameObject("CombatObjectPool").transform;
        // 기존 Enemy의 World 좌표/크기를 유지하며 게임 Scene에 수명을 묶는다.
        _root.SetParent(stageRoot, true);
    }

    /// <summary>
    /// 같은 Prefab의 비활성 객체를 재사용하거나 생성하여 지정 위치에서 활성화한다.
    /// 호출자는 반환 직후 Bind/Initialize를 끝내고 Tick 대상에 등록한다.
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (_disposed || _root == null || prefab == null)
        {
            return null;
        }

        if (!_available.TryGetValue(prefab, out var available))
        {
            available = new Stack<GameObject>();
            _available.Add(prefab, available);
        }

        GameObject instance = null;
        while (available.Count > 0 && instance == null)
        {
            instance = available.Pop();
            if (instance == null)
            {
                // Scene 정리 등으로 이미 파괴된 Unity Object 참조는 재사용하지 않는다.
                _prefabs.Remove(instance);
            }
        }

        if (instance == null)
        {
            instance = Object.Instantiate(prefab, position, rotation, _root);
            _prefabs.Add(instance, prefab);
        }

        instance.transform.SetParent(_root, false);
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.transform.localScale = prefab.transform.localScale;
        _inUse.Add(instance);
        instance.SetActive(true);
        return instance;
    }

    /// <summary>
    /// 호출자가 상태를 정리한 객체를 비활성화해 원래 Prefab의 Pool에 돌려놓는다.
    /// 중복 반환, 다른 Pool의 객체, null/파괴된 객체는 false를 반환한다.
    /// </summary>
    public bool Return(GameObject instance)
    {
        if (_disposed || instance == null || !_inUse.Remove(instance))
        {
            return false;
        }

        instance.SetActive(false);
        if (_root != null)
        {
            instance.transform.SetParent(_root, false);
        }

        var prefab = _prefabs[instance];
        if (prefab != null)
        {
            _available[prefab].Push(instance);
        }

        return true;
    }

    /// <summary>
    /// Scene을 떠날 때 보관 중인 객체와 Root를 파괴한다. 이후 Get은 사용할 수 없다.
    /// EnemyManager는 이 메서드 전에 활성 Enemy를 정상 반환하여 Owner를 해제한다.
    /// </summary>
    public void DestroyAll()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var instance in _prefabs.Keys)
        {
            if (instance != null)
            {
                instance.SetActive(false);
                Object.Destroy(instance);
            }
        }

        _inUse.Clear();
        _prefabs.Clear();
        _available.Clear();
        if (_root != null)
        {
            // Prefab 미지정 시 사용하는 비활성 자리표시 원본도 함께 정리된다.
            Object.Destroy(_root.gameObject);
        }
    }
}
