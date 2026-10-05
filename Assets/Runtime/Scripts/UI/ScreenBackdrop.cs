using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 창 뒤에 깔 흐린 화면 캡처. 그 프레임 화면을 담으려면 프레임 끝에서 Capture를 부른다.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class ScreenBackdrop : MonoBehaviour
{
    [Tooltip("화면을 절반씩 줄이는 횟수. 많을수록 더 흐려진다.")]
    [SerializeField, Range(0, 5)] private int _downSampleSteps = 3;

    private RawImage _image;
    private RenderTexture _captured;

    private RawImage Image => _image != null ? _image : _image = GetComponent<RawImage>();

    private void OnDestroy()
    {
        Release();
    }

    /// <summary>
    /// 현재 화면을 캡처해 줄인 뒤 배경으로 쓴다. WaitForEndOfFrame 뒤에 불러야 한다.
    /// </summary>
    public void Capture()
    {
        Release();

        var width = Screen.width;
        var height = Screen.height;
        var source = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        ScreenCapture.CaptureScreenshotIntoRenderTexture(source);

        // 절반씩 줄여 가며 쌍선형으로 옮기면 늘려 그릴 때 흐려 보인다.
        for (var i = 0; i < _downSampleSteps && width > 1 && height > 1; i++)
        {
            width /= 2;
            height /= 2;
            var next = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            next.filterMode = FilterMode.Bilinear;
            Graphics.Blit(source, next);
            RenderTexture.ReleaseTemporary(source);
            source = next;
        }

        _captured = source;
        _captured.filterMode = FilterMode.Bilinear;
        _captured.wrapMode = TextureWrapMode.Clamp;
        Image.texture = _captured;

        // DirectX 계열은 화면 캡처가 위아래로 뒤집혀 나온다.
        Image.uvRect = SystemInfo.graphicsUVStartsAtTop ? new Rect(0f, 1f, 1f, -1f) : new Rect(0f, 0f, 1f, 1f);
    }

    /// <summary>
    /// 캡처한 텍스처를 돌려준다.
    /// </summary>
    public void Release()
    {
        if (_captured == null)
        {
            return;
        }

        Image.texture = null;
        RenderTexture.ReleaseTemporary(_captured);
        _captured = null;
    }
}
