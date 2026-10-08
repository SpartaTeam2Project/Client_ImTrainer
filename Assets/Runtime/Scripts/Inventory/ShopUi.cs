using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 창 왼쪽의 구매 목록. 가격 버튼을 누르면 몬스터볼로 사고, 잠근 칸은 새로고침에서 빠진다.
/// 사면 가격 버튼의 볼이 열린 뒤 도장이 찍힌다.
/// </summary>
public class ShopUi : MonoBehaviour
{
    private const string SOLD_LABEL = "매진";
    private const string BALL_OPEN_SOUND = "shop_pb";
    private const string STAMP_SOUND = "shop_stamp";
    private const string REROLL_SOUND = "shop_reroll";
    private const string ERROR_SOUND = "error";

    private static readonly Color LOCKED_COLOR = Color.white;
    private static readonly Color UNLOCKED_COLOR = new Color(0.6f, 0.6f, 0.6f, 1f);

    private const float STAMP_START_SCALE = 2.2f;
    private const float STAMP_START_ANGLE = -13f;
    private const float STAMP_DROP_DURATION = 0.22f;
    private const float STAMP_FADE_DURATION = 0.12f;
    private const float DIM_FADE_DURATION = 0.2f;
    // 볼이 열리는 그림 한 장을 보여 주는 시간.
    private const float BALL_FRAME_DURATION = 0.12f;

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
        public GameObject Captured;
        public Image Dim;
        public Image Stamp;
    }

    /// <summary>
    /// 도장이 다 찍혔을 때의 모습. 프리팹에 놓인 값을 그대로 쓴다.
    /// </summary>
    private struct CapturedPose
    {
        public float DimAlpha;
        public Vector3 StampScale;
        public Vector3 StampAngles;
    }

    [SerializeField] private OfferRow[] _rows = new OfferRow[ItemManager.SHOP_OFFER_COUNT];
    [SerializeField] private Button _refreshButton;
    [SerializeField] private TMP_Text _refreshPrice;
    [SerializeField] private Sprite _lockedSprite;
    [SerializeField] private Sprite _unlockedSprite;

    private Action _onChanged;
    private Action<int> _onSelect;
    private IShopPurchaseEffect _purchaseEffect;
    private CapturedPose[] _poses;
    private Sequence[] _stampTweens;
    private int _pendingStampIndex = -1;
    private PressFlash _refreshPress;

    private void Awake()
    {
        EnsureCapturedState();
        for (var i = 0; i < _rows.Length; i++)
        {
            var row = _rows[i];
            if (row == null || row.Slot == null || row.Buy == null)
            {
                Debug.LogError("상점 칸이나 가격 버튼이 비어 있습니다: " + i);
                continue;
            }

            var index = i;
            row.Slot.Button.onClick.AddListener(() => _onSelect?.Invoke(index));
            row.Buy.onClick.AddListener(() => Purchase(index));
            if (row.Lock != null)
            {
                row.Lock.onClick.AddListener(() => ToggleLock(index));
            }
        }

        if (_refreshButton != null)
        {
            _refreshButton.onClick.AddListener(() =>
            {
                _refreshPress.Punch();
                RefreshOffers();
            });
        }

        _refreshPress = PressFlash.ForButton(_refreshButton);

        if (_refreshPrice != null)
        {
            _refreshPrice.text = ItemManager.SHOP_REFRESH_PRICE.ToString();
        }
    }

    /// <summary>
    /// 연출 도중 창이 닫히면 가격 버튼을 끄고 도장을 다 찍힌 모습으로 맞춘다.
    /// </summary>
    private void OnDisable()
    {
        _pendingStampIndex = -1;
        _refreshPress?.Release();
        for (var i = 0; i < _rows.Length; i++)
        {
            if (_stampTweens[i] != null)
            {
                ShowCaptured(i);
            }
        }
    }

    /// <summary>
    /// 구매나 새로고침이 끝나면 onChanged를, 포켓몬 칸을 누르면 onSelect(칸 번호)를 호출한다.
    /// 구매 연출의 각 단계는 purchaseEffect에 알린다. 없으면 null.
    /// </summary>
    public void Bind(Action onChanged, Action<int> onSelect, IShopPurchaseEffect purchaseEffect)
    {
        _onChanged = onChanged;
        _onSelect = onSelect;
        _purchaseEffect = purchaseEffect;
    }

    /// <summary>
    /// 상점 칸과 잠금 상태를 다시 그린다. selectedIndex 칸에 선택 표시를 켠다. 없으면 -1.
    /// </summary>
    public void Refresh(int playerId, int selectedIndex)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
        {
            return;
        }

        EnsureCapturedState();
        var offers = itemManager.GetShopOffers(playerId);
        for (var i = 0; i < _rows.Length; i++)
        {
            var row = _rows[i];
            if (row == null || row.Slot == null)
            {
                continue;
            }

            var offer = offers != null && i < offers.Count ? offers[i] : null;
            var item = itemManager.CreateOfferItem(offer);
            if (item == null)
            {
                row.Slot.ShowEmpty();
                SetPrice(row, null, 0, false);
                SetName(row, SOLD_LABEL);
                SetTypes(row, null);
                SetLock(row, false, false);
                HideCaptured(i);
                continue;
            }

            var visual = itemManager.GetVisual(item.uid);
            var portrait = visual != null ? MonsterVisualData.FirstFrame(visual.Icon) : null;
            row.Slot.ShowPokemon(portrait, item.name, item.upgradeLevel, 1, false, !offer.Purchased && i == selectedIndex);
            SetName(row, item.name);
            SetTypes(row, visual);
            if (offer.Purchased)
            {
                SetLock(row, false, false);
                RefreshCaptured(i, item.currencyId);
                continue;
            }

            SetPrice(row, CurrencyIcon(item.currencyId), item.price, true);
            SetLock(row, true, offer.Locked);
            HideCaptured(i);
        }
    }

    /// <summary>
    /// F키로 새로고침한다. 버튼을 거치지 않아서 잠깐 눌린 그림으로 바꿔 누른 느낌을 준다.
    /// </summary>
    public void PressRefresh()
    {
        _refreshPress.Play();
        RefreshOffers();
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

        if (itemManager.TryRefreshShop(playerId))
        {
            PlaySound(REROLL_SOUND);
        }
        else
        {
            PlaySound(ERROR_SOUND);
        }

        _onChanged?.Invoke();
    }

    /// <summary>
    /// 해당 칸 포켓몬을 산다. 버튼과 숫자키(1~4)가 같이 쓴다.
    /// </summary>
    public void Purchase(int index)
    {
        if (index < 0 || index >= ItemManager.SHOP_OFFER_COUNT)
        {
            return;
        }

        if (!TryGetContext(out var itemManager, out var playerId))
        {
            return;
        }

        // 구매 중에 나가는 InventoryChanged로 Refresh가 먼저 불리므로 산다고 미리 표시해 둔다.
        _pendingStampIndex = index;
        if (!itemManager.TryPurchase(playerId, index))
        {
            _pendingStampIndex = -1;
        }

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

    /// <summary>
    /// 방금 산 칸이면 볼을 열고 도장을 찍고, 이미 산 칸이면 찍힌 모습으로 둔다. 연출 중이면 그대로 둔다.
    /// </summary>
    private void RefreshCaptured(int index, string currencyId)
    {
        if (index == _pendingStampIndex)
        {
            _pendingStampIndex = -1;
            PlayPurchase(index, currencyId);
            return;
        }

        if (_stampTweens[index] == null)
        {
            ShowCaptured(index);
        }
    }

    /// <summary>
    /// 가격 버튼의 볼 그림이 열리는 그림으로 차례로 바뀐 뒤 버튼이 사라진다.
    /// 이어서 Dim이 서서히 어두워지고, 도장이 크게 떠 있다가 내려와 쾅 찍힌다.
    /// </summary>
    private void PlayPurchase(int index, string currencyId)
    {
        var row = _rows[index];
        KillStamp(index);
        if (row.Buy != null)
        {
            row.Buy.interactable = false;
        }

        _purchaseEffect?.OnPurchased(index);
        var sequence = DOTween.Sequence();
        if (row.PriceIcon != null && TryGetOpenIcons(currencyId, out var opening, out var open))
        {
            // 볼 소리는 누르자마자 나고, 그림은 한 박자 뒤부터 바뀐다.
            PlaySound(BALL_OPEN_SOUND);
            sequence.AppendInterval(BALL_FRAME_DURATION);
            sequence.AppendCallback(() => row.PriceIcon.sprite = opening);
            sequence.AppendInterval(BALL_FRAME_DURATION);
            sequence.AppendCallback(() =>
            {
                row.PriceIcon.sprite = open;
                _purchaseEffect?.OnBallOpened(index, row.PriceIcon.rectTransform);
            });
            sequence.AppendInterval(BALL_FRAME_DURATION);
        }
        else if (row.Buy != null)
        {
            // 열리는 그림이 없어도 가격 버튼이 사라지기 전에 광선은 쏜다.
            sequence.AppendCallback(() => _purchaseEffect?.OnBallOpened(index, (RectTransform)row.Buy.transform));
        }

        sequence.AppendCallback(() => SetPrice(row, null, 0, false));
        var stampAt = sequence.Duration();
        if (row.Captured != null)
        {
            var pose = _poses[index];
            row.Captured.SetActive(true);
            // 커진 도장이 아래 칸에 가리지 않게 이 칸을 맨 위에 그린다.
            row.Captured.transform.parent.SetAsLastSibling();

            if (row.Dim != null)
            {
                SetAlpha(row.Dim, 0f);
                sequence.Insert(stampAt, row.Dim.DOFade(pose.DimAlpha, DIM_FADE_DURATION));
            }

            if (row.Stamp != null)
            {
                var stamp = row.Stamp.rectTransform;
                stamp.localScale = pose.StampScale * STAMP_START_SCALE;
                stamp.localEulerAngles = pose.StampAngles + new Vector3(0f, 0f, STAMP_START_ANGLE);
                SetAlpha(row.Stamp, 0f);
                sequence.Insert(stampAt, stamp.DOScale(pose.StampScale, STAMP_DROP_DURATION).SetEase(Ease.InBack));
                sequence.Insert(stampAt, stamp.DOLocalRotate(pose.StampAngles, STAMP_DROP_DURATION).SetEase(Ease.InQuad));
                sequence.Insert(stampAt, row.Stamp.DOFade(1f, STAMP_FADE_DURATION));
                // 도장이 다 내려와 닿는 순간에 소리를 낸다.
                sequence.InsertCallback(stampAt + STAMP_DROP_DURATION, () => PlaySound(STAMP_SOUND));
            }
        }

        sequence.SetUpdate(true).SetLink(gameObject).OnComplete(() => _stampTweens[index] = null);
        _stampTweens[index] = sequence;
    }

    private void ShowCaptured(int index)
    {
        KillStamp(index);
        var row = _rows[index];
        SetPrice(row, null, 0, false);
        if (row.Captured == null)
        {
            return;
        }

        var pose = _poses[index];
        row.Captured.SetActive(true);
        if (row.Dim != null)
        {
            SetAlpha(row.Dim, pose.DimAlpha);
        }

        if (row.Stamp != null)
        {
            row.Stamp.rectTransform.localScale = pose.StampScale;
            row.Stamp.rectTransform.localEulerAngles = pose.StampAngles;
            SetAlpha(row.Stamp, 1f);
        }
    }

    private void HideCaptured(int index)
    {
        KillStamp(index);
        var row = _rows[index];
        if (row.Captured != null)
        {
            row.Captured.SetActive(false);
        }
    }

    private void KillStamp(int index)
    {
        if (_stampTweens[index] == null)
        {
            return;
        }

        _stampTweens[index].Kill();
        _stampTweens[index] = null;
    }

    /// <summary>
    /// 창이 꺼진 채로 Refresh가 먼저 불릴 수 있어서 Awake와 Refresh 둘 다에서 준비한다.
    /// </summary>
    private void EnsureCapturedState()
    {
        if (_poses != null)
        {
            return;
        }

        _poses = new CapturedPose[_rows.Length];
        _stampTweens = new Sequence[_rows.Length];
        for (var i = 0; i < _rows.Length; i++)
        {
            _poses[i] = ReadPose(_rows[i]);
        }
    }

    private static CapturedPose ReadPose(OfferRow row)
    {
        var pose = new CapturedPose { DimAlpha = 1f, StampScale = Vector3.one, StampAngles = Vector3.zero };
        if (row == null)
        {
            return pose;
        }

        if (row.Dim != null)
        {
            pose.DimAlpha = row.Dim.color.a;
        }

        if (row.Stamp != null)
        {
            pose.StampScale = row.Stamp.rectTransform.localScale;
            pose.StampAngles = row.Stamp.rectTransform.localEulerAngles;
        }

        return pose;
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        var color = graphic.color;
        color.a = alpha;
        graphic.color = color;
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
    /// 가격 버튼에 화폐 그림과 가격을 넣는다. 매진이면 버튼을 끈다.
    /// </summary>
    private void SetPrice(OfferRow row, Sprite icon, int amount, bool available)
    {
        if (row.Buy != null)
        {
            row.Buy.gameObject.SetActive(available);
            row.Buy.interactable = available;
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

    private static Sprite CurrencyIcon(string currencyId)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies))
        {
            return null;
        }

        return currencies.GetIcon(currencyId);
    }

    private static void PlaySound(string name)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(name);
    }

    private static bool TryGetOpenIcons(string currencyId, out Sprite opening, out Sprite open)
    {
        opening = null;
        open = null;
        return Managers.Instance != null
            && Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies)
            && currencies.TryGetOpenIcons(currencyId, out opening, out open);
    }
}
