using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 결과 화면 패널을 하나씩 만들고 패널 스크립트 필드를 연결한다.
/// 배치는 부모 비율 앵커라서 화면 비율이 바뀌어도 세 열이 유지된다.
/// </summary>
public static class StageResultPanelBuilders
{
    private const float HEADER_HEIGHT = 96f;
    private const float PARTY_RADIUS = 280f;
    private const int EQUIPMENT_SLOT_COUNT = 6;
    private static readonly Vector2 EQUIPMENT_CELL = new Vector2(150f, 150f);

    public static void BuildHeader(StageResultUiFactory factory, Transform layout, out TextMeshProUGUI stageName, out TextMeshProUGUI line)
    {
        stageName = factory.CreateLabel("StageName", layout, 48f, TextAlignmentOptions.Center, "스테이지", new Color(0.8f, 0.85f, 0.95f, 1f));
        SetTop(stageName.rectTransform, 190f, 70f);
        line = factory.CreateLabel("Line", layout, 36f, TextAlignmentOptions.Center, string.Empty, new Color(1f, 1f, 1f, 0.8f));
        SetTop(line.rectTransform, 255f, 56f);
    }

    public static StagePlayerStatPanel BuildPlayerStats(StageResultUiFactory factory, Transform body, StageStatRowView rowPrefab, WeaponAbilitiesDatabase abilities)
    {
        var panel = CreatePanel(factory, "PlayerStats", body, new Vector2(0f, 0f), new Vector2(0.24f, 1f), "주요 속성");
        var root = CreateContentRoot(panel, HEADER_HEIGHT + 12f);
        var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        ConfigureColumn(layout, 4f);

        var view = panel.gameObject.AddComponent<StagePlayerStatPanel>();
        var so = new SerializedObject(view);
        so.FindProperty("_root").objectReferenceValue = root;
        so.FindProperty("_rowPrefab").objectReferenceValue = rowPrefab;
        so.FindProperty("_abilities").objectReferenceValue = abilities;
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    public static StagePartyPanel BuildParty(StageResultUiFactory factory, Transform body)
    {
        var area = StageResultUiFactory.CreateArea("Party", body, new Vector2(0.26f, 0.5f), new Vector2(0.72f, 1f));
        var trainerFrame = StageResultUiFactory.CreateFixed("Trainer", area, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 240f));
        factory.AddImage(trainerFrame.gameObject, StageResultUiFactory.SLOT_DARK, true);
        var trainer = factory.AddIcon(StageResultUiFactory.CreateArea("Portrait", trainerFrame, Vector2.zero, Vector2.one).gameObject);
        var level = factory.CreateLabel("Level", trainerFrame, 44f, TextAlignmentOptions.Bottom, "Lv.1", Color.white);
        StageResultUiFactory.Inset(level.rectTransform, 0f, 0f, 0f, 6f);

        var slots = new StagePartySlotView[WeaponSlots.MAX_COUNT];
        for (var i = 0; i < slots.Length; i++)
        {
            var slot = (WeaponSlot)i;
            slots[i] = StageResultRowPrefabBuilder.CreatePartySlot(factory, area, "Slot_" + slot, WeaponSlots.GetDirection(slot) * PARTY_RADIUS);
        }

        var view = area.gameObject.AddComponent<StagePartyPanel>();
        var so = new SerializedObject(view);
        so.FindProperty("_trainer").objectReferenceValue = trainer;
        so.FindProperty("_level").objectReferenceValue = level;
        StageResultUiFactory.SetObjects(so, "_slots", slots);
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    public static StageDataPanel BuildData(StageResultUiFactory factory, Transform body, StageDataCellView cellPrefab)
    {
        var panel = CreatePanel(factory, "DataStats", body, new Vector2(0.26f, 0f), new Vector2(0.72f, 0.48f), "데이터 통계");
        var title = panel.Find("Header/Title").GetComponent<TextMeshProUGUI>();
        title.color = StageResultUiFactory.VALUE_GREEN;
        var root = CreateContentRoot(panel, HEADER_HEIGHT + 12f);
        var grid = root.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = StageResultRowPrefabBuilder.DATA_CELL_SIZE;
        grid.spacing = new Vector2(16f, 4f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        var view = panel.gameObject.AddComponent<StageDataPanel>();
        var so = new SerializedObject(view);
        so.FindProperty("_title").objectReferenceValue = title;
        so.FindProperty("_root").objectReferenceValue = root;
        so.FindProperty("_cellPrefab").objectReferenceValue = cellPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    public static StageDamagePanel BuildDamage(StageResultUiFactory factory, Transform body, StageDamageRowView rowPrefab, WeaponAbilitiesDatabase abilities)
    {
        var panel = CreatePanel(factory, "DamageStats", body, new Vector2(0.74f, 0.36f), new Vector2(1f, 1f), "피해 통계");
        var mode = factory.CreateLabel("Mode", panel.Find("Header"), 32f, TextAlignmentOptions.Right, "판 전체  [R]", new Color(1f, 1f, 1f, 0.75f));
        StageResultUiFactory.Inset(mode.rectTransform, 0f, 24f, 0f, 0f);

        var scroll = CreateContentRoot(panel, HEADER_HEIGHT + 12f);
        scroll.name = "Scroll";
        var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        var viewport = StageResultUiFactory.CreateArea("Viewport", scroll, Vector2.zero, Vector2.one);
        factory.AddImage(viewport.gameObject, Color.white, false).raycastTarget = true;
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        var content = StageResultUiFactory.CreateTopBar("Content", viewport, 0f, 0f);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        ConfigureColumn(layout, 8f);
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.viewport = viewport;
        scrollRect.content = content;

        var view = panel.gameObject.AddComponent<StageDamagePanel>();
        var so = new SerializedObject(view);
        so.FindProperty("_modeText").objectReferenceValue = mode;
        so.FindProperty("_root").objectReferenceValue = content;
        so.FindProperty("_rowPrefab").objectReferenceValue = rowPrefab;
        so.FindProperty("_abilities").objectReferenceValue = abilities;
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    /// <summary>
    /// 장비는 아직 없다. 나중에 장비가 들어오면 이 칸을 채우는 패널 스크립트를 붙인다.
    /// </summary>
    public static void BuildEquipment(StageResultUiFactory factory, Transform body)
    {
        var panel = CreatePanel(factory, "Equipment", body, new Vector2(0.74f, 0f), new Vector2(1f, 0.34f), "장비");
        var root = CreateContentRoot(panel, HEADER_HEIGHT + 12f);
        var grid = root.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = EQUIPMENT_CELL;
        grid.spacing = new Vector2(24f, 24f);
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        for (var i = 0; i < EQUIPMENT_SLOT_COUNT; i++)
        {
            var slot = StageResultUiFactory.CreateRect("EmptySlot" + (i + 1), root);
            factory.AddImage(slot.gameObject, StageResultUiFactory.SLOT_DARK, true);
        }
    }

    private static RectTransform CreatePanel(StageResultUiFactory factory, string name, Transform parent, Vector2 min, Vector2 max, string title)
    {
        var panel = StageResultUiFactory.CreateArea(name, parent, min, max);
        factory.AddImage(panel.gameObject, StageResultUiFactory.PANEL_DARK, true);
        var header = StageResultUiFactory.CreateTopBar("Header", panel, 0f, HEADER_HEIGHT);
        factory.AddImage(header.gameObject, StageResultUiFactory.HEADER_DARK, true);
        var label = factory.CreateLabel("Title", header, 52f, TextAlignmentOptions.Center, title, Color.white);
        StageResultUiFactory.Inset(label.rectTransform, 0f, 0f, 0f, 0f);
        return panel;
    }

    private static RectTransform CreateContentRoot(Transform panel, float top)
    {
        var root = StageResultUiFactory.CreateArea("Content", panel, Vector2.zero, Vector2.one);
        StageResultUiFactory.Inset(root, 20f, 20f, top, 16f);
        return root;
    }

    private static void ConfigureColumn(VerticalLayoutGroup layout, float spacing)
    {
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static void SetTop(RectTransform rect, float top, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -top);
        rect.sizeDelta = new Vector2(0f, height);
    }
}
