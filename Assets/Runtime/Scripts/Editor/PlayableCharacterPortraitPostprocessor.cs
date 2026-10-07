using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// 초상화 시트를 다시 자르거나 바꾸면 그 시트를 쓰는 캐릭터의 그림 영역을 다시 잰다.
/// 칸 크기가 바뀌었는데 예전 영역이 남으면 초상화가 엉뚱한 곳에서 잘린다.
/// </summary>
public class PlayableCharacterPortraitPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (importedAssets.Length == 0)
        {
            return;
        }

        var imported = new HashSet<string>(importedAssets);
        foreach (var guid in AssetDatabase.FindAssets("t:PlayableCharacterData"))
        {
            var data = AssetDatabase.LoadAssetAtPath<PlayableCharacterData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data == null || !imported.Contains(data.PortraitPath))
            {
                continue;
            }

            if (data.RefreshPortraitArea())
            {
                AssetDatabase.SaveAssetIfDirty(data);
            }
        }
    }
}
