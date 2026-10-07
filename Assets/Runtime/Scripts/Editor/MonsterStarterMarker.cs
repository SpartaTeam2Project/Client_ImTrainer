using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 1·2·4세대 진화 라인의 첫 단계 포켓몬을 시작 가능으로 켜고 MonsterDatabase에 등록한다.
/// 베이비가 있는 라인은 베이비만, 진화하지 않는 포켓몬은 전설·환상까지 포함한다.
/// 이미 켜진 다른 종은 끄지 않는다.
/// </summary>
public static class MonsterStarterMarker
{
    private const string MONSTER_FOLDER = "Assets/Runtime/SO/Monsters";
    private const string DATABASE_PATH = MONSTER_FOLDER + "/MonsterDatabase.asset";

    private static readonly Regex ASSET_NAME = new Regex(@"^Monster(\d{4})(_.+)?$");

    private static readonly HashSet<int> STARTER_DEX_NUMBERS = new HashSet<int>
    {
        // 세대마다 기획에서 고른 일부 종은 시작 가능에서 뺐다

        // 1세대: 피카츄·삐삐·푸린·시라소몬·홍수몬·럭키·마임맨·루주라·에레브·마그마·잠만보는 베이비가 있어 제외
        1, 4, 7, 10, 13, 16, 19, 23, 41, 50, 52, 54, 56, 60, 63, 66, 69, 74, 79, 81,
        83, 86, 88, 90, 92, 95, 98, 102, 104, 109, 111, 116, 120, 123, 129, 131, 133, 137, 142, 144,
        145, 146, 147, 150, 151,

        // 2세대: 마릴·꼬지모·마자용·만타인은 베이비가 있어 제외
        152, 155, 158, 172, 173, 174, 175, 179, 214, 216, 228, 236, 239, 240, 241, 243, 244, 245, 246, 249,
        250, 251,

        // 4세대: 이전 세대에서 진화하는 신규 진화형은 제외
        387, 390, 393, 396, 403, 427, 440, 443, 446, 447, 453, 459, 479, 483, 484, 485, 486, 487, 488,
        491, 493,
    };

    [MenuItem("Tools/Monster/시작 가능 포켓몬 설정 (1·2·4세대)")]
    private static void Mark()
    {
        var database = AssetDatabase.LoadAssetAtPath<MonsterDatabase>(DATABASE_PATH);
        if (database == null)
        {
            Debug.LogError($"[MonsterStarterMarker] {DATABASE_PATH}를 찾지 못했다.");
            return;
        }

        var starters = new SortedDictionary<int, MonsterVisualData>();
        int marked = 0;

        var guids = AssetDatabase.FindAssets("t:MonsterVisualData", new[] { MONSTER_FOLDER });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var match = ASSET_NAME.Match(Path.GetFileNameWithoutExtension(path));
            if (!match.Success || match.Groups[2].Success)
            {
                continue;
            }

            int dexNumber = int.Parse(match.Groups[1].Value);
            if (!STARTER_DEX_NUMBERS.Contains(dexNumber))
            {
                continue;
            }

            var data = AssetDatabase.LoadAssetAtPath<MonsterVisualData>(path);
            if (data == null)
            {
                continue;
            }

            starters[dexNumber] = data;

            var serialized = new SerializedObject(data);
            var startableProperty = serialized.FindProperty("_startable");
            if (startableProperty.boolValue)
            {
                continue;
            }

            startableProperty.boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            marked++;
        }

        int added = AddToDatabase(database, starters.Values);

        AssetDatabase.SaveAssets();

        var missing = STARTER_DEX_NUMBERS.Where(dex => !starters.ContainsKey(dex)).OrderBy(dex => dex)
            .Select(dex => $"Monster{dex:0000}").ToList();

        var log = new StringBuilder();
        log.AppendLine($"[MonsterStarterMarker] 대상 {starters.Count}/{STARTER_DEX_NUMBERS.Count}개, 시작 가능 {marked}개 켬, 데이터베이스에 {added}개 추가");
        AppendList(log, "에셋을 찾지 못한 번호", missing);
        Debug.Log(log.ToString());
    }

    private static int AddToDatabase(MonsterDatabase database, IEnumerable<MonsterVisualData> starters)
    {
        var serialized = new SerializedObject(database);
        var monstersProperty = serialized.FindProperty("_monsters");

        var registered = new HashSet<Object>();
        for (int i = 0; i < monstersProperty.arraySize; i++)
        {
            registered.Add(monstersProperty.GetArrayElementAtIndex(i).objectReferenceValue);
        }

        int added = 0;
        foreach (var starter in starters)
        {
            if (!registered.Add(starter))
            {
                continue;
            }

            int index = monstersProperty.arraySize;
            monstersProperty.InsertArrayElementAtIndex(index);
            monstersProperty.GetArrayElementAtIndex(index).objectReferenceValue = starter;
            added++;
        }

        if (added > 0)
        {
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        return added;
    }

    private static void AppendList(StringBuilder log, string title, List<string> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        log.AppendLine($"{title} {items.Count}개");
        foreach (var item in items)
        {
            log.AppendLine($"  {item}");
        }
    }
}
