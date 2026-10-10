using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 진화목록.txt를 읽어 기본형 몬스터 SO의 다음 진화를 표대로 덮어쓴다. 표에 없는 종은 최종 진화로 비운다.
/// 메가진화와 거다이맥스 에셋은 건드리지 않는다.
/// </summary>
public static class MonsterEvolutionFiller
{
    private const string MONSTER_FOLDER = "Assets/Runtime/SO/Monsters";
    private const string TABLE_PATH = MONSTER_FOLDER + "/진화목록.txt";
    private const int MAX_EVOLUTION_DEPTH = 2;

    // 갈래 진화(라플레시아, 아르코)를 상점에서 살 수 있게 기본종으로 연다.
    private static readonly int[] STARTABLE_DEX = { 43 };

    private static readonly Regex TABLE_LINE = new Regex(@"^(\d{4})\s*>\s*([\d\s]+?)\s*(#.*)?$");
    private static readonly Regex ASSET_NAME = new Regex(@"^Monster(\d{4})$");

    [MenuItem("Tools/Monster/진화 채우기")]
    private static void Fill()
    {
        var table = LoadTable(out var invalidLines);
        var assets = LoadBaseForms();
        var log = new StringBuilder();
        var previous = new Dictionary<int, int>();
        var missing = new List<string>();
        foreach (var pair in table)
        {
            foreach (var next in pair.Value)
            {
                if (previous.ContainsKey(next))
                {
                    log.AppendLine($"이전 종이 둘인 번호 {next:0000}: {previous[next]:0000}, {pair.Key:0000}");
                }

                previous[next] = pair.Key;
                if (!assets.ContainsKey(next))
                {
                    missing.Add($"{pair.Key:0000} > {next:0000}");
                }
            }

            if (!assets.ContainsKey(pair.Key))
            {
                missing.Add($"{pair.Key:0000}");
            }
        }

        int changed = 0;
        foreach (var pair in assets)
        {
            var serialized = new SerializedObject(pair.Value);
            var targets = table.TryGetValue(pair.Key, out var list) ? list : new List<int>();
            var dirty = WriteEvolutions(serialized.FindProperty("_evolutions"), targets, assets);
            if (System.Array.IndexOf(STARTABLE_DEX, pair.Key) >= 0)
            {
                var startable = serialized.FindProperty("_startable");
                dirty |= !startable.boolValue;
                startable.boolValue = true;
            }

            if (!dirty)
            {
                continue;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pair.Value);
            changed++;
        }

        AssetDatabase.SaveAssets();

        var tooDeep = new List<string>();
        var hiddenStartable = new List<string>();
        foreach (var pair in assets)
        {
            var basic = pair.Key;
            var depth = 0;
            while (previous.TryGetValue(basic, out var before) && depth <= MAX_EVOLUTION_DEPTH)
            {
                basic = before;
                depth++;
            }

            if (depth > MAX_EVOLUTION_DEPTH)
            {
                tooDeep.Add($"{pair.Key:0000}");
            }

            // 이전 종이 생긴 종은 상점 후보에서 빠지므로, 계통의 기본종이 시작 가능이 아니면 계통째 상점에서 사라진다.
            if (depth > 0 && pair.Value.Startable && assets.TryGetValue(basic, out var basicData) && !basicData.Startable)
            {
                hiddenStartable.Add($"{pair.Key:0000} {pair.Value.MonsterName} (기본종 {basic:0000} {basicData.MonsterName})");
            }
        }

        log.Insert(0, $"[MonsterEvolutionFiller] 표 {table.Count}줄, 기본형 에셋 {assets.Count}개 중 {changed}개 바꿈\n");
        AppendList(log, "읽지 못한 줄", invalidLines);
        AppendList(log, "에셋이 없는 번호", missing);
        AppendList(log, "3단을 넘는 계통", tooDeep);
        AppendList(log, "기본종이 시작 가능이 아니라 상점에 안 나오는 종", hiddenStartable);
        Debug.Log(log.ToString());
    }

    /// <summary>
    /// 배열을 targets 순서대로 맞춘다. 바뀐 게 있으면 true.
    /// </summary>
    private static bool WriteEvolutions(SerializedProperty property, List<int> targets, Dictionary<int, MonsterVisualData> assets)
    {
        var values = new List<MonsterVisualData>();
        foreach (var target in targets)
        {
            if (assets.TryGetValue(target, out var data))
            {
                values.Add(data);
            }
        }

        var same = property.arraySize == values.Count;
        for (var i = 0; same && i < values.Count; i++)
        {
            same = property.GetArrayElementAtIndex(i).objectReferenceValue == values[i];
        }

        if (same)
        {
            return false;
        }

        property.arraySize = values.Count;
        for (var i = 0; i < values.Count; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        return true;
    }

    private static Dictionary<int, List<int>> LoadTable(out List<string> invalidLines)
    {
        var table = new Dictionary<int, List<int>>();
        invalidLines = new List<string>();
        foreach (var raw in File.ReadAllLines(TABLE_PATH, Encoding.UTF8))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#"))
            {
                continue;
            }

            var match = TABLE_LINE.Match(line);
            if (!match.Success)
            {
                invalidLines.Add(line);
                continue;
            }

            var targets = new List<int>();
            foreach (var token in match.Groups[2].Value.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries))
            {
                targets.Add(int.Parse(token));
            }

            table[int.Parse(match.Groups[1].Value)] = targets;
        }

        return table;
    }

    private static Dictionary<int, MonsterVisualData> LoadBaseForms()
    {
        var assets = new Dictionary<int, MonsterVisualData>();
        foreach (var guid in AssetDatabase.FindAssets("t:MonsterVisualData", new[] { MONSTER_FOLDER }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var match = ASSET_NAME.Match(Path.GetFileNameWithoutExtension(path));
            var data = match.Success ? AssetDatabase.LoadAssetAtPath<MonsterVisualData>(path) : null;
            if (data != null)
            {
                assets[int.Parse(match.Groups[1].Value)] = data;
            }
        }

        return assets;
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
