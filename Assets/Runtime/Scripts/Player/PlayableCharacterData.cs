using UnityEngine;

/// <summary>
/// 고를 수 있는 트레이너 한 명. 인트로 획득 대상이고 세대와 성별이 같으면 인트로에서 획득한다.
/// </summary>
[CreateAssetMenu(fileName = "PlayableCharacter", menuName = "Player/Playable Character")]
public class PlayableCharacterData : ScriptableObject
{
    private const int MIN_GENERATION = 1;
    private const float DEFAULT_PORTRAIT_FRAME_RATE = 10f;
#if UNITY_EDITOR
    // 그림 경계 바깥 여백. 유니티 자동 자르기와 같은 1픽셀이라 _portraitSize 값을 그대로 쓴다.
    private const int PORTRAIT_AREA_PADDING = 1;
#endif

    [SerializeField] private string _characterName = string.Empty;
    [SerializeField, Min(MIN_GENERATION)] private int _generation = MIN_GENERATION;
    [SerializeField] private TrainerGender _gender = TrainerGender.Boy;
    [SerializeField] private string _unlockCondition = string.Empty;
    [SerializeField] private bool _unlockedByIntro;
    [SerializeField] private Sprite _inGameSprite;
    [SerializeField] private Sprite _portrait;
    [SerializeField] private Vector2 _portraitSize;
    // _portrait 칸 안에서 그림이 있는 영역. 픽셀 단위이고 칸 왼쪽 아래가 원점이다. 에디터가 _portrait를 바꿀 때 채운다.
    [SerializeField, HideInInspector] private Rect _portraitArea;

    [Header("Portrait Animation")]
    [Tooltip("보관함에서 포커스되면 한 번 재생하는 프레임. 비어 있으면 에디터가 초상화 시트 순서(위 줄부터, 왼쪽부터)로 채운다.")]
    [SerializeField] private Sprite[] _portraitFrames = System.Array.Empty<Sprite>();
    [Tooltip("초당 프레임 수. 0이면 기본값 10.")]
    [SerializeField, Min(0f)] private float _portraitFrameRate;

    [Header("Versus")]
    [Tooltip("보스 VS 화면 왼쪽에 나오는 등 사진.")]
    [SerializeField] private Sprite _versusBack;

    [Header("Idle")]
    [SerializeField] private Sprite[] _idleDown = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _idleUp = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _idleLeft = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _idleRight = System.Array.Empty<Sprite>();

    [Header("Walk")]
    [SerializeField] private Sprite[] _walkDown = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _walkUp = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _walkLeft = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _walkRight = System.Array.Empty<Sprite>();

    [System.NonSerialized] private Sprite _croppedPortrait;

    public string CharacterName => _characterName ?? string.Empty;

    public int Generation => _generation < MIN_GENERATION ? MIN_GENERATION : _generation;

    public TrainerGender Gender => _gender;

    public string UnlockCondition => _unlockCondition ?? string.Empty;

    /// <summary>
    /// 인트로에서 세대와 성별을 고르면 바로 얻는 캐릭터면 true.
    /// </summary>
    public bool UnlockedByIntro => _unlockedByIntro;

    public Sprite InGameSprite => _inGameSprite;

    /// <summary>
    /// 초상화 시트는 같은 크기 칸으로 잘려 있어서 그림 둘레에 여백이 있다.
    /// 여백을 뺀 그림 영역만 담은 스프라이트를 돌려준다. 보관함 칸은 아랫변을 발끝으로 쓴다.
    /// </summary>
    public Sprite Portrait
    {
        get
        {
            if (_croppedPortrait == null)
            {
                _croppedPortrait = CropPortrait();
            }

            return _croppedPortrait;
        }
    }

    public Vector2 PortraitSize => _portraitSize;

    /// <summary>
    /// 여백까지 담긴 칸 그대로의 초상화. 애니메이션 프레임과 크기와 기준점이 같다.
    /// </summary>
    public Sprite PortraitSheetFrame => _portrait;

    /// <summary>
    /// 칸 안에서 그림이 있는 영역. 픽셀 단위이고 칸 왼쪽 아래가 원점이다. 재지 않았으면 칸 전체.
    /// </summary>
    public Rect PortraitArea
    {
        get
        {
            if (_portraitArea.width > 0f && _portraitArea.height > 0f)
            {
                return _portraitArea;
            }

            return _portrait != null ? new Rect(Vector2.zero, _portrait.rect.size) : default;
        }
    }

    public Sprite[] PortraitFrames => _portraitFrames ?? System.Array.Empty<Sprite>();

    public float PortraitFrameRate => _portraitFrameRate > 0f ? _portraitFrameRate : DEFAULT_PORTRAIT_FRAME_RATE;

    public Sprite VersusBack => _versusBack;

    public Sprite[] IdleDown => _idleDown;

    public Sprite[] IdleUp => _idleUp;

    public Sprite[] IdleLeft => _idleLeft;

    public Sprite[] IdleRight => _idleRight;

    public Sprite[] WalkDown => _walkDown;

    public Sprite[] WalkUp => _walkUp;

    public Sprite[] WalkLeft => _walkLeft;

    public Sprite[] WalkRight => _walkRight;

    private Sprite CropPortrait()
    {
        if (_portrait == null)
        {
            return null;
        }

        var cell = _portrait.rect;
        var area = _portraitArea;
        if (area.width <= 0f || area.height <= 0f || (area.width >= cell.width && area.height >= cell.height))
        {
            return _portrait;
        }

        // 같은 텍스처의 일부를 가리키는 스프라이트라 텍스처를 새로 만들지 않는다.
        var rect = new Rect(cell.x + area.x, cell.y + area.y, area.width, area.height);
        var sprite = Sprite.Create(_portrait.texture, rect, new Vector2(0.5f, 0.5f), _portrait.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        sprite.name = _portrait.name;
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 초상화 PNG 경로. 시트를 다시 자르면 에디터가 이 경로로 영향 받는 캐릭터를 찾는다.
    /// </summary>
    public string PortraitPath => _portrait != null ? UnityEditor.AssetDatabase.GetAssetPath(_portrait) : string.Empty;

    /// <summary>
    /// 그림 영역을 다시 잰다. 바뀌었으면 저장 대상으로 표시하고 true.
    /// 시트만 다시 잘리면 OnValidate가 불리지 않아서 임포트 후처리도 이 함수를 부른다.
    /// </summary>
    public bool RefreshPortraitArea()
    {
        _croppedPortrait = null;
        var changed = FillPortraitFrames();
        var area = MeasurePortraitArea();
        if (area != _portraitArea)
        {
            _portraitArea = area;
            changed = true;
        }

        if (changed)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }

        return changed;
    }

    /// <summary>
    /// 프레임 목록이 비어 있으면 초상화 시트의 칸을 위 줄부터, 왼쪽부터 채운다. 직접 넣은 목록은 건드리지 않는다.
    /// </summary>
    private bool FillPortraitFrames()
    {
        if (_portrait == null || (_portraitFrames != null && _portraitFrames.Length > 0))
        {
            return false;
        }

        var path = UnityEditor.AssetDatabase.GetAssetPath(_portrait);
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        var frames = new System.Collections.Generic.List<Sprite>();
        foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
        {
            if (asset is Sprite sprite)
            {
                frames.Add(sprite);
            }
        }

        if (frames.Count == 0)
        {
            return false;
        }

        // 유니티 좌표는 아래가 0이라 y가 큰 칸이 위 줄이다.
        frames.Sort((a, b) =>
        {
            var row = b.rect.y.CompareTo(a.rect.y);
            return row != 0 ? row : a.rect.x.CompareTo(b.rect.x);
        });
        _portraitFrames = frames.ToArray();
        return true;
    }

    private void OnValidate()
    {
        RefreshPortraitArea();
    }

    /// <summary>
    /// 원본 PNG를 읽어서 칸 안의 불투명 픽셀 경계에 여백을 더해 잰다. 텍스처가 Read/Write 꺼져 있어도 된다.
    /// </summary>
    private Rect MeasurePortraitArea()
    {
        if (_portrait == null)
        {
            return default;
        }

        var path = UnityEditor.AssetDatabase.GetAssetPath(_portrait);
        if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
        {
            return _portraitArea;
        }

        var texture = new Texture2D(2, 2);
        try
        {
            if (!texture.LoadImage(System.IO.File.ReadAllBytes(path)))
            {
                return _portraitArea;
            }

            var cell = _portrait.rect;
            var cellX = (int)cell.x;
            var cellY = (int)cell.y;
            var width = (int)cell.width;
            var height = (int)cell.height;
            if (cellX + width > texture.width || cellY + height > texture.height)
            {
                return _portraitArea;
            }

            var pixels = texture.GetPixels32();
            int minX = width, minY = height, maxX = -1, maxY = -1;
            for (var y = 0; y < height; y++)
            {
                var row = (cellY + y) * texture.width + cellX;
                for (var x = 0; x < width; x++)
                {
                    if (pixels[row + x].a == 0)
                    {
                        continue;
                    }

                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            if (maxX < 0)
            {
                return default;
            }

            minX = Mathf.Max(0, minX - PORTRAIT_AREA_PADDING);
            minY = Mathf.Max(0, minY - PORTRAIT_AREA_PADDING);
            maxX = Mathf.Min(width - 1, maxX + PORTRAIT_AREA_PADDING);
            maxY = Mathf.Min(height - 1, maxY + PORTRAIT_AREA_PADDING);
            return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
        finally
        {
            DestroyImmediate(texture);
        }
    }
#endif
}
