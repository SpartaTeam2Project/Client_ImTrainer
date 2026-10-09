using UnityEngine;

/// <summary>
/// 결과 화면 아이콘과 이름 조회를 한곳에 모은다. 판이 끝난 뒤에도 카탈로그와 데이터베이스는 남아 있다.
/// </summary>
public static class StageResultIcons
{
    public static Sprite Monster(int uid)
    {
        if (uid < 0 || Managers.Instance == null || !Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
        {
            return null;
        }

        var visual = itemManager.GetVisual(uid);
        return visual != null ? MonsterVisualData.FirstFrame(visual.Icon) : null;
    }

    public static string MonsterName(int uid)
    {
        if (uid < 0 || Managers.Instance == null || !Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
        {
            return string.Empty;
        }

        var item = itemManager.TryGetItem(uid);
        return item != null ? item.name : string.Empty;
    }

    public static Sprite Trainer()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AccountManager>(out var account))
        {
            return null;
        }

        var playable = account.ResolveSelectedPlayable();
        return playable != null ? playable.Portrait : null;
    }

    public static Sprite Ability(WeaponAbilitiesDatabase database, WeaponAbilityType abilityType)
    {
        var ability = database != null ? database.GetWeaponAbility(abilityType) : null;
        return ability != null ? ability.Icon : null;
    }

    public static Sprite Currency(string currencyId)
    {
        return TryGetCurrencies(out var currencies) ? currencies.GetIcon(currencyId) : null;
    }

    public static string CurrencyName(string currencyId)
    {
        return TryGetCurrencies(out var currencies) ? currencies.GetName(currencyId) : currencyId;
    }

    private static bool TryGetCurrencies(out CurrenciesManager currencies)
    {
        currencies = null;
        return Managers.Instance != null && Managers.Instance.TryGetManager(out currencies);
    }
}
