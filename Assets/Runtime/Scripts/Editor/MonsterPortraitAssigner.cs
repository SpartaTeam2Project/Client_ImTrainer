using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// 준비 폴더의 초상화 시트(이름.png + 이름.json)를 Portrait 폴더로 복사하고 JSON 칸대로 잘라 몬스터 SO의 정보창 애니메이션에 넣는다.
/// 시트 이름은 pokerogue 형식(1, 3-mega, 6-mega-y, 3-gigantamax, 201-a)이다. 1장짜리 메가와 거다이맥스는 준비 단계에서 GIF 시트로 바꿔 둔다.
/// 이미 값이 있어도 규칙대로 덮어쓴다. 다시 실행하면 바뀐 것만 고친다.
/// </summary>
public static class MonsterPortraitAssigner
{
    private const string MONSTER_FOLDER = "Assets/Runtime/SO/Monsters";
    private const string PORTRAIT_FOLDER = "Assets/Runtime/UI/Sprites/Monster/Portrait";
    private const string SOURCE_PREF_KEY = "MonsterPortraitAssigner.SourceFolder";
    private const int CHUNK_SIZE = 100;
    private const string LOG_PREFIX = "[MonsterPortraitAssigner]";

    private static readonly Regex ASSET_NAME = new Regex(@"^Monster(\d{4})(?:_(.+))?$");

    private sealed class Entry
    {
        // SO는 묶음마다 내려가므로 경로만 들고 넣기 직전에 불러온다.
        public string AssetPath;
        public string SheetName;
    }

    private sealed class Report
    {
        public int Assigned;
        public int Copied;
        public int Resliced;
        public readonly List<string> Missing = new List<string>();
        public readonly List<string> Skipped = new List<string>();
        public readonly List<string> Errors = new List<string>();
    }

    [MenuItem("Tools/Monster/초상화 채우기")]
    private static void Assign()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            Debug.LogWarning($"{LOG_PREFIX} 플레이 중이거나 컴파일 중이라 실행하지 않음");
            return;
        }

        var source = EditorUtility.OpenFolderPanel("초상화 준비 폴더 (이름.png + 이름.json)", EditorPrefs.GetString(SOURCE_PREF_KEY, string.Empty), string.Empty);
        if (string.IsNullOrEmpty(source))
        {
            return;
        }

        EditorPrefs.SetString(SOURCE_PREF_KEY, source);
        var report = new Report();
        var entries = CollectEntries(source, report);
        var sheets = entries.GroupBy(entry => entry.SheetName).ToList();

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        AssetDatabase.DisallowAutoRefresh();
        EditorApplication.LockReloadAssemblies();
        try
        {
            for (var i = 0; i < sheets.Count; i += CHUNK_SIZE)
            {
                var chunk = sheets.Skip(i).Take(CHUNK_SIZE).ToList();
                if (EditorUtility.DisplayCancelableProgressBar("초상화 채우기", $"{chunk[0].Key} ~ {chunk[chunk.Count - 1].Key}", (float)i / sheets.Count))
                {
                    report.Errors.Add("취소됨");
                    break;
                }

                // 한 묶음의 오류로 전체가 멈추지 않게 기록만 하고 다음 묶음으로 간다. 다시 실행하면 이어진다.
                try
                {
                    ProcessChunk(chunk, source, factories, report);
                }
                catch (Exception exception)
                {
                    report.Errors.Add($"{chunk[0].Key} ~ {chunk[chunk.Count - 1].Key}: {exception.Message}");
                    Debug.LogException(exception);
                }
            }
        }
        finally
        {
            EditorApplication.UnlockReloadAssemblies();
            AssetDatabase.AllowAutoRefresh();
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        WriteLog(entries.Count, report);
    }

    private static List<Entry> CollectEntries(string source, Report report)
    {
        var entries = new List<Entry>();
        foreach (var guid in AssetDatabase.FindAssets("t:MonsterVisualData", new[] { MONSTER_FOLDER }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var match = ASSET_NAME.Match(Path.GetFileNameWithoutExtension(path));
            if (!match.Success)
            {
                report.Skipped.Add(path);
                continue;
            }

            var form = match.Groups[2].Success ? match.Groups[2].Value : null;
            var sheetName = MonsterFormSpriteNames.Candidates(int.Parse(match.Groups[1].Value), form, "D")
                .FirstOrDefault(name => File.Exists(SourcePath(source, name, ".png")) && File.Exists(SourcePath(source, name, ".json")));
            if (sheetName == null)
            {
                report.Missing.Add(path);
                continue;
            }

            entries.Add(new Entry { AssetPath = path, SheetName = sheetName });
        }

        return entries;
    }

    private static void ProcessChunk(List<IGrouping<string, Entry>> chunk, string source, SpriteDataProviderFactories factories, Report report)
    {
        // 1. 없거나 내용이 다른 시트만 복사하고 한 번에 임포트한다.
        var copied = new List<string>();
        foreach (var sheet in chunk)
        {
            var from = SourcePath(source, sheet.Key, ".png");
            var to = AssetPath(sheet.Key);
            if (File.Exists(to) && File.ReadAllBytes(to).AsSpan().SequenceEqual(File.ReadAllBytes(from)))
            {
                continue;
            }

            File.Copy(from, to, true);
            copied.Add(to);
        }

        if (copied.Count > 0)
        {
            using (new AssetDatabase.AssetEditingScope())
            {
                foreach (var path in copied)
                {
                    AssetDatabase.ImportAsset(path);
                }
            }
        }

        report.Copied += copied.Count;

        // 2. 설정과 칸을 맞춘다. 바뀐 시트만 모아서 한 번에 다시 임포트한다.
        var layouts = new Dictionary<string, PortraitSheetSlicer.Layout>();
        var changed = new List<TextureImporter>();
        foreach (var sheet in chunk)
        {
            var path = AssetPath(sheet.Key);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                report.Errors.Add($"{path}: 텍스처 임포터가 없음");
                continue;
            }

            if (!PortraitSheetSlicer.TryReadLayout(SourcePath(source, sheet.Key, ".json"), out var layout, out var error))
            {
                report.Errors.Add($"{path}: {error}");
                continue;
            }

            importer.GetSourceTextureWidthAndHeight(out var width, out var height);
            var settingsChanged = MonsterSpriteSheetSlicer.ApplyImportSettings(importer, width, height);
            var cellsChanged = PortraitSheetSlicer.ApplyCells(importer, factories, layout, out error);
            if (error != null)
            {
                report.Errors.Add($"{path}: {error}");
                continue;
            }

            if (settingsChanged || cellsChanged)
            {
                changed.Add(importer);
            }

            layouts.Add(sheet.Key, layout);
        }

        if (changed.Count > 0)
        {
            using (new AssetDatabase.AssetEditingScope())
            {
                foreach (var importer in changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        report.Resliced += changed.Count;

        // 3. SO에 넣는다.
        foreach (var sheet in chunk)
        {
            if (!layouts.TryGetValue(sheet.Key, out var layout))
            {
                continue;
            }

            var frames = PortraitSheetSlicer.LoadFrames(AssetPath(sheet.Key), layout);
            if (frames == null)
            {
                report.Errors.Add($"{AssetPath(sheet.Key)}: 잘린 스프라이트를 찾지 못함");
                continue;
            }

            foreach (var entry in sheet)
            {
                var data = AssetDatabase.LoadAssetAtPath<MonsterVisualData>(entry.AssetPath);
                if (data == null)
                {
                    report.Errors.Add($"{entry.AssetPath}: SO를 불러오지 못함");
                    continue;
                }

                if (Apply(data, frames))
                {
                    report.Assigned++;
                }
            }
        }

        // 고친 SO를 먼저 저장해야 내릴 때 변경이 사라지지 않는다.
        AssetDatabase.SaveAssets();
        EditorUtility.UnloadUnusedAssetsImmediate();
    }

    private static bool Apply(MonsterVisualData data, Sprite[] frames)
    {
        var serialized = new SerializedObject(data);
        var property = serialized.FindProperty("_infoAnimation");

        bool same = property.arraySize == frames.Length;
        for (var i = 0; same && i < frames.Length; i++)
        {
            same = property.GetArrayElementAtIndex(i).objectReferenceValue == frames[i];
        }

        if (same)
        {
            return false;
        }

        property.arraySize = frames.Length;
        for (var i = 0; i < frames.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        return true;
    }

    private static string SourcePath(string source, string sheetName, string extension)
    {
        return Path.Combine(source, sheetName + extension);
    }

    private static string AssetPath(string sheetName)
    {
        return $"{PORTRAIT_FOLDER}/{sheetName}.png";
    }

    private static void WriteLog(int total, Report report)
    {
        var log = new StringBuilder();
        log.AppendLine($"{LOG_PREFIX} 대상 {total}개, 초상화 {report.Assigned}개 채움, 시트 복사 {report.Copied}개, 다시 자름 {report.Resliced}개");
        AppendList(log, "원본 시트가 없는 에셋", report.Missing);
        AppendList(log, "건너뛴 에셋", report.Skipped);
        AppendList(log, "오류", report.Errors);

        if (report.Errors.Count > 0)
        {
            Debug.LogWarning(log.ToString());
        }
        else
        {
            Debug.Log(log.ToString());
        }
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
