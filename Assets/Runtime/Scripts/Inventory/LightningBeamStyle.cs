using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 지그재그로 꺾인 빨간 빔이 볼에서 칸 쪽으로 자라나는 광선.
/// 칸에 닿으면 꺾인 모양을 짧게 계속 바꿔 지직거리다가 사라진다.
/// </summary>
[Serializable]
public class LightningBeamStyle : PurchaseBeamStyle
{
    [Tooltip("선 그림. 비우면 흰 사각형으로 그린다.")]
    [SerializeField] private Sprite _lineSprite;
    [Tooltip("빔 끝의 빛 그림.")]
    [SerializeField] private Sprite _tipSprite;
    [SerializeField] private Color _outerColor = new Color(1f, 0.1f, 0.06f, 0.85f);
    [SerializeField] private Color _coreColor = new Color(1f, 0.9f, 0.85f, 1f);
    [SerializeField] private float _outerWidth = 18f;
    [SerializeField] private float _coreWidth = 6f;
    [SerializeField] private float _tipSize = 64f;
    [Tooltip("꺾인 마디 수.")]
    [SerializeField] private int _segments = 8;
    [Tooltip("꺾임점이 직선에서 옆으로 벗어나는 최대 거리.")]
    [SerializeField] private float _jitter = 26f;
    [SerializeField] private float _growDuration = 0.14f;
    [SerializeField] private float _holdDuration = 0.2f;
    [Tooltip("칸에 닿은 뒤 꺾인 모양을 다시 뽑는 간격.")]
    [SerializeField] private float _flickerInterval = 0.04f;
    [SerializeField] private float _fadeDuration = 0.15f;

    public override Sequence Play(BeamImagePool pool, Vector2 from, Vector2 to, Action onArrive)
    {
        var count = Mathf.Max(1, _segments);
        var points = new Vector2[count + 1];
        var outers = new Image[count];
        var cores = new Image[count];
        for (var i = 0; i < count; i++)
        {
            outers[i] = pool.Get(_lineSprite, _outerColor);
        }

        // 흰 심은 빨간 선 위에, 끝 빛은 맨 위에 그린다.
        for (var i = 0; i < count; i++)
        {
            cores[i] = pool.Get(_lineSprite, _coreColor);
        }

        var tip = pool.Get(_tipSprite, _outerColor);
        tip.rectTransform.sizeDelta = Vector2.one * _tipSize;

        var side = Perpendicular(from, to);
        var reveal = 0f;
        var alpha = 1f;

        void Shuffle()
        {
            points[0] = from;
            points[count] = to;
            for (var i = 1; i < count; i++)
            {
                var rate = (float)i / count;
                // 가운데일수록 더 크게 꺾어서 양 끝은 볼과 칸에 붙어 있게 한다.
                var offset = UnityEngine.Random.Range(-_jitter, _jitter) * Mathf.Sin(rate * Mathf.PI);
                points[i] = Vector2.Lerp(from, to, rate) + side * offset;
            }
        }

        void Draw()
        {
            var total = 0f;
            for (var i = 0; i < count; i++)
            {
                total += Vector2.Distance(points[i], points[i + 1]);
            }

            var remain = total * reveal;
            var end = from;
            for (var i = 0; i < count; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                var length = Vector2.Distance(a, b);
                var shown = Mathf.Clamp(remain, 0f, length);
                remain -= length;
                if (shown > 0f)
                {
                    end = length > 0f ? Vector2.Lerp(a, b, shown / length) : b;
                }

                PlaceLine(outers[i], a, b, shown, _outerWidth, WithAlpha(_outerColor, alpha));
                PlaceLine(cores[i], a, b, shown, _coreWidth, WithAlpha(_coreColor, alpha));
            }

            tip.rectTransform.localPosition = end;
            tip.color = WithAlpha(_outerColor, alpha);
        }

        Shuffle();
        Draw();
        var sequence = DOTween.Sequence()
            .Append(DOVirtual.Float(0f, 1f, _growDuration, value =>
            {
                reveal = value;
                Draw();
            }).SetEase(Ease.OutQuad))
            .AppendCallback(() => onArrive?.Invoke());

        var interval = Mathf.Max(0.01f, _flickerInterval);
        var flickers = Mathf.Max(1, Mathf.CeilToInt(_holdDuration / interval));
        for (var i = 0; i < flickers; i++)
        {
            sequence.AppendCallback(() =>
            {
                Shuffle();
                alpha = UnityEngine.Random.Range(0.6f, 1f);
                Draw();
            });
            sequence.AppendInterval(interval);
        }

        return sequence.Append(DOVirtual.Float(1f, 0f, _fadeDuration, value =>
        {
            alpha = value;
            Draw();
        }));
    }

    /// <summary>
    /// a에서 b 방향으로 shown 길이만큼만 선을 늘린다.
    /// </summary>
    private static void PlaceLine(Image line, Vector2 a, Vector2 b, float shown, float width, Color color)
    {
        var rect = line.rectTransform;
        var direction = b - a;
        rect.pivot = new Vector2(0f, 0.5f);
        rect.localPosition = a;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        rect.sizeDelta = new Vector2(shown, width);
        line.color = shown > 0f ? color : Color.clear;
    }
}
