using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 중앙 위 파티 배치. 트레이너 초상과 레벨, 둘레의 장착 칸 여섯 개.
/// </summary>
public class StagePartyPanel : MonoBehaviour
{
    [SerializeField] private Image _trainer;
    [SerializeField] private TMP_Text _level;
    [SerializeField] private StagePartySlotView[] _slots = new StagePartySlotView[WeaponSlots.MAX_COUNT];

    public void Bind(StageResult result)
    {
        if (_trainer != null)
        {
            _trainer.sprite = StageResultIcons.Trainer();
            _trainer.enabled = _trainer.sprite != null;
        }

        if (_level != null)
        {
            _level.text = "Lv." + result.ReachedLevel;
        }

        for (var i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null)
            {
                _slots[i].Clear();
            }
        }

        var party = result.Party;
        for (var i = 0; i < party.Length; i++)
        {
            var slot = party[i].Slot;
            if (slot >= 0 && slot < _slots.Length && _slots[slot] != null)
            {
                _slots[slot].Bind(StageResultIcons.Monster(party[i].Uid), party[i].UpgradeLevel);
            }
        }
    }
}
