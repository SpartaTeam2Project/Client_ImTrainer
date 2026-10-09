using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// UIGameScene 프리팹의 클리어 화면에 판 결과 패널을 만든다.
/// 새로 만든 오브젝트는 StageCompleteScreen 아래 ResultLayout에 모으므로 다시 실행하면 그 부분만 새로 만든다.
/// </summary>
public static class StageCompleteScreenBuilder
{
    private const string GAME_SCENE_PREFAB = "Assets/Runtime/UI/UIGameScene.prefab";
    private const string LAYOUT_NAME = "ResultLayout";
    private const string BODY_NAME = "Body";
    private const float BODY_SIDE = 120f;
    private const float BODY_TOP = 330f;
    private const float BODY_BOTTOM = 240f;
    private const float TITLE_TOP = 40f;
    private const float TITLE_HEIGHT = 150f;
    private const float BUTTON_BOTTOM = 110f;

    /// <summary>
    /// 매니저 등록과 결과 화면 생성을 한 번에 한다. 배치 모드 -executeMethod로도 부른다.
    /// </summary>
    [MenuItem("Tools/Stage/Build Stage Result (All)")]
    public static void BuildAll()
    {
        StageResultManagersRegistrar.Register();
        Build();
    }

    [MenuItem("Tools/Stage/Build Stage Complete Screen")]
    public static void Build()
    {
        var root = PrefabUtility.LoadPrefabContents(GAME_SCENE_PREFAB);
        try
        {
            var screen = root.GetComponentInChildren<StageCompleteScreen>(true);
            if (screen == null)
            {
                Debug.LogError("UIGameScene 프리팹에서 StageCompleteScreen을 찾지 못했습니다.");
                return;
            }

            var screenSo = new SerializedObject(screen);
            var title = screenSo.FindProperty("_titleText").objectReferenceValue as TextMeshProUGUI;
            var factory = new StageResultUiFactory(title);
            if (!factory.IsValid)
            {
                Debug.LogError("클리어 화면 제목에서 TMP 폰트를 찾지 못했습니다.");
                return;
            }

            var abilities = FindAbilitiesDatabase();
            var statRow = StageResultRowPrefabBuilder.BuildStatRow(factory);
            var dataCell = StageResultRowPrefabBuilder.BuildDataCell(factory);
            var damageRow = StageResultRowPrefabBuilder.BuildDamageRow(factory);

            PlaceTitleAndButton(screen.transform, title, screenSo.FindProperty("_button").objectReferenceValue as Component);
            var layout = RecreateLayout(screen.transform);
            var body = StageResultUiFactory.CreateArea(BODY_NAME, layout, Vector2.zero, Vector2.one);
            StageResultUiFactory.Inset(body, BODY_SIDE, BODY_SIDE, BODY_TOP, BODY_BOTTOM);

            StageResultPanelBuilders.BuildHeader(factory, layout, out var stageName, out var line);
            var statPanel = StageResultPanelBuilders.BuildPlayerStats(factory, body, statRow, abilities);
            var partyPanel = StageResultPanelBuilders.BuildParty(factory, body);
            var dataPanel = StageResultPanelBuilders.BuildData(factory, body, dataCell);
            var damagePanel = StageResultPanelBuilders.BuildDamage(factory, body, damageRow, abilities);
            StageResultPanelBuilders.BuildEquipment(factory, body);

            screenSo.FindProperty("_stageNameText").objectReferenceValue = stageName;
            screenSo.FindProperty("_lineText").objectReferenceValue = line;
            screenSo.FindProperty("_resultLayout").objectReferenceValue = layout.gameObject;
            screenSo.FindProperty("_playerStatPanel").objectReferenceValue = statPanel;
            screenSo.FindProperty("_partyPanel").objectReferenceValue = partyPanel;
            screenSo.FindProperty("_dataPanel").objectReferenceValue = dataPanel;
            screenSo.FindProperty("_damagePanel").objectReferenceValue = damagePanel;
            screenSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, GAME_SCENE_PREFAB);
            Debug.Log("클리어 결과 화면을 만들었습니다.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>
    /// 기존 제목은 위, 계속 버튼은 아래로 옮겨 가운데를 결과 패널에 비운다.
    /// </summary>
    private static void PlaceTitleAndButton(Transform screen, TextMeshProUGUI title, Component button)
    {
        if (title != null)
        {
            var rect = title.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -TITLE_TOP);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, TITLE_HEIGHT);
        }

        if (button != null && button.transform is RectTransform buttonRect)
        {
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(0f, BUTTON_BOTTOM);
            buttonRect.SetAsLastSibling();
        }
    }

    private static RectTransform RecreateLayout(Transform screen)
    {
        var old = screen.Find(LAYOUT_NAME);
        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }

        var layout = StageResultUiFactory.CreateArea(LAYOUT_NAME, screen, Vector2.zero, Vector2.one);
        // 계속 버튼이 패널 위에 오도록 버튼 바로 앞에 둔다.
        layout.SetSiblingIndex(Mathf.Max(0, screen.childCount - 2));
        return layout;
    }

    private static WeaponAbilitiesDatabase FindAbilitiesDatabase()
    {
        var guids = AssetDatabase.FindAssets("t:WeaponAbilitiesDatabase");
        if (guids.Length == 0)
        {
            Debug.LogWarning("WeaponAbilitiesDatabase 에셋이 없어 스탯 아이콘을 비워 둡니다.");
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<WeaponAbilitiesDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }
}
