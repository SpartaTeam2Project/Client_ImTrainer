using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 세대별 도감목록 txt를 읽어 몬스터 SO의 비어 있는 이름, 도감 번호, 세대를 채운다.
/// 이미 값이 있는 필드는 건드리지 않는다.
/// </summary>
public static class MonsterDexFiller
{
    private const string MONSTER_FOLDER = "Assets/Runtime/SO/Monsters";
    private const string DEX_LIST_PATTERN = "*_도감목록.txt";
    private const int DEFAULT_GENERATION = 1;

    private static readonly Regex DEX_LINE = new Regex(@"^(\d{4})\s+(.+)$");
    private static readonly Regex DEX_FILE = new Regex(@"^(\d+)gen_");
    private static readonly Regex ASSET_NAME = new Regex(@"^Monster(\d{4})(_.+)?$");

    [MenuItem("Tools/Monster/도감 정보 채우기")]
    private static void Fill()
    {
        var names = new Dictionary<int, string>();
        var generations = new Dictionary<int, int>();
        LoadDexLists(names, generations);

        int filledName = 0;
        int filledDex = 0;
        int filledGeneration = 0;
        var skipped = new List<string>();
        var missing = new List<string>();

        var guids = AssetDatabase.FindAssets("t:MonsterVisualData", new[] { MONSTER_FOLDER });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<MonsterVisualData>(path);
            var match = ASSET_NAME.Match(Path.GetFileNameWithoutExtension(path));
            if (data == null || !match.Success)
            {
                skipped.Add(path);
                continue;
            }

            int dexNumber = int.Parse(match.Groups[1].Value);
            bool isBaseForm = !match.Groups[2].Success;
            if (!names.TryGetValue(dexNumber, out var dexName))
            {
                missing.Add(path);
                continue;
            }

            var serialized = new SerializedObject(data);
            bool changed = false;

            var dexProperty = serialized.FindProperty("_dexNumber");
            if (dexProperty.intValue == 0)
            {
                dexProperty.intValue = dexNumber;
                filledDex++;
                changed = true;
            }

            var nameProperty = serialized.FindProperty("_monsterName");
            if (isBaseForm && string.IsNullOrWhiteSpace(nameProperty.stringValue))
            {
                nameProperty.stringValue = dexName;
                filledName++;
                changed = true;
            }

            var generationProperty = serialized.FindProperty("_generation");
            int generation = generations[dexNumber];
            if (generationProperty.intValue <= DEFAULT_GENERATION && generation != DEFAULT_GENERATION)
            {
                generationProperty.intValue = generation;
                filledGeneration++;
                changed = true;
            }

            if (!changed)
            {
                continue;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }

        AssetDatabase.SaveAssets();

        var log = new StringBuilder();
        log.AppendLine($"[MonsterDexFiller] 에셋 {guids.Length}개 확인, 이름 {filledName} / 도감 번호 {filledDex} / 세대 {filledGeneration} 채움");
        AppendList(log, "목록에 없는 번호", missing);
        AppendList(log, "건너뛴 에셋", skipped);
        Debug.Log(log.ToString());
    }

    private static void LoadDexLists(Dictionary<int, string> names, Dictionary<int, int> generations)
    {
        foreach (var file in Directory.GetFiles(MONSTER_FOLDER, DEX_LIST_PATTERN, SearchOption.AllDirectories))
        {
            var fileMatch = DEX_FILE.Match(Path.GetFileName(file));
            if (!fileMatch.Success)
            {
                continue;
            }

            int generation = int.Parse(fileMatch.Groups[1].Value);
            foreach (var line in File.ReadAllLines(file, Encoding.UTF8))
            {
                var match = DEX_LINE.Match(line.Trim());
                if (!match.Success)
                {
                    continue;
                }

                int dexNumber = int.Parse(match.Groups[1].Value);
                names[dexNumber] = match.Groups[2].Value.Trim();
                generations[dexNumber] = generation;
            }
        }
    }

    private static void AppendList(StringBuilder log, string title, List<string> paths)
    {
        if (paths.Count == 0)
        {
            return;
        }

        log.AppendLine($"{title} {paths.Count}개");
        foreach (var path in paths)
        {
            log.AppendLine($"  {path}");
        }
    }
}
