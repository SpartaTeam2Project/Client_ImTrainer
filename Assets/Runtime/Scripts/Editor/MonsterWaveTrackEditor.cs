using System.Collections.Generic;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

/// <summary>
/// 트랙 이름 왼쪽 아이콘을 걷기 아래 방향 첫 장으로 바꾼다.
/// </summary>
[CustomTimelineEditor(typeof(MonsterWaveTrack))]
public class MonsterWaveTrackEditor : TrackEditor
{
    private static readonly Dictionary<EntityId, Texture2D> _icons = new Dictionary<EntityId, Texture2D>();

    /// <summary>
    /// 트랙에 몬스터가 있으면 그 그림의 첫 걷기 아래 프레임을 헤더 아이콘으로 쓴다.
    /// </summary>
    public override TrackDrawOptions GetTrackOptions(TrackAsset track, Object binding)
    {
        var options = base.GetTrackOptions(track, binding);
        if (track is MonsterWaveTrack wave)
        {
            var icon = GetIcon(wave.Monster);
            if (icon != null)
            {
                options.icon = icon;
            }
        }

        return options;
    }

    private static Texture2D GetIcon(MonsterVisualData monster)
    {
        var sprite = FirstWalkDown(monster);
        if (sprite == null)
        {
            return null;
        }

        var id = sprite.GetEntityId();
        if (_icons.TryGetValue(id, out var cached) && cached != null)
        {
            return cached;
        }

        var icon = CropSprite(sprite);
        if (icon != null)
        {
            _icons[id] = icon;
        }

        return icon;
    }

    private static Sprite FirstWalkDown(MonsterVisualData monster)
    {
        if (monster == null || monster.Walk == null)
        {
            return null;
        }

        var frames = monster.Walk.Down;
        if (frames == null)
        {
            return null;
        }

        for (var i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
            {
                return frames[i];
            }
        }

        return null;
    }

    /// <summary>
    /// 시트에서 한 칸만 자른다. 원본은 읽기가 꺼져 있어 픽셀 배열로는 가져올 수 없다.
    /// </summary>
    private static Texture2D CropSprite(Sprite sprite)
    {
        var texture = sprite.texture;
        if (texture == null)
        {
            return null;
        }

        var area = sprite.textureRect;
        var width = Mathf.RoundToInt(area.width);
        var height = Mathf.RoundToInt(area.height);
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        var sheet = RenderTexture.GetTemporary(
            texture.width,
            texture.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default);
        var previous = RenderTexture.active;
        Graphics.Blit(texture, sheet);
        RenderTexture.active = sheet;

        var icon = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        icon.ReadPixels(new Rect(area.x, area.y, width, height), 0, 0);
        icon.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(sheet);
        return icon;
    }
}
