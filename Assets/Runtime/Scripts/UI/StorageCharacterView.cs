using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 스토리지 트레이너 칸. 잠금, 획득 그림, 포커스를 프리팹 자식으로 켠다.
/// </summary>
public class StorageCharacterView : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private const float PORTRAIT_TO_SLOT_SCALE = 3.2f;

    [SerializeField] private Image _lockSilhouette;
    [SerializeField] private Image _unlockAnimation;
    [SerializeField] private GameObject _select;

    private Action<StorageCharacterView> _onFocus;
    private Action<StorageCharacterView> _onConfirm;

    public PlayableCharacterData Data { get; private set; }

    public bool IsUnlocked { get; private set; }

    /// <summary>
    /// 캐릭터와 획득 상태를 칸에 반영한다.
    /// </summary>
    public void Bind(PlayableCharacterData data, bool unlocked, Action<StorageCharacterView> onFocus, Action<StorageCharacterView> onConfirm)
    {
        Data = data;
        IsUnlocked = unlocked;
        _onFocus = onFocus;
        _onConfirm = onConfirm;
        ApplyLockState();
        SetFocused(false);
    }

    /// <summary>
    /// 포커스 표시를 켜거나 끈다.
    /// </summary>
    public void SetFocused(bool focused)
    {
        if (_select != null)
        {
            _select.SetActive(focused);
        }
    }

    /// <summary>
    /// 포인터가 칸 위에 오면 포커스만 옮긴다.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        _onFocus?.Invoke(this);
    }

    /// <summary>
    /// 칸을 클릭하면 포커스를 옮기고 확정을 시도한다.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        _onFocus?.Invoke(this);
        _onConfirm?.Invoke(this);
    }

    private void ApplyLockState()
    {
        if (_lockSilhouette != null)
        {
            _lockSilhouette.gameObject.SetActive(!IsUnlocked);
            _lockSilhouette.sprite = Data != null ? Data.Portrait : null;
        }

        if (_unlockAnimation != null)
        {
            _unlockAnimation.gameObject.SetActive(IsUnlocked);
            _unlockAnimation.sprite = Data != null ? Data.Portrait : null;
        }

        ApplySlotSize();
    }

    private void ApplySlotSize()
    {
        if (Data == null)
        {
            return;
        }

        var portrait = Data.PortraitSize;
        var width = portrait.x * PORTRAIT_TO_SLOT_SCALE;
        var height = portrait.y * PORTRAIT_TO_SLOT_SCALE;
        if (width <= 0f || height <= 0f)
        {
            return;
        }

        var rect = (RectTransform)transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(width, height);
    }
}
