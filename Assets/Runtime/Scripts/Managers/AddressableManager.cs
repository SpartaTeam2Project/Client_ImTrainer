using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Addressables 에셋을 불러오고 내린다. 무엇을 불러오는지는 모르고, 에셋 GUID와 묶음 이름만 안다.
/// 같은 에셋은 여러 곳이 요청해도 한 번만 불러온다. 묶음 이름으로 모아 두었다가 묶음째 내린다.
/// 예: 한 판에 쓰는 몬스터 그림은 "run" 묶음으로 불러오고 타이틀로 돌아갈 때 내린다.
/// </summary>
public class AddressableManager : BaseManager
{
    private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();

    private class Entry
    {
        public AsyncOperationHandle Handle;
        public readonly HashSet<string> Groups = new HashSet<string>();
    }

    public override void Cleanup()
    {
        ReleaseAll();
        base.Cleanup();
    }

    /// <summary>
    /// 이미 불러온 에셋을 돌려준다. 아직이거나 실패했으면 null.
    /// </summary>
    public T Get<T>(AssetReference reference) where T : Object
    {
        if (!IsValid(reference) || !_entries.TryGetValue(reference.AssetGUID, out var entry))
        {
            return null;
        }

        return entry.Handle.IsDone ? entry.Handle.Result as T : null;
    }

    /// <summary>
    /// 에셋을 비동기로 불러온다. 이미 불러왔거나 불러오는 중이면 그 결과를 기다린다.
    /// </summary>
    public async UniTask<T> LoadAsync<T>(AssetReference reference, string group, CancellationToken cancellationToken = default) where T : Object
    {
        var entry = Acquire<T>(reference, group);
        if (entry == null)
        {
            return null;
        }

        while (!entry.Handle.IsDone)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        return Result<T>(entry, reference);
    }

    /// <summary>
    /// 에셋을 지금 바로 불러온다. 프레임이 멈출 수 있어서 미리 불러오지 못한 경우의 대비로만 쓴다.
    /// </summary>
    public T Load<T>(AssetReference reference, string group) where T : Object
    {
        var entry = Acquire<T>(reference, group);
        if (entry == null)
        {
            return null;
        }

        if (!entry.Handle.IsDone)
        {
            entry.Handle.WaitForCompletion();
        }

        return Result<T>(entry, reference);
    }

    /// <summary>
    /// 여러 에셋을 한꺼번에 불러오고 모두 끝날 때까지 기다린다.
    /// </summary>
    public async UniTask PreloadAsync<T>(IEnumerable<AssetReference> references, string group, CancellationToken cancellationToken = default) where T : Object
    {
        var tasks = new List<UniTask<T>>();
        foreach (var reference in references)
        {
            tasks.Add(LoadAsync<T>(reference, group, cancellationToken));
        }

        await UniTask.WhenAll(tasks);
    }

    /// <summary>
    /// 묶음에서 에셋을 뺀다. 어떤 묶음에도 남지 않은 에셋은 내린다.
    /// </summary>
    public void ReleaseGroup(string group)
    {
        var released = new List<string>();
        foreach (var pair in _entries)
        {
            if (pair.Value.Groups.Remove(group) && pair.Value.Groups.Count == 0)
            {
                released.Add(pair.Key);
            }
        }

        foreach (var key in released)
        {
            Release(key);
        }
    }

    /// <summary>
    /// 불러온 에셋을 모두 내린다.
    /// </summary>
    public void ReleaseAll()
    {
        foreach (var key in new List<string>(_entries.Keys))
        {
            Release(key);
        }
    }

    private Entry Acquire<T>(AssetReference reference, string group) where T : Object
    {
        if (!IsValid(reference))
        {
            return null;
        }

        var key = reference.AssetGUID;
        if (!_entries.TryGetValue(key, out var entry))
        {
            // AssetReference.LoadAssetAsync는 참조 하나에 핸들 하나만 허용해서 키로 직접 불러온다.
            entry = new Entry { Handle = Addressables.LoadAssetAsync<T>(reference.RuntimeKey) };
            _entries.Add(key, entry);
        }

        entry.Groups.Add(group);
        return entry;
    }

    private T Result<T>(Entry entry, AssetReference reference) where T : Object
    {
        if (entry.Handle.Status == AsyncOperationStatus.Succeeded)
        {
            return entry.Handle.Result as T;
        }

        Debug.LogError($"[AddressableManager] 불러오기 실패: {reference.AssetGUID} {entry.Handle.OperationException?.Message}");
        Release(reference.AssetGUID);
        return null;
    }

    private void Release(string key)
    {
        if (!_entries.TryGetValue(key, out var entry))
        {
            return;
        }

        _entries.Remove(key);
        if (entry.Handle.IsValid())
        {
            Addressables.Release(entry.Handle);
        }
    }

    private static bool IsValid(AssetReference reference)
    {
        return reference != null && reference.RuntimeKeyIsValid();
    }
}
