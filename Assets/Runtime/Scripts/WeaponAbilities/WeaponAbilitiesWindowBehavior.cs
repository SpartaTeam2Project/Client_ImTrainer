using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레벨업 때 능력 카드를 최대 세 장 보여 준다.
/// </summary>
public class WeaponAbilitiesWindowBehavior : LevelUpChoice
{
    private const int MAX_CARDS = 3;
    private const int CANVAS_SORT_ORDER = 500;

    private Canvas _canvas;
    private readonly List<Button> _cards = new List<Button>();
    private Action _onChosen;
    private int _playerId;

    /// <summary>
    /// 능력 매니저가 뽑아 둔 카드를 열고, 고르면 onChosen을 호출한다.
    /// </summary>
    public override void Open(int playerId, int level, Action onChosen)
    {
        _playerId = playerId;
        _onChosen = onChosen;
        EnsureUi();
        FillCards();
        _canvas.gameObject.SetActive(true);
    }

    private void EnsureUi()
    {
        if (_canvas != null)
        {
            return;
        }

        var canvasObject = new GameObject("Abilities Window");
        canvasObject.transform.SetParent(transform, false);
        _canvas = canvasObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = CANVAS_SORT_ORDER;
        canvasObject.AddComponent<GraphicRaycaster>();
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var panel = CreateRect("Panel", canvasObject.transform);
        var panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.65f);
        Stretch(panel);

        var title = CreateText("Title", panel, "레벨업", 48);
        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -40f);
        titleRect.sizeDelta = new Vector2(800f, 80f);

        for (var i = 0; i < MAX_CARDS; i++)
        {
            var card = CreateCard(panel, i);
            _cards.Add(card);
        }
    }

    private void FillCards()
    {
        IReadOnlyList<WeaponAbilityData> offers = Array.Empty<WeaponAbilityData>();
        if (Managers.Instance != null && Managers.Instance.TryGetManager<WeaponAbilityManager>(out var abilityManager))
        {
            offers = abilityManager.CurrentOffers;
        }

        for (var i = 0; i < _cards.Count; i++)
        {
            var card = _cards[i];
            var visible = offers != null && i < offers.Count && offers[i] != null;
            card.gameObject.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            var data = offers[i];
            var label = card.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                var title = string.IsNullOrEmpty(data.Title) ? data.WeaponAbilityType.ToString() : data.Title;
                label.text = string.IsNullOrEmpty(data.Description) ? title : title + "\n" + data.Description;
            }

            card.onClick.RemoveAllListeners();
            var chosen = data;
            card.onClick.AddListener(() => Choose(chosen));
        }
    }

    private void Choose(WeaponAbilityData data)
    {
        if (Managers.Instance != null && Managers.Instance.TryGetManager<WeaponAbilityManager>(out var abilityManager))
        {
            abilityManager.ApplyOffer(_playerId, data);
        }

        if (_canvas != null)
        {
            _canvas.gameObject.SetActive(false);
        }

        var chosen = _onChosen;
        _onChosen = null;
        chosen?.Invoke();
    }

    private Button CreateCard(RectTransform parent, int index)
    {
        var rect = CreateRect("Card " + index, parent);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(420f, 160f);
        rect.anchoredPosition = new Vector2(0f, 80f - index * 180f);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.15f, 0.18f, 0.24f, 1f);
        var button = rect.gameObject.AddComponent<Button>();
        var label = CreateText("Label", rect, string.Empty, 28);
        Stretch(label.rectTransform);
        label.margin = new Vector4(16f, 12f, 16f, 12f);
        return button;
    }

    private static TextMeshProUGUI CreateText(string name, RectTransform parent, string value, int fontSize)
    {
        var rect = CreateRect(name, parent);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        text.text = value;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var rectObject = new GameObject(name);
        rectObject.transform.SetParent(parent, false);
        return rectObject.AddComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
