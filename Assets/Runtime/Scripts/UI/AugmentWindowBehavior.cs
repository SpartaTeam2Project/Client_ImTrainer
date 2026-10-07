using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UpgradeManager가 미리 추첨해 둔 증강 선택지를 카드 형태로 최대 3개 표시한다.
/// 실제 증강 적용과 Level 관리는 UpgradeManager가 담당한다.
/// 프리팹 루트에 붙으며, 루트를 켜고 끄는 것으로 창을 열고 닫는다.
/// </summary>
public sealed class AugmentWindowBehavior : LevelUpChoice
{
    private const float CARD_POP_DURATION = 0.28f;
    private const float CARD_POP_INTERVAL = 0.08f;
    private const float CARD_START_SCALE = 0.7f;

    [SerializeField] private AugmentCard[] _cards;

    // 창이 다시 열릴 때 이전 Animation이 남지 않도록 현재 Sequence를 보관한다.
    private Sequence _openSequence;

    private Action _onChosen;


    private void OnDestroy()
    {
        _openSequence?.Kill();
    }

    /// <summary>
    /// 창이 떠 있으면 숫자키 1~3으로 카드를 고른다. 클릭과 같은 Choose를 탄다.
    /// </summary>
    private void Update()
    {
        if (Managers.Instance == null ||
            !Managers.Instance.TryGetManager<InputManager>(out var inputManager))
        {
            return;
        }

        var slot = inputManager.ConsumeNumberSlot();
        if (slot < 0 || slot >= _cards.Length || !_cards[slot].gameObject.activeSelf)
        {
            return;
        }

        Choose(slot);
    }


    public override void Open(int playerId, int level, Action onChosen)
    {
        _onChosen = onChosen;

        FillCards();

        gameObject.SetActive(true);

        PlayOpenAnimation();
    }

    private void FillCards()
    {
        if (Managers.Instance == null ||
            !Managers.Instance.TryGetManager<UpgradeManager>(out var upgradeManager))
        {
            return;
        }

        var choices = upgradeManager.CurrentChoices;

        for (var i = 0; i < _cards.Length; i++)
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

            card.Label.text =
                $"{choice.Name}\n\n" +
                $"Lv.{currentLevel} → Lv.{nextLevel}/{choice.MaxLevel}\n\n" +
                FormatEffects(choice);


            var icon = Resources.Load<Sprite>(choice.IconPath);

            if (icon != null)
            {
                // 회색 ArtBackground 위에 실제 Sprite를 표시한다.
                card.Icon.sprite = icon;
                card.Icon.color = Color.white;
                card.Icon.enabled = true;
            }
            else
            {
                // 아이콘만 숨긴다.
                // ArtBackground는 별도 Image라서 그대로 남는다.
                card.Icon.sprite = null;
                card.Icon.color = Color.clear;

                Debug.LogWarning(
                    $"[Augment UI] Sprite를 찾지 못했습니다. " +
                    $"Upgrade={choice.Id}, Path={choice.IconPath}",
                    this
                );
            }

            card.Button.onClick.RemoveAllListeners();

            var choiceIndex = i;
            card.Button.onClick.AddListener(() => Choose(choiceIndex));
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

        gameObject.SetActive(false);

        var chosen = _onChosen;
        _onChosen = null;
        chosen?.Invoke();
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

        for (var i = 0; i < _cards.Length; i++)
        {
            var card = _cards[i];

            if (!card.gameObject.activeSelf)
            {
                continue;
            }

            var rect = card.transform as RectTransform;
            var canvasGroup = card.CanvasGroup;

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
}
