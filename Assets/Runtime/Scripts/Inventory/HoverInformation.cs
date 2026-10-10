using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 창 오른쪽에 고른 포켓몬의 이름, 타입, 성, 무기 능력, 진화 계통을 보여 준다.
/// 진화 계통은 기본, 1진화, 2진화, 메가진화, 거다이맥스 칸에 고정해서 그리고, 나머지 갈래 진화는 갈래 칸에 그린다.
/// </summary>
public class HoverInformation : MonoBehaviour
{
    private const string DAMAGE_PREFIX = "공격력: ";
    private const float EVOLUTION_ICON_SCALE = 2f;
    private const string UNKNOWN_NAME = "???";
    private const string BRANCH_NAME = "갈래";

    // 스토리지 정보창의 잠금 실루엣과 같은 색.
    private static readonly Color LOCKED_ICON_COLOR = new Color32(0, 0, 0, 237);

    [SerializeField] private GameObject _content;
    [SerializeField] private GameObject _emptyLabel;

    [Header("Pokemon")]
    [SerializeField] private Image _portrait;
    [SerializeField] private TMP_Text _monsterName;
    [SerializeField] private MonsterTypeView _typeView;
    [SerializeField] private Image[] _starImages = new Image[Item.STAR_MAX];

    [Header("Ability")]
    [SerializeField] private Image _abilityIcon;
    [SerializeField] private TMP_Text _abilityTitle;
    [SerializeField] private TMP_Text _abilityDescription;
    [SerializeField] private TMP_Text _damage;
    [Tooltip("공격 능력 제목을 타입 색으로 칠한다")]
    [SerializeField] private MonsterTypeDatabase _typeDatabase;

    [Serializable]
    private class EvolutionView
    {
        public GameObject Root;
        public Image Icon;
        public Image Highlight;
        public TMP_Text Stage;
        public TMP_Text Name;

        [NonSerialized] public Vector2 DefaultIconSize;
    }

    [Header("Evolution")]
    [Tooltip("Basic, Stage1, Stage2, Mega, VMax 순서")]
    [SerializeField] private EvolutionView[] _evolutionViews = new EvolutionView[EvolutionLine.STAGE_COUNT];
    [Tooltip("단계 칸에 들어가지 않은 갈래 진화. 갈래 수만큼 켠다")]
    [SerializeField] private EvolutionView[] _branchViews = new EvolutionView[EvolutionLine.MAX_BRANCH];
    [Tooltip("진화 칸 가로 스크롤. 다른 종을 고르면 맨 앞으로 되돌린다")]
    [SerializeField] private ScrollRect _evolutionScroll;

    private Color _defaultTitleColor = Color.white;
    private int _scrolledUid = EvolutionLine.NONE;

    private void Awake()
    {
        if (_abilityTitle != null)
        {
            _defaultTitleColor = _abilityTitle.color;
        }

        StoreDefaultIconSizes(_evolutionViews);
        StoreDefaultIconSizes(_branchViews);
        Clear();
    }

    /// <summary>
    /// 한 마리의 정보를 채운다. 정의가 없으면 비운다.
    /// </summary>
    public void Show(Item item)
    {
        if (item == null || Managers.Instance == null || !Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
        {
            Clear();
            return;
        }

        var visual = itemManager.GetVisual(item.uid);
        SetVisible(true);
        var portrait = visual != null ? MonsterVisualData.FirstFrame(visual.InfoAnimation) : null;
        SetImage(_portrait, portrait);
        SetText(_monsterName, item.name);
        if (_typeView != null)
        {
            _typeView.Show(visual);
        }

        InventoryItem.ShowStars(_starImages, item.upgradeLevel);
        ShowAbility(visual != null ? visual.WeaponAbility : null, item.upgradeLevel);
        ShowEvolution(itemManager, item.uid);
    }

    private static void StoreDefaultIconSizes(EvolutionView[] views)
    {
        for (var i = 0; i < views.Length; i++)
        {
            var view = views[i];
            if (view != null && view.Icon != null)
            {
                view.DefaultIconSize = view.Icon.rectTransform.sizeDelta;
            }
        }
    }

    /// <summary>
    /// 고른 칸이 없을 때의 빈 상태로 둔다.
    /// </summary>
    public void Clear()
    {
        SetVisible(false);
    }

    private void ShowAbility(WeaponAbilityData ability, int star)
    {
        if (ability == null)
        {
            SetImage(_abilityIcon, null);
            SetText(_abilityTitle, string.Empty);
            SetText(_abilityDescription, string.Empty);
            SetActive(_damage, false);
            return;
        }

        SetImage(_abilityIcon, ability.Icon);
        SetText(_abilityTitle, ability.Title);
        SetTitleColor(ability);
        SetText(_abilityDescription, ability.Description);
        var level = ability.GetLevel(ItemManager.ToAbilityLevel(star, ability)) as IWeaponAbilityDamage;
        SetActive(_damage, level != null);
        if (level != null)
        {
            SetText(_damage, DAMAGE_PREFIX + level.Damage.ToString("0.##"));
        }
    }

    /// <summary>
    /// 공격 능력이면 그 타입 색으로, 패시브면 프리팹 기본 색으로 둔다.
    /// </summary>
    private void SetTitleColor(WeaponAbilityData ability)
    {
        if (_abilityTitle == null)
        {
            return;
        }

        var color = _defaultTitleColor;
        if (_typeDatabase != null
            && ability.TryGetElementType(out var attackType)
            && _typeDatabase.TryGetColor(attackType, out var typeColor))
        {
            color = typeColor;
        }

        _abilityTitle.color = color;
    }

    private void ShowEvolution(ItemManager itemManager, int uid)
    {
        var line = itemManager.GetEvolutionLine(uid);
        // 계정이 없는 테스트 씬에서는 모두 획득한 것으로 보여 준다.
        Managers.Instance.TryGetManager<AccountManager>(out var account);
        for (var i = 0; i < _evolutionViews.Length; i++)
        {
            var view = _evolutionViews[i];
            if (view == null)
            {
                continue;
            }

            var stage = (EvolutionStage)i;
            ShowEvolutionView(view, itemManager, account, line.Get(stage), StageName(stage), stage == EvolutionStage.Basic, line.IsSelected(stage));
        }

        for (var i = 0; i < _branchViews.Length; i++)
        {
            var view = _branchViews[i];
            if (view != null)
            {
                var branch = line.GetBranch(i);
                ShowEvolutionView(view, itemManager, account, branch, BRANCH_NAME, false, branch >= 0 && branch == line.SelectedUid);
            }
        }

        // 같은 종을 다시 그릴 때는 보고 있던 스크롤 위치를 둔다.
        if (_evolutionScroll != null && uid != _scrolledUid)
        {
            _scrolledUid = uid;
            _evolutionScroll.StopMovement();
            _evolutionScroll.horizontalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// 진화 칸 하나를 채운다. 종이 없으면 칸을 끈다. 얻지 못한 종은 실루엣과 ???로 가린다.
    /// </summary>
    private void ShowEvolutionView(EvolutionView view, ItemManager itemManager, AccountManager account, int uid, string stageName, bool alwaysShowStage, bool selected)
    {
        var target = itemManager.TryGetItem(uid);
        if (view.Root != null)
        {
            view.Root.SetActive(target != null);
        }

        if (target == null)
        {
            return;
        }

        var visual = itemManager.GetVisual(target.uid);
        var obtained = account == null || account.HasObtainedMonster(visual);
        SetImage(view.Icon, visual != null ? MonsterVisualData.FirstFrame(visual.Icon) : null);
        if (view.Icon != null)
        {
            view.Icon.color = obtained ? Color.white : LOCKED_ICON_COLOR;
        }

        SetEvolutionIconSize(view, visual);
        SetText(view.Stage, obtained || alwaysShowStage ? stageName : UNKNOWN_NAME);
        SetText(view.Name, obtained ? target.name : UNKNOWN_NAME);
        if (view.Highlight != null)
        {
            view.Highlight.enabled = selected;
        }
    }

    /// <summary>
    /// 아이콘 크기에 맞춰 진화 칸 그림 크기를 정한다. 크기가 없으면 프리팹 크기로 되돌린다.
    /// </summary>
    private static void SetEvolutionIconSize(EvolutionView view, MonsterVisualData visual)
    {
        if (view.Icon == null)
        {
            return;
        }

        view.Icon.rectTransform.sizeDelta = visual != null && visual.TryGetIconSize(EVOLUTION_ICON_SCALE, out var size)
            ? size
            : view.DefaultIconSize;
    }

    private static string StageName(EvolutionStage stage)
    {
        switch (stage)
        {
            case EvolutionStage.Stage1:
                return "1진화";
            case EvolutionStage.Stage2:
                return "2진화";
            case EvolutionStage.Mega:
                return "메가진화";
            case EvolutionStage.VMax:
                return "거다이맥스";
            default:
                return "기본";
        }
    }

    private void SetVisible(bool visible)
    {
        if (_content != null)
        {
            _content.SetActive(visible);
        }

        if (_emptyLabel != null)
        {
            _emptyLabel.SetActive(!visible);
        }
    }

    private static void SetImage(Image image, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.enabled = sprite != null;
        image.preserveAspect = true;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private static void SetActive(Component component, bool active)
    {
        if (component != null)
        {
            component.gameObject.SetActive(active);
        }
    }
}
