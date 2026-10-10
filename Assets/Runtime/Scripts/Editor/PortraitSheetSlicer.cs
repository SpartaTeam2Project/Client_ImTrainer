using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// TexturePacker JSON(pokerogue 초상화 시트)의 칸 위치대로 시트를 자른다.
/// 프레임을 파일명 순으로 세우고 고유 칸마다 스프라이트 하나를 만든다. 같은 칸을 여러 번 쓰는 프레임은 같은 스프라이트를 가리킨다.
/// 피벗은 잘리기 전 원본 크기의 가운데에 둔다. 그래서 칸마다 잘린 크기가 달라도 그림이 흔들리지 않는다.
/// 같은 이름의 칸은 기존 ID를 이어 받아서 다시 잘라도 SO 참조가 유지된다.
/// </summary>
public static class PortraitSheetSlicer
{
    /// <summary>
    /// 시트에서 잘라 낼 칸 목록과 재생 순서. Sequence는 파일명 순 프레임마다 Cells의 번호다.
    /// </summary>
    public sealed class Layout
    {
        public RectInt[] Cells;
        public Vector2[] SourceCenters;
        public int[] Sequence;
    }

    [Serializable]
    private class SheetJson
    {
        public TextureJson[] textures;
        public FrameJson[] frames;
    }

    [Serializable]
    private class TextureJson
    {
        public FrameJson[] frames;
    }

    [Serializable]
    private class FrameJson
    {
        public string filename;
        public BoxJson frame;
        public BoxJson spriteSourceSize;
        public SizeJson sourceSize;
    }

    [Serializable]
    private class BoxJson
    {
        public int x;
        public int y;
        public int w;
        public int h;
    }

    [Serializable]
    private class SizeJson
    {
        public int w;
        public int h;
    }

    /// <summary>
    /// 시트 왼쪽 위부터 처음 나온 순서로 센 index번째 칸의 스프라이트 이름.
    /// </summary>
    public static string SpriteName(string textureName, int index)
    {
        return $"{textureName}_{index}";
    }

    /// <summary>
    /// JSON을 읽어 칸 목록을 만든다. 두 형식(textures[0].frames, 최상위 frames)을 모두 읽는다. 실패하면 error에 이유를 담고 false.
    /// </summary>
    public static bool TryReadLayout(string jsonPath, out Layout layout, out string error)
    {
        layout = null;
        error = null;
        SheetJson sheet;
        try
        {
            sheet = JsonUtility.FromJson<SheetJson>(File.ReadAllText(jsonPath));
        }
        catch (Exception exception)
        {
            error = $"JSON을 읽지 못함: {exception.Message}";
            return false;
        }

        var frames = sheet?.textures != null && sheet.textures.Length > 0 ? sheet.textures[0].frames : sheet?.frames;
        if (frames == null || frames.Length == 0)
        {
            error = "프레임이 없음";
            return false;
        }

        var cellIndex = new Dictionary<RectInt, int>();
        var cells = new List<RectInt>();
        var centers = new List<Vector2>();
        var sequence = new List<int>();
        foreach (var frame in frames.OrderBy(f => f.filename, StringComparer.Ordinal))
        {
            var cell = new RectInt(frame.frame.x, frame.frame.y, frame.frame.w, frame.frame.h);
            if (!cellIndex.TryGetValue(cell, out var index))
            {
                index = cells.Count;
                cellIndex.Add(cell, index);
                cells.Add(cell);
                centers.Add(SourceCenter(frame));
            }

            sequence.Add(index);
        }

        layout = new Layout { Cells = cells.ToArray(), SourceCenters = centers.ToArray(), Sequence = sequence.ToArray() };
        return true;
    }

    /// <summary>
    /// 칸 목록대로 자른다. 이미 같으면 false. MonsterSpriteSheetSlicer.ApplyImportSettings로 Multiple 모드를 맞춘 뒤 불러야 한다.
    /// 실패하면 error에 이유를 담고 false.
    /// </summary>
    public static bool ApplyCells(TextureImporter importer, SpriteDataProviderFactories factories, Layout layout, out string error)
    {
        error = null;
        if (importer.spriteImportMode != SpriteImportMode.Multiple)
        {
            error = "Multiple 모드가 아님";
            return false;
        }

        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        provider.GetDataProvider<ITextureDataProvider>().GetTextureActualWidthAndHeight(out var width, out var height);

        var existing = new Dictionary<string, SpriteRect>();
        var usedIds = new HashSet<int>();
        foreach (var rect in provider.GetSpriteRects())
        {
            existing[rect.name] = rect;
            usedIds.Add(rect.spriteID.GetHashCode());
        }

        var textureName = Path.GetFileNameWithoutExtension(importer.assetPath);
        var rects = new SpriteRect[layout.Cells.Length];
        var changed = existing.Count != rects.Length;
        for (var i = 0; i < rects.Length; i++)
        {
            var cell = layout.Cells[i];
            if (cell.xMax > width || cell.yMax > height)
            {
                error = $"칸 {cell}이 시트 {width}x{height} 밖에 있음";
                return false;
            }

            // JSON은 위가 0이고 텍스처 좌표는 아래가 0이다.
            var area = new Rect(cell.x, height - cell.y - cell.height, cell.width, cell.height);
            var pivot = new Vector2(layout.SourceCenters[i].x / cell.width, layout.SourceCenters[i].y / cell.height);
            var name = SpriteName(textureName, i);
            if (!existing.TryGetValue(name, out var rect))
            {
                rect = new SpriteRect { name = name, spriteID = NewSpriteId(usedIds) };
                changed = true;
            }
            else if (rect.rect != area || rect.alignment != SpriteAlignment.Custom || rect.pivot != pivot || rect.border != Vector4.zero)
            {
                changed = true;
            }

            rect.rect = area;
            rect.alignment = SpriteAlignment.Custom;
            rect.pivot = pivot;
            rect.border = Vector4.zero;
            rects[i] = rect;
        }

        if (!changed)
        {
            return false;
        }

        provider.SetSpriteRects(rects);
        // 이름과 ID 표를 같이 써야 나중에 다시 잘라도 같은 이름이 같은 fileID를 받는다.
        var pairs = rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)).ToArray();
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
        provider.Apply();
        EditorUtility.SetDirty(importer);
        return true;
    }

    /// <summary>
    /// 재임포트한 시트에서 재생 순서대로 스프라이트를 꺼낸다. 칸 하나라도 없으면 null.
    /// </summary>
    public static Sprite[] LoadFrames(string assetPath, Layout layout)
    {
        var textureName = Path.GetFileNameWithoutExtension(assetPath);
        var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToDictionary(sprite => sprite.name);
        var frames = new Sprite[layout.Sequence.Length];
        for (var i = 0; i < frames.Length; i++)
        {
            if (!sprites.TryGetValue(SpriteName(textureName, layout.Sequence[i]), out frames[i]))
            {
                return null;
            }
        }

        return frames;
    }

    // 잘린 칸 왼쪽 아래에서 본 원본 크기의 가운데. 잘린 칸의 원본 위치는 spriteSourceSize(위가 0)다.
    private static Vector2 SourceCenter(FrameJson frame)
    {
        var size = frame.sourceSize;
        var offset = frame.spriteSourceSize;
        if (size == null || offset == null || size.w <= 0 || size.h <= 0 || offset.w <= 0 || offset.h <= 0)
        {
            return new Vector2(frame.frame.w * 0.5f, frame.frame.h * 0.5f);
        }

        return new Vector2(size.w * 0.5f - offset.x, offset.y + offset.h - size.h * 0.5f);
    }

    // 새 칸의 fileID는 spriteID 해시(32비트)로 정해진다. 한 시트 안에서 겹치지 않게 고른다.
    private static GUID NewSpriteId(HashSet<int> usedIds)
    {
        while (true)
        {
            var id = GUID.Generate();
            if (usedIds.Add(id.GetHashCode()))
            {
                return id;
            }
        }
    }
}
