using UnityEngine;

/// <summary>
/// 플레이어 주위를 돌며 닿은 적에게 한 번 피해를 준다.
/// </summary>
public class GrassTypeAttackProjectileBehavior : MonoBehaviour
{
    private const float SHOW_SECONDS = 0.5f;
    private const float MIN_SCALE = 0.01f;

    [SerializeField] private CircleCollider2D hitCollider;

    private int _playerId;
    private float _damageMultiplier;
    private MonsterType _attackType;
    private float _scale;
    private float _scaleTarget;
    private bool _shown;
    private bool _hiding;

    #region Unity Methods

    private void Awake()
    {
        if (hitCollider == null)
        {
            hitCollider = GetComponent<CircleCollider2D>();
        }
    }

    private void Update()
    {
        if (!_shown || WeaponAbilityManager.IsCombatPaused())
        {
            return;
        }

        var speed = Mathf.Max(MIN_SCALE, _scaleTarget) / SHOW_SECONDS;
        if (_hiding)
        {
            speed = Mathf.Max(MIN_SCALE, _scale) / SHOW_SECONDS;
        }

        _scale = Mathf.MoveTowards(_scale, _scaleTarget, speed * Time.deltaTime);
        transform.localScale = Vector3.one * _scale;
        if (_hiding && _scale <= 0f)
        {
            Deactivate();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_shown || _hiding || WeaponAbilityManager.IsCombatPaused())
        {
            return;
        }

        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null || Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return;
        }

        if (!playerManager.TryGetPlayer(_playerId, out var player))
        {
            return;
        }

        enemy.ApplyDamage(_damageMultiplier * player.damageMultiplier, _attackType);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 크기를 키우며 피해를 줄 수 있게 한다.
    /// </summary>
    public void Show(int playerId, float damageMultiplier, MonsterType attackType, float sizeMultiplier)
    {
        _playerId = playerId;
        _damageMultiplier = damageMultiplier;
        _attackType = attackType;
        _shown = true;
        _hiding = false;
        _scale = 0f;
        _scaleTarget = Mathf.Max(MIN_SCALE, sizeMultiplier);
        transform.localScale = Vector3.zero;
        if (hitCollider != null)
        {
            hitCollider.enabled = true;
        }
    }

    /// <summary>
    /// 크기를 줄인 뒤 끈다.
    /// </summary>
    public void Hide()
    {
        if (!_shown)
        {
            return;
        }

        _hiding = true;
        _scaleTarget = 0f;
    }

    /// <summary>
    /// 바로 끄고 풀로 되돌린다.
    /// </summary>
    public void Clear()
    {
        Deactivate();
    }

    #endregion

    #region Private Methods

    private void Deactivate()
    {
        _shown = false;
        _hiding = false;
        _scale = 0f;
        transform.localScale = Vector3.one;
        if (hitCollider != null)
        {
            hitCollider.enabled = false;
        }

        gameObject.SetActive(false);
    }

    #endregion
}
