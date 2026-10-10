using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 폼목록.txt를 읽어 메가진화·거다이맥스 폼 SO를 기본 SO 옆에 만들거나 표대로 덮어쓴다.
/// 크기, 동작 그림, 무기 능력, 세대, 상점 값은 기본 SO에서 복사한다. 폼 전용 SpriteCollab 그림이 없으면 기본형 세트를 같이 쓴다.
/// _Mega는 기본 SO의 메가진화 칸에, _VMAX는 거다이맥스 칸에, _MegaY·_MegaZ 같은 메가 갈래는 메가진화 갈래 목록에 연결한다.
/// 아이콘과 초상화는 이 메뉴 뒤에 아이콘 채우기, 초상화 채우기로 넣는다.
/// </summary>
public static class MonsterFormCreator
{
    private const string MONSTER_FOLDER = "Assets/Runtime/SO/Monsters";
    private const string TABLE_PATH = MONSTER_FOLDER + "/폼목록.txt";
    private const string MEGA_FORM = "Mega";
    private const string VMAX_FORM = "VMAX";
    private const string LOG_PREFIX = "[MonsterFormCreator]";

    // 기본 SO에서 그대로 가져오는 칸
    private static readonly string[] COPIED_FIELDS =
    {
        "_scale", "_animations", "_bodyMaterial", "_ability", "_generation", "_dexNumber",
        "_shopPrice", "_shopCurrencyId", "_legendary",
    };

    private static readonly Regex TABLE_LINE = new Regex(@"^(\d{4})_(\w+)\s+(\S+)\s+([A-Za-z]+)(?:/([A-Za-z]+))?\s+(\d+)\s*(#.*)?$");
    private static readonly Regex ASSET_NAME = new Regex(@"^Monster(\d{4})$");

    private sealed class FormRow
    {
        public int Dex;
        public string Form;
        public string Name;
        public MonsterType PrimaryType;
        public MonsterType? SecondaryType;
        public int BaseStatTotal;

        public string AssetName => $"Monster{Dex:0000}_{Form}";
    }

    [MenuItem("Tools/Monster/폼 만들기")]
    private static void Create()
    {
        var invalid = new List<string>();
        var rows = LoadTable(invalid);
        var bases = LoadBaseForms();

        int created = 0;
        int updated = 0;
        int linked = 0;
        var missing = new List<string>();
        foreach (var row in rows)
        {
            if (!bases.TryGetValue(row.Dex, out var baseForm))
            {
                missing.Add(row.AssetName);
                continue;
            }

            var form = GetOrCreate(baseForm, row.AssetName, out var isNew);
            if (Write(form, baseForm, row))
            {
                if (isNew)
                {
                    created++;
                }
                else
                {
                    updated++;
                }
            }

            if (Link(baseForm, form, row.Form))
            {
                linked++;
            }
        }

        AssetDatabase.SaveAssets();

        var log = new StringBuilder();
        log.AppendLine($"{LOG_PREFIX} 표 {rows.Count}줄, 생성 {created}개, 갱신 {updated}개, 연결 {linked}개");
        AppendList(log, "기본 SO가 없는 폼", missing);
        AppendList(log, "읽지 못한 줄", invalid);
        if (missing.Count > 0 || invalid.Count > 0)
        {
            Debug.LogWarning(log.ToString());
        }
        else
        {
            Debug.Log(log.ToString());
        }
    }

    private static List<FormRow> LoadTable(List<string> invalid)
    {
        var rows = new List<FormRow>();
        foreach (var raw in File.ReadAllLines(TABLE_PATH, Encoding.UTF8))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#"))
            {
                continue;
            }

            var match = TABLE_LINE.Match(line);
            MonsterType secondary = MonsterType.Normal;
            if (!match.Success
                || !Enum.TryParse(match.Groups[4].Value, out MonsterType primary)
                || (match.Groups[5].Success && !Enum.TryParse(match.Groups[5].Value, out secondary)))
            {
                invalid.Add(line);
                continue;
            }

            rows.Add(new FormRow
            {
                Dex = int.Parse(match.Groups[1].Value),
                Form = match.Groups[2].Value,
                Name = match.Groups[3].Value,
                PrimaryType = primary,
                SecondaryType = match.Groups[5].Success ? secondary : (MonsterType?)null,
                BaseStatTotal = int.Parse(match.Groups[6].Value),
            });
        }

        return rows;
    }

    private static Dictionary<int, MonsterVisualData> LoadBaseForms()
    {
        var bases = new Dictionary<int, MonsterVisualData>();
        foreach (var guid in AssetDatabase.FindAssets("t:MonsterVisualData", new[] { MONSTER_FOLDER }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var match = ASSET_NAME.Match(Path.GetFileNameWithoutExtension(path));
            if (match.Success)
            {
                bases[int.Parse(match.Groups[1].Value)] = AssetDatabase.LoadAssetAtPath<MonsterVisualData>(path);
            }
        }

        return bases;
    }

    // 기본 SO와 같은 폴더에 둔다.
    private static MonsterVisualData GetOrCreate(MonsterVisualData baseForm, string assetName, out bool isNew)
    {
        var folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(baseForm)).Replace('\\', '/');
        var path = $"{folder}/{assetName}.asset";
        var form = AssetDatabase.LoadAssetAtPath<MonsterVisualData>(path);
        isNew = form == null;
        if (isNew)
        {
            form = ScriptableObject.CreateInstance<MonsterVisualData>();
            AssetDatabase.CreateAsset(form, path);
        }

        return form;
    }

    private static bool Write(MonsterVisualData form, MonsterVisualData baseForm, FormRow row)
    {
        var serialized = new SerializedObject(form);
        var source = new SerializedObject(baseForm);
        foreach (var field in COPIED_FIELDS)
        {
            serialized.CopyFromSerializedProperty(source.FindProperty(field));
        }

        serialized.FindProperty("_monsterName").stringValue = row.Name;
        serialized.FindProperty("_primaryType").intValue = (int)row.PrimaryType;
        serialized.FindProperty("_hasSecondaryType").boolValue = row.SecondaryType.HasValue;
        serialized.FindProperty("_secondaryType").intValue = (int)(row.SecondaryType ?? MonsterType.Normal);
        serialized.FindProperty("_baseStatTotal").intValue = row.BaseStatTotal;
        serialized.FindProperty("_evolutions").arraySize = 0;
        serialized.FindProperty("_megaEvolution").objectReferenceValue = null;
        serialized.FindProperty("_extraMegaEvolutions").arraySize = 0;
        serialized.FindProperty("_vmaxEvolution").objectReferenceValue = null;
        serialized.FindProperty("_startable").boolValue = false;

        if (!serialized.ApplyModifiedPropertiesWithoutUndo())
        {
            return false;
        }

        EditorUtility.SetDirty(form);
        return true;
    }

    private static bool Link(MonsterVisualData baseForm, MonsterVisualData form, string formName)
    {
        // MegaY, MegaZ 같은 메가 갈래는 메가진화 칸 대신 갈래 목록에 넣는다.
        if (formName != MEGA_FORM && formName.StartsWith(MEGA_FORM))
        {
            return LinkExtraMega(baseForm, form);
        }

        var field = formName == MEGA_FORM ? "_megaEvolution" : formName == VMAX_FORM ? "_vmaxEvolution" : null;
        if (field == null)
        {
            return false;
        }

        var serialized = new SerializedObject(baseForm);
        var property = serialized.FindProperty(field);
        if (property.objectReferenceValue == form)
        {
            return false;
        }

        property.objectReferenceValue = form;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(baseForm);
        return true;
    }

    private static bool LinkExtraMega(MonsterVisualData baseForm, MonsterVisualData form)
    {
        var serialized = new SerializedObject(baseForm);
        var property = serialized.FindProperty("_extraMegaEvolutions");
        for (var i = 0; i < property.arraySize; i++)
        {
            if (property.GetArrayElementAtIndex(i).objectReferenceValue == form)
            {
                return false;
            }
        }

        property.arraySize++;
        property.GetArrayElementAtIndex(property.arraySize - 1).objectReferenceValue = form;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(baseForm);
        return true;
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
