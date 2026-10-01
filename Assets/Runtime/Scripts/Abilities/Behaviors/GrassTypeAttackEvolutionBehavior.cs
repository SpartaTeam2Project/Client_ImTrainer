using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 풀 타입 진화. 별이 플레이어 주위를 계속 돈다.
/// </summary>
public class GrassTypeAttackEvolutionBehavior : AbilityBehavior<GrassTypeAttackEvolutionData, GrassTypeAttackEvolutionLevel>
{
    private const float MIN_MULTIPLIER = 0.01f;
    private const float RADIUS_BLEND_SECONDS = 0.5f;

    [SerializeField] private GameObject starPrefab;

    private StageObjectPool<GrassTypeAttackProjectileBehavior> _pool;
    private readonly List<GrassTypeAttackProjectileBehavior> _stars = new List<GrassTypeAttackProjectileBehavior>();
    private float _angle;
    private float _radiusMultiplier;
    private float _radiusTarget;

    #region Unity Methods

    private void Awake()
    {
        _pool = new StageObjectPool<GrassTypeAttackProjectileBehavior>(starPrefab, transform);
    }

    private void LateUpdate()
    {
        if (AbilityLevel == null || !TryGetPlayer(out var player))
        {
            return;
        }

        if (AbilityManager.IsCombatPaused())
        {
            return;
        }

        transform.position = player.transform.position;
        var blendSpeed = 1f / RADIUS_BLEND_SECONDS;
        _radiusMultiplier = Mathf.MoveTowards(_radiusMultiplier, _radiusTarget, blendSpeed * Time.deltaTime);
        _angle += AbilityLevel.AngularSpeed * Mathf.Max(MIN_MULTIPLIER, player.projectileSpeedMultiplier) * Time.deltaTime;
        PlaceStars(player.sizeMultiplier);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 돌고 있던 별을 치운다.
    /// </summary>
    public override void Clear()
    {
        ClearStars();
        _pool?.DestroyAll();
        base.Clear();
    }

    #endregion

    #region Private Methods

    protected override void SetAbilityLevel(int levelId)
    {
        base.SetAbilityLevel(levelId);
        ClearStars();
        if (_pool == null || AbilityLevel == null || !TryGetPlayer(out var player))
        {
            return;
        }

        var attackType = ResolveAttackType();
        var count = Mathf.Max(0, AbilityLevel.ProjectilesCount);
        for (var i = 0; i < count; i++)
        {
            var star = _pool.Get();
            if (star == null)
            {
                continue;
            }

            star.Show(PlayerId, AbilityLevel.Damage, attackType, player.sizeMultiplier);
            _stars.Add(star);
        }

        _radiusMultiplier = 0f;
        _radiusTarget = 1f;
    }

    private void ClearStars()
    {
        for (var i = 0; i < _stars.Count; i++)
        {
            if (_stars[i] != null)
            {
                _stars[i].Clear();
            }
        }

        _stars.Clear();
    }

    private void PlaceStars(float sizeMultiplier)
    {
        var count = _stars.Count;
        if (count == 0 || AbilityLevel == null)
        {
            return;
        }

        var distance = AbilityLevel.Radius * _radiusMultiplier * sizeMultiplier;
        for (var i = 0; i < count; i++)
        {
            if (_stars[i] == null)
            {
                continue;
            }

            var projectileAngle = 360f / count * i + _angle;
            _stars[i].transform.localPosition = Quaternion.Euler(0f, 0f, projectileAngle) * Vector3.up * distance;
        }
    }

    #endregion
}
