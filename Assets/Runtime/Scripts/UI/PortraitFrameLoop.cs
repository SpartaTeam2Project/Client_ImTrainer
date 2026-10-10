using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 포켓몬 초상화 프레임을 Image에 반복 재생한다. 뷰가 Update에서 Tick을 부른다.
/// 원본(pokerogue, GIF)은 한 장 100ms(10fps)다. 같은 칸이 이어지는 프레임은 그만큼 멈춰 보인다.
/// </summary>
public class PortraitFrameLoop
{
    public const float DEFAULT_FRAME_RATE = 15f;
    private const float MIN_FRAME_RATE = 1f;

    private readonly Image _image;
    private readonly PortraitFitter _fitter;
    private Sprite[] _frames;
    private int _index;
    private float _timer;
    private bool _playing;
    private float _frameSeconds = 1f / DEFAULT_FRAME_RATE;

    public PortraitFrameLoop(Image image, PortraitFitter fitter)
    {
        _image = image;
        _fitter = fitter;
    }

    /// <summary>
    /// 첫 장을 보여 주고 animate면 frameRate(초당 장 수)로 재생을 시작한다. 2장 미만이면 첫 장에서 멈춘다. 보여 준 첫 장을 돌려준다.
    /// </summary>
    public Sprite Play(Sprite[] frames, bool animate, float frameRate = DEFAULT_FRAME_RATE)
    {
        _frameSeconds = 1f / Mathf.Max(MIN_FRAME_RATE, frameRate);
        _frames = frames;
        _index = FirstIndex(frames);
        _timer = 0f;
        var first = _index >= 0 ? frames[_index] : null;
        _playing = animate && first != null && frames.Length > 1;
        ShowFrame(first);
        return first;
    }

    public void Stop()
    {
        _playing = false;
        _frames = null;
    }

    /// <summary>
    /// 흐른 시간만큼 장을 넘긴다. 일시정지 중에도 돌도록 unscaled 시간을 넘긴다.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (!_playing)
        {
            return;
        }

        _timer += deltaTime;
        if (_timer < _frameSeconds)
        {
            return;
        }

        while (_timer >= _frameSeconds)
        {
            _timer -= _frameSeconds;
            _index = (_index + 1) % _frames.Length;
        }

        // 빈 칸은 건너뛰고 직전 그림을 유지한다.
        var frame = _frames[_index];
        if (frame != null)
        {
            ShowFrame(frame);
        }
    }

    private void ShowFrame(Sprite frame)
    {
        if (_image == null)
        {
            return;
        }

        _image.sprite = frame;
        _fitter?.Show(frame);
    }

    private static int FirstIndex(Sprite[] frames)
    {
        if (frames == null)
        {
            return -1;
        }

        for (var i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
            {
                return i;
            }
        }

        return -1;
    }
}
