using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 피격, 패배, 승리 같은 게임 이벤트에 맞춰 화면 효과를 씬 기본 룩 위에 덧씌운다.
/// 효과마다 전용 Volume을 두고 weight만 트윈해서, weight 0이면 씬 Global Volume 값을 건드리지 않는다.
/// </summary>
public class PostProcessManager : BaseManager
{
    private const float EFFECT_PRIORITY = 10f;

    [Header("피격 비네트")]
    [SerializeField] private Color _hitColor = new Color(0.8f, 0f, 0f);
    [SerializeField, Range(0f, 1f)] private float _hitIntensity = 0.35f;
    [SerializeField, Range(0f, 1f)] private float _hitSmoothness = 0.6f;
    [SerializeField] private float _hitInSeconds = 0.05f;
    [SerializeField] private float _hitOutSeconds = 0.25f;

    [Header("패배 흑백")]
    [SerializeField, Range(-100f, 0f)] private float _defeatSaturation = -100f;
    [SerializeField] private float _defeatFadeSeconds = 1f;

    [Header("승리 밝기")]
    [SerializeField] private float _victoryExposure = 0.4f;
    [SerializeField] private float _victoryInSeconds = 0.2f;
    [SerializeField] private float _victoryOutSeconds = 0.8f;

    private Volume _hitVolume;
    private Volume _defeatVolume;
    private Volume _victoryVolume;

    #region Unity Methods

    /// <summary>
    /// 효과용 Volume을 만들고 이벤트를 구독한다.
    /// </summary>
    public override UniTask InitializeAsync()
    {
        _hitVolume = CreateVolume("HitVolume", profile =>
        {
            var vignette = profile.Add<Vignette>();
            vignette.color.Override(_hitColor);
            vignette.intensity.Override(_hitIntensity);
            vignette.smoothness.Override(_hitSmoothness);
        });

        _defeatVolume = CreateVolume("DefeatVolume", profile =>
        {
            profile.Add<ColorAdjustments>().saturation.Override(_defeatSaturation);
        });

        _victoryVolume = CreateVolume("VictoryVolume", profile =>
        {
            profile.Add<ColorAdjustments>().postExposure.Override(_victoryExposure);
        });

        if (Managers.Instance != null && Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            eventManager.Subscribe<PlayerDamaged>(HandlePlayerDamaged);
            eventManager.Subscribe<GameStateChanged>(HandleGameStateChanged);
            eventManager.Subscribe<ReturnedToTitle>(HandleReturnedToTitle);
        }

        return base.InitializeAsync();
    }

    public override void Cleanup()
    {
        if (Managers.Instance != null && Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            eventManager.Unsubscribe<PlayerDamaged>(HandlePlayerDamaged);
            eventManager.Unsubscribe<GameStateChanged>(HandleGameStateChanged);
            eventManager.Unsubscribe<ReturnedToTitle>(HandleReturnedToTitle);
        }

        DestroyVolume(_hitVolume);
        DestroyVolume(_defeatVolume);
        DestroyVolume(_victoryVolume);
        _hitVolume = null;
        _defeatVolume = null;
        _victoryVolume = null;
        base.Cleanup();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 모든 화면 효과를 즉시 끈다.
    /// </summary>
    public void ResetEffects()
    {
        SetWeight(_hitVolume, 0f);
        SetWeight(_defeatVolume, 0f);
        SetWeight(_victoryVolume, 0f);
    }

    #endregion

    #region Private Methods

    private void HandlePlayerDamaged(PlayerDamaged e)
    {
        if (!Managers.Instance.TryGetManager<PlayerManager>(out var playerManager) || e.PlayerId != playerManager.LocalPlayerId)
        {
            return;
        }

        Pulse(_hitVolume, _hitInSeconds, _hitOutSeconds);
    }

    private void HandleGameStateChanged(GameStateChanged e)
    {
        switch (e.Next)
        {
            case GameState.Defeat:
                FadeWeight(_defeatVolume, 1f, _defeatFadeSeconds);
                break;
            case GameState.Victory:
                Pulse(_victoryVolume, _victoryInSeconds, _victoryOutSeconds);
                break;
            case GameState.Menu:
            case GameState.Loading:
                ResetEffects();
                break;
            case GameState.Playing when e.Previous == GameState.Loading:
                ResetEffects();
                break;
        }
    }

    private void HandleReturnedToTitle(ReturnedToTitle e)
    {
        ResetEffects();
    }

    private Volume CreateVolume(string volumeName, System.Action<VolumeProfile> setup)
    {
        var go = new GameObject(volumeName);
        go.transform.SetParent(transform, false);

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = volumeName;
        setup(profile);

        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = EFFECT_PRIORITY;
        volume.weight = 0f;
        volume.sharedProfile = profile;
        return volume;
    }

    private static void DestroyVolume(Volume volume)
    {
        if (volume == null)
        {
            return;
        }

        DOTween.Kill(volume);
        Destroy(volume.sharedProfile);
        Destroy(volume.gameObject);
    }

    private static void SetWeight(Volume volume, float weight)
    {
        if (volume == null)
        {
            return;
        }

        DOTween.Kill(volume);
        volume.weight = weight;
    }

    private static void FadeWeight(Volume volume, float to, float duration)
    {
        if (volume == null)
        {
            return;
        }

        DOTween.Kill(volume);
        DOTween.To(() => volume.weight, value => volume.weight = value, to, duration)
            .SetTarget(volume)
            .SetUpdate(true);
    }

    private static void Pulse(Volume volume, float inSeconds, float outSeconds)
    {
        if (volume == null)
        {
            return;
        }

        DOTween.Kill(volume);
        DOTween.Sequence()
            .Append(DOTween.To(() => volume.weight, value => volume.weight = value, 1f, inSeconds))
            .Append(DOTween.To(() => volume.weight, value => volume.weight = value, 0f, outSeconds))
            .SetTarget(volume)
            .SetUpdate(true);
    }

    #endregion
}
