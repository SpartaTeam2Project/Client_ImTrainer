using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 마주 본 뒤에 여는 VS 화면에 쓰는 그림.
/// </summary>
public struct BossTrainerVersusCast
{
    public BossTrainerVersusView View;
    public Sprite RedBar;
    public Sprite PlayerBack;
    public Sprite[] RivalFrames;
    public Sprite[] SlashFrames;
    public Sprite Versus;
}

/// <summary>
/// 빨간 바가 흐르는 동안 초상화, 슬래시, VS를 순서대로 올린다.
/// </summary>
public static class BossTrainerVersus
{
    private const float HOLD_SECONDS = 1f;
    private const float SCROLL_WIDTHS_PER_SECOND = 0.75f;
    private const float SLIDE_SECONDS = 0.55f;
    private const float FRAME_SECONDS = 0.16f;
    private const float SLASH_FRAME_SECONDS = 0.06f;
    private const float SLASH_SWEEP_SECONDS = 0.4f;
    private const float SLASH_STAGGER_SECONDS = 0.06f;
    private const int SLASH_PIECES = 4;
    private const float MUSIC_DELAY_SECONDS = 0.6f;
    private const float BATTLE_START_SECONDS = 2f;
    private const string LAST_BATTLE_MUSIC_NAME = "LastBattle";

    private static BossTrainerVersusView _view;
    private static RectTransform _firstBar;
    private static RectTransform _secondBar;
    private static Image _rivalImage;
    private static float _rivalPixelScale;
    private static Sprite[] _slashFrames;
    private static int _slashFrame;
    private static float _slashTimer;

    /// <summary>
    /// VS 화면을 연다. 브금 뒤 조작이 있거나 2초가 지나면 true.
    /// 그림이 비어 있으면 화면만 건너뛰고 true를 돌려 보스전으로 넘어간다.
    /// </summary>
    public static async UniTask<bool> PlayAsync(int token, BossTrainerVersusCast cast)
    {
        Stop();
        if (!HasCast(cast))
        {
            Debug.LogWarning("보스 VS 화면이 비어 있습니다. 화면을 건너뛰고 보스전을 엽니다. 클립에 공용 프리팹, 빨간 바, 등 사진, 보스 프레임, 슬래시, VS를 넣으세요.");
            return true;
        }

        if (!await WaitSecondsAsync(token, HOLD_SECONDS))
        {
            return false;
        }

        _view = Object.Instantiate(cast.View);
        _view.name = "BossTrainerVersus";
        var playerRest = _view.Player.rectTransform.anchoredPosition;
        var rivalRest = _view.Rival.rectTransform.anchoredPosition;
        _view.Player.sprite = cast.PlayerBack;
        _view.Rival.sprite = cast.RivalFrames[0];
        _view.Rival.preserveAspect = false;
        _rivalImage = _view.Rival;
        _rivalPixelScale = _view.Rival.rectTransform.sizeDelta.y / Mathf.Max(1f, cast.RivalFrames[0].rect.height);
        HideUntilShown();
        _view.Player.gameObject.SetActive(false);
        _view.Rival.gameObject.SetActive(false);

        await UniTask.Yield();
        if (!IsCurrent(token) || _view == null)
        {
            return false;
        }

        _view.Player.rectTransform.anchoredPosition = OffscreenFrom(_view.Player.rectTransform, playerRest, true);
        _view.Rival.rectTransform.anchoredPosition = OffscreenFrom(_view.Rival.rectTransform, rivalRest, false);
        _view.Player.gameObject.SetActive(true);
        _view.Rival.gameObject.SetActive(true);
        PlaceBars(cast.RedBar);
        if (!await SlideAsync(token, _view.Player.rectTransform, _view.Player.rectTransform.anchoredPosition, playerRest, SLIDE_SECONDS))
        {
            return false;
        }

        if (!await SlideAsync(token, _view.Rival.rectTransform, _view.Rival.rectTransform.anchoredPosition, rivalRest, SLIDE_SECONDS))
        {
            return false;
        }

        if (!await PlayRivalFramesAsync(token, cast.RivalFrames))
        {
            return false;
        }

        if (!await SweepSlashAsync(token, cast.SlashFrames))
        {
            return false;
        }

        ShowVersus(cast.Versus);
        if (!await WaitSecondsAsync(token, MUSIC_DELAY_SECONDS))
        {
            return false;
        }

        PlayMusic(LAST_BATTLE_MUSIC_NAME);
        return await WaitForBattleStartAsync(token);
    }

    /// <summary>
    /// VS 화면을 치운다.
    /// </summary>
    public static void Stop()
    {
        if (_view != null)
        {
            Object.Destroy(_view.gameObject);
        }

        _view = null;
        _firstBar = null;
        _secondBar = null;
        _rivalImage = null;
        _rivalPixelScale = 0f;
        _slashFrames = null;
        _slashFrame = 0;
        _slashTimer = 0f;
    }

    private static void HideUntilShown()
    {
        var slashes = _view.Slashes;
        for (var i = 0; i < slashes.Length; i++)
        {
            if (slashes[i] != null)
            {
                PrepareFade(slashes[i]);
            }
        }

        PrepareFade(_view.Versus);
    }

    private static void PlaceBars(Sprite sprite)
    {
        Canvas.ForceUpdateCanvases();
        var width = Mathf.Max(1f, _view.Band.rect.width);
        _firstBar = PlaceBar(_view.FirstBar, sprite, width, 0f);
        _secondBar = PlaceBar(_view.SecondBar, sprite, width, -width);
    }

    private static RectTransform PlaceBar(Image bar, Sprite sprite, float width, float x)
    {
        bar.sprite = sprite;
        var rect = bar.rectTransform;
        var position = rect.anchoredPosition;
        position.x = x;
        rect.anchoredPosition = position;
        var size = rect.sizeDelta;
        size.x = width;
        rect.sizeDelta = size;
        return rect;
    }

    private static void TickScroll()
    {
        if (_view == null || _firstBar == null || _secondBar == null)
        {
            return;
        }

        var width = _view.Band.rect.width;
        if (width <= 1f)
        {
            return;
        }

        var step = width * SCROLL_WIDTHS_PER_SECOND * Time.deltaTime;
        var first = _firstBar.anchoredPosition;
        var second = _secondBar.anchoredPosition;
        first.x += step;
        second.x += step;
        if (first.x >= width)
        {
            first.x = second.x - width;
        }

        if (second.x >= width)
        {
            second.x = first.x - width;
        }

        _firstBar.anchoredPosition = first;
        _secondBar.anchoredPosition = second;
    }

    /// <summary>
    /// 왼쪽 화염은 왼쪽 끝에서, 오른쪽 화염은 오른쪽 끝에서 타듯이 번진다.
    /// </summary>
    private static async UniTask<bool> SweepSlashAsync(int token, Sprite[] frames)
    {
        if (_view == null || frames == null || frames.Length < SLASH_PIECES)
        {
            return IsCurrent(token);
        }

        var slashes = _view.Slashes;
        var count = slashes != null ? Mathf.Min(slashes.Length, SLASH_PIECES) : 0;
        if (count == 0)
        {
            return IsCurrent(token);
        }

        for (var i = 0; i < count; i++)
        {
            var image = slashes[i];
            if (image == null)
            {
                continue;
            }

            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = i % 2 == 0 ? 0 : 1;
            image.fillAmount = 0f;
        }

        BeginSlash(frames);
        var duration = SLASH_SWEEP_SECONDS + SLASH_STAGGER_SECONDS;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            if (!IsCurrent(token) || _view == null)
            {
                return false;
            }

            TickScroll();
            TickSlash();
            elapsed += Time.deltaTime;
            for (var i = 0; i < count; i++)
            {
                var image = slashes[i];
                if (image == null)
                {
                    continue;
                }

                var linear = Mathf.Clamp01((elapsed - SlashStagger(i)) / SLASH_SWEEP_SECONDS);
                var blend = linear * linear * (3f - 2f * linear);
                image.fillAmount = blend;
            }

            await UniTask.Yield();
        }

        for (var i = 0; i < count; i++)
        {
            if (slashes[i] != null)
            {
                slashes[i].fillAmount = 1f;
            }
        }

        return IsCurrent(token);
    }

    private static float SlashStagger(int index)
    {
        return index == 0 || index == 3 ? 0f : SLASH_STAGGER_SECONDS;
    }

    /// <summary>
    /// 슬래시 네 칸을 시트 순서대로 바꿔 화염이 일렁이게 한다.
    /// </summary>
    private static void BeginSlash(Sprite[] frames)
    {
        if (_view == null || frames == null || frames.Length < SLASH_PIECES)
        {
            return;
        }

        _slashFrames = frames;
        _slashFrame = 0;
        _slashTimer = 0f;
        ApplySlashFrame();
    }

    private static void TickSlash()
    {
        if (_view == null || _slashFrames == null || _slashFrames.Length < SLASH_PIECES)
        {
            return;
        }

        var groups = _slashFrames.Length / SLASH_PIECES;
        if (groups <= 1)
        {
            return;
        }

        _slashTimer += Time.deltaTime;
        if (_slashTimer < SLASH_FRAME_SECONDS)
        {
            return;
        }

        _slashTimer -= SLASH_FRAME_SECONDS;
        _slashFrame = (_slashFrame + 1) % groups;
        ApplySlashFrame();
    }

    private static void ApplySlashFrame()
    {
        var slashes = _view.Slashes;
        if (slashes == null)
        {
            return;
        }

        var pieceCount = Mathf.Min(slashes.Length, SLASH_PIECES);
        for (var i = 0; i < pieceCount; i++)
        {
            var image = slashes[i];
            var index = _slashFrame * SLASH_PIECES + i;
            if (image == null || index >= _slashFrames.Length || _slashFrames[index] == null)
            {
                continue;
            }

            image.sprite = _slashFrames[index];
            Show(image);
        }
    }

    private static async UniTask<bool> PlayRivalFramesAsync(int token, Sprite[] frames)
    {
        for (var i = 0; i < frames.Length; i++)
        {
            if (_rivalImage == null || !IsCurrent(token))
            {
                return false;
            }

            if (frames[i] != null)
            {
                _rivalImage.sprite = frames[i];
                _rivalImage.rectTransform.sizeDelta = frames[i].rect.size * _rivalPixelScale;
            }

            if (!await WaitSecondsAsync(token, FRAME_SECONDS))
            {
                return false;
            }
        }

        return IsCurrent(token);
    }

    private static void ShowVersus(Sprite sprite)
    {
        _view.Versus.sprite = sprite;
        Show(_view.Versus);
    }

    private static void Show(Image image)
    {
        var group = image.GetComponent<CanvasGroup>();
        if (group == null)
        {
            return;
        }

        group.alpha = 1f;
    }

    private static CanvasGroup PrepareFade(Image image)
    {
        var group = image.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = image.gameObject.AddComponent<CanvasGroup>();
        }

        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        return group;
    }

    private static Vector2 OffscreenFrom(RectTransform rect, Vector2 rest, bool fromLeft)
    {
        var canvasWidth = CanvasWidth();
        var width = rect.sizeDelta.x;
        if (fromLeft)
        {
            return new Vector2(-width - rect.anchorMin.x * canvasWidth, rest.y);
        }

        return new Vector2(width + (1f - rect.anchorMin.x) * canvasWidth, rest.y);
    }

    private static async UniTask<bool> SlideAsync(int token, RectTransform rect, Vector2 from, Vector2 to, float seconds)
    {
        var elapsed = 0f;
        var duration = Mathf.Max(0.05f, seconds);
        while (elapsed < duration)
        {
            if (!IsCurrent(token) || rect == null)
            {
                return false;
            }

            TickScroll();
            TickSlash();
            elapsed += Time.deltaTime;
            rect.anchoredPosition = Vector2.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            await UniTask.Yield();
        }

        if (rect != null)
        {
            rect.anchoredPosition = to;
        }

        return IsCurrent(token);
    }

    private static async UniTask<bool> WaitSecondsAsync(int token, float seconds)
    {
        var elapsed = 0f;
        while (elapsed < seconds)
        {
            if (!IsCurrent(token))
            {
                return false;
            }

            TickScroll();
            TickSlash();
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }

        return IsCurrent(token);
    }

    private static async UniTask<bool> WaitForBattleStartAsync(int token)
    {
        var elapsed = 0f;
        while (elapsed < BATTLE_START_SECONDS)
        {
            if (!IsCurrent(token))
            {
                return false;
            }

            TickScroll();
            TickSlash();
            if (ConsumeActionPressed())
            {
                return true;
            }

            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }

        return IsCurrent(token);
    }

    private static bool ConsumeActionPressed()
    {
        return Managers.Instance != null
            && Managers.Instance.TryGetManager<InputManager>(out var inputManager)
            && inputManager.ConsumeActionPressed();
    }

    private static void PlayMusic(string musicName)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlayMusic(musicName);
    }

    private static float CanvasWidth()
    {
        var rect = _view.GetComponent<RectTransform>();
        return rect != null ? rect.rect.width : 0f;
    }

    private static bool HasCast(BossTrainerVersusCast cast)
    {
        return cast.View != null
            && cast.View.IsReady()
            && cast.RedBar != null
            && cast.PlayerBack != null
            && cast.RivalFrames != null
            && cast.RivalFrames.Length > 0
            && cast.RivalFrames[0] != null
            && cast.SlashFrames != null
            && cast.SlashFrames.Length > 0
            && cast.SlashFrames[0] != null
            && cast.Versus != null;
    }

    private static bool IsCurrent(int token)
    {
        return BossArenaPlayback.IsCurrent(token);
    }
}
