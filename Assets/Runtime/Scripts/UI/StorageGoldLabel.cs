using TMPro;

/// <summary>
/// 스토리지 창에 로컬 플레이어의 보유 골드를 보여 주고, 바뀌면 다시 쓴다.
/// </summary>
public sealed class StorageGoldLabel
{
    private readonly TMP_Text _text;
    private bool _subscribed;

    public StorageGoldLabel(TMP_Text text)
    {
        _text = text;
    }

    public void Subscribe()
    {
        if (_subscribed || Managers.Instance == null || !Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            return;
        }

        eventManager.Subscribe<CurrencyAmountChanged>(HandleChanged);
        _subscribed = true;
    }

    public void Unsubscribe()
    {
        if (!_subscribed || Managers.Instance == null || !Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            _subscribed = false;
            return;
        }

        eventManager.Unsubscribe<CurrencyAmountChanged>(HandleChanged);
        _subscribed = false;
    }

    public void Refresh()
    {
        var amount = 0;
        if (Managers.Instance != null && Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies))
        {
            var save = currencies.GetCurrency(ResolvePlayerId(), CurrenciesManager.POCKET_DOLLAR_ID, true);
            if (save != null)
            {
                amount = save.Amount;
            }
        }

        SetText(amount);
    }

    private void HandleChanged(CurrencyAmountChanged changed)
    {
        if (!changed.IsMetaBalance || changed.CurrencyId != CurrenciesManager.POCKET_DOLLAR_ID || changed.PlayerId != ResolvePlayerId())
        {
            return;
        }

        SetText(changed.Amount);
    }

    private static int ResolvePlayerId()
    {
        if (Managers.Instance != null && Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return playerManager.LocalPlayerId;
        }

        return 1;
    }

    private void SetText(int amount)
    {
        if (_text != null)
        {
            _text.text = amount.ToString();
        }
    }
}
