using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 창 왼쪽의 구매 목록. 칸을 누르면 몬스터볼로 사고, 잠근 칸은 새로고침에서 빠진다.
/// </summary>
public class ShopUi : MonoBehaviour
{
    private const string SOLD_LABEL = "매진";

    private static readonly Color LOCKED_COLOR = new Color(1f, 0.82f, 0.3f, 1f);
    private static readonly Color UNLOCKED_COLOR = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Serializable]
    private class OfferRow
    {
        public InventoryItem Slot;
        public TMP_Text Name;
        public Button Lock;
        public Image LockIcon;
    }

    [SerializeField] private OfferRow[] _rows = new OfferRow[ItemManager.SHOP_OFFER_COUNT];
    [SerializeField] private Button _refreshButton;
    [SerializeField] private TMP_Text _refreshPrice;
    [SerializeField] private Sprite _lockedSprite;
    [SerializeField] private Sprite _unlockedSprite;

    private Action _onChanged;

    private void Awake()
    {
        for (var i = 0; i < _rows.Length; i++)
        {
            var row = _rows[i];
            if (row == null || row.Slot == null)
            {
                Debug.LogError("상점 칸이 비어 있습니다: " + i);
                continue;
            }

            var index = i;
            row.Slot.Button.onClick.AddListener(() => Purchase(index));
            if (row.Lock != null)
            {
                row.Lock.onClick.AddListener(() => ToggleLock(index));
            }
        }

        if (_refreshButton != null)
        {
            _refreshButton.onClick.AddListener(RefreshOffers);
        }

        if (_refreshPrice != null)
        {
            _refreshPrice.text = ItemManager.SHOP_REFRESH_PRICE.ToString();
        }
    }

    /// <summary>
    /// 구매나 새로고침이 끝나면 onChanged를 호출한다.
    /// </summary>
    public void Bind(Action onChanged)
    {
        _onChanged = onChanged;
    }

    /// <summary>
    /// 상점 칸과 잠금 상태를 다시 그린다.
    /// </summary>
    public void Refresh(int playerId)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
        {
            return;
        }

        var offers = itemManager.GetShopOffers(playerId);
        var icon = MonsterBallIcon();
        for (var i = 0; i < _rows.Length; i++)
        {
            var row = _rows[i];
            if (row == null || row.Slot == null)
            {
                continue;
            }

            var offer = offers != null && i < offers.Count ? offers[i] : null;
            var item = offer != null && !offer.Sold ? itemManager.TryGetItem(offer.Uid) : null;
            if (item == null)
            {
                row.Slot.ShowEmpty();
                row.Slot.HidePrice();
                SetName(row, SOLD_LABEL);
                SetLock(row, false, false);
                continue;
            }

            var visual = itemManager.GetVisual(item.uid);
            var portrait = visual != null ? MonsterVisualData.FirstFrame(visual.Icon) : null;
            row.Slot.ShowPokemon(portrait, item.name, Item.STAR_MIN, 1, false, false);
            row.Slot.ShowPrice(icon, item.price);
            SetName(row, item.name);
            SetLock(row, true, offer.Locked);
        }
    }

    /// <summary>
    /// 몬스터볼을 내고 잠기지 않은 칸을 다시 뽑는다. 버튼과 F키가 같이 쓴다.
    /// </summary>
    public void RefreshOffers()
    {
        if (!TryGetContext(out var itemManager, out var playerId))
        {
            return;
        }

        itemManager.TryRefreshShop(playerId);
        _onChanged?.Invoke();
    }

    private void Purchase(int index)
    {
        if (!TryGetContext(out var itemManager, out var playerId))
        {
            return;
        }

        itemManager.TryPurchase(playerId, index);
        _onChanged?.Invoke();
    }

    private void ToggleLock(int index)
    {
        if (!TryGetContext(out var itemManager, out var playerId))
        {
            return;
        }

        itemManager.ToggleShopLock(playerId, index);
        _onChanged?.Invoke();
    }

    private void SetName(OfferRow row, string value)
    {
        if (row.Name != null)
        {
            row.Name.text = value;
        }
    }

    private void SetLock(OfferRow row, bool available, bool locked)
    {
        if (row.Lock != null)
        {
            row.Lock.interactable = available;
        }

        if (row.LockIcon == null)
        {
            return;
        }

        var sprite = locked ? _lockedSprite : _unlockedSprite;
        if (sprite != null)
        {
            row.LockIcon.sprite = sprite;
        }

        row.LockIcon.color = locked ? LOCKED_COLOR : UNLOCKED_COLOR;
    }

    private static bool TryGetContext(out ItemManager itemManager, out int playerId)
    {
        itemManager = null;
        playerId = 0;
        if (Managers.Instance == null
            || !Managers.Instance.TryGetManager<ItemManager>(out itemManager)
            || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return false;
        }

        playerId = playerManager.LocalPlayerId;
        return true;
    }

    private static Sprite MonsterBallIcon()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies))
        {
            return null;
        }

        return currencies.GetIcon(CurrenciesManager.MONSTER_BALL_ID);
    }
}
