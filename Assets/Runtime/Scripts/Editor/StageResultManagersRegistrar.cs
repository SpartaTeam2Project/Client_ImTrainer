using UnityEditor;
using UnityEngine;

/// <summary>
/// Managers 프리팹에 판 통계와 랭킹 매니저를 붙이고 Managers 필드에 연결한다. 이미 있으면 연결만 다시 한다.
/// </summary>
public static class StageResultManagersRegistrar
{
    private const string MANAGERS_PREFAB = "Assets/Runtime/Prefabs/Management/Managers.prefab";

    [MenuItem("Tools/Stage/Register Stage Result Managers")]
    public static void Register()
    {
        var root = PrefabUtility.LoadPrefabContents(MANAGERS_PREFAB);
        try
        {
            var managers = root.GetComponentInChildren<Managers>(true);
            if (managers == null)
            {
                Debug.LogError("Managers 프리팹에서 Managers를 찾지 못했습니다.");
                return;
            }

            var so = new SerializedObject(managers);
            so.FindProperty("_stageStatsManager").objectReferenceValue = GetOrAdd<StageStatsManager>(managers.gameObject);
            so.FindProperty("_rankManager").objectReferenceValue = GetOrAdd<RankManager>(managers.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, MANAGERS_PREFAB);
            Debug.Log("StageStatsManager와 RankManager를 Managers 프리팹에 연결했습니다.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
}
