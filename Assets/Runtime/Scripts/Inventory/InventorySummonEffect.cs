using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점에서 산 포켓몬이 광선을 타고 칸에 들어올 때의 빨간 실루엣.
/// 아이콘 위에 같은 그림의 실루엣을 따로 겹쳐서, 아이콘의 그림이나 진화 머티리얼은 건드리지 않는다.
/// 새로 생긴 칸이면 산 순간부터 빈 칸처럼 보이게 둔다. 광선이 닿으면 칸 색이 돌아오고, 실루엣이 끝나면 별을 튕기며 보여 준다.
/// </summary>
public class InventorySummonEffect : MonoBehaviour
{
    private const float START_SCALE = 0.6f;
    private const float GROW_DURATION = 0.2f;
    private const float HOLD_DURATION = 0.12f;
    private const float FADE_DURATION = 0.35f;

    private InventoryItem _owner;
    private Image _icon;
    private Material _material;
    private Image _silhouette;
    private IconHitBlend _blend;
    private Tween _tween;
    private Sprite _target;
    private bool _hidingStars;
    private bool _hidingFrame;

    public void Bind(InventoryItem owner, Image icon, Material material)
    {
        _owner = owner;
        _icon = icon;
        _material = material;
    }

    /// <summary>
    /// 포켓몬을 샀다. 새로 생긴 칸이면 광선이 닿을 때까지 테두리와 배경을 빈 칸 색으로 두고,
    /// 광선 연출이 끝날 때까지 아이콘과 별을 숨긴다.
    /// 칸이 그려진 같은 프레임에 불러야 아이콘이 한 번도 보이지 않는다.
    /// CanvasRenderer 알파로 숨겨서 그사이 칸을 다시 그려 색이 바뀌어도 숨긴 채로 남는다.
    /// </summary>
    public void Begin(bool hide)
    {
        Stop();
        if (!hide || _icon == null)
        {
            return;
        }

        _target = _icon.sprite;
        _icon.canvasRenderer.SetAlpha(0f);
        _hidingStars = _owner != null;
        _hidingFrame = _owner != null;
        if (_owner != null)
        {
            _owner.SetStarsHidden(true);
            _owner.SetFrameEmpty(true);
        }
    }

    /// <summary>
    /// 광선이 닿았다. 빨간 실루엣이 커지며 나타났다가 원래 색으로 돌아오고, 숨긴 별이 튕기며 나타난다.
    /// </summary>
    public void Play()
    {
        KillTween();
        RevealFrame();
        if (_icon == null || _icon.sprite == null || !EnsureSilhouette())
        {
            Finish();
            return;
        }

        _target = _icon.sprite;
        _icon.canvasRenderer.SetAlpha(0f);
        _silhouette.sprite = _icon.sprite;
        _silhouette.preserveAspect = _icon.preserveAspect;
        _silhouette.enabled = true;
        _silhouette.rectTransform.localScale = Vector3.one * START_SCALE;
        _blend.Apply();
        _blend.Set(1f);

        _tween = DOTween.Sequence()
            .Append(_silhouette.rectTransform.DOScale(1f, GROW_DURATION).SetEase(Ease.OutBack))
            .AppendInterval(HOLD_DURATION)
            .Append(DOVirtual.Float(1f, 0f, FADE_DURATION, _blend.Set).SetEase(Ease.InQuad))
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                _tween = null;
                Finish();
            });
    }

    /// <summary>
    /// 숨기거나 실루엣을 띄운 그림과 다른 포켓몬이 그려지면 연출을 끝낸다. 가방이 다시 정렬된 경우다.
    /// </summary>
    public void NotifyShown(Sprite portrait)
    {
        if (_target != null && portrait != _target)
        {
            Stop();
        }
    }

    /// <summary>
    /// 연출을 멈추고 아이콘과 별을 바로 보이게 되돌린다.
    /// </summary>
    public void Stop()
    {
        KillTween();
        Restore();
        if (_hidingStars)
        {
            _hidingStars = false;
            _owner.SetStarsHidden(false);
        }
    }

    private void OnDisable()
    {
        Stop();
    }

    private void OnDestroy()
    {
        _blend?.Dispose();
    }

    /// <summary>
    /// 실루엣이 다 빠졌다. 아이콘을 되돌리고, 숨겨 둔 별이 있으면 1성부터 차례로 튕기며 보여 준다.
    /// </summary>
    private void Finish()
    {
        Restore();
        if (!_hidingStars)
        {
            return;
        }

        _hidingStars = false;
        _owner.RevealStarsInOrder();
    }

    private void RevealFrame()
    {
        if (!_hidingFrame)
        {
            return;
        }

        _hidingFrame = false;
        _owner.SetFrameEmpty(false);
    }

    private void Restore()
    {
        RevealFrame();
        _target = null;
        if (_icon != null)
        {
            _icon.canvasRenderer.SetAlpha(1f);
        }

        if (_silhouette != null)
        {
            _blend.Clear();
            _silhouette.enabled = false;
        }
    }

    private void KillTween()
    {
        if (_tween == null)
        {
            return;
        }

        _tween.Kill();
        _tween = null;
    }

    /// <summary>
    /// 실루엣을 아이콘의 자식으로 만들어 아이콘 크기에 맞춘다. 머티리얼이 없으면 만들지 않는다.
    /// </summary>
    private bool EnsureSilhouette()
    {
        if (_silhouette != null)
        {
            return true;
        }

        if (_material == null)
        {
            return false;
        }

        var part = new GameObject("SummonSilhouette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        part.layer = _icon.gameObject.layer;
        var rect = (RectTransform)part.transform;
        rect.SetParent(_icon.rectTransform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        _silhouette = part.GetComponent<Image>();
        _silhouette.raycastTarget = false;
        _silhouette.enabled = false;
        _blend = new IconHitBlend(_silhouette, _material);
        return true;
    }
}
