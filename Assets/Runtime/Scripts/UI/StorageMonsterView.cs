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
    private const int DEFAULT_BOB_PIXELS = 1;
    private const float DEFAULT_BOB_INTERVAL = 0.25f;

    // 아이콘 시트에서 가장 큰 아이콘 크기. 모든 칸을 이 크기로 맞춘다.
    private static readonly Vector2 MAX_ICON_SIZE = new Vector2(40f, 30f);

    [SerializeField] private Image _lockSilhouette;
    [SerializeField] private Image _unlockAnimation;
    [SerializeField] private GameObject _select;
    // 포커스된 아이콘이 위로 뜨는 원본 픽셀 수. 화면에는 아이콘 배율을 곱해 그린다.
    [SerializeField] private int _bobPixels = DEFAULT_BOB_PIXELS;
    [SerializeField] private float _bobInterval = DEFAULT_BOB_INTERVAL;

    private Action<StorageMonsterView> _onFocus;
    private Action<StorageMonsterView> _onConfirm;
    private bool _focused;
    private bool _bobbing;
    private bool _bobUp;
    private float _bobTimer;
    private int _frameIndex;

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
        _focused = false;
        StopBob();
        ApplyLockState();
        SetFocused(false);
    }

    /// <summary>
    /// 포커스 표시를 켜거나 끈다. 획득한 포켓몬은 포커스된 동안 위아래로 끊어 움직이며 아이콘 프레임을 넘긴다.
    /// </summary>
    public void SetFocused(bool focused)
    {
        if (_select != null)
        {
            _select.SetActive(focused);
        }

        // 포커스가 바뀔 때마다 모든 칸에 다시 불리므로 바뀐 순간에만 움직임을 건드린다.
        if (focused == _focused)
        {
            return;
        }

        _focused = focused;
        if (focused)
        {
            // 위로 뜬 아이콘이 옆 칸 위에 그려지게 맨 앞으로 옮긴다. 칸 위치는 목록 순서로 정해져서 그대로다.
            transform.SetAsLastSibling();
            StartBob();
        }
        else
        {
            StopBob();
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
        if (!_bobbing)
        {
            return;
        }

        _bobTimer += Time.unscaledDeltaTime;
        while (_bobTimer >= _bobInterval)
        {
            _bobTimer -= _bobInterval;
            StepBob();
        }
    }

    private void OnDisable()
    {
        _focused = false;
        StopBob();
    }

    private void StartBob()
    {
        _bobbing = false;
        if (!IsUnlocked || Data == null || _unlockAnimation == null || _bobInterval <= 0f)
        {
            return;
        }

        _bobUp = false;
        _bobTimer = 0f;
        _frameIndex = NextFrameIndex(Data.Icon, -1);
        _bobbing = true;
    }

    /// <summary>
    /// 중간 위치 없이 아래와 위를 오가고, 같은 박자로 아이콘 프레임을 넘긴다.
    /// </summary>
    private void StepBob()
    {
        _bobUp = !_bobUp;
        _unlockAnimation.rectTransform.anchoredPosition = new Vector2(0f, _bobUp ? _bobPixels * ICON_PIXEL_SCALE : 0f);

        var frames = Data.Icon;
        var next = NextFrameIndex(frames, _frameIndex);
        if (next < 0 || next == _frameIndex)
        {
            return;
        }

        _frameIndex = next;
        _unlockAnimation.sprite = frames[next];
        _unlockAnimation.rectTransform.sizeDelta = IconSize(frames[next]);
    }

    /// <summary>
    /// 아래 위치와 첫 프레임으로 되돌린다.
    /// </summary>
    private void StopBob()
    {
        _bobbing = false;
        if (_unlockAnimation == null)
        {
            return;
        }

        _unlockAnimation.rectTransform.anchoredPosition = Vector2.zero;
        if (Data != null)
        {
            var icon = MonsterVisualData.FirstFrame(Data.Icon);
            _unlockAnimation.sprite = icon;
            _unlockAnimation.rectTransform.sizeDelta = IconSize(icon);
        }
    }

    /// <summary>
    /// current 다음의 비어 있지 않은 프레임 번호. 끝에서는 처음으로 돌아가고, 없으면 -1.
    /// </summary>
    private static int NextFrameIndex(Sprite[] frames, int current)
    {
        if (frames == null || frames.Length == 0)
        {
            return -1;
        }

        for (var step = 1; step <= frames.Length; step++)
        {
            var index = (current + step) % frames.Length;
            if (index >= 0 && frames[index] != null)
            {
                return index;
            }
        }

        return -1;
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
