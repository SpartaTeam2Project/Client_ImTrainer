using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class EventManager : BaseManager
{
    private readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

    /// <summary>
    /// 이벤트 매니저 초기화
    /// </summary>
    public override UniTask InitializeAsync()
    {
        return base.InitializeAsync();
    }

    /// <summary>
    /// 구독을 모두 해제한다.
    /// </summary>
    public override void Cleanup()
    {
        _handlers.Clear();
        base.Cleanup();
    }

    public void Subscribe<T>(Action<T> handler)
    {
        if (handler == null)
        {
            return;
        }

        var key = typeof(T);
        if (_handlers.TryGetValue(key, out var existing))
        {
            _handlers[key] = Delegate.Combine(existing, handler);
            return;
        }

        _handlers[key] = handler;
    }

    public void Unsubscribe<T>(Action<T> handler)
    {
        if (handler == null)
        {
            return;
        }

        var key = typeof(T);
        if (!_handlers.TryGetValue(key, out var existing))
        {
            return;
        }

        var updated = Delegate.Remove(existing, handler);
        if (updated == null)
        {
            _handlers.Remove(key);
            return;
        }

        _handlers[key] = updated;
    }

    public void Publish<T>(T eventData)
    {
        if (!_handlers.TryGetValue(typeof(T), out var existing))
        {
            return;
        }

        if (existing is Action<T> handler)
        {
            handler.Invoke(eventData);
        }
    }
}
