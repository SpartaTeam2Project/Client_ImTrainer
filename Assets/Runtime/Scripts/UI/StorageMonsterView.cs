using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 스토리지 포켓몬 칸. 잠금, 획득 그림, 포커스를 프리팹 자식으로 켠다.
/// </summary>
public class StorageMonsterView : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private const float PORTRAIT_TO_SLOT_SCALE = 3.2f;
    private const string LOCKED_SELECT_SOUND = "error";

    [SerializeField] private Image _lockSilhouette;
    [SerializeField] private Image _unlockAnimation;
    [SerializeField] private GameObject _select;

    private Action<StorageMonsterView> _onFocus;
    private Action<StorageMonsterView> _onConfirm;

    public MonsterVisualData Data { get; private set; }

    public bool IsUnlocked { get; private set; }

    /// <summary>
    /// 포켓몬과 보유 상태를 칸에 반영한다.
    /// </summary>
    public void Bind(MonsterVisualData data, bool unlocked, Action<StorageMonsterView> onFocus, Action<StorageMonsterView> onConfirm)
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
    /// 잠긴 칸을 고르면 에러 효과음을 낸다.
    /// </summary>
    public void PlayLockedSelect()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(LOCKED_SELECT_SOUND);
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
            _lockSilhouette.sprite = Data != null ? MonsterVisualData.FirstFrame(Data.Icon) : null;
        }

        if (_unlockAnimation != null)
        {
            _unlockAnimation.gameObject.SetActive(IsUnlocked);
            _unlockAnimation.sprite = Data != null ? MonsterVisualData.FirstFrame(Data.Icon) : null;
        }

        ApplySlotSize();
    }

    private void ApplySlotSize()
    {
        if (Data == null)
        {
            return;
        }

        var portrait = Data.IconSize;
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
