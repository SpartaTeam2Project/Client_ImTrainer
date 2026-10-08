using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 상점에서 산 포켓몬이 가방 칸으로 들어가는 빨간 광선. 인벤토리 캔버스 맨 위에 둔다.
/// 연달아 사면 광선 여러 줄기가 같이 날아간다.
/// </summary>
public class PurchaseBeam : MonoBehaviour
{
    public enum BeamStyle
    {
        Projectile,
        Lightning
    }

    private struct ActiveBeam
    {
        public Tween Tween;
        public BeamImagePool Pool;
    }

    [SerializeField] private BeamStyle _style = BeamStyle.Projectile;
    [SerializeField] private ProjectileBeamStyle _projectile = new ProjectileBeamStyle();
    [SerializeField] private LightningBeamStyle _lightning = new LightningBeamStyle();

    private readonly List<ActiveBeam> _active = new List<ActiveBeam>();
    private readonly Stack<BeamImagePool> _freePools = new Stack<BeamImagePool>();

    private RectTransform Root => (RectTransform)transform;

    /// <summary>
    /// from 가운데에서 to 가운데로 광선을 쏜다. 닿는 순간 onArrive를 부른다. 도중에 멈추면 부르지 않는다.
    /// </summary>
    public void Play(RectTransform from, RectTransform to, Action onArrive)
    {
        if (from == null || to == null)
        {
            onArrive?.Invoke();
            return;
        }

        // 창이 열린 뒤 만들어지는 끌기 그림보다도 위에 그린다.
        transform.SetAsLastSibling();
        var pool = _freePools.Count > 0 ? _freePools.Pop() : new BeamImagePool(Root);
        var style = _style == BeamStyle.Lightning ? (PurchaseBeamStyle)_lightning : _projectile;
        Tween tween = style.Play(pool, ToLocal(from), ToLocal(to), onArrive);
        // 창이 열려 있으면 시간이 멈춰 있어서 시간 정지와 상관없이 돈다.
        tween.SetUpdate(true).SetLink(gameObject).OnKill(() => Release(tween));
        _active.Add(new ActiveBeam { Tween = tween, Pool = pool });
    }

    /// <summary>
    /// 날아가는 광선을 모두 지운다.
    /// </summary>
    public void Stop()
    {
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            if (i < _active.Count)
            {
                _active[i].Tween.Kill();
            }
        }
    }

    private void OnDisable()
    {
        Stop();
    }

    private void Release(Tween tween)
    {
        for (var i = 0; i < _active.Count; i++)
        {
            if (_active[i].Tween != tween)
            {
                continue;
            }

            _active[i].Pool.HideAll();
            _freePools.Push(_active[i].Pool);
            _active.RemoveAt(i);
            return;
        }
    }

    private Vector2 ToLocal(RectTransform rect)
    {
        return Root.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
    }
}
