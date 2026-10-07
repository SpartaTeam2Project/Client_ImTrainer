using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 스토리지 포켓몬 칸. 잠금, 획득 그림, 포커스를 프리팹 자식으로 켠다.
/// </summary>
public class StorageMonsterView : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IStorageSlotView<StorageMonsterView, MonsterVisualData>
{
    // 아이콘 픽셀 배율. 정수라야 픽셀이 고르게 보인다.
    private const int ICON_PIXEL_SCALE = 3;
    private const string LOCKED_SELECT_SOUND = "error";

    // 아이콘 시트에서 가장 큰 아이콘 크기. 모든 칸을 이 크기로 맞춘다.
    private static readonly Vector2 MAX_ICON_SIZE = new Vector2(40f, 30f);

    [SerializeField] private Image _lockSilhouette;
    [SerializeField] private Image _unlockAnimation;
    [SerializeField] private GameObject _select;

    private Action<StorageMonsterView> _onFocus;
    private Action<StorageMonsterView> _onConfirm;

    public MonsterVisualData Data { get; private set; }

    public bool IsUnlocked { get; private set; }

    /// <summary>
    /// 아이콘을 그릴 크기. 원래 픽셀 크기에 같은 배율을 곱한다. 엔트리 칸도 이 크기를 쓴다.
    /// </summary>
    public static Vector2 IconSize(Sprite icon)
    {
        return icon != null ? icon.rect.size * ICON_PIXEL_SCALE : Vector2.zero;
    }

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
        var icon = Data != null ? MonsterVisualData.FirstFrame(Data.Icon) : null;

        if (_lockSilhouette != null)
        {
            _lockSilhouette.gameObject.SetActive(!IsUnlocked);
            ApplyIcon(_lockSilhouette, icon);
        }

        if (_unlockAnimation != null)
        {
            _unlockAnimation.gameObject.SetActive(IsUnlocked);
            ApplyIcon(_unlockAnimation, icon);
        }

        ApplySlotSize();
    }

    /// <summary>
    /// 모든 칸을 같은 크기로 맞춘다. 칸이 같아야 줄과 열이 반듯하게 선다.
    /// </summary>
    private void ApplySlotSize()
    {
        var rect = (RectTransform)transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = MAX_ICON_SIZE * ICON_PIXEL_SCALE;
    }

    /// <summary>
    /// 아이콘을 원래 픽셀 크기에 같은 배율을 곱해 칸 가운데에 그린다.
    /// </summary>
    private static void ApplyIcon(Image image, Sprite icon)
    {
        image.sprite = icon;
        image.preserveAspect = false;

        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = IconSize(icon);
    }
}
