using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 경험치 사탕 머티리얼의 발광을 숨 쉬듯 밝게/어둡게 반복(펄스)하고,
/// Shine이 켜진 머티리얼은 빛줄기를 주기적으로 훑고 지나가게 한다.
/// 원본 머티리얼마다 타이밍이 다른 런타임 복사본 몇 개를 만들고, 사탕은 그중 하나를 무작위로 받는다.
/// 복사본 수만큼만 드로우콜이 나뉘므로 배칭을 크게 깨지 않고 연출이 엇갈린다.
/// </summary>
public class ExperienceGemGlow
{
    private const int VARIANT_COUNT = 4;

    private const float PULSE_PERIOD = 1.6f;
    private const float PULSE_MIN_RATE = 0.4f;

    private const float MIN_SWEEP_INTERVAL = 1.8f;
    private const float MAX_SWEEP_INTERVAL = 3.2f;
    private const float SWEEP_DURATION = 0.5f;
    private const float SWEEP_START = -0.2f;
    private const float SWEEP_END = 1.2f;

    private const string GLOW_KEYWORD = "GLOW_ON";
    private const string SHINE_KEYWORD = "SHINE_ON";

    private static readonly int GlowId = Shader.PropertyToID("_Glow");
    private static readonly int ShineLocationId = Shader.PropertyToID("_ShineLocation");

    private readonly Dictionary<Material, List<GlowVariant>> _variantsBySource = new Dictionary<Material, List<GlowVariant>>();
    private readonly List<GlowVariant> _variants = new List<GlowVariant>();

    private class GlowVariant
    {
        public Material Material;
        public float BaseGlow;
        public float PulseOffset;
        public bool HasShine;
        public float SweepInterval;
        public float SweepOffset;
    }

    #region Public Methods

    /// <summary>
    /// 원본 머티리얼의 런타임 복사본 중 하나를 무작위로 돌려준다. 원본 에셋은 건드리지 않는다.
    /// Glow와 Shine이 모두 꺼진 머티리얼은 복사하지 않고 원본을 그대로 돌려준다.
    /// </summary>
    public Material GetRuntimeMaterial(Material source)
    {
        if (source == null || (!source.IsKeywordEnabled(GLOW_KEYWORD) && !source.IsKeywordEnabled(SHINE_KEYWORD)))
        {
            return source;
        }

        if (!_variantsBySource.TryGetValue(source, out var variants))
        {
            variants = CreateVariants(source);
            _variantsBySource.Add(source, variants);
        }

        return variants[Random.Range(0, variants.Count)].Material;
    }

    /// <summary>
    /// 복사본마다 자기 시작 시점에 맞춰 발광 세기와 빛줄기 위치를 바꾼다.
    /// </summary>
    public void Tick(float time)
    {
        for (var i = 0; i < _variants.Count; i++)
        {
            var variant = _variants[i];
            if (variant.Material == null)
            {
                continue;
            }

            variant.Material.SetFloat(GlowId, variant.BaseGlow * ResolvePulseRate(time + variant.PulseOffset));
            if (variant.HasShine)
            {
                variant.Material.SetFloat(ShineLocationId, ResolveShineLocation(time + variant.SweepOffset, variant.SweepInterval));
            }
        }
    }

    /// <summary>
    /// 만든 복사본을 모두 지운다.
    /// </summary>
    public void Dispose()
    {
        for (var i = 0; i < _variants.Count; i++)
        {
            if (_variants[i].Material != null)
            {
                Object.Destroy(_variants[i].Material);
            }
        }

        _variants.Clear();
        _variantsBySource.Clear();
    }

    #endregion

    #region Private Methods

    private List<GlowVariant> CreateVariants(Material source)
    {
        var hasShine = source.IsKeywordEnabled(SHINE_KEYWORD);
        var baseGlow = source.GetFloat(GlowId);
        var variants = new List<GlowVariant>(VARIANT_COUNT);
        for (var i = 0; i < VARIANT_COUNT; i++)
        {
            var material = new Material(source);
            material.name = source.name;

            var sweepInterval = Random.Range(MIN_SWEEP_INTERVAL, MAX_SWEEP_INTERVAL);
            var variant = new GlowVariant
            {
                Material = material,
                BaseGlow = baseGlow,
                PulseOffset = Random.Range(0f, PULSE_PERIOD),
                HasShine = hasShine,
                SweepInterval = sweepInterval,
                SweepOffset = Random.Range(0f, sweepInterval),
            };

            if (hasShine)
            {
                material.SetFloat(ShineLocationId, SWEEP_START);
            }

            variants.Add(variant);
            _variants.Add(variant);
        }

        return variants;
    }

    private static float ResolvePulseRate(float time)
    {
        var wave = (Mathf.Sin(time * (2f * Mathf.PI / PULSE_PERIOD)) + 1f) * 0.5f;
        return Mathf.Lerp(PULSE_MIN_RATE, 1f, wave);
    }

    private static float ResolveShineLocation(float time, float interval)
    {
        var elapsed = Mathf.Repeat(time, interval);
        if (elapsed >= SWEEP_DURATION)
        {
            return SWEEP_START;
        }

        return Mathf.Lerp(SWEEP_START, SWEEP_END, elapsed / SWEEP_DURATION);
    }

    #endregion
}
