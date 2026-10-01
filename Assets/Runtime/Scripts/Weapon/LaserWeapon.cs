using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가장 가까운 적 방향으로 관통 레이저를 쏜다. 맞은 자리에서 멈추지 않고 사거리 끝까지 간다.
/// </summary>
public class LaserWeapon : Weapon
{
    private const float DEFAULT_BEAM_DURATION = 0.18f;

    [Header("Beam")]
    [SerializeField] private LaserBeam _beamPrefab;
    [SerializeField] private float _beamDuration = DEFAULT_BEAM_DURATION;

    private readonly List<LaserBeam> _beams = new List<LaserBeam>();
    private Transform _beamRoot;

    #region Protected Methods

    /// <summary>
    /// 조준 방향으로 빔 하나를 깐다.
    /// </summary>
    protected override void LaunchOne(Vector2 origin, Vector2 direction)
    {
        var beam = GetBeam();
        beam.Fire(origin, direction, Stats.AttackRange, Stats.HitRadius, _beamDuration, Stats.Damage, AttackType);
    }

    /// <summary>
    /// 켜져 있는 빔의 표시 시간을 줄인다.
    /// </summary>
    protected override void TickShots(float deltaTime)
    {
        for (var i = 0; i < _beams.Count; i++)
        {
            var beam = _beams[i];
            if (beam != null && beam.IsActive)
            {
                beam.Tick(deltaTime);
            }
        }
    }

    /// <summary>
    /// 켜져 있던 빔을 끈다.
    /// </summary>
    protected override void DismissActiveShots()
    {
        for (var i = 0; i < _beams.Count; i++)
        {
            var beam = _beams[i];
            if (beam != null && beam.IsActive)
            {
                beam.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 빔 뿌리를 지운다.
    /// </summary>
    protected override void ClearShots()
    {
        if (_beamRoot != null)
        {
            Destroy(_beamRoot.gameObject);
            _beamRoot = null;
        }

        _beams.Clear();
    }

    /// <summary>
    /// 빔을 무기 밖 뿌리에 둔다. 무기 크기에 같이 늘어나지 않게 한다.
    /// </summary>
    protected override void OnInitialized()
    {
        EnsureBeamRoot();
    }

    #endregion

    #region Private Methods

    private LaserBeam GetBeam()
    {
        for (var i = 0; i < _beams.Count; i++)
        {
            var beam = _beams[i];
            if (beam != null && !beam.IsActive)
            {
                return beam;
            }
        }

        EnsureBeamRoot();
        var created = CreateBeam();
        _beams.Add(created);
        return created;
    }

    private LaserBeam CreateBeam()
    {
        if (_beamPrefab == null)
        {
            return LaserBeam.Create(_beamRoot);
        }

        var beam = Instantiate(_beamPrefab, _beamRoot);
        beam.gameObject.SetActive(false);
        return beam;
    }

    private void EnsureBeamRoot()
    {
        if (_beamRoot != null)
        {
            return;
        }

        var rootObject = new GameObject("WeaponBeams");
        _beamRoot = rootObject.transform;
    }

    #endregion
}
