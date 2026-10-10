using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 중심을 지나는 직선이 반시계로 돌며 화면을 덮고 걷어내는 전환 연출.
/// 처음 접근할 때 전용 캔버스를 만들고 씬이 바뀌어도 유지된다.
/// </summary>
public class ScreenTransition : MonoBehaviour
{
    private const string WIPE_MATERIAL_PATH = "Materials/ScreenWipe";
    private const float DEFAULT_DURATION = 0.76f;
    private const float CLOSED_HOLD_DURATION = 0.7f;
    private const float DEFAULT_FADE_DURATION = 0.5f;
    private const float PIXEL_SIZE = 67.5f;
    private const int SORTING_ORDER = 1000;
    private const float CLOSED_PROGRESS = 1f;
    private const float OPENED_PROGRESS = 2f;

    private static readonly int PROGRESS_ID = Shader.PropertyToID("_Progress");
    private static readonly int PIXEL_SIZE_ID = Shader.PropertyToID("_PixelSize");
    private static readonly int RESOLUTION_ID = Shader.PropertyToID("_Resolution");

    private static ScreenTransition _instance;

    private Image _image;
    private Material _material;
    private Tween _wipeTween;
    private float _progress;

    public static ScreenTransition Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Create();
            }

            return _instance;
        }
    }

    /// <summary>
    /// 화면이 조금이라도 덮여 있으면 true.
    /// </summary>
    public bool IsCovering => _progress > 0f && _progress < OPENED_PROGRESS;

    /// <summary>
    /// 화면을 완전히 덮고 멈춰 있으면 true.
    /// </summary>
    public bool IsClosed => Mathf.Approximately(_progress, CLOSED_PROGRESS);

    #region Unity Methods

    private void OnDestroy()
    {
        KillTween();
        if (_material != null)
        {
            Destroy(_material);
        }

        if (_instance == this)
        {
            _instance = null;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 빈 화면에서 시작해 화면을 완전히 덮고, holdDuration 만큼 덮인 채로 기다린다.
    /// 반환한 트윈의 OnComplete는 대기까지 끝난 뒤에 호출된다.
    /// </summary>
    public Tween Close(float duration = DEFAULT_DURATION, float holdDuration = CLOSED_HOLD_DURATION)
    {
        var wipe = PlayWipe(0f, CLOSED_PROGRESS, duration);
        if (holdDuration <= 0f)
        {
            return wipe;
        }

        _wipeTween = DOTween.Sequence()
            .Append(wipe)
            .AppendInterval(holdDuration)
            .SetUpdate(true);
        return _wipeTween;
    }

    /// <summary>
    /// 덮인 화면을 같은 회전 방향으로 걷어낸다.
    /// </summary>
    public Tween Open(float duration = DEFAULT_DURATION)
    {
        return PlayWipe(CLOSED_PROGRESS, OPENED_PROGRESS, duration);
    }

    /// <summary>
    /// 덮인 화면을 회전 대신 알파로 서서히 걷어내 아래 화면이 페이드인되게 한다.
    /// 페이드 중에도 덮인 상태로 보고 입력을 막는다.
    /// </summary>
    public Tween FadeOut(float duration = DEFAULT_FADE_DURATION)
    {
        KillTween();
        SetProgress(CLOSED_PROGRESS);
        SetAlpha(1f);
        _wipeTween = _image.DOFade(0f, duration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                SetProgress(OPENED_PROGRESS);
                SetAlpha(1f);
            });
        return _wipeTween;
    }

    /// <summary>
    /// 빈 화면에서 회전 대신 알파로 서서히 덮는다. FadeOut의 반대.
    /// 시작부터 덮인 상태로 보고 입력을 막는다.
    /// </summary>
    public Tween FadeIn(float duration = DEFAULT_FADE_DURATION)
    {
        KillTween();
        SetProgress(CLOSED_PROGRESS);
        SetAlpha(0f);
        _wipeTween = _image.DOFade(1f, duration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true);
        return _wipeTween;
    }

    /// <summary>
    /// 화면이 다 덮일 때까지 알파로 페이드한다.
    /// </summary>
    public UniTask FadeInAsync(CancellationToken cancellationToken = default, float duration = DEFAULT_FADE_DURATION)
    {
        return WaitForTween(FadeIn(duration), cancellationToken);
    }

    /// <summary>
    /// 화면을 덮고 대기까지 끝날 때까지 기다린다.
    /// </summary>
    public UniTask CloseAsync(CancellationToken cancellationToken = default, float duration = DEFAULT_DURATION, float holdDuration = CLOSED_HOLD_DURATION)
    {
        return WaitForTween(Close(duration, holdDuration), cancellationToken);
    }

    /// <summary>
    /// 화면이 다 걷힐 때까지 기다린다.
    /// </summary>
    public UniTask OpenAsync(CancellationToken cancellationToken = default, float duration = DEFAULT_DURATION)
    {
        return WaitForTween(Open(duration), cancellationToken);
    }

    /// <summary>
    /// 연출 없이 즉시 덮거나 걷는다.
    /// </summary>
    public void SetCovered(bool isCovered)
    {
        KillTween();
        SetAlpha(1f);
        SetProgress(isCovered ? CLOSED_PROGRESS : OPENED_PROGRESS);
    }

    #endregion

    #region Private Methods

    private static ScreenTransition Create()
    {
        var root = new GameObject(nameof(ScreenTransition));
        DontDestroyOnLoad(root);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SORTING_ORDER;

        // 세로 1080 기준으로 맞춰 해상도가 달라도 블록 줄 수가 같게 한다.
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        root.AddComponent<GraphicRaycaster>();

        var transition = root.AddComponent<ScreenTransition>();
        transition.BuildImage();
        return transition;
    }

    private void BuildImage()
    {
        var imageObject = new GameObject("Wipe", typeof(RectTransform));
        imageObject.transform.SetParent(transform, false);

        var rectTransform = (RectTransform)imageObject.transform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        _image = imageObject.AddComponent<Image>();
        _image.raycastTarget = false;

        var template = Resources.Load<Material>(WIPE_MATERIAL_PATH);
        if (template == null)
        {
            Debug.LogError($"화면 전환 머티리얼을 찾을 수 없음: {WIPE_MATERIAL_PATH}");
            return;
        }

        _material = new Material(template);
        _material.SetFloat(PIXEL_SIZE_ID, PIXEL_SIZE);
        _image.material = _material;

        // 생성 직후에는 Rect 크기가 0이라 바로 레이아웃을 계산해 둔다.
        Canvas.ForceUpdateCanvases();
        SetProgress(0f);
    }

    private Tween PlayWipe(float from, float to, float duration)
    {
        KillTween();
        SetAlpha(1f);
        SetProgress(from);
        _wipeTween = DOTween.To(() => _progress, SetProgress, to, duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true);
        return _wipeTween;
    }

    private void SetAlpha(float alpha)
    {
        var color = _image.color;
        color.a = alpha;
        _image.color = color;
    }

    private void SetProgress(float progress)
    {
        _progress = progress;
        if (_material == null)
        {
            return;
        }

        _material.SetFloat(PROGRESS_ID, progress);
        _material.SetVector(RESOLUTION_ID, _image.rectTransform.rect.size);

        // 덮여 있는 동안만 입력을 막는다.
        _image.enabled = IsCovering;
        _image.raycastTarget = IsCovering;
    }

    private static async UniTask WaitForTween(Tween tween, CancellationToken cancellationToken)
    {
        await UniTask.WaitUntil(() => !tween.IsActive() || tween.IsComplete(), PlayerLoopTiming.Update, cancellationToken);
    }

    private void KillTween()
    {
        if (_wipeTween != null && _wipeTween.IsActive())
        {
            _wipeTween.Kill();
        }

        _wipeTween = null;
    }

    #endregion
}
