using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 창 왼쪽의 구매 목록. 가격 버튼을 누르면 몬스터볼로 사고, 잠근 칸은 새로고침에서 빠진다.
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
        public MonsterTypeView Types;
        public Button Lock;
        public Image LockIcon;
        public Button Buy;
        public Image PriceIcon;
        public TMP_Text PriceCount;
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
            if (row == null || row.Slot == null || row.Buy == null)
            {
                Debug.LogError("상점 칸이나 가격 버튼이 비어 있습니다: " + i);
                continue;
            }

            var index = i;
            row.Buy.onClick.AddListener(() => Purchase(index));
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
                SetPrice(row, null, 0, false);
                SetName(row, SOLD_LABEL);
                SetTypes(row, null);
                SetLock(row, false, false);
                continue;
            }

            var visual = itemManager.GetVisual(item.uid);
            var portrait = visual != null ? MonsterVisualData.FirstFrame(visual.Icon) : null;
            row.Slot.ShowPokemon(portrait, item.name, Item.STAR_MIN, 1, false, false);
            SetPrice(row, icon, item.price, true);
            SetName(row, item.name);
            SetTypes(row, visual);
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

    private void SetTypes(OfferRow row, MonsterVisualData visual)
    {
        if (row.Types != null)
        {
            row.Types.Show(visual);
        }
    }

    /// <summary>
    /// 가격 버튼에 몬스터볼 그림과 가격을 넣는다. 매진이면 버튼을 끈다.
    /// </summary>
    private void SetPrice(OfferRow row, Sprite icon, int amount, bool available)
    {
        if (row.Buy != null)
        {
            row.Buy.gameObject.SetActive(available);
        }

        if (row.PriceIcon != null)
        {
            row.PriceIcon.sprite = icon;
            row.PriceIcon.enabled = icon != null;
            row.PriceIcon.preserveAspect = true;
        }

        if (row.PriceCount != null)
        {
            row.PriceCount.text = available ? amount.ToString() : string.Empty;
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
