using System;

/// <summary>
/// 스토리지 목록 칸의 공통 동작. 트레이너 칸과 포켓몬 칸이 같이 쓴다.
/// </summary>
public interface IStorageSlotView
{
    bool IsUnlocked { get; }

    void SetFocused(bool focused);

    void PlayLockedSelect();
}

/// <summary>
/// 데이터를 받아 그리는 스토리지 목록 칸.
/// </summary>
public interface IStorageSlotView<TView, TData> : IStorageSlotView
{
    TData Data { get; }

    void Bind(TData data, bool unlocked, Action<TView> onFocus, Action<TView> onConfirm);
}
