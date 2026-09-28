using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 조준 방향으로 사거리 끝까지 깔리는 직선 빔. 선 위의 적에게 발사 순간 한 번씩 맞는다.
/// </summary>
public class LaserBeam : MonoBehaviour
{
    private const float DIRECTION_SQR_EPSILON = 0.0001f;
    private const float MIN_VISUAL_THICKNESS = 0.08f;
    private const int BEAM_SORTING_ORDER = 12;
    private static readonly Color BEAM_COLOR = new Color(0.4f, 0.95f, 1f, 0.9f);

    private readonly List<Enemy> _hits = new List<Enemy>();

    private SpriteRenderer _renderer;
    private float _remaining;

    public bool IsActive => gameObject.activeSelf;

    #region Unity Methods

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer == null)
        {
            return;
        }

        if (_renderer.sprite == null)
        {
            _renderer.sprite = PrototypeSprite.WhiteSquare;
            _renderer.color = BEAM_COLOR;
        }

        _renderer.sortingOrder = BEAM_SORTING_ORDER;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 자리표시 빔을 만들어 꺼 둔다.
    /// </summary>
    public static LaserBeam Create(Transform parent)
    {
        var actorObject = new GameObject("LaserBeam");
        actorObject.transform.SetParent(parent, false);

        var renderer = actorObject.AddComponent<SpriteRenderer>();
        renderer.sprite = PrototypeSprite.WhiteSquare;
        renderer.color = BEAM_COLOR;
        renderer.sortingOrder = BEAM_SORTING_ORDER;

        var beam = actorObject.AddComponent<LaserBeam>();
        actorObject.SetActive(false);
        return beam;
    }

    /// <summary>
    /// 선분을 깔고, 두께 안의 적에게 한 번 피해를 준다. 보이는 동안에는 다시 맞지 않는다.
    /// </summary>
    public void Fire(Vector2 origin, Vector2 direction, float length, float thickness, float duration, float damage, MonsterType attackType)
    {
        var forward = direction.sqrMagnitude > DIRECTION_SQR_EPSILON ? direction.normalized : Vector2.right;
        var beamLength = Mathf.Max(0f, length);
        var midpoint = origin + forward * (beamLength * 0.5f);
        var angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;

        transform.SetPositionAndRotation(midpoint, Quaternion.Euler(0f, 0f, angle));
        transform.localScale = new Vector3(beamLength, Mathf.Max(MIN_VISUAL_THICKNESS, thickness * 2f), 1f);
        _remaining = Mathf.Max(0f, duration);
        gameObject.SetActive(true);
        ApplyDamageAlong(origin, forward, beamLength, thickness, damage, attackType);
    }

    /// <summary>
    /// 표시 시간이 끝나면 끈다.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (!IsActive)
        {
            return;
        }

        _remaining -= deltaTime;
        if (_remaining <= 0f)
        {
            gameObject.SetActive(false);
        }
    }

    #endregion

    #region Private Methods

    private void ApplyDamageAlong(Vector2 origin, Vector2 forward, float length, float thickness, float damage, MonsterType attackType)
    {
        if (damage <= 0f || Managers.Instance == null || !Managers.Instance.TryGetManager<EnemyManager>(out var enemyManager))
        {
            return;
        }

        enemyManager.CollectAlongSegment(origin, forward, length, thickness, _hits);
        for (var i = 0; i < _hits.Count; i++)
        {
            var enemy = _hits[i];
            if (enemy != null && enemy.IsAlive)
            {
                enemy.ApplyDamage(damage, attackType);
            }
        }
    }

    #endregion
}
