using System.Text;
using UnityEngine;

/// <summary>
/// 복제한 오브젝트에서 원본 컴포넌트와 같은 자리에 있는 컴포넌트를 찾는다.
/// 원본 루트에서 컴포넌트까지의 자식 경로를 복제본 루트에 그대로 따라간다.
/// </summary>
public static class HierarchyCounterpart
{
    /// <summary>
    /// source가 sourceRoot 아래에 있으면 cloneRoot 아래 같은 경로의 T를 돌려준다. 없으면 null.
    /// </summary>
    public static T Find<T>(T source, Transform sourceRoot, Transform cloneRoot) where T : Component
    {
        if (source == null || sourceRoot == null || cloneRoot == null)
        {
            return null;
        }

        var path = RelativePath(source.transform, sourceRoot);
        if (path == null)
        {
            return null;
        }

        var target = path.Length == 0 ? cloneRoot : cloneRoot.Find(path);
        return target != null ? target.GetComponent<T>() : null;
    }

    // root 바로 아래부터 이름을 / 로 이은 경로. root 자신이면 빈 문자열, root 아래가 아니면 null.
    private static string RelativePath(Transform target, Transform root)
    {
        var builder = new StringBuilder();
        for (var current = target; current != null; current = current.parent)
        {
            if (current == root)
            {
                return builder.ToString();
            }

            builder.Insert(0, builder.Length > 0 ? current.name + "/" : current.name);
        }

        return null;
    }
}
