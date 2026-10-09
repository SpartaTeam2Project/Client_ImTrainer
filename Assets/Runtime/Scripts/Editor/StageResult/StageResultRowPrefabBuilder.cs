using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 결과 화면 행 프리팹 네 종을 만들어 저장한다. 다시 실행하면 같은 경로에 덮어쓴다.
/// </summary>
public static class StageResultRowPrefabBuilder
{
    public const string FOLDER = "Assets/Runtime/UI/StageResult";

    public const float STAT_ROW_HEIGHT = 80f;
    public const float DAMAGE_ROW_HEIGHT = 120f;
    public static readonly Vector2 DATA_CELL_SIZE = new Vector2(460f, 64f);
    public static readonly Vector2 PARTY_SLOT_SIZE = new Vector2(170f, 170f);

    public static StageStatRowView BuildStatRow(StageResultUiFactory factory)
    {
        var root = CreateRow("StageStatRow", STAT_ROW_HEIGHT);
        try
        {
            var icon = factory.AddIcon(StageResultUiFactory.CreateFixed("Icon", root, new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(60f, 60f)).gameObject);
            var label = factory.CreateLabel("Label", root, 40f, TextAlignmentOptions.Left, "스탯", Color.white);
            Stretch(label.rectTransform, 90f, 160f);
            var value = factory.CreateLabel("Value", root, 40f, TextAlignmentOptions.Right, "0", StageResultUiFactory.VALUE_YELLOW);
            SetRightBox(value.rectTransform, 150f);

            var view = root.gameObject.AddComponent<StageStatRowView>();
            var so = new SerializedObject(view);
            so.FindProperty("_icon").objectReferenceValue = icon;
            so.FindProperty("_label").objectReferenceValue = label;
            so.FindProperty("_value").objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            return Save(root.gameObject).GetComponent<StageStatRowView>();
        }
        finally
        {
            Object.DestroyImmediate(root.gameObject);
        }
    }

    public static StageDataCellView BuildDataCell(StageResultUiFactory factory)
    {
        var root = StageResultUiFactory.CreateRect("StageDataCell", null);
        root.sizeDelta = DATA_CELL_SIZE;
        try
        {
            var label = factory.CreateLabel("Label", root, 38f, TextAlignmentOptions.Left, "항목", Color.white);
            Stretch(label.rectTransform, 10f, 160f);
            var value = factory.CreateLabel("Value", root, 38f, TextAlignmentOptions.Right, "0", StageResultUiFactory.VALUE_GREEN);
            SetRightBox(value.rectTransform, 150f);

            var view = root.gameObject.AddComponent<StageDataCellView>();
            var so = new SerializedObject(view);
            so.FindProperty("_label").objectReferenceValue = label;
            so.FindProperty("_value").objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            return Save(root.gameObject).GetComponent<StageDataCellView>();
        }
        finally
        {
            Object.DestroyImmediate(root.gameObject);
        }
    }

    public static StageDamageRowView BuildDamageRow(StageResultUiFactory factory)
    {
        var root = CreateRow("StageDamageRow", DAMAGE_ROW_HEIGHT);
        try
        {
            var group = root.gameObject.AddComponent<CanvasGroup>();
            factory.AddImage(root.gameObject, StageResultUiFactory.HEADER_DARK, true);
            var icon = factory.AddIcon(StageResultUiFactory.CreateFixed("Icon", root, new Vector2(0f, 0.5f), new Vector2(64f, 10f), new Vector2(96f, 96f)).gameObject);
            var stars = factory.CreateStars(root, new Vector2(0f, 0f), new Vector2(64f, 16f), 26f);

            var damage = factory.CreateLabel("Damage", root, 40f, TextAlignmentOptions.Left, "0", Color.white);
            SetBox(damage.rectTransform, new Vector2(0f, 1f), new Vector2(130f, -8f), new Vector2(300f, 56f));
            var dps = factory.CreateLabel("Dps", root, 40f, TextAlignmentOptions.Right, "0/s", Color.white);
            SetBox(dps.rectTransform, new Vector2(1f, 1f), new Vector2(-20f, -8f), new Vector2(260f, 56f));
            var percent = factory.CreateLabel("Percent", root, 30f, TextAlignmentOptions.Left, "100%", Color.white);
            SetBox(percent.rectTransform, new Vector2(0f, 0f), new Vector2(130f, 10f), new Vector2(110f, 44f));

            var barBack = StageResultUiFactory.CreateRect("Bar", root);
            barBack.anchorMin = new Vector2(0f, 0f);
            barBack.anchorMax = new Vector2(1f, 0f);
            barBack.pivot = new Vector2(0.5f, 0f);
            barBack.offsetMin = new Vector2(250f, 18f);
            barBack.offsetMax = new Vector2(-20f, 38f);
            factory.AddImage(barBack.gameObject, new Color(0f, 0f, 0f, 0.6f), true);
            var fillRect = StageResultUiFactory.CreateArea("Fill", barBack, Vector2.zero, Vector2.one);
            var fill = factory.AddImage(fillRect.gameObject, StageResultUiFactory.BAR_BLUE, true);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;

            var view = root.gameObject.AddComponent<StageDamageRowView>();
            var so = new SerializedObject(view);
            so.FindProperty("_group").objectReferenceValue = group;
            so.FindProperty("_icon").objectReferenceValue = icon;
            StageResultUiFactory.SetObjects(so, "_stars", stars);
            so.FindProperty("_damage").objectReferenceValue = damage;
            so.FindProperty("_dps").objectReferenceValue = dps;
            so.FindProperty("_percent").objectReferenceValue = percent;
            so.FindProperty("_presenceFill").objectReferenceValue = fill;
            so.ApplyModifiedPropertiesWithoutUndo();
            return Save(root.gameObject).GetComponent<StageDamageRowView>();
        }
        finally
        {
            Object.DestroyImmediate(root.gameObject);
        }
    }

    /// <summary>
    /// 파티 칸은 칸마다 위치가 달라 프리팹으로 두지 않고 패널 안에 바로 만든다.
    /// </summary>
    public static StagePartySlotView CreatePartySlot(StageResultUiFactory factory, Transform parent, string name, Vector2 position)
    {
        var root = StageResultUiFactory.CreateFixed(name, parent, new Vector2(0.5f, 0.5f), position, PARTY_SLOT_SIZE);
        factory.AddImage(root.gameObject, StageResultUiFactory.SLOT_DARK, true);
        var icon = factory.AddIcon(StageResultUiFactory.CreateFixed("Icon", root, new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(130f, 130f)).gameObject);
        var stars = factory.CreateStars(root, new Vector2(0.5f, 0f), new Vector2(0f, 20f), 28f);

        var view = root.gameObject.AddComponent<StagePartySlotView>();
        var so = new SerializedObject(view);
        so.FindProperty("_icon").objectReferenceValue = icon;
        StageResultUiFactory.SetObjects(so, "_stars", stars);
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    private static RectTransform CreateRow(string name, float height)
    {
        var root = StageResultUiFactory.CreateRect(name, null);
        root.sizeDelta = new Vector2(600f, height);
        var layout = root.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;
        return root;
    }

    private static void Stretch(RectTransform rect, float left, float right)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(left, 0f);
        rect.offsetMax = new Vector2(-right, 0f);
    }

    private static void SetRightBox(RectTransform rect, float width)
    {
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-10f, 0f);
        rect.sizeDelta = new Vector2(width, 0f);
    }

    private static void SetBox(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static GameObject Save(GameObject go)
    {
        if (!AssetDatabase.IsValidFolder(FOLDER))
        {
            AssetDatabase.CreateFolder("Assets/Runtime/UI", "StageResult");
        }

        return PrefabUtility.SaveAsPrefabAsset(go, FOLDER + "/" + go.name + ".prefab");
    }
}
