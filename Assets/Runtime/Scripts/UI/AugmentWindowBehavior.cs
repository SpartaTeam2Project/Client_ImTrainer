using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UpgradeManager가 미리 추첨해 둔 증강 선택지를 카드 형태로 최대 3개 표시한다.
/// 실제 증강 적용과 Level 관리는 UpgradeManager가 담당한다.
/// </summary>
public sealed class AugmentWindowBehavior : LevelUpChoice
{
    private const int MAX_CARDS = 3;
    private const int CANVAS_SORT_ORDER = 500;

    private Canvas _canvas;

    [Header("Card Slot")]
    private readonly List<Button> _cards = new List<Button>();
    private readonly List<Image> _cardIcons = new List<Image>();
    private readonly List<TextMeshProUGUI> _cardLabels = new List<TextMeshProUGUI>();

    [Header("Anim")]
    private const float CARD_POP_DURATION = 0.28f;
    private const float CARD_POP_INTERVAL = 0.08f;
    private const float CARD_START_SCALE = 0.7f;

    private readonly List<CanvasGroup> _cardCanvasGroups = new List<CanvasGroup>();

    // 창이 다시 열릴 때 이전 Animation이 남지 않도록 현재 Sequence를 보관한다.
    private Sequence _openSequence;

    private Action _onChosen;


    private void OnDestroy()
    {
        _openSequence?.Kill();
    }


    public override void Open(int playerId, int level, Action onChosen)
    {
        _onChosen = onChosen;

        EnsureUi();
        FillCards();

        _canvas.gameObject.SetActive(true);

        PlayOpenAnimation();
    }

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
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);
        Stretch(panel);

        var title = CreateText("Title", panel, "증강 선택", 54);

        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -45f);
        titleRect.sizeDelta = new Vector2(900f, 80f);

        for (var i = 0; i < MAX_CARDS; i++)
        {
            CreateCard(panel, i);
        }
    }

    private void FillCards()
    {
        if (Managers.Instance == null ||
            !Managers.Instance.TryGetManager<UpgradeManager>(out var upgradeManager))
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
            var currentLevel = upgradeManager.GetLevel(choice.Id);
            var nextLevel = currentLevel + 1;

            _cardLabels[i].text =
                $"{choice.Name}\n\n" +
                $"Lv.{currentLevel} → Lv.{nextLevel}/{choice.MaxLevel}\n\n" +
                FormatEffects(choice);


            var icon = Resources.Load<Sprite>(choice.IconPath);

            if (icon != null)
            {
                // 회색 ArtBackground 위에 실제 Sprite를 표시한다.
                _cardIcons[i].sprite = icon;
                _cardIcons[i].color = Color.white;
                _cardIcons[i].enabled = true;
            }
            else
            {
                // 아이콘만 숨긴다.
                // ArtBackground는 별도 Image라서 그대로 남는다.
                _cardIcons[i].sprite = null;
                _cardIcons[i].color = Color.clear;

                Debug.LogWarning(
                    $"[Augment UI] Sprite를 찾지 못했습니다. " +
                    $"Upgrade={choice.Id}, Path={choice.IconPath}",
                    this
                );
            }

            card.onClick.RemoveAllListeners();

            var choiceIndex = i;
            card.onClick.AddListener(() => Choose(choiceIndex));
        }
    }

    private static string FormatEffects(UpgradeManager.UpgradeDefinition choice)
    {
        if (choice.Effects == null || choice.Effects.Count == 0)
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

            var value = $"{effect.Value * 100f:+0.#;-0.#;0}%";

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
                    lines.Add($"{effect.Type} {effect.Value:+0.###;-0.###;0}");
                    break;
            }
        }

        return string.Join("\n", lines);
    }

    private void Choose(int choiceIndex)
    {
        if (Managers.Instance == null ||
            !Managers.Instance.TryGetManager<UpgradeManager>(out var upgradeManager))
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

    private void CreateCard(RectTransform parent, int index)
    {
        var cardRect = CreateRect("Card " + index, parent);

        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);

        // 세로형 카드
        cardRect.sizeDelta = new Vector2(320f, 560f);

        // 3장의 카드를 가로로 정렬한다.
        cardRect.anchoredPosition =
            new Vector2(-370f + index * 370f, -30f);

        var cardBackground =
            cardRect.gameObject.AddComponent<Image>();

        cardBackground.color =
            new Color(0.12f, 0.14f, 0.20f, 1f);

        var button =
            cardRect.gameObject.AddComponent<Button>();


        var canvasGroup =
            cardRect.gameObject.AddComponent<CanvasGroup>();

        // =========================================================
        // 이미지 영역
        //
        // ArtBackground : 고정 회색 배경
        //     └ Icon     : 실제 증강 Sprite
        // =========================================================

        var artRect = CreateRect("ArtBackground", cardRect);

        artRect.anchorMin = new Vector2(0.5f, 1f);
        artRect.anchorMax = new Vector2(0.5f, 1f);
        artRect.pivot = new Vector2(0.5f, 1f);

        artRect.sizeDelta = new Vector2(240f, 250f);

        artRect.anchoredPosition = new Vector2(0f, -35f);

        // 항상 보이는 회색 더미 이미지 영역
        var artBackground =
            artRect.gameObject.AddComponent<Image>();

        artBackground.color =
            new Color(0.28f, 0.30f, 0.36f, 1f);


        // 실제 증강 Sprite는 배경과 별개의 자식 Image로 만든다.
        var iconRect = CreateRect("Icon", artRect);

        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);

        // 배경을 완전히 덮지 않도록 조금 작게 표시
        iconRect.sizeDelta = new Vector2(170f, 170f);

        // 이미지 자체도 배경 중앙보다 약간 아래쪽에 위치
        iconRect.anchoredPosition = new Vector2(0f, -15f);

        var iconImage =
            iconRect.gameObject.AddComponent<Image>();

        // Sprite의 원래 종횡비 유지
        iconImage.preserveAspect = true;

        // FillCards에서 Sprite가 들어오기 전에는 투명하게 둔다.
        iconImage.color = Color.clear;


        // =========================================================
        // 설명 영역
        // =========================================================

        var description =
            CreateText(
                "Description",
                cardRect,
                string.Empty,
                32
            );

        var descriptionRect =
            description.rectTransform;

        descriptionRect.anchorMin = new Vector2(0.5f, 0f);
        descriptionRect.anchorMax = new Vector2(0.5f, 0f);
        descriptionRect.pivot = new Vector2(0.5f, 0f);

        descriptionRect.sizeDelta = new Vector2(280f, 210f);
        descriptionRect.anchoredPosition = new Vector2(0f, 20f);

        description.margin =
            new Vector4(12f, 10f, 12f, 10f);

        description.alignment =
            TextAlignmentOptions.Center;


        // Runtime에서 카드 내용을 갱신할 객체들만 보관한다.
        _cards.Add(button);
        _cardIcons.Add(iconImage);
        _cardLabels.Add(description);
        _cardCanvasGroups.Add(canvasGroup);
    }

    /// <summary>
    /// 증강 선택창이 열릴 때 활성화된 카드들을 왼쪽부터 순차적으로 Pop-up한다.
    ///
    /// flow:
    /// Card 초기화
    /// => Scale 0.7 / Alpha 0
    /// => 카드별 0.08초 간격
    /// => Scale 1 / Alpha 1
    /// </summary>
    private void PlayOpenAnimation()
    {
        // 이전 창의 Tween이 아직 살아 있다면 제거한다.
        _openSequence?.Kill();

        _openSequence = DOTween.Sequence();

        // 현재 Upgrade 선택 중에는 Time.timeScale == 0이다.
        // 따라서 실제 시간 기준으로 Tween이 동작하도록 한다.
        _openSequence.SetUpdate(true);

        for (var i = 0; i < _cards.Count; i++)
        {
            var card = _cards[i];

            if (!card.gameObject.activeSelf)
            {
                continue;
            }

            var rect = card.transform as RectTransform;
            var canvasGroup = _cardCanvasGroups[i];

            // Animation 시작 상태
            rect.localScale = Vector3.one * CARD_START_SCALE;
            canvasGroup.alpha = 0f;

            var delay = i * CARD_POP_INTERVAL;

            // Scale:
            // 0.7 → 1.0
            // OutBack을 사용해서 목표 크기를 살짝 넘었다 돌아오는 Pop 느낌을 낸다.
            _openSequence.Insert(
                delay,
                rect.DOScale(
                        Vector3.one,
                        CARD_POP_DURATION)
                    .SetEase(Ease.OutBack)
            );

            // Fade:
            // Scale Animation과 동시에 투명도도 올린다.
            _openSequence.Insert(
                delay,
                canvasGroup.DOFade(
                    1f,
                    CARD_POP_DURATION * 0.65f)
            );
        }
    }

    private static TextMeshProUGUI CreateText(
        string name,
        RectTransform parent,
        string value,
        int fontSize)
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

    private static RectTransform CreateRect(
        string name,
        Transform parent)
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
