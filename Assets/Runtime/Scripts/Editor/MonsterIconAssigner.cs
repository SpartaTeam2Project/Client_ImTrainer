using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 아이콘 시트의 스프라이트를 도감 번호에 맞춰 몬스터 SO의 아이콘과 아이콘 크기에 넣는다.
/// 스프라이트 이름은 0001, 0003-mega, 0003-gigantamax 형식이다. 이로치(0001s)는 쓰지 않는다.
/// 이미 값이 있어도 규칙대로 덮어쓴다.
/// </summary>
public static class MonsterIconAssigner
{
    private const string MONSTER_FOLDER = "Assets/Runtime/SO/Monsters";
    private const string ICON_FOLDER = "Assets/Runtime/UI/Sprites/Monster/Icon";

    private static readonly Regex ASSET_NAME = new Regex(@"^Monster(\d{4})(?:_(.+))?$");

    // 기본 이름 스프라이트가 없고 폼 이름만 있는 종
    private static readonly Dictionary<int, string> DEFAULT_FORM_SUFFIX = new Dictionary<int, string>
    {
        { 201, "-a" },
        { 412, "-plant" },
        { 413, "-plant" },
        { 421, "-overcast" },
        { 422, "-west" },
        { 423, "-west" },
        { 487, "-altered" },
        { 492, "-land" },
        { 493, "-normal" },
    };

    // 폼 에셋 접미사별로 앞에서부터 찾는 스프라이트 접미사
    private static readonly Dictionary<string, string[]> FORM_SUFFIXES = new Dictionary<string, string[]>
    {
        { "Mega", new[] { "-mega", "-mega-y", "-mega-x" } },
        { "VMAX", new[] { "-gigantamax" } },
    };

    [MenuItem("Tools/Monster/아이콘 채우기")]
    private static void Assign()
    {
        var sprites = LoadSprites();

        int assigned = 0;
        int noSheet = 0;
        var missing = new List<string>();
        var skipped = new List<string>();

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

            string dex = match.Groups[1].Value;
            string form = match.Groups[2].Success ? match.Groups[2].Value : null;
            var sprite = FindSprite(sprites, dex, form);
            if (sprite == null)
            {
                if (sprites.Keys.Any(name => name.StartsWith(dex)))
                {
                    missing.Add(path);
                }
                else
                {
                    noSheet++;
                }
                continue;
            }

            if (Apply(data, sprite))
            {
                assigned++;
            }
        }

        AssetDatabase.SaveAssets();

        var log = new StringBuilder();
        log.AppendLine($"[MonsterIconAssigner] 에셋 {guids.Length}개 확인, 아이콘 {assigned}개 채움, 시트 없는 에셋 {noSheet}개");
        AppendList(log, "스프라이트를 찾지 못한 에셋", missing);
        AppendList(log, "건너뛴 에셋", skipped);
        Debug.Log(log.ToString());
    }

    private static Dictionary<string, Sprite> LoadSprites()
    {
        var sprites = new Dictionary<string, Sprite>();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ICON_FOLDER }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
            {
                sprites[sprite.name] = sprite;
            }
        }

        return sprites;
    }

    private static Sprite FindSprite(Dictionary<string, Sprite> sprites, string dex, string form)
    {
        IEnumerable<string> candidates;
        if (form == null)
        {
            var defaultForm = DEFAULT_FORM_SUFFIX.TryGetValue(int.Parse(dex), out var suffix) ? dex + suffix : null;
            candidates = new[] { dex, defaultForm };
        }
        else if (FORM_SUFFIXES.TryGetValue(form, out var suffixes))
        {
            candidates = suffixes.Select(s => dex + s);
        }
        else
        {
            return null;
        }

        foreach (var name in candidates)
        {
            if (name != null && sprites.TryGetValue(name, out var sprite))
            {
                return sprite;
            }
        }

        return null;
    }

    private static bool Apply(MonsterVisualData data, Sprite sprite)
    {
        var serialized = new SerializedObject(data);
        var iconProperty = serialized.FindProperty("_icon");
        var sizeProperty = serialized.FindProperty("_iconSize");
        var size = sprite.rect.size;

        bool same = iconProperty.arraySize == 1
            && iconProperty.GetArrayElementAtIndex(0).objectReferenceValue == sprite
            && sizeProperty.vector2Value == size;
        if (same)
        {
            return false;
        }

        iconProperty.arraySize = 1;
        iconProperty.GetArrayElementAtIndex(0).objectReferenceValue = sprite;
        sizeProperty.vector2Value = size;

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        return true;
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
