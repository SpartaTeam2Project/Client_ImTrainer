using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Image 하나에 AllIn1 HitEffect 머티리얼 복사본을 씌우고 _HitEffectBlend를 바꾼다.
/// 진화 연출과 소환 실루엣이 같이 쓴다.
/// </summary>
public sealed class IconHitBlend
{
    private static readonly int HIT_EFFECT_BLEND_ID = Shader.PropertyToID("_HitEffectBlend");

    private readonly Image _image;
    private readonly Material _source;
    private Material _instance;

    public IconHitBlend(Image image, Material source)
    {
        _image = image;
        _source = source;
    }

    public bool Valid => _image != null && _source != null;

    /// <summary>
    /// 처음 쓸 때 머티리얼을 복사해 두고, Image에 그 복사본을 씌운다.
    /// </summary>
    public void Apply()
    {
        if (!Valid)
        {
            return;
        }

        if (_instance == null)
        {
            _instance = new Material(_source);
        }

        _image.material = _instance;
    }

    /// <summary>
    /// 마스크 안에서는 그리는 머티리얼이 따로 복사돼서 둘 다 바꾼다.
    /// </summary>
    public void Set(float blend)
    {
        if (_instance != null)
        {
            _instance.SetFloat(HIT_EFFECT_BLEND_ID, blend);
        }

        if (_image != null && _image.materialForRendering != _instance && _image.material == _instance)
        {
            _image.materialForRendering.SetFloat(HIT_EFFECT_BLEND_ID, blend);
        }
    }

    /// <summary>
    /// 빛을 빼고 Image를 기본 머티리얼로 되돌린다.
    /// </summary>
    public void Clear()
    {
        Set(0f);
        if (_image != null)
        {
            _image.material = null;
        }
    }

    public void Dispose()
    {
        if (_instance != null)
        {
            Object.Destroy(_instance);
            _instance = null;
        }
    }
}
