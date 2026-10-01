using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UpgradeManager가 미리 추첨해 둔 증강 선택지를 최대 3개 표시한다.
/// 실제 증강 적용과 Level 관리는 UpgradeManager가 담당한다.
/// </summary>
public sealed class AugmentWindowBehavior : LevelUpChoice
{
    private const int MAX_CARDS = 3;
    private const int CANVAS_SORT_ORDER = 500;

    private Canvas _canvas;
    private readonly List<Button> _cards = new List<Button>();

    private Action _onChosen;

    /// <summary>
    /// 현재 UpgradeManager.CurrentChoices를 화면에 표시한다.
    /// </summary>
    public override void Open(int playerId, int level, Action onChosen)
    {
        _onChosen = onChosen;

        EnsureUi();
        FillCards();

        _canvas.gameObject.SetActive(true);
    }

    /// <summary>
    /// 최초 1회만 증강 선택용 Runtime UI를 생성한다.
    /// </summary>
    private void EnsureUi()
    {
        if (_canvas != null)
        {
            return;
        }

        var canvasObject = new GameObject("Augment Window");
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

        var title = CreateText(
            "Title",
            panel,
            "증강 선택",
            48
        );

        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -40f);
        titleRect.sizeDelta = new Vector2(800f, 80f);

        for (var i = 0; i < MAX_CARDS; i++)
        {
            _cards.Add(CreateCard(panel, i));
        }
    }

    /// <summary>
    /// UpgradeManager가 추첨해 둔 CurrentChoices를 카드에 채운다.
    /// 이 클래스에서는 선택지를 다시 추첨하지 않는다.
    /// </summary>
    private void FillCards()
    {
        if (Managers.Instance == null ||
            !Managers.Instance.TryGetManager<UpgradeManager>(
                out var upgradeManager))
        {
            return;
        }

        var choices = upgradeManager.CurrentChoices;

        for (var i = 0; i < _cards.Count; i++)
        {
            var card = _cards[i];

            var visible =
                choices != null &&
                i < choices.Count &&
                choices[i] != null;

            card.gameObject.SetActive(visible);

            if (!visible)
            {
                continue;
            }

            var choice = choices[i];

            var label =
                card.GetComponentInChildren<TextMeshProUGUI>();

            if (label != null)
            {
                var currentLevel =
                    upgradeManager.GetLevel(choice.Id);

                var nextLevel =
                    currentLevel + 1;

                label.text =
                    $"{choice.Name}\n" +
                    $"Lv.{currentLevel} → " +
                    $"Lv.{nextLevel}/{choice.MaxLevel}" +
                    FormatEffects(choice);
            }

            card.onClick.RemoveAllListeners();

            var choiceIndex = i;

            card.onClick.AddListener(
                () => Choose(choiceIndex)
            );
        }
    }

    /// <summary>
    /// Upgrade 효과를 UI 표시용 문자열로 변환한다.
    /// </summary>
    private static string FormatEffects(
        UpgradeManager.UpgradeDefinition choice)
    {
        if (choice.Effects == null ||
            choice.Effects.Count == 0)
        {
            return string.Empty;
        }

        var lines = new List<string>();

        foreach (var effect in choice.Effects)
        {
            if (effect == null)
            {
                continue;
            }

            var value =
                $"{effect.Value * 100f:+0.#;-0.#;0}%";

            switch (effect.Type)
            {
                case UpgradeManager.UpgradeEffectType.MoveSpeed:
                    lines.Add($"이동 속도 {value}");
                    break;

                case UpgradeManager.UpgradeEffectType.ReceivedDamage:
                    lines.Add($"받는 피해 {value}");
                    break;

                case UpgradeManager.UpgradeEffectType.Experience:
                    lines.Add($"획득 경험치 {value}");
                    break;

                case UpgradeManager.UpgradeEffectType.Damage:
                    lines.Add($"주는 피해 {value}");
                    break;

                default:
                    lines.Add(
                        $"{effect.Type} " +
                        $"{effect.Value:+0.###;-0.###;0}"
                    );
                    break;
            }
        }

        return lines.Count > 0
            ? "\n" + string.Join("\n", lines)
            : string.Empty;
    }

    /// <summary>
    /// 클릭한 카드 index를 UpgradeManager에 전달한다.
    /// 적용 성공 시 창을 닫고 완료 callback을 호출한다.
    /// </summary>
    private void Choose(int choiceIndex)
    {
        if (Managers.Instance == null ||
            !Managers.Instance.TryGetManager<UpgradeManager>(
                out var upgradeManager))
        {
            return;
        }

        if (!upgradeManager.TrySelectChoice(choiceIndex))
        {
            return;
        }

        if (_canvas != null)
        {
            _canvas.gameObject.SetActive(false);
        }

        var chosen = _onChosen;
        _onChosen = null;

        chosen?.Invoke();
    }

    private Button CreateCard(
        RectTransform parent,
        int index)
    {
        var rect =
            CreateRect("Card " + index, parent);

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);
        rect.anchorMax =
            new Vector2(0.5f, 0.5f);
        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.sizeDelta =
            new Vector2(420f, 180f);

        rect.anchoredPosition =
            new Vector2(0f, 80f - index * 200f);

        var image =
            rect.gameObject.AddComponent<Image>();

        image.color =
            new Color(0.15f, 0.18f, 0.24f, 1f);

        var button =
            rect.gameObject.AddComponent<Button>();

        var label =
            CreateText(
                "Label",
                rect,
                string.Empty,
                26
            );

        Stretch(label.rectTransform);

        label.margin =
            new Vector4(16f, 12f, 16f, 12f);

        return button;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        RectTransform parent,
        string value,
        int fontSize)
    {
        var rect =
            CreateRect(name, parent);

        var text =
            rect.gameObject.AddComponent<TextMeshProUGUI>();

        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font =
                TMP_Settings.defaultFontAsset;
        }

        text.text = value;
        text.fontSize = fontSize;
        text.alignment =
            TextAlignmentOptions.Center;
        text.color = Color.white;

        return text;
    }

    private static RectTransform CreateRect(
        string name,
        Transform parent)
    {
        var rectObject =
            new GameObject(name);

        rectObject.transform.SetParent(
            parent,
            false
        );

        return rectObject.AddComponent<RectTransform>();
    }

    private static void Stretch(
        RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
