using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스토리지에서 포커스된 트레이너나 포켓몬의 초상화, 세대, 이름, 잠금 조건을 보여 준다.
/// </summary>
public class StorageInfoView : MonoBehaviour
{
    private const string GENERATION_SUFFIX = "세대";
    private const string DEX_NAME_FORMAT = "No.{0:0000} {1}";
    private static readonly Color32 LOCKED_PORTRAIT_COLOR = new Color32(0, 0, 0, 237);
    private static readonly Color32 UNLOCKED_PORTRAIT_COLOR = new Color32(255, 255, 255, 255);

    [SerializeField] private Image _portrait;
    [SerializeField] private TMP_Text _generation;
    [SerializeField] private TMP_Text _characterName;
    [SerializeField] private TMP_Text _unlockCondition;

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
        ApplyPortrait(data.Portrait, !slot.IsUnlocked);
        if (_generation != null)
        {
            _generation.text = data.Generation + GENERATION_SUFFIX;
        }

        if (_characterName != null)
        {
            _characterName.text = data.CharacterName;
        }

        ApplyUnlockCondition(!slot.IsUnlocked, data.UnlockCondition);
    }

    /// <summary>
    /// 포커스된 포켓몬의 초상화, 세대, 도감 번호, 이름, 잠금 조건을 보여 준다.
    /// </summary>
    public void Show(StorageMonsterView slot)
    {
        if (slot == null || slot.Data == null)
        {
            Clear();
            return;
        }

        var data = slot.Data;
        ApplyPortrait(MonsterVisualData.FirstFrame(data.InfoAnimation), !slot.IsUnlocked);
        if (_generation != null)
        {
            _generation.text = data.Generation + GENERATION_SUFFIX;
        }

        if (_characterName != null)
        {
            _characterName.text = string.Format(DEX_NAME_FORMAT, data.DexNumber, data.MonsterName);
        }

        ApplyUnlockCondition(!slot.IsUnlocked, data.UnlockCondition);
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
