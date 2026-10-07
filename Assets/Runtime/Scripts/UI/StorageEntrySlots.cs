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
    private MonsterVisualData[] _visuals = System.Array.Empty<MonsterVisualData>();
    private Image[] _icons = System.Array.Empty<Image>();
    private Sprite[] _defaultSprites = System.Array.Empty<Sprite>();
    private Color[] _defaultColors = System.Array.Empty<Color>();

    public StorageEntrySlots(Image[] monsterImages, GameObject[] locks, GameObject choose, Image chooseImage, GameObject notChosen)
    {
        _monsterImages = monsterImages;
        _locks = locks;
        _choose = choose;
        _chooseImage = chooseImage;
        _notChosen = notChosen;
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

        if (chosen && _chooseImage != null)
        {
            _chooseImage.sprite = data.InGameSprite;
        }

        if (_notChosen != null)
        {
            _notChosen.SetActive(!chosen);
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
            && _defaultSprites.Length == count
            && _defaultColors.Length == count)
        {
            ShowLocks();
            return;
        }

        _visuals = new MonsterVisualData[count];
        _icons = new Image[count];
        _defaultSprites = new Sprite[count];
        _defaultColors = new Color[count];
        for (var i = 0; i < count; i++)
        {
            var image = _monsterImages[i];
            _defaultSprites[i] = image != null ? image.sprite : null;
            _defaultColors[i] = image != null ? image.color : Color.white;
            _icons[i] = image != null ? CreateIcon(image) : null;
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

        // 채운 칸은 투명하게 두고 아이콘 자식만 보인다. 투명해도 마우스는 받는다.
        var filled = portrait != null;
        image.sprite = _defaultSprites[index];
        image.color = filled ? Color.clear : _defaultColors[index];

        var icon = _icons[index];
        if (icon == null)
        {
            return;
        }

        icon.gameObject.SetActive(filled);
        icon.sprite = portrait;
        icon.rectTransform.sizeDelta = StorageMonsterView.IconSize(portrait);
    }
}
