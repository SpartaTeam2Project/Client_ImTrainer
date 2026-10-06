using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 몬스터볼은 스테이지 안에서만 쌓고, 포켓달러는 판이 끝나면 메타 잔액에 남긴다.
/// </summary>
public class CurrenciesManager : BaseManager
{
    public const string MONSTER_BALL_ID = "monster_ball";
    public const string SUPER_BALL_ID = "super_ball";
    public const string HYPER_BALL_ID = "hyper_ball";
    public const string MASTER_BALL_ID = "master_ball";
    public const string POCKET_DOLLAR_ID = "pocket_dollar";

    private const string META_PREFS_PREFIX = "currency_meta_";

    [SerializeField] private CurrenciesDatabase _database;

    private readonly Dictionary<int, Dictionary<string, CurrencySave>> _stageAmounts = new Dictionary<int, Dictionary<string, CurrencySave>>();
    private readonly Dictionary<int, Dictionary<string, CurrencySave>> _metaAmounts = new Dictionary<int, Dictionary<string, CurrencySave>>();

    private bool _missingDatabaseLogged;

    #region Unity Methods

    /// <summary>
    /// 재화 매니저 초기화
    /// </summary>
    public override UniTask InitializeAsync()
    {
        if (_database == null)
        {
            LogMissingDatabase();
        }

        return base.InitializeAsync();
    }

    /// <summary>
    /// 메모리에 둔 스테이지 수량을 비운다. 저장된 메타 잔액은 남긴다.
    /// </summary>
    public override void Cleanup()
    {
        _stageAmounts.Clear();
        _metaAmounts.Clear();
        base.Cleanup();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 플레이어의 재화 수량을 가져온다. meta가 true면 판이 끝나도 남는 잔액이다.
    /// </summary>
    public CurrencySave GetCurrency(int playerId, string currencyId, bool meta)
    {
        var data = FindCurrency(currencyId);
        if (data == null)
        {
            return null;
        }

        if (meta)
        {
            return data.KeepAfterStage ? GetOrCreateMeta(playerId, currencyId) : null;
        }

        return GetOrCreateStage(playerId, currencyId);
    }

    /// <summary>
    /// 스테이지 안에서 모은 수량에 더한다.
    /// </summary>
    public void Deposit(int playerId, string currencyId, int amount)
    {
        if (amount <= 0 || FindCurrency(currencyId) == null)
        {
            return;
        }

        var stage = GetOrCreateStage(playerId, currencyId);
        stage.Deposit(amount);
        Publish(playerId, currencyId, stage.Amount, false);
    }

    /// <summary>
    /// 판이 끝나도 남는 메타 잔액에 더한다. 메타 잔액이 없는 재화면 무시한다.
    /// </summary>
    public void DepositMeta(int playerId, string currencyId, int amount)
    {
        var data = FindCurrency(currencyId);
        if (amount <= 0 || data == null || !data.KeepAfterStage)
        {
            return;
        }

        var meta = GetOrCreateMeta(playerId, currencyId);
        meta.Deposit(amount);
        SaveMeta(playerId, currencyId, meta.Amount);
        Publish(playerId, currencyId, meta.Amount, true);
    }

    /// <summary>
    /// 재화 아이콘. 없으면 null.
    /// </summary>
    public Sprite GetIcon(string currencyId)
    {
        var data = FindCurrency(currencyId);
        return data == null ? null : data.Icon;
    }

    /// <summary>
    /// 수량이 충분하면 뺀다. meta가 true면 포켓달러 메타 잔액에서 뺀다.
    /// </summary>
    public bool TryWithdraw(int playerId, string currencyId, int amount, bool meta)
    {
        var data = FindCurrency(currencyId);
        if (data == null || amount <= 0)
        {
            return false;
        }

        if (meta && !data.KeepAfterStage)
        {
            return false;
        }

        var save = meta ? GetOrCreateMeta(playerId, currencyId) : GetOrCreateStage(playerId, currencyId);
        if (save == null || !save.TryWithdraw(amount))
        {
            return false;
        }

        if (meta)
        {
            SaveMeta(playerId, currencyId, save.Amount);
        }

        Publish(playerId, currencyId, save.Amount, meta);
        return true;
    }

    /// <summary>
    /// 이번 판 수량을 0으로 만든다. 메타 잔액은 바꾸지 않는다.
    /// </summary>
    public void ClearStage(int playerId)
    {
        if (_database == null)
        {
            LogMissingDatabase();
            return;
        }

        for (var i = 0; i < _database.Count; i++)
        {
            var data = _database.GetCurrency(i);
            if (data == null || string.IsNullOrEmpty(data.Id))
            {
                continue;
            }

            var stage = GetOrCreateStage(playerId, data.Id);
            if (stage.Amount == 0)
            {
                continue;
            }

            stage.Clear();
            Publish(playerId, data.Id, 0, false);
        }
    }

    /// <summary>
    /// 이번 판 수량을 결과로 만든다. 포켓달러는 메타 잔액에 더한 뒤 스테이지 수량을 비운다.
    /// </summary>
    public CurrencyAmount[] FinishStage(int playerId)
    {
        if (_database == null)
        {
            LogMissingDatabase();
            return System.Array.Empty<CurrencyAmount>();
        }

        var results = new List<CurrencyAmount>(_database.Count);
        for (var i = 0; i < _database.Count; i++)
        {
            var data = _database.GetCurrency(i);
            if (data == null || string.IsNullOrEmpty(data.Id))
            {
                continue;
            }

            var stage = GetOrCreateStage(playerId, data.Id);
            var amount = stage.Amount;
            results.Add(new CurrencyAmount(data.Id, amount));
            if (data.KeepAfterStage && amount > 0)
            {
                var meta = GetOrCreateMeta(playerId, data.Id);
                meta.Deposit(amount);
                SaveMeta(playerId, data.Id, meta.Amount);
                Publish(playerId, data.Id, meta.Amount, true);
            }

            if (amount > 0)
            {
                stage.Clear();
                Publish(playerId, data.Id, 0, false);
            }
        }

        return results.ToArray();
    }

    #endregion

    #region Private Methods

    private CurrencyData FindCurrency(string currencyId)
    {
        if (_database == null)
        {
            LogMissingDatabase();
            return null;
        }

        return _database.GetCurrency(currencyId);
    }

    private CurrencySave GetOrCreateStage(int playerId, string currencyId)
    {
        if (!_stageAmounts.TryGetValue(playerId, out var amounts))
        {
            amounts = new Dictionary<string, CurrencySave>();
            _stageAmounts.Add(playerId, amounts);
        }

        if (!amounts.TryGetValue(currencyId, out var save))
        {
            save = new CurrencySave(0);
            amounts.Add(currencyId, save);
        }

        return save;
    }

    private CurrencySave GetOrCreateMeta(int playerId, string currencyId)
    {
        if (!_metaAmounts.TryGetValue(playerId, out var amounts))
        {
            amounts = new Dictionary<string, CurrencySave>();
            _metaAmounts.Add(playerId, amounts);
        }

        if (!amounts.TryGetValue(currencyId, out var save))
        {
            save = new CurrencySave(LoadMeta(playerId, currencyId));
            amounts.Add(currencyId, save);
        }

        return save;
    }

    private static int LoadMeta(int playerId, string currencyId)
    {
        return PlayerPrefs.GetInt(GetMetaKey(playerId, currencyId), 0);
    }

    private static void SaveMeta(int playerId, string currencyId, int amount)
    {
        PlayerPrefs.SetInt(GetMetaKey(playerId, currencyId), amount);
        PlayerPrefs.Save();
    }

    private static string GetMetaKey(int playerId, string currencyId)
    {
        return META_PREFS_PREFIX + playerId + "_" + currencyId;
    }

    private void Publish(int playerId, string currencyId, int amount, bool isMetaBalance)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            return;
        }

        eventManager.Publish(new CurrencyAmountChanged(playerId, currencyId, amount, isMetaBalance));
    }

    private void LogMissingDatabase()
    {
        if (_missingDatabaseLogged)
        {
            return;
        }

        _missingDatabaseLogged = true;
        Debug.LogError("CurrenciesManager에 CurrenciesDatabase가 없습니다.");
    }

    #endregion
}
