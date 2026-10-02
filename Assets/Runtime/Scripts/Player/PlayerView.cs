using DG.Tweening;
using UnityEngine;

/// <summary>
/// 플레이어가 바라보는 네 방향.
/// </summary>
public enum PlayerFacing
{
    Down = 0,
    Up = 1,
    Left = 2,
    Right = 3
}

/// <summary>
/// Idle / Walk 스프라이트를 방향에 맞춰 재생한다. 칸이 비면 자리표시 사각형을 쓴다.
/// </summary>
public class PlayerView : MonoBehaviour
{
    private const float DEFAULT_FRAMES_PER_SECOND = 8f;
    public const int DEATH_SORTING_ORDER = 102;
    private const float DEATH_FADE_DURATION = 0.3f;
    private const float DEATH_BACKGROUND_ALPHA = 0.98f;
    private const float DEATH_BOTTOM_ALPHA = 0.8f;
    private const float HIT_FLASH_DURATION = 0.15f;
    private static readonly Color PLACEHOLDER_COLOR = new Color(0.95f, 0.85f, 0.2f, 1f);
    private static readonly int HIT_EFFECT_BLEND_ID = Shader.PropertyToID("_HitEffectBlend");

    [Header("Renderer")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private SpriteRenderer _deathBackground;
    [SerializeField] private SpriteRenderer _deathBottom;

    [Header("Timing")]
    [SerializeField] private float _framesPerSecond = DEFAULT_FRAMES_PER_SECOND;

    [Header("Idle")]
    [SerializeField] private Sprite[] _idleDown;
    [SerializeField] private Sprite[] _idleUp;
    [SerializeField] private Sprite[] _idleLeft;
    [SerializeField] private Sprite[] _idleRight;

    [Header("Walk")]
    [SerializeField] private Sprite[] _walkDown;
    [SerializeField] private Sprite[] _walkUp;
    [SerializeField] private Sprite[] _walkLeft;
    [SerializeField] private Sprite[] _walkRight;

    private PlayerFacing _facing = PlayerFacing.Right;
    private bool _isMoving;
    private bool _hasVisual;
    private Sprite[] _currentFrames;
    private int _frameIndex;
    private float _frameTimer;
    private Tweener _backgroundFade;
    private Tweener _bottomFade;
    private Tweener _hitFlash;
    private Material _bodyMaterial;

    #region Unity Methods

    private void Awake()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // 이 플레이어만 번쩍이도록 머티리얼 인스턴스를 한 번 만든다.
        if (_spriteRenderer != null)
        {
            _bodyMaterial = _spriteRenderer.material;
        }
    }

    private void Update()
    {
        AdvanceFrames();
    }

    private void OnDisable()
    {
        KillFade(_backgroundFade);
        KillFade(_bottomFade);
        _backgroundFade = null;
        _bottomFade = null;
        StopHitFlash();
    }

    private void OnDestroy()
    {
        if (_bodyMaterial != null)
        {
            Destroy(_bodyMaterial);
            _bodyMaterial = null;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 플레이어블 데이터의 Idle / Walk 스프라이트로 이 뷰의 애니메이션을 바꾼다.
    /// </summary>
    public void ApplyPlayable(PlayableCharacterData data)
    {
        if (data == null)
        {
            return;
        }

        _idleDown = data.IdleDown;
        _idleUp = data.IdleUp;
        _idleLeft = data.IdleLeft;
        _idleRight = data.IdleRight;
        _walkDown = data.WalkDown;
        _walkUp = data.WalkUp;
        _walkLeft = data.WalkLeft;
        _walkRight = data.WalkRight;
        _hasVisual = false;
        SetVisual(_isMoving, FacingToVector(_facing));
    }

    /// <summary>
    /// 이동 여부와 시선으로 Idle / Walk 클립을 고른다. 멈출 때는 마지막 방향을 유지한다.
    /// </summary>
    public void SetVisual(bool isMoving, Vector2 lookDirection)
    {
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            _facing = CalculateFacing(lookDirection);
        }

        var frames = ResolveFrames(isMoving, _facing);
        if (_hasVisual && frames == _currentFrames && isMoving == _isMoving)
        {
            return;
        }

        _hasVisual = true;
        _isMoving = isMoving;
        _currentFrames = frames;
        _frameIndex = 0;
        _frameTimer = 0f;
        ApplyCurrentSprite();
    }

    /// <summary>
    /// 화면을 어둡게 덮고 발밑 원만 밝혀, 캐릭터 스프라이트가 그 위에 남게 한다.
    /// 사망 직후 시간이 멈추므로 페이드는 스케일을 무시한다.
    /// </summary>
    public void ShowDeathLight()
    {
        StopHitFlash();

        if (_spriteRenderer != null)
        {
            _spriteRenderer.sortingOrder = DEATH_SORTING_ORDER;
        }

        _backgroundFade = FadeDeathSprite(_deathBackground, DEATH_BACKGROUND_ALPHA, _backgroundFade);
        _bottomFade = FadeDeathSprite(_deathBottom, DEATH_BOTTOM_ALPHA, _bottomFade);
    }

    /// <summary>
    /// 피격 순간 Hit Effect를 가득 켰다가 짧게 줄인다. 연달아 맞으면 처음부터 다시 시작한다.
    /// 맞은 직후 시간이 멈춰도 굳지 않도록 스케일을 무시한다.
    /// </summary>
    public void PlayHitFlash()
    {
        if (!HasHitEffect())
        {
            return;
        }

        KillFade(_hitFlash);
        _bodyMaterial.SetFloat(HIT_EFFECT_BLEND_ID, 1f);
        _hitFlash = _bodyMaterial.DOFloat(0f, HIT_EFFECT_BLEND_ID, HIT_FLASH_DURATION).SetUpdate(true);
    }

    #endregion

    #region Private Methods

    private bool HasHitEffect()
    {
        // 대체 생성 경로는 기본 스프라이트 머티리얼이라 Hit Effect가 없다.
        return _bodyMaterial != null && _bodyMaterial.HasProperty(HIT_EFFECT_BLEND_ID);
    }

    private void StopHitFlash()
    {
        KillFade(_hitFlash);
        _hitFlash = null;
        if (HasHitEffect())
        {
            _bodyMaterial.SetFloat(HIT_EFFECT_BLEND_ID, 0f);
        }
    }

    private Tweener FadeDeathSprite(SpriteRenderer renderer, float alpha, Tweener current)
    {
        if (renderer == null)
        {
            Debug.LogError("사망 조명 스프라이트가 없습니다.");
            return null;
        }

        KillFade(current);
        renderer.gameObject.SetActive(true);
        var color = renderer.color;
        color.a = 0f;
        renderer.color = color;
        return renderer.DOFade(alpha, DEATH_FADE_DURATION).SetUpdate(true);
    }

    private static void KillFade(Tweener tween)
    {
        if (tween == null)
        {
            return;
        }

        tween.Kill();
    }

    private void AdvanceFrames()
    {
        if (_currentFrames == null || _currentFrames.Length <= 1 || _framesPerSecond <= 0f)
        {
            return;
        }

        _frameTimer += Time.deltaTime;
        var frameDuration = 1f / _framesPerSecond;
        var changed = false;
        while (_frameTimer >= frameDuration)
        {
            _frameTimer -= frameDuration;
            _frameIndex = (_frameIndex + 1) % _currentFrames.Length;
            changed = true;
        }

        if (changed)
        {
            ApplyCurrentSprite();
        }
    }

    private void ApplyCurrentSprite()
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        var sprite = GetFrameSprite();
        if (sprite == null)
        {
            _spriteRenderer.sprite = PrototypeSprite.WhiteSquare;
            _spriteRenderer.color = PLACEHOLDER_COLOR;
            return;
        }

        _spriteRenderer.sprite = sprite;
        _spriteRenderer.color = Color.white;
    }

    private Sprite GetFrameSprite()
    {
        if (_currentFrames == null || _currentFrames.Length == 0)
        {
            return null;
        }

        if (_frameIndex < 0 || _frameIndex >= _currentFrames.Length)
        {
            return null;
        }

        return _currentFrames[_frameIndex];
    }

    private Sprite[] ResolveFrames(bool isMoving, PlayerFacing facing)
    {
        var primary = GetFrames(isMoving, facing);
        if (HasFrames(primary))
        {
            return primary;
        }

        if (isMoving)
        {
            var idle = GetFrames(false, facing);
            if (HasFrames(idle))
            {
                return idle;
            }
        }

        if (HasFrames(_idleDown))
        {
            return _idleDown;
        }

        return null;
    }

    private Sprite[] GetFrames(bool isMoving, PlayerFacing facing)
    {
        if (isMoving)
        {
            switch (facing)
            {
                case PlayerFacing.Down:
                    return _walkDown;
                case PlayerFacing.Up:
                    return _walkUp;
                case PlayerFacing.Left:
                    return _walkLeft;
                default:
                    return _walkRight;
            }
        }

        switch (facing)
        {
            case PlayerFacing.Down:
                return _idleDown;
            case PlayerFacing.Up:
                return _idleUp;
            case PlayerFacing.Left:
                return _idleLeft;
            default:
                return _idleRight;
        }
    }

    private static bool HasFrames(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private static Vector2 FacingToVector(PlayerFacing facing)
    {
        switch (facing)
        {
            case PlayerFacing.Down:
                return Vector2.down;
            case PlayerFacing.Up:
                return Vector2.up;
            case PlayerFacing.Left:
                return Vector2.left;
            default:
                return Vector2.right;
        }
    }

    /// <summary>
    /// 대각선은 절댓값이 큰 축을 쓴다. 같으면 위아래를 고른다.
    /// </summary>
    private static PlayerFacing CalculateFacing(Vector2 lookDirection)
    {
        if (Mathf.Abs(lookDirection.x) > Mathf.Abs(lookDirection.y))
        {
            return lookDirection.x < 0f ? PlayerFacing.Left : PlayerFacing.Right;
        }

        return lookDirection.y < 0f ? PlayerFacing.Down : PlayerFacing.Up;
    }

    #endregion
}
