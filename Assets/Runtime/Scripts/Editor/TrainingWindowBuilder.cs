using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UITitleScene 프리팹의 트레이닝 창을 스킬트리 형태로 다시 짠다.
/// 새로 만든 오브젝트는 Panel 아래 SkillTree에 모으므로 다시 실행하면 그 부분만 새로 만든다.
/// </summary>
public static class TrainingWindowBuilder
{
    private const string TITLE_SCENE_PREFAB = "Assets/Runtime/UI/UITitleScene.prefab";
    private const string SLOT_PREFAB = "Assets/Runtime/UI/TrainingSlot.prefab";
    private const string CONTAINER_NAME = "SkillTree";
    private const string UI_SPRITE = "UI/Skin/UISprite.psd";

    private static readonly Vector2 PANEL_SIZE = new Vector2(1700f, 900f);
    private static readonly Vector2 CELL_SIZE = new Vector2(80f, 104f);
    private static readonly Vector2 CELL_SPACING = new Vector2(14f, 10f);
    private const int COLUMN_CELLS = 3;
    private const int ROW_CELLS = 4;
    private const float COLUMNS_LEFT = 40f;
    private const float COLUMNS_TOP = -110f;
    private const float COLUMN_GAP = 40f;
    private const float FRAME_THICKNESS = 4f;

    private static readonly Color PANEL_DARK = new Color(0.08f, 0.09f, 0.12f, 0.9f);
    private static readonly Color SELECT_COLOR = new Color(1f, 0.85f, 0.2f, 1f);
    private static readonly Color LOCK_OVERLAY = new Color(0f, 0f, 0f, 0.55f);

    private static readonly (TrainingCategory category, string title, Color color)[] CATEGORIES =
    {
        (TrainingCategory.Attack, "공격", new Color(1f, 0.55f, 0.15f, 1f)),
        (TrainingCategory.Defense, "방어", new Color(0.25f, 0.55f, 1f, 1f)),
        (TrainingCategory.Resource, "자원", new Color(0.3f, 0.85f, 0.35f, 1f)),
        (TrainingCategory.Ancient, "고대", new Color(0.7f, 0.35f, 0.95f, 1f)),
    };

    private static Sprite _uiSprite;
    private static TMP_FontAsset _font;
    private static Material _fontMaterial;

    [MenuItem("Tools/Training/Build Skill Tree UI")]
    public static void Build()
    {
        _uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(UI_SPRITE);
        var root = PrefabUtility.LoadPrefabContents(TITLE_SCENE_PREFAB);
        try
        {
            var window = root.GetComponentInChildren<UITrainingWindow>(true);
            if (window == null)
            {
                Debug.LogError("UITitleScene 프리팹에서 UITrainingWindow를 찾지 못했습니다.");
                return;
            }

            var panel = window.transform.Find("Panel") as RectTransform;
            if (panel == null)
            {
                Debug.LogError("Training Window 아래 Panel이 없습니다.");
                return;
            }

            if (!ResolveFont(panel))
            {
                Debug.LogError("트레이닝 창 Title에서 TMP 폰트를 찾지 못했습니다.");
                return;
            }

            var slotPrefab = BuildSlotPrefab();
            BuildWindow(window, panel, slotPrefab);
            PrefabUtility.SaveAsPrefabAsset(root, TITLE_SCENE_PREFAB);
            Debug.Log("트레이닝 스킬트리 UI를 만들었습니다.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static bool ResolveFont(Transform panel)
    {
        var title = panel.Find("Title");
        var label = title != null ? title.GetComponent<TMP_Text>() : panel.GetComponentInChildren<TMP_Text>(true);
        if (label == null || label.font == null)
        {
            return false;
        }

        _font = label.font;
        _fontMaterial = label.fontSharedMaterial;
        return true;
    }

    #region Slot

    private static TrainingSlotBehavior BuildSlotPrefab()
    {
        var go = new GameObject("TrainingSlot", typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        try
        {
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = CELL_SIZE;

            // 버튼 클릭 영역. 보이지 않게 둔다.
            var hit = AddImage(go, new Color(0f, 0f, 0f, 0f), null);
            var button = go.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;

            var iconSize = CELL_SIZE.x;
            var frame = AddImage(CreateTopCenter("Frame", go.transform, 0f, iconSize, iconSize).gameObject, Color.white, _uiSprite);
            var background = AddImage(CreateTopCenter("Background", go.transform, -FRAME_THICKNESS, iconSize - FRAME_THICKNESS * 2f, iconSize - FRAME_THICKNESS * 2f).gameObject, Color.black, _uiSprite);
            var iconImage = AddImage(CreateTopCenter("Icon", go.transform, -12f, iconSize - 24f, iconSize - 24f).gameObject, Color.white, null);
            iconImage.raycastTarget = false;
            var lockOverlay = CreateTopCenter("LockOverlay", go.transform, -FRAME_THICKNESS, iconSize - FRAME_THICKNESS * 2f, iconSize - FRAME_THICKNESS * 2f).gameObject;
            AddImage(lockOverlay, LOCK_OVERLAY, null).raycastTarget = false;

            var selected = CreateTopCenter("Selected", go.transform, 6f, iconSize + 12f, iconSize + 12f).gameObject;
            BuildBrackets(selected.transform, iconSize + 12f);
            selected.SetActive(false);

            var level = CreateLabel("Level", go.transform, 20f, TextAlignmentOptions.Center, "0/5");
            SetTopCenter(level.rectTransform, -iconSize - 2f, CELL_SIZE.x + 20f, CELL_SIZE.y - iconSize);

            frame.raycastTarget = false;
            background.raycastTarget = false;

            var slot = go.AddComponent<TrainingSlotBehavior>();
            var so = new SerializedObject(slot);
            so.FindProperty("_rect").objectReferenceValue = rect;
            so.FindProperty("_button").objectReferenceValue = button;
            so.FindProperty("_background").objectReferenceValue = background;
            so.FindProperty("_frame").objectReferenceValue = frame;
            so.FindProperty("_icon").objectReferenceValue = iconImage;
            so.FindProperty("_lockOverlay").objectReferenceValue = lockOverlay;
            so.FindProperty("_levelLabel").objectReferenceValue = level;
            so.FindProperty("_selected").objectReferenceValue = selected;
            so.ApplyModifiedPropertiesWithoutUndo();

            var saved = PrefabUtility.SaveAsPrefabAsset(go, SLOT_PREFAB);
            return saved.GetComponent<TrainingSlotBehavior>();
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    /// <summary>
    /// 선택 표시용 모서리 브래킷 네 개.
    /// </summary>
    private static void BuildBrackets(Transform parent, float size)
    {
        const float length = 16f;
        const float thickness = 3f;
        for (var corner = 0; corner < 4; corner++)
        {
            var x = corner % 2 == 0 ? 0f : 1f;
            var y = corner < 2 ? 1f : 0f;
            var pivot = new Vector2(x, y);
            CreateCornerBar(parent, pivot, new Vector2(length, thickness));
            CreateCornerBar(parent, pivot, new Vector2(thickness, length));
        }
    }

    private static void CreateCornerBar(Transform parent, Vector2 corner, Vector2 size)
    {
        var rect = CreateRect("Bracket", parent);
        rect.anchorMin = corner;
        rect.anchorMax = corner;
        rect.pivot = corner;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        AddImage(rect.gameObject, SELECT_COLOR, null).raycastTarget = false;
    }

    #endregion

    #region Window

    private static void BuildWindow(UITrainingWindow window, RectTransform panel, TrainingSlotBehavior slotPrefab)
    {
        panel.sizeDelta = PANEL_SIZE;

        var oldScroll = panel.Find("Scroll");
        if (oldScroll != null)
        {
            Object.DestroyImmediate(oldScroll.gameObject);
        }

        var oldContainer = panel.Find(CONTAINER_NAME);
        if (oldContainer != null)
        {
            Object.DestroyImmediate(oldContainer.gameObject);
        }

        var container = CreateRect(CONTAINER_NAME, panel);
        container.anchorMin = Vector2.zero;
        container.anchorMax = Vector2.one;
        container.offsetMin = Vector2.zero;
        container.offsetMax = Vector2.zero;
        container.SetSiblingIndex(1);

        BuildCurrency(container, out var currencyLabel, out var currencyIcon);
        var columns = BuildColumns(container, out var columnsWidth);
        var detail = BuildDetail(container, columnsWidth);

        var back = panel.Find("Back") as RectTransform;
        if (back == null)
        {
            Debug.LogError("Panel 아래 Back 버튼이 없습니다.");
            return;
        }

        var reset = BuildResetButton(container, back, columnsWidth, out var resetSelect, out var resetLabel);
        back.anchorMin = new Vector2(1f, 0f);
        back.anchorMax = new Vector2(1f, 0f);
        back.pivot = new Vector2(0.5f, 0.5f);
        back.anchoredPosition = new Vector2(-150f, 60f);
        back.SetAsLastSibling();

        var so = new SerializedObject(window);
        so.FindProperty("_slotPrefab").objectReferenceValue = slotPrefab;
        var columnsProperty = so.FindProperty("_columns");
        columnsProperty.arraySize = columns.Length;
        for (var i = 0; i < columns.Length; i++)
        {
            columnsProperty.GetArrayElementAtIndex(i).objectReferenceValue = columns[i];
        }

        so.FindProperty("_detail").objectReferenceValue = detail;
        so.FindProperty("_currencyLabel").objectReferenceValue = currencyLabel;
        so.FindProperty("_currencyIcon").objectReferenceValue = currencyIcon;
        so.FindProperty("_resetButton").objectReferenceValue = reset;
        so.FindProperty("_resetSelect").objectReferenceValue = resetSelect;
        so.FindProperty("_resetLabel").objectReferenceValue = resetLabel;
        so.FindProperty("_backButton").objectReferenceValue = back.GetComponent<Button>();
        var backSelect = back.Find("Selected");
        so.FindProperty("_backSelect").objectReferenceValue = backSelect != null ? backSelect.gameObject : null;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildCurrency(Transform parent, out TMP_Text label, out Image icon)
    {
        var box = CreateRect("Currency", parent);
        SetTopLeft(box, COLUMNS_LEFT, -30f, 220f, 56f);
        AddImage(box.gameObject, PANEL_DARK, _uiSprite);

        var iconRect = CreateRect("Icon", box);
        SetTopLeft(iconRect, 16f, -12f, 32f, 32f);
        icon = AddImage(iconRect.gameObject, Color.white, null);
        icon.preserveAspect = true;

        label = CreateLabel("Amount", box, 28f, TextAlignmentOptions.Left, "0");
        SetTopLeft(label.rectTransform, 58f, -6f, 150f, 44f);
    }

    private static TrainingCategoryColumn[] BuildColumns(Transform parent, out float totalWidth)
    {
        var slotsWidth = COLUMN_CELLS * CELL_SIZE.x + (COLUMN_CELLS - 1) * CELL_SPACING.x;
        var slotsHeight = ROW_CELLS * CELL_SIZE.y + (ROW_CELLS - 1) * CELL_SPACING.y;
        var columns = new TrainingCategoryColumn[CATEGORIES.Length];
        for (var i = 0; i < CATEGORIES.Length; i++)
        {
            var (category, title, color) = CATEGORIES[i];
            var rect = CreateRect("Column " + category, parent);
            SetTopLeft(rect, COLUMNS_LEFT + i * (slotsWidth + COLUMN_GAP), COLUMNS_TOP, slotsWidth, slotsHeight + 96f);

            var titleLabel = CreateLabel("Title", rect, 32f, TextAlignmentOptions.Center, title);
            SetTopLeft(titleLabel.rectTransform, 0f, 0f, slotsWidth, 40f);
            titleLabel.color = color;

            var underline = CreateRect("Underline", rect);
            SetTopLeft(underline, slotsWidth * 0.15f, -44f, slotsWidth * 0.7f, 2f);
            var underlineImage = AddImage(underline.gameObject, color, null);

            var levelLabel = CreateLabel("Level", rect, 26f, TextAlignmentOptions.Center, "Lv.0");
            SetTopLeft(levelLabel.rectTransform, 0f, -48f, slotsWidth, 36f);

            var slots = CreateRect("Slots", rect);
            SetTopLeft(slots, 0f, -96f, slotsWidth, slotsHeight);

            var column = rect.gameObject.AddComponent<TrainingCategoryColumn>();
            var so = new SerializedObject(column);
            so.FindProperty("_category").enumValueIndex = (int)category;
            so.FindProperty("_title").stringValue = title;
            so.FindProperty("_color").colorValue = color;
            so.FindProperty("_titleLabel").objectReferenceValue = titleLabel;
            so.FindProperty("_levelLabel").objectReferenceValue = levelLabel;
            so.FindProperty("_underline").objectReferenceValue = underlineImage;
            so.FindProperty("_slotsParent").objectReferenceValue = slots;
            so.FindProperty("_cellSize").vector2Value = CELL_SIZE;
            so.FindProperty("_spacing").vector2Value = CELL_SPACING;
            so.ApplyModifiedPropertiesWithoutUndo();
            columns[i] = column;
        }

        totalWidth = CATEGORIES.Length * slotsWidth + (CATEGORIES.Length - 1) * COLUMN_GAP;
        return columns;
    }

    private static TrainingDetailPanel BuildDetail(Transform parent, float columnsWidth)
    {
        var left = COLUMNS_LEFT + columnsWidth + COLUMN_GAP;
        var width = PANEL_SIZE.x - left - COLUMNS_LEFT;
        var rect = CreateRect("Detail", parent);
        SetTopLeft(rect, left, COLUMNS_TOP, width, 640f);
        AddImage(rect.gameObject, PANEL_DARK, _uiSprite);

        var inner = width - 40f;
        var title = CreateLabel("Title", rect, 32f, TextAlignmentOptions.Center, "");
        SetTopLeft(title.rectTransform, 20f, -20f, inner, 42f);
        var level = CreateLabel("Level", rect, 26f, TextAlignmentOptions.Center, "(0/5)");
        SetTopLeft(level.rectTransform, 20f, -62f, inner, 34f);

        const float iconSize = 128f;
        var iconLeft = (width - iconSize) * 0.5f;
        var frameRect = CreateRect("IconFrame", rect);
        SetTopLeft(frameRect, iconLeft, -106f, iconSize, iconSize);
        var frame = AddImage(frameRect.gameObject, Color.white, _uiSprite);
        var backgroundRect = CreateRect("IconBackground", rect);
        SetTopLeft(backgroundRect, iconLeft + FRAME_THICKNESS, -106f - FRAME_THICKNESS, iconSize - FRAME_THICKNESS * 2f, iconSize - FRAME_THICKNESS * 2f);
        var background = AddImage(backgroundRect.gameObject, Color.black, _uiSprite);
        var iconRect = CreateRect("Icon", rect);
        SetTopLeft(iconRect, iconLeft + 16f, -122f, iconSize - 32f, iconSize - 32f);
        var icon = AddImage(iconRect.gameObject, Color.white, null);

        var description = CreateLabel("Description", rect, 24f, TextAlignmentOptions.TopLeft, "");
        SetTopLeft(description.rectTransform, 20f, -254f, inner, 150f);
        var effect = CreateLabel("Effect", rect, 24f, TextAlignmentOptions.Left, "");
        SetTopLeft(effect.rectTransform, 20f, -410f, inner, 34f);

        var costRoot = CreateRect("Cost", rect);
        SetTopLeft(costRoot, 20f, -452f, inner, 40f);
        var costIconRect = CreateRect("Icon", costRoot);
        SetTopLeft(costIconRect, 0f, -4f, 32f, 32f);
        var costIcon = AddImage(costIconRect.gameObject, Color.white, null);
        costIcon.preserveAspect = true;
        var costLabel = CreateLabel("Amount", costRoot, 28f, TextAlignmentOptions.Left, "0");
        SetTopLeft(costLabel.rectTransform, 40f, 0f, inner - 40f, 40f);

        var max = CreateLabel("Max", rect, 28f, TextAlignmentOptions.Left, "최대 레벨");
        SetTopLeft(max.rectTransform, 20f, -452f, inner, 40f);
        max.color = new Color(1f, 0.85f, 0.35f, 1f);
        max.gameObject.SetActive(false);

        var lockLabel = CreateLabel("Lock", rect, 26f, TextAlignmentOptions.Center, "");
        SetTopLeft(lockLabel.rectTransform, 20f, -570f, inner, 44f);
        lockLabel.richText = true;

        var detail = rect.gameObject.AddComponent<TrainingDetailPanel>();
        var so = new SerializedObject(detail);
        so.FindProperty("_titleLabel").objectReferenceValue = title;
        so.FindProperty("_levelLabel").objectReferenceValue = level;
        so.FindProperty("_iconBackground").objectReferenceValue = background;
        so.FindProperty("_iconFrame").objectReferenceValue = frame;
        so.FindProperty("_icon").objectReferenceValue = icon;
        so.FindProperty("_descriptionLabel").objectReferenceValue = description;
        so.FindProperty("_effectLabel").objectReferenceValue = effect;
        so.FindProperty("_costRoot").objectReferenceValue = costRoot.gameObject;
        so.FindProperty("_costIcon").objectReferenceValue = costIcon;
        so.FindProperty("_costLabel").objectReferenceValue = costLabel;
        so.FindProperty("_maxLabel").objectReferenceValue = max.gameObject;
        so.FindProperty("_lockLabel").objectReferenceValue = lockLabel;
        so.ApplyModifiedPropertiesWithoutUndo();
        return detail;
    }

    /// <summary>
    /// 뒤로 버튼을 복제해 같은 모양의 리셋 버튼을 만든다.
    /// </summary>
    private static Button BuildResetButton(Transform parent, RectTransform back, float columnsWidth, out GameObject select, out TMP_Text label)
    {
        var copy = Object.Instantiate(back.gameObject, parent);
        copy.name = "Reset";
        var rect = (RectTransform)copy.transform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(COLUMNS_LEFT + columnsWidth * 0.5f, 60f);

        var button = copy.GetComponent<Button>();
        button.onClick.RemoveAllListeners();

        var selectTransform = copy.transform.Find("Selected");
        select = selectTransform != null ? selectTransform.gameObject : null;
        if (select != null)
        {
            select.SetActive(false);
        }

        var labelTransform = copy.transform.Find("Label");
        label = labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;
        if (label != null)
        {
            label.text = "리셋";
        }

        return button;
    }

    #endregion

    #region Helpers

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static RectTransform CreateTopCenter(string name, Transform parent, float y, float width, float height)
    {
        var rect = CreateRect(name, parent);
        SetTopCenter(rect, y, width, height);
        return rect;
    }

    private static void SetTopCenter(RectTransform rect, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static Image AddImage(GameObject go, Color color, Sprite sprite)
    {
        go.AddComponent<CanvasRenderer>();
        var image = go.AddComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        if (sprite != null)
        {
            image.type = Image.Type.Sliced;
        }

        return image;
    }

    private static TMP_Text CreateLabel(string name, Transform parent, float size, TextAlignmentOptions alignment, string text)
    {
        var rect = CreateRect(name, parent);
        rect.gameObject.AddComponent<CanvasRenderer>();
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = _font;
        label.fontSharedMaterial = _fontMaterial;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.text = text;
        return label;
    }

    #endregion
}
