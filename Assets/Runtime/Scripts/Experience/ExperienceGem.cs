using UnityEngine;

/// <summary>
/// 적이 떨어뜨리는 경험치 구슬. 경험치량과 프레임 그림은 프리팹이 가진다.
/// </summary>
public class ExperienceGem : MonoBehaviour
{
    private const int SORTING_ORDER = 2;
    private const int SPRITE_SIZE = 16;
    private const float PIXELS_PER_UNIT = 16f;
    private const float DEFAULT_FRAMES_PER_SECOND = 8f;

    private static Sprite _sharedSprite;

    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private float _xp = 1f;

    [Header("Animation")]
    [SerializeField] private Sprite[] _frames;
    [SerializeField] private float _framesPerSecond = DEFAULT_FRAMES_PER_SECOND;

    private ExperienceSound _sound;
    private int _frameIndex;
    private float _frameTimer;

    public float Xp => _xp;

    public bool IsAttracting { get; private set; }

    #region Unity Methods

    private void Awake()
    {
        CacheRenderer();
        _sound = GetComponent<ExperienceSound>();
    }

    private void OnEnable()
    {
        RestartAnimation();
    }

    private void Update()
    {
        AdvanceFrames(Time.deltaTime);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 바닥에 구슬을 두고 획득 전 상태로 되돌린다.
    /// </summary>
    public void Prepare(Vector2 position)
    {
        IsAttracting = false;
        transform.position = position;
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            return;
        }

        RestartAnimation();
    }

    /// <summary>
    /// 자석 범위에 들어왔으므로 플레이어를 계속 따라간다.
    /// </summary>
    public void BeginAttract()
    {
        IsAttracting = true;
    }

    /// <summary>
    /// 목표 지점까지 한 스텝 이동한다.
    /// </summary>
    public void MoveToward(Vector2 target, float step)
    {
        var current = (Vector2)transform.position;
        transform.position = Vector2.MoveTowards(current, target, step);
    }

    /// <summary>
    /// 목표와의 거리 제곱을 돌려준다.
    /// </summary>
    public float GetDistanceSqr(Vector2 target)
    {
        return ((Vector2)transform.position - target).sqrMagnitude;
    }

    /// <summary>
    /// 프리팹에 넣은 획득음을 재생한다.
    /// </summary>
    public void PlayPickupSound()
    {
        if (_sound == null)
        {
            _sound = GetComponent<ExperienceSound>();
        }

        if (_sound != null)
        {
            _sound.Play();
        }
    }

    /// <summary>
    /// 풀로 되돌린다.
    /// </summary>
    public void Release()
    {
        IsAttracting = false;
        gameObject.SetActive(false);
    }

    #endregion

    #region Private Methods

    private void RestartAnimation()
    {
        _frameIndex = 0;
        _frameTimer = 0f;
        ApplyFrame();
    }

    private void AdvanceFrames(float deltaTime)
    {
        if (!HasFrames() || _frames.Length <= 1 || _framesPerSecond <= 0f)
        {
            return;
        }

        _frameTimer += deltaTime;
        var frameDuration = 1f / _framesPerSecond;
        var changed = false;
        while (_frameTimer >= frameDuration)
        {
            _frameTimer -= frameDuration;
            _frameIndex = (_frameIndex + 1) % _frames.Length;
            changed = true;
        }

        if (changed)
        {
            ApplyFrame();
        }
    }

    private void ApplyFrame()
    {
        CacheRenderer();
        if (_spriteRenderer == null)
        {
            return;
        }

        _spriteRenderer.sortingOrder = SORTING_ORDER;
        if (!HasFrames())
        {
            if (_spriteRenderer.sprite == null)
            {
                _spriteRenderer.sprite = GetSharedSprite();
            }

            return;
        }

        if (_frameIndex < 0 || _frameIndex >= _frames.Length)
        {
            _frameIndex = 0;
        }

        var sprite = _frames[_frameIndex];
        if (sprite == null)
        {
            return;
        }

        _spriteRenderer.sprite = sprite;
        _spriteRenderer.color = Color.white;
    }

    private bool HasFrames()
    {
        return _frames != null && _frames.Length > 0;
    }

    private void CacheRenderer()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private static Sprite GetSharedSprite()
    {
        if (_sharedSprite != null)
        {
            return _sharedSprite;
        }

        var texture = new Texture2D(SPRITE_SIZE, SPRITE_SIZE, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.filterMode = FilterMode.Point;
        var center = (SPRITE_SIZE - 1) * 0.5f;
        var radius = center;
        for (var y = 0; y < SPRITE_SIZE; y++)
        {
            for (var x = 0; x < SPRITE_SIZE; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                var alpha = 0f;
                if (distance <= radius)
                {
                    alpha = distance <= radius - 1f ? 1f : radius - distance;
                }

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        _sharedSprite = Sprite.Create(texture, new Rect(0f, 0f, SPRITE_SIZE, SPRITE_SIZE), new Vector2(0.5f, 0.5f), PIXELS_PER_UNIT);
        _sharedSprite.hideFlags = HideFlags.HideAndDontSave;
        return _sharedSprite;
    }

    #endregion
}
