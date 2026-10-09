using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 중앙 위 파티 배치. 트레이너 초상과 레벨, 둘레의 장착 칸 여섯 개.
/// 칸은 인벤토리 장착 칸과 같은 InventoryItem을 보기 전용으로 쓴다.
/// </summary>
public class StagePartyPanel : MonoBehaviour
{
    // 장착 칸 한 칸에는 포켓몬이 하나만 들어간다.
    private const int SLOT_COUNT = 1;

    [SerializeField] private Image _trainer;
    [SerializeField] private TMP_Text _level;
    [SerializeField] private InventoryItem[] _slots = new InventoryItem[WeaponSlots.MAX_COUNT];

    private void Awake()
    {
        for (var i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
            {
                Debug.LogError("결과 화면 파티 칸이 비어 있습니다: " + i);
                continue;
            }

            if (_slots[i].Button != null)
            {
                _slots[i].Button.interactable = false;
            }
        }
    }

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
                _slots[i].ShowEmpty();
            }
        }

        var party = result.Party;
        for (var i = 0; i < party.Length; i++)
        {
            var slot = party[i].Slot;
            if (slot >= 0 && slot < _slots.Length && _slots[slot] != null)
            {
                var uid = party[i].Uid;
                _slots[slot].ShowPokemon(StageResultIcons.Monster(uid), StageResultIcons.MonsterName(uid),
                    party[i].UpgradeLevel, SLOT_COUNT, false, false);
            }
        }
    }
}
