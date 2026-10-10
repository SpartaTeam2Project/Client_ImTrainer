using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스토리지에서 포커스된 트레이너나 포켓몬의 초상화, 세대(포켓몬은 도감 번호), 이름, 잠금 조건을 보여 준다.
/// </summary>
public class StorageInfoView : MonoBehaviour
{
    private const string GENERATION_SUFFIX = "세대";
    private const string DEX_NUMBER_FORMAT = "No.{0:0000}";
    private const string LOCKED_NAME = "???";
    private const float DEFAULT_REFERENCE_PIXELS = 96f;
    private static readonly Color32 LOCKED_PORTRAIT_COLOR = new Color32(0, 0, 0, 237);
    private static readonly Color32 UNLOCKED_PORTRAIT_COLOR = new Color32(255, 255, 255, 255);

    [SerializeField] private Image _portrait;
    [SerializeField] private TMP_Text _generation;
    [SerializeField] private TMP_Text _characterName;
    [SerializeField] private TMP_Text _unlockCondition;
    [Tooltip("포켓몬 초상화에서 이 원본 픽셀 수가 칸의 짧은 변 길이가 된다. 모든 포켓몬이 같은 배율이라 덩치 차이가 보인다")]
    [SerializeField, Min(1f)] private float _referencePixels = DEFAULT_REFERENCE_PIXELS;
    [Tooltip("포켓몬 초상화 애니메이션의 초당 장 수. 원본은 10이다")]
    [SerializeField, Min(1f)] private float _portraitFrameRate = PortraitFrameLoop.DEFAULT_FRAME_RATE;

    private PortraitFitter _portraitFitter;
    private PortraitFrameLoop _portraitLoop;

    private PortraitFitter PortraitFitter => _portraitFitter ??= new PortraitFitter(_portrait);

    private PortraitFrameLoop PortraitLoop => _portraitLoop ??= new PortraitFrameLoop(_portrait, PortraitFitter);

    /// <summary>
    /// 포커스된 칸의 정보를 채운다. 칸이 없으면 비운다.
    /// </summary>
    public void Show(StorageCharacterView slot)
    {
        if (slot == null || slot.Data == null)
        {
            Clear();
            return;
        }

        var data = slot.Data;
        StopMonsterPortrait();
        ApplyPortrait(data.Portrait, !slot.IsUnlocked);
        if (_generation != null)
        {
            _generation.text = data.Generation + GENERATION_SUFFIX;
        }

        if (_characterName != null)
        {
            _characterName.text = slot.IsUnlocked ? data.CharacterName : LOCKED_NAME;
        }

        ApplyUnlockCondition(!slot.IsUnlocked, data.UnlockCondition);
    }

    /// <summary>
    /// 포커스된 포켓몬의 초상화, 도감 번호, 이름, 잠금 조건을 보여 준다. 획득한 포켓몬은 초상화 애니메이션을 반복 재생한다.
    /// </summary>
    public void Show(StorageMonsterView slot)
    {
        if (slot == null || slot.Data == null)
        {
            Clear();
            return;
        }

        var data = slot.Data;
        // 잠긴 칸은 첫 장 실루엣만 보여 준다.
        PortraitFitter.Fit(data.InfoAnimation, _referencePixels);
        var portrait = PortraitLoop.Play(data.InfoAnimation, slot.IsUnlocked, _portraitFrameRate);
        ApplyPortrait(portrait, !slot.IsUnlocked);
        if (_generation != null)
        {
            _generation.text = string.Format(DEX_NUMBER_FORMAT, data.DexNumber);
        }

        if (_characterName != null)
        {
            _characterName.text = slot.IsUnlocked ? data.MonsterName : LOCKED_NAME;
        }

        ApplyUnlockCondition(!slot.IsUnlocked, data.UnlockCondition);
    }

    private void Update()
    {
        _portraitLoop?.Tick(Time.unscaledDeltaTime);
    }

    private void StopMonsterPortrait()
    {
        _portraitLoop?.Stop();
        PortraitFitter.Restore();
    }

    private void ApplyPortrait(Sprite portrait, bool locked = false)
    {
        if (_portrait == null)
        {
            return;
        }

        _portrait.sprite = portrait;
        _portrait.preserveAspect = true;
        _portrait.enabled = portrait != null;
        _portrait.color = locked ? LOCKED_PORTRAIT_COLOR : UNLOCKED_PORTRAIT_COLOR;
    }

    private void ApplyUnlockCondition(bool locked, string condition)
    {
        if (_unlockCondition == null)
        {
            return;
        }

        _unlockCondition.gameObject.SetActive(locked);
        if (locked)
        {
            _unlockCondition.text = condition ?? string.Empty;
        }
    }

    private void Clear()
    {
        StopMonsterPortrait();
        ApplyPortrait(null, false);
        if (_generation != null)
        {
            _generation.text = string.Empty;
        }

        if (_characterName != null)
        {
            _characterName.text = string.Empty;
        }

        ApplyUnlockCondition(false, string.Empty);
    }
}
