using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 빨간 빛덩어리가 살짝 휜 곡선을 따라 날아가고 뒤에 잔상이 남는 광선.
/// 칸에 닿으면 빛이 퍼지며 사라지고, 잔상도 칸으로 따라 들어간다.
/// </summary>
[Serializable]
public class ProjectileBeamStyle : PurchaseBeamStyle
{
    [SerializeField] private Sprite _sprite;
    [SerializeField] private Color _outerColor = new Color(1f, 0.12f, 0.08f, 0.9f);
    [SerializeField] private Color _coreColor = new Color(1f, 0.85f, 0.8f, 1f);
    [SerializeField] private float _headSize = 64f;
    [SerializeField] private float _coreSize = 26f;
    [Tooltip("잔상 원 개수.")]
    [SerializeField] private int _trailCount = 10;
    [Tooltip("잔상 원 사이 간격. 날아가는 진행률(0~1) 단위다.")]
    [SerializeField] private float _trailGap = 0.04f;
    [Tooltip("두 점 사이 거리에 대해 곡선이 옆으로 휘는 비율.")]
    [SerializeField] private float _arc = 0.18f;
    [SerializeField] private float _flyDuration = 0.3f;
    [SerializeField] private float _burstDuration = 0.2f;
    [SerializeField] private float _burstScale = 2.5f;

    public override Sequence Play(BeamImagePool pool, Vector2 from, Func<Vector2> to, Action onArrive)
    {
        var count = Mathf.Max(0, _trailCount);
        var trail = new Image[count];
        for (var i = 0; i < count; i++)
        {
            trail[i] = pool.Get(_sprite, _outerColor);
        }

        // 머리는 잔상 위에 그려지게 나중에 꺼낸다.
        var outer = pool.Get(_sprite, _outerColor);
        var core = pool.Get(_sprite, _coreColor);
        outer.rectTransform.sizeDelta = Vector2.one * _headSize;
        core.rectTransform.sizeDelta = Vector2.one * _coreSize;

        // progress가 1을 넘으면 머리는 칸에 멈추고 잔상만 칸으로 따라 들어간다.
        void Place(float progress, float fade)
        {
            var target = to();
            var control = (from + target) * 0.5f + Perpendicular(from, target) * (Vector2.Distance(from, target) * _arc);
            var head = Bezier(from, control, target, Mathf.Min(progress, 1f));
            outer.rectTransform.localPosition = head;
            core.rectTransform.localPosition = head;
            for (var i = 0; i < count; i++)
            {
                var t = progress - (i + 1) * _trailGap;
                var rate = 1f - (i + 1f) / (count + 1f);
                var rect = trail[i].rectTransform;
                rect.localPosition = Bezier(from, control, target, Mathf.Clamp01(t));
                rect.sizeDelta = Vector2.one * (_headSize * Mathf.Lerp(0.3f, 0.9f, rate));
                trail[i].color = WithAlpha(_outerColor, t < 0f ? 0f : rate * 0.7f * fade);
            }
        }

        Place(0f, 1f);
        var catchUp = (count + 1) * _trailGap;
        return DOTween.Sequence()
            .Append(DOVirtual.Float(0f, 1f, _flyDuration, progress => Place(progress, 1f)).SetEase(Ease.InQuad))
            .AppendCallback(() => onArrive?.Invoke())
            .Append(DOVirtual.Float(0f, 1f, _burstDuration, k =>
            {
                Place(1f + k * catchUp, 1f - k);
                outer.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, _burstScale, k);
                outer.color = WithAlpha(_outerColor, 1f - k);
                core.color = WithAlpha(_coreColor, 1f - k);
            }).SetEase(Ease.OutQuad));
    }

    private static Vector2 Bezier(Vector2 from, Vector2 control, Vector2 to, float t)
    {
        var u = 1f - t;
        return u * u * from + 2f * u * t * control + t * t * to;
    }
}
