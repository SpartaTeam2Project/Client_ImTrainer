using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

/// <summary>
/// MonsterVisualData마다 쓸 MonsterAnimationSet 에셋을 찾거나 만들고 Addressables에 등록한다.
/// 형태 SO는 SpriteCollab에 자기 그림이 있을 때만 따로 세트를 갖고, 없으면 기본형 세트를 같이 가리킨다.
/// 그래야 번들에 같은 텍스처가 두 번 들어가지 않는다.
/// </summary>
public static class MonsterAnimationAssets
{
    public const string SET_FOLDER = "Assets/Runtime/SO/MonsterAnimations";
    public const string GROUP_NAME = "MonsterAnimations";
    private const string ADDRESS_PREFIX = "MonsterAnim/";
    private const string SET_PREFIX = "Anim";
    private const string ASSET_GUID_FIELD = "m_AssetGUID";

    private static readonly Regex ASSET_NAME = new Regex(@"^Monster(\d{4})(?:_(.+))?$");

    /// <summary>
    /// SO 이름으로 세트 키를 정한다. 예: Monster0001 → 0001, Monster0006_Mega → 0006_Mega, Monster0003_Mega → 0003.
    /// 이름 형식이 아니면 null.
    /// </summary>
    public static string SetKey(string visualName)
    {
        var match = ASSET_NAME.Match(visualName);
        if (!match.Success)
        {
            return null;
        }

        var dex = match.Groups[1].Value;
        if (!match.Groups[2].Success)
        {
            return dex;
        }

        var form = match.Groups[2].Value;
        return MonsterSpriteImporter.HasFormFolder(dex, form) ? $"{dex}_{form}" : dex;
    }

    /// <summary>
    /// 연결된 세트. 없으면 null.
    /// </summary>
    public static MonsterAnimationSet Find(MonsterVisualData visual)
    {
        return visual != null && visual.Animations != null ? visual.Animations.editorAsset : null;
    }

    /// <summary>
    /// 연결된 세트를 돌려준다. 없으면 키에 맞는 세트를 찾거나 만들고, Addressables에 등록한 뒤 SO에 연결한다.
    /// </summary>
    public static MonsterAnimationSet GetOrCreate(MonsterVisualData visual, out bool created)
    {
        created = false;
        var existing = Find(visual);
        if (existing != null)
        {
            Register(existing);
            return existing;
        }

        var key = SetKey(visual.name);
        if (key == null)
        {
            return null;
        }

        var path = SetPath(AssetDatabase.GetAssetPath(visual), key);
        var set = AssetDatabase.LoadAssetAtPath<MonsterAnimationSet>(path);
        if (set == null)
        {
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            set = ScriptableObject.CreateInstance<MonsterAnimationSet>();
            AssetDatabase.CreateAsset(set, path);
            created = true;
        }

        Register(set);
        Link(visual, set);
        return set;
    }

    /// <summary>
    /// 세트를 MonsterAnimations 그룹에 "MonsterAnim/{키}" 주소로 넣는다. 이미 있으면 주소만 맞춘다.
    /// </summary>
    public static void Register(MonsterAnimationSet set)
    {
        var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        var group = EnsureGroup(settings);
        var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(set));
        var entry = settings.FindAssetEntry(guid);
        if (entry == null || entry.parentGroup != group)
        {
            entry = settings.CreateOrMoveEntry(guid, group, false, false);
        }

        var address = ADDRESS_PREFIX + set.name.Substring(SET_PREFIX.Length);
        if (entry.address != address)
        {
            entry.SetAddress(address, false);
        }
    }

    /// <summary>
    /// 여러 에셋을 등록한 뒤 한 번 저장한다.
    /// </summary>
    public static void SaveSettings()
    {
        var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        if (settings == null)
        {
            return;
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
        AssetDatabase.SaveAssets();
    }

    private static void Link(MonsterVisualData visual, MonsterAnimationSet set)
    {
        var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(set));
        using (var serialized = new SerializedObject(visual))
        {
            var property = serialized.FindProperty("_animations").FindPropertyRelative(ASSET_GUID_FIELD);
            if (property.stringValue == guid)
            {
                return;
            }

            property.stringValue = guid;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(visual);
    }

    // 세트는 SO와 같은 세대/범위 폴더 구조로 둔다. 형태가 기본형 세트를 쓰면 같은 폴더의 기본형 파일을 가리킨다.
    private static string SetPath(string visualPath, string key)
    {
        var folder = Path.GetDirectoryName(visualPath).Replace('\\', '/');
        var relative = folder.Length > MonsterSpriteImporter.MONSTER_FOLDER.Length
            ? folder.Substring(MonsterSpriteImporter.MONSTER_FOLDER.Length)
            : string.Empty;
        return $"{SET_FOLDER}{relative}/{SET_PREFIX}{key}.asset";
    }

    // 종마다 번들 하나: 그 판에 나오는 종의 텍스처만 올라온다.
    private static AddressableAssetGroup EnsureGroup(AddressableAssetSettings settings)
    {
        var group = settings.FindGroup(GROUP_NAME);
        if (group == null)
        {
            group = settings.CreateGroup(GROUP_NAME, false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
        }

        var schema = group.GetSchema<BundledAssetGroupSchema>();
        if (schema != null && schema.BundleMode != BundledAssetGroupSchema.BundlePackingMode.PackSeparately)
        {
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            EditorUtility.SetDirty(schema);
        }

        return group;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
