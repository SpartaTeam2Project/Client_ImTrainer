using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스토리지 엔트리 칸. 고른 트레이너와 함께 출발할 포켓몬, 잠긴 칸 표시를 맡는다.
/// </summary>
public sealed class StorageEntrySlots
{
    private readonly Image[] _monsterImages;
    private readonly GameObject[] _locks;
    private readonly GameObject _choose;
    private readonly Image _chooseImage;
    private readonly GameObject _notChosen;
    // 트레이너 그림이 놓인 원래 자리. 위아래 움직임은 이 자리를 기준으로 한다.
    private readonly Vector2 _chooseBasePosition;
    private MonsterVisualData[] _visuals = System.Array.Empty<MonsterVisualData>();
    private Image[] _icons = System.Array.Empty<Image>();
    private Image[] _empties = System.Array.Empty<Image>();
    private int[] _frameIndices = System.Array.Empty<int>();
    // 엔트리 칸은 포커스와 상관없이 모두 같은 박자로 위아래로 움직인다.
    private bool _bobUp;
    private float _bobTimer;

    public StorageEntrySlots(Image[] monsterImages, GameObject[] locks, GameObject choose, Image chooseImage, GameObject notChosen)
    {
        _monsterImages = monsterImages;
        _locks = locks;
        _choose = choose;
        _chooseImage = chooseImage;
        _notChosen = notChosen;
        _chooseBasePosition = chooseImage != null ? chooseImage.rectTransform.anchoredPosition : Vector2.zero;
    }

    public PlayableCharacterData Character { get; private set; }

    /// <summary>
    /// 트레이너 칸을 채우거나 비운다.
    /// </summary>
    public void SetCharacter(PlayableCharacterData data)
    {
        Character = data;
        var chosen = data != null;
        if (_choose != null)
        {
            _choose.SetActive(chosen);
        }

        if (_chooseImage != null)
        {
            if (chosen)
            {
                _chooseImage.sprite = data.InGameSprite;
            }

            // 새로 고른 트레이너도 엔트리 포켓몬과 같은 높이에서 시작한다.
            _chooseImage.rectTransform.anchoredPosition = _chooseBasePosition + (chosen ? BobPosition() : Vector2.zero);
        }

        if (_notChosen != null)
        {
            _notChosen.SetActive(!chosen);
        }
    }

    /// <summary>
    /// 고른 트레이너와 잠기지 않은 엔트리 칸을 스토리지 포켓몬 칸처럼 위아래로 끊어 움직인다.
    /// 빈 칸은 빈 칸 그림이 움직이고, 채워진 칸은 포켓몬 아이콘이 움직이며 프레임도 넘긴다.
    /// </summary>
    public void TickBob(float deltaTime)
    {
        EnsureState();
        _bobTimer += deltaTime;
        while (_bobTimer >= StorageMonsterView.DEFAULT_BOB_INTERVAL)
        {
            _bobTimer -= StorageMonsterView.DEFAULT_BOB_INTERVAL;
            StepBob();
        }
    }

    /// <summary>
    /// 잠기지 않은 엔트리 칸 중 채워진 첫 포켓몬을 돌려준다.
    /// </summary>
    public bool TryGetFirstMonster(out MonsterVisualData data)
    {
        EnsureState();
        for (var i = 0; i < _visuals.Length; i++)
        {
            if (IsLocked(i) || _visuals[i] == null)
            {
                continue;
            }

            data = _visuals[i];
            return true;
        }

        data = null;
        return false;
    }

    public bool IsLocked(int index)
    {
        return _locks != null
            && index >= 0
            && index < _locks.Length
            && _locks[index] != null;
    }

    /// <summary>
    /// 잠기지 않은 칸에 포켓몬이 들어 있는지 본다.
    /// </summary>
    public bool HasMonster(int index)
    {
        return !IsLocked(index) && index >= 0 && index < _visuals.Length && _visuals[index] != null;
    }

    public void ClearMonsters()
    {
        EnsureState();
        for (var i = 0; i < _visuals.Length; i++)
        {
            _visuals[i] = null;
            ApplySprite(i, null);
        }
    }

    /// <summary>
    /// 채워진 마지막 칸을 비운다. 비울 칸이 없으면 false.
    /// </summary>
    public bool ClearLastMonster()
    {
        EnsureState();
        for (var i = _visuals.Length - 1; i >= 0; i--)
        {
            if (IsLocked(i) || _visuals[i] == null)
            {
                continue;
            }

            _visuals[i] = null;
            ApplySprite(i, null);
            return true;
        }

        return false;
    }

    public void ReleaseMonster(int index)
    {
        EnsureState();
        if (!HasMonster(index))
        {
            return;
        }

        _visuals[index] = null;
        ApplySprite(index, null);
    }

    /// <summary>
    /// 잠기지 않은 빈 칸 중 첫 칸에 포켓몬을 넣는다. 빈 칸이 없으면 false.
    /// </summary>
    public bool TryFill(MonsterVisualData data)
    {
        EnsureState();
        for (var i = 0; i < _visuals.Length; i++)
        {
            if (IsLocked(i) || _visuals[i] != null)
            {
                continue;
            }

            _visuals[i] = data;
            ApplySprite(i, data != null ? MonsterVisualData.FirstFrame(data.Icon) : null);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 트레이너가 골라져 있고 잠기지 않은 엔트리 칸이 모두 찼는지 본다.
    /// </summary>
    public bool IsFull()
    {
        if (Character == null)
        {
            return false;
        }

        EnsureState();
        for (var i = 0; i < _visuals.Length; i++)
        {
            if (!IsLocked(i) && _visuals[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private void EnsureState()
    {
        var count = _monsterImages != null ? _monsterImages.Length : 0;
        if (_visuals.Length == count
            && _icons.Length == count
            && _empties.Length == count
            && _frameIndices.Length == count)
        {
            ShowLocks();
            return;
        }

        _visuals = new MonsterVisualData[count];
        _icons = new Image[count];
        _empties = new Image[count];
        _frameIndices = new int[count];
        for (var i = 0; i < count; i++)
        {
            var image = _monsterImages[i];
            if (image == null || IsLocked(i))
            {
                continue;
            }

            _icons[i] = CreateIcon(image);
            _empties[i] = CreateEmpty(image);
            ApplySprite(i, null);
        }

        ShowLocks();
    }

    /// <summary>
    /// 칸 안에 포켓몬 아이콘용 자식 Image를 만든다. 칸 크기와 select 테두리는 그대로 두고 아이콘만 스토리지와 같은 크기로 그린다.
    /// </summary>
    private static Image CreateIcon(Image slot)
    {
        var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(slot.transform, false);
        // 맨 앞 형제로 두어 select 테두리와 잠금 표시가 아이콘 위에 그려지게 한다.
        rect.SetAsFirstSibling();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        var icon = go.GetComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = false;
        go.SetActive(false);
        return icon;
    }

    /// <summary>
    /// 칸의 빈 칸 그림을 그대로 옮긴 자식 Image를 만든다. 칸은 제자리에서 마우스를 받고 이 그림만 위아래로 움직인다.
    /// </summary>
    private static Image CreateEmpty(Image slot)
    {
        var go = new GameObject("Empty", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(slot.transform, false);
        // 아이콘처럼 맨 앞 형제로 두어 칸의 다른 자식이 위에 그려지게 한다.
        rect.SetAsFirstSibling();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = slot.rectTransform.pivot;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var empty = go.GetComponent<Image>();
        empty.raycastTarget = false;
        empty.sprite = slot.sprite;
        empty.color = slot.color;
        empty.material = slot.material;
        empty.type = slot.type;
        empty.preserveAspect = slot.preserveAspect;
        empty.fillCenter = slot.fillCenter;
        empty.pixelsPerUnitMultiplier = slot.pixelsPerUnitMultiplier;
        return empty;
    }

    private void StepBob()
    {
        _bobUp = !_bobUp;
        if (Character != null && _chooseImage != null)
        {
            _chooseImage.rectTransform.anchoredPosition = _chooseBasePosition + BobPosition();
        }

        for (var i = 0; i < _visuals.Length; i++)
        {
            var empty = _empties[i];
            if (!IsLocked(i) && empty != null)
            {
                empty.rectTransform.anchoredPosition = BobPosition();
            }

            var icon = _icons[i];
            if (!HasMonster(i) || icon == null)
            {
                continue;
            }

            icon.rectTransform.anchoredPosition = BobPosition();

            var frames = _visuals[i].Icon;
            var next = StorageMonsterView.NextFrameIndex(frames, _frameIndices[i]);
            if (next < 0 || next == _frameIndices[i])
            {
                continue;
            }

            _frameIndices[i] = next;
            icon.sprite = frames[next];
            icon.rectTransform.sizeDelta = StorageMonsterView.IconSize(frames[next]);
        }
    }

    private Vector2 BobPosition()
    {
        return new Vector2(0f, _bobUp ? StorageMonsterView.BobHeight(StorageMonsterView.DEFAULT_BOB_PIXELS) : 0f);
    }

    private void ShowLocks()
    {
        if (_locks == null)
        {
            return;
        }

        for (var i = 0; i < _locks.Length; i++)
        {
            if (_locks[i] != null)
            {
                _locks[i].SetActive(true);
            }
        }
    }

    private void ApplySprite(int index, Sprite portrait)
    {
        if (IsLocked(index) || _monsterImages == null || index < 0 || index >= _monsterImages.Length)
        {
            return;
        }

        var image = _monsterImages[index];
        if (image == null)
        {
            return;
        }

        // 칸은 늘 투명하게 두고 빈 칸 그림이나 아이콘 자식만 보인다. 투명해도 마우스는 받는다.
        var filled = portrait != null;
        image.color = Color.clear;

        var empty = _empties[index];
        if (empty != null)
        {
            empty.gameObject.SetActive(!filled);
            empty.rectTransform.anchoredPosition = BobPosition();
        }

        var icon = _icons[index];
        if (icon == null)
        {
            return;
        }

        icon.gameObject.SetActive(filled);
        icon.sprite = portrait;
        icon.rectTransform.sizeDelta = StorageMonsterView.IconSize(portrait);
        // 새로 넣은 포켓몬은 첫 프레임에서 시작해 다른 칸과 같은 높이로 맞춘다.
        icon.rectTransform.anchoredPosition = filled ? BobPosition() : Vector2.zero;
        _frameIndices[index] = filled && _visuals[index] != null
            ? StorageMonsterView.NextFrameIndex(_visuals[index].Icon, -1)
            : -1;
    }
}
