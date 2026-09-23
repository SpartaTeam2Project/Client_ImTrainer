using UnityEngine;

/// <summary>
/// 적 스프라이트 2장 루프와 좌우 반전을 담당한다. 칸이 비면 자리표시 사각형을 쓴다.
/// </summary>
public class EnemyView : MonoBehaviour
{
    private const float DEFAULT_FRAMES_PER_SECOND = 8f;
    private const float FLIP_X_EPSILON = 0.0001f;
    private static readonly Color PLACEHOLDER_COLOR = new Color(0.85f, 0.2f, 0.25f, 1f);

    [Header("Renderer")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private bool _spriteFacesRight = false;

    [Header("Timing")]
    [SerializeField] private float _framesPerSecond = DEFAULT_FRAMES_PER_SECOND;

    [Header("Walk")]
    [SerializeField] private Sprite[] _walkSprites;

    private bool _isMoving;
    private bool _hasVisual;
    private bool _facesLeft;
    private int _frameIndex;
    private float _frameTimer;

    #region Unity Methods

    private void Awake()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        AdvanceFrames();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 이동 중이면 걷기 프레임을 돌리고, 좌우 입력이 있으면 그쪽으로 뒤집는다.
    /// </summary>
    public void SetVisual(bool isMoving, Vector2 lookDirection)
    {
        UpdateFacing(lookDirection);

        if (!_hasVisual || isMoving != _isMoving)
        {
            _hasVisual = true;
            _isMoving = isMoving;
            _frameIndex = 0;
            _frameTimer = 0f;
        }

        ApplyCurrentSprite();
        ApplyFlip();
    }

    #endregion

    #region Private Methods

    private void UpdateFacing(Vector2 lookDirection)
    {
        if (Mathf.Abs(lookDirection.x) <= FLIP_X_EPSILON)
        {
            return;
        }

        _facesLeft = lookDirection.x < 0f;
    }

    private void AdvanceFrames()
    {
        if (!_isMoving || !HasWalkSprites() || _walkSprites.Length <= 1 || _framesPerSecond <= 0f)
        {
            return;
        }

        _frameTimer += Time.deltaTime;
        var frameDuration = 1f / _framesPerSecond;
        var changed = false;
        while (_frameTimer >= frameDuration)
        {
            _frameTimer -= frameDuration;
            _frameIndex = (_frameIndex + 1) % _walkSprites.Length;
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

        var sprite = GetCurrentSprite();
        if (sprite == null)
        {
            _spriteRenderer.sprite = PrototypeSprite.WhiteSquare;
            _spriteRenderer.color = PLACEHOLDER_COLOR;
            return;
        }

        _spriteRenderer.sprite = sprite;
        _spriteRenderer.color = Color.white;
    }

    private void ApplyFlip()
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        var flipX = _spriteFacesRight ? _facesLeft : !_facesLeft;
        _spriteRenderer.flipX = flipX;
    }

    private Sprite GetCurrentSprite()
    {
        if (!HasWalkSprites())
        {
            return null;
        }

        if (!_isMoving)
        {
            return _walkSprites[0];
        }

        if (_frameIndex < 0 || _frameIndex >= _walkSprites.Length)
        {
            return _walkSprites[0];
        }

        return _walkSprites[_frameIndex];
    }

    private bool HasWalkSprites()
    {
        return _walkSprites != null && _walkSprites.Length > 0 && _walkSprites[0] != null;
    }

    #endregion
}
