using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 몬스터 SO를 지금 클래스 모양으로 다시 저장한다. 클래스에서 지운 필드는 다시 저장하기 전까지 파일에 남아서
/// 옛 텍스처 참조가 의존성으로 잡힌다. 필드를 지운 뒤 한 번 실행한다.
/// </summary>
public static class MonsterAssetReserializer
{
    [MenuItem("Tools/Monster/몬스터 SO 다시 저장")]
    private static void Reserialize()
    {
        var paths = AssetDatabase.FindAssets("t:MonsterVisualData", new[] { MonsterSpriteImporter.MONSTER_FOLDER })
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .ToArray();
        AssetDatabase.ForceReserializeAssets(paths, ForceReserializeAssetsOptions.ReserializeAssets);
        Debug.Log($"[MonsterAssetReserializer] 몬스터 SO {paths.Length}개를 다시 저장했다.");
    }
}
