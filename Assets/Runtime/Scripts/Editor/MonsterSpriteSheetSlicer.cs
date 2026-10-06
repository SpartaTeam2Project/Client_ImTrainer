using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// SpriteCollab 시트의 임포트 설정을 맞추고 칸 크기 격자로 자른다.
/// 칸 중심이 PMD 개체 원점이라 모든 장의 피벗을 칸 중심에 둔다. 그래서 동작이 바뀌어도 그림이 흔들리지 않는다.
/// 같은 이름의 칸은 기존 ID를 이어 받아서 다시 잘라도 SO 참조가 유지된다.
/// </summary>
public static class MonsterSpriteSheetSlicer
{
    private const float PIXELS_PER_UNIT = 100f;
    private const int MIN_TEXTURE_SIZE = 2048;
    private const int MAX_TEXTURE_SIZE = 8192;
    private const uint SPRITE_EXTRUDE = 1;
    // 기존 몬스터 시트와 같은 압축이다. 픽셀 번짐이 거슬리면 Uncompressed로 바꾸고 시트를 다시 자른다.
    private const TextureImporterCompression COMPRESSION = TextureImporterCompression.Compressed;

    // 바꾼 뒤 "시트 다시 자르기"로 실행하면 칸 이름이 같아서 SO 참조가 그대로 남는다.
    private static readonly Vector2 PIVOT = new Vector2(0.5f, 0.5f);

    /// <summary>
    /// 시트 왼쪽 위부터 가로로 센 index번째 칸의 스프라이트 이름.
    /// </summary>
    public static string SpriteName(string textureName, int index)
    {
        return $"{textureName}_{index}";
    }

    /// <summary>
    /// 임포트 설정을 맞춘다. 바꾼 값이 있으면 true. 격자보다 먼저 불러야 한다.
    /// </summary>
    public static bool ApplyImportSettings(TextureImporter importer, int width, int height)
    {
        var maxTextureSize = FitTextureSize(width, height);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        if (HasImportSettings(importer, settings, maxTextureSize))
        {
            return false;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PIXELS_PER_UNIT;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = maxTextureSize;
        importer.textureCompression = COMPRESSION;
        importer.crunchedCompression = false;

        // 메시 모양, 여백, 물리 모양은 TextureImporter 속성이 없어서 설정 묶음으로 쓴다.
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.Tight;
        settings.spriteExtrude = SPRITE_EXTRUDE;
        settings.spriteGenerateFallbackPhysicsShape = false;
        importer.SetTextureSettings(settings);
        EditorUtility.SetDirty(importer);
        return true;
    }

    /// <summary>
    /// 칸 크기 격자로 자른다. 이미 같은 격자면 false. force면 같아도 다시 쓴다.
    /// 임포트 설정을 맞춘 뒤 불러야 한다. 실패하면 error에 이유를 담고 false.
    /// </summary>
    public static bool ApplyGrid(TextureImporter importer, SpriteDataProviderFactories factories, int frameWidth, int frameHeight, bool force, out string error)
    {
        error = null;
        // provider는 만들 때의 임포트 모드를 기억하고, Multiple이 아니면 칸 목록 쓰기를 조용히 무시한다.
        if (importer.spriteImportMode != SpriteImportMode.Multiple)
        {
            error = "Multiple 모드가 아님";
            return false;
        }

        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        provider.GetDataProvider<ITextureDataProvider>().GetTextureActualWidthAndHeight(out var width, out var height);
        if (width <= 0 || height <= 0 || width % frameWidth != 0 || height % frameHeight != 0)
        {
            error = $"시트 {width}x{height}가 칸 {frameWidth}x{frameHeight}로 나눠지지 않음";
            return false;
        }

        var existing = new Dictionary<string, SpriteRect>();
        var usedIds = new HashSet<int>();
        foreach (var rect in provider.GetSpriteRects())
        {
            existing[rect.name] = rect;
            usedIds.Add(rect.spriteID.GetHashCode());
        }

        var columns = width / frameWidth;
        var rows = height / frameHeight;
        var textureName = Path.GetFileNameWithoutExtension(importer.assetPath);
        var rects = new SpriteRect[columns * rows];
        var changed = force || existing.Count != rects.Length;
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var index = row * columns + column;
                var name = SpriteName(textureName, index);
                // 텍스처 좌표는 아래에서 시작한다. 시트 맨 윗줄(0행)이 가장 큰 y다.
                var cell = new Rect(column * frameWidth, height - (row + 1) * frameHeight, frameWidth, frameHeight);
                if (!existing.TryGetValue(name, out var rect))
                {
                    rect = new SpriteRect { name = name, spriteID = NewSpriteId(usedIds) };
                    changed = true;
                }
                else if (rect.rect != cell || rect.alignment != SpriteAlignment.Custom || rect.pivot != PIVOT || rect.border != Vector4.zero)
                {
                    changed = true;
                }

                rect.rect = cell;
                rect.alignment = SpriteAlignment.Custom;
                rect.pivot = PIVOT;
                rect.border = Vector4.zero;
                rects[index] = rect;
            }
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

    private static bool HasImportSettings(TextureImporter importer, TextureImporterSettings settings, int maxTextureSize)
    {
        return importer.textureType == TextureImporterType.Sprite
            && importer.spriteImportMode == SpriteImportMode.Multiple
            && Mathf.Approximately(importer.spritePixelsPerUnit, PIXELS_PER_UNIT)
            && importer.filterMode == FilterMode.Point
            && importer.wrapModeU == TextureWrapMode.Clamp
            && importer.wrapModeV == TextureWrapMode.Clamp
            && !importer.mipmapEnabled
            && importer.alphaIsTransparency
            && !importer.isReadable
            && importer.npotScale == TextureImporterNPOTScale.None
            && importer.maxTextureSize == maxTextureSize
            && importer.textureCompression == COMPRESSION
            && !importer.crunchedCompression
            && settings.spriteMeshType == SpriteMeshType.Tight
            && settings.spriteExtrude == SPRITE_EXTRUDE
            && !settings.spriteGenerateFallbackPhysicsShape;
    }

    // 원본 크기보다 작으면 시트가 줄어들어 픽셀이 뭉개진다. 원본 최대 변은 1976이라 대부분 2048이다.
    private static int FitTextureSize(int width, int height)
    {
        var longest = Mathf.Max(width, height);
        var size = MIN_TEXTURE_SIZE;
        while (size < longest && size < MAX_TEXTURE_SIZE)
        {
            size *= 2;
        }

        return size;
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
