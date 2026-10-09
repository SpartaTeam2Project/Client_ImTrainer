using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 제목 글자를 한 글자씩 위에서 떨어뜨려 "쾅" 하고 착지시킨다.
/// 부모 CanvasGroup과 따로 보이므로, 부모 창이 투명한 동안에도 제목만 먼저 보여 줄 수 있다.
/// 판 결과 중에는 시간이 멈춰 있으므로 연출은 모두 실제 시간으로 돈다.
/// </summary>
[RequireComponent(typeof(TMP_Text), typeof(CanvasGroup))]
public class TitleDropText : MonoBehaviour
{
    private const float PUNCH_DURATION = 0.2f;
    private const int PUNCH_VIBRATO = 8;
    private const float PUNCH_ELASTICITY = 0.5f;
    private const float FADE_IN_SPEED = 3f;

    [Header("낙하")]
    [SerializeField] private float _dropHeight = 140f;
    [SerializeField] private float _letterInterval = 0.14f;
    [SerializeField] private float _fallDuration = 0.16f;

    [Header("착지")]
    [SerializeField] private float _squashDuration = 0.24f;
    [Range(0f, 0.9f)]
    [SerializeField] private float _squash = 0.35f;
    [SerializeField] private string _impactSound = "shop_stamp";
    [SerializeField] private float _titlePunch = 10f;
    [SerializeField] private float _cameraShakeStrength = 0.12f;
    [SerializeField] private float _cameraShakeDuration = 0.15f;

    private readonly LetterDropMesh _mesh = new LetterDropMesh();
    private TMP_Text _text;
    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Vector2 _restPosition;
    private CancellationTokenSource _playCancellation;
    private Tweener _punchTween;
    private Tweener _fadeTween;
    private bool _playing;

    #region Unity Methods

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = transform as RectTransform;
        if (_rectTransform != null)
        {
            _restPosition = _rectTransform.anchoredPosition;
        }

        _canvasGroup.ignoreParentGroups = true;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.alpha = 0f;
    }

    private void OnDisable()
    {
        Stop();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 지금 글자로 낙하 연출을 시작하고, 마지막 글자가 자리 잡기까지 걸리는 초(실제 시간)를 돌려준다.
    /// </summary>
    public float Play()
    {
        Stop();
        KillFade();
        _canvasGroup.alpha = 1f;
        _mesh.Capture(_text);
        var count = _mesh.LetterCount;
        if (count <= 0)
        {
            return 0f;
        }

        // 첫 프레임에 완성된 글자가 비치지 않게 바로 숨겨 둔다.
        for (var i = 0; i < count; i++)
        {
            _mesh.Apply(i, Vector2.zero, Vector2.one, 0f);
        }

        _mesh.Push();
        _playing = true;
        _playCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        PlayAsync(_playCancellation.Token).Forget();
        return Duration(count);
    }

    /// <summary>
    /// 부모 창과 함께 사라지도록 제목 알파를 내린다.
    /// </summary>
    public void FadeOut(float duration)
    {
        KillFade();
        _fadeTween = _canvasGroup.DOFade(0f, duration).SetUpdate(true);
    }

    /// <summary>
    /// 연출을 멈추고 글자와 제목 위치를 제자리로 돌린다.
    /// </summary>
    public void Stop()
    {
        if (_playCancellation != null)
        {
            _playCancellation.Cancel();
            _playCancellation.Dispose();
            _playCancellation = null;
        }

        KillPunch();
        if (_playing)
        {
            _playing = false;
            _mesh.Restore();
        }
    }

    #endregion

    #region Private Methods

    private async UniTaskVoid PlayAsync(CancellationToken token)
    {
        var count = _mesh.LetterCount;
        var landed = new bool[count];
        var total = Duration(count);
        var elapsed = 0f;
        while (true)
        {
            for (var i = 0; i < count; i++)
            {
                ApplyLetter(i, elapsed - i * _letterInterval, ref landed[i]);
            }

            _mesh.Push();
            if (elapsed >= total)
            {
                break;
            }

            var canceled = await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow();
            if (canceled)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
        }

        _playing = false;
        _mesh.Restore();
    }

    // time은 이 글자의 낙하가 시작된 뒤 흐른 시간. 0보다 작으면 아직 차례가 아니다.
    private void ApplyLetter(int letter, float time, ref bool landed)
    {
        if (time < 0f)
        {
            _mesh.Apply(letter, Vector2.zero, Vector2.one, 0f);
            return;
        }

        if (time < _fallDuration)
        {
            // 점점 빨라지며 떨어진다.
            var progress = time / _fallDuration;
            var height = _dropHeight * (1f - progress * progress);
            _mesh.Apply(letter, new Vector2(0f, height), Vector2.one, progress * FADE_IN_SPEED);
            return;
        }

        if (!landed)
        {
            landed = true;
            Impact();
        }

        // 착지 직후 납작했다가 살짝 늘어난 뒤 제 모양으로 돌아온다.
        var squashProgress = _squashDuration > 0f ? Mathf.Clamp01((time - _fallDuration) / _squashDuration) : 1f;
        var scaleY = 1f - _squash * (1f - squashProgress) * Mathf.Cos(squashProgress * 1.5f * Mathf.PI);
        _mesh.Apply(letter, Vector2.zero, new Vector2(2f - scaleY, scaleY), 1f);
    }

    private void Impact()
    {
        PlaySound(_impactSound);
        if (Managers.Instance != null && Managers.Instance.TryGetManager<CameraManager>(out var cameraManager))
        {
            cameraManager.Shake(_cameraShakeStrength, _cameraShakeDuration);
        }

        if (_rectTransform == null || _titlePunch <= 0f)
        {
            return;
        }

        KillPunch();
        _punchTween = _rectTransform
            .DOPunchAnchorPos(Vector2.down * _titlePunch, PUNCH_DURATION, PUNCH_VIBRATO, PUNCH_ELASTICITY)
            .SetUpdate(true);
    }

    private float Duration(int count)
    {
        return Mathf.Max(0, count - 1) * _letterInterval + _fallDuration + _squashDuration;
    }

    private void KillPunch()
    {
        if (_punchTween != null)
        {
            _punchTween.Kill();
            _punchTween = null;
        }

        if (_rectTransform != null)
        {
            _rectTransform.anchoredPosition = _restPosition;
        }
    }

    private void KillFade()
    {
        if (_fadeTween == null)
        {
            return;
        }

        _fadeTween.Kill();
        _fadeTween = null;
    }

    private static void PlaySound(string name)
    {
        if (string.IsNullOrEmpty(name) || Managers.Instance == null
            || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(name);
    }

    #endregion
}
