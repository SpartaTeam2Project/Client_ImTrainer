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
    private const string LOCKED_SELECT_SOUND = "error";

    [SerializeField] private Image _lockSilhouette;
    [SerializeField] private Image _unlockAnimation;
    [SerializeField] private GameObject _select;

    private Action<StorageCharacterView> _onFocus;
    private Action<StorageCharacterView> _onConfirm;
    private bool _focused;
    private bool _playing;
    private int _frameIndex;
    private float _frameTimer;
    // 초상화 칸 1픽셀이 화면에서 차지하는 크기. 그림 영역이 칸에 꽉 차도록 맞춘 값이다.
    private Vector2 _pixelScale = Vector2.one;

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
        _focused = false;
        ApplyLockState();
        SetFocused(false);
    }

    /// <summary>
    /// 포커스 표시를 켜거나 끈다. 획득한 캐릭터는 포커스되는 순간 초상화 애니메이션을 한 번 재생한다.
    /// </summary>
    public void SetFocused(bool focused)
    {
        if (_select != null)
        {
            _select.SetActive(focused);
        }

        // 포커스가 바뀔 때마다 모든 칸에 다시 불리므로 바뀐 순간에만 재생을 건드린다.
        if (focused == _focused)
        {
            return;
        }

        _focused = focused;
        if (focused)
        {
            // 칸 밖으로 넘치는 동작이 옆 칸 위에 그려지게 맨 앞으로 옮긴다. 칸 위치는 목록 순서로 정해져서 그대로다.
            transform.SetAsLastSibling();
            StartPortraitAnimation();
        }
        else
        {
            StopPortraitAnimation();
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

    private void Update()
    {
        if (!_playing)
        {
            return;
        }

        var frames = Data.PortraitFrames;
        var frameSeconds = 1f / Data.PortraitFrameRate;
        _frameTimer += Time.unscaledDeltaTime;
        while (_frameTimer >= frameSeconds)
        {
            _frameTimer -= frameSeconds;
            if (_frameIndex >= frames.Length - 1)
            {
                // 한 번만 재생하고 마지막 장면에서 멈춘다.
                _playing = false;
                return;
            }

            _frameIndex++;
            ShowFrame(frames[_frameIndex]);
        }
    }

    private void StartPortraitAnimation()
    {
        _playing = false;
        if (!IsUnlocked || Data == null || _unlockAnimation == null || Data.PortraitFrames.Length < 2)
        {
            return;
        }

        _frameIndex = 0;
        _frameTimer = 0f;
        _playing = true;
        ShowFrame(Data.PortraitFrames[0]);
    }

    private void StopPortraitAnimation()
    {
        _playing = false;
        if (IsUnlocked && Data != null)
        {
            ShowFrame(Data.PortraitSheetFrame);
        }
    }

    private void ApplyLockState()
    {
        _playing = false;
        if (_lockSilhouette != null)
        {
            _lockSilhouette.gameObject.SetActive(!IsUnlocked);
            _lockSilhouette.sprite = Data != null ? Data.Portrait : null;
        }

        ApplySlotSize();

        if (_unlockAnimation != null)
        {
            _unlockAnimation.gameObject.SetActive(IsUnlocked);
            // 동작 중 칸 밖으로 넘친 그림이 옆 칸의 마우스 판정을 가로채지 않게 한다.
            _unlockAnimation.raycastTarget = false;
            _unlockAnimation.preserveAspect = false;
            ShowFrame(Data != null ? Data.PortraitSheetFrame : null);
        }
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

        var area = Data.PortraitArea;
        _pixelScale = area.width > 0f && area.height > 0f
            ? new Vector2(width / area.width, height / area.height)
            : Vector2.one;
    }

    /// <summary>
    /// 칸 그대로의 프레임을 그린다. 그림 영역이 칸에 꽉 차게 놓아서 정지 그림은 예전과 같고,
    /// 다른 프레임은 같은 기준점에서 칸 밖으로 넘친다.
    /// </summary>
    private void ShowFrame(Sprite frame)
    {
        if (_unlockAnimation == null)
        {
            return;
        }

        _unlockAnimation.sprite = frame;
        if (frame == null || Data == null)
        {
            return;
        }

        var area = Data.PortraitArea;
        var image = _unlockAnimation.rectTransform;
        image.anchorMin = Vector2.zero;
        image.anchorMax = Vector2.zero;
        image.pivot = Vector2.zero;
        image.anchoredPosition = -Vector2.Scale(area.position, _pixelScale);
        image.sizeDelta = Vector2.Scale(frame.rect.size, _pixelScale);
    }
}
