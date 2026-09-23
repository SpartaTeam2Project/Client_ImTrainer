using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// 선택한 폰트(ttf/otf)로 한글용 Dynamic TMP 폰트 에셋을 생성한다.
/// 결과물은 소스 폰트와 같은 폴더에 "{폰트명} TMP.asset" 으로 저장된다.
/// </summary>
public class FontAssetCreator : EditorWindow
{
    private static readonly int[] ATLAS_SIZES = { 512, 1024, 2048, 4096 };
    private static readonly string[] ATLAS_SIZE_LABELS = { "512", "1024", "2048", "4096" };

    // 일반 폰트(SDF) 기본값
    private const int SDF_SAMPLING_POINT_SIZE = 90;
    private const int SDF_ATLAS_PADDING = 9;

    // 픽셀 폰트(Raster) 기본값
    private const int PIXEL_SAMPLING_POINT_SIZE = 16;
    private const int PIXEL_ATLAS_PADDING = 2;

    private Font _sourceFont;
    private bool _isPixelFont;
    private int _samplingPointSize = SDF_SAMPLING_POINT_SIZE;
    private int _atlasPadding = SDF_ATLAS_PADDING;
    private int _atlasSize = 1024;

    [MenuItem("Tools/Fonts/Create TMP Font Asset")]
    private static void Open()
    {
        var window = GetWindow<FontAssetCreator>("TMP Font Asset Creator");
        window._sourceFont = Selection.activeObject as Font;
        window.minSize = new Vector2(320, 200);
    }

    private void OnGUI()
    {
        _sourceFont = (Font)EditorGUILayout.ObjectField("Source Font", _sourceFont, typeof(Font), false);

        EditorGUI.BeginChangeCheck();
        _isPixelFont = EditorGUILayout.Toggle("Pixel Font", _isPixelFont);
        if (EditorGUI.EndChangeCheck())
        {
            _samplingPointSize = _isPixelFont ? PIXEL_SAMPLING_POINT_SIZE : SDF_SAMPLING_POINT_SIZE;
            _atlasPadding = _isPixelFont ? PIXEL_ATLAS_PADDING : SDF_ATLAS_PADDING;
        }

        _samplingPointSize = Mathf.Max(1, EditorGUILayout.IntField("Sampling Point Size", _samplingPointSize));
        _atlasPadding = Mathf.Max(0, EditorGUILayout.IntField("Atlas Padding", _atlasPadding));
        _atlasSize = EditorGUILayout.IntPopup("Atlas Size", _atlasSize, ATLAS_SIZE_LABELS, ATLAS_SIZES);

        EditorGUILayout.HelpBox(
            _isPixelFont
                ? "Raster + Point 필터. 폰트 디자인 기준 크기(예: 16)의 배수로 쓸 때 가장 선명하다."
                : "SDF. 크기를 자유롭게 바꿔도 선명하고 외곽선/그림자 효과를 쓸 수 있다.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(_sourceFont == null))
        {
            if (GUILayout.Button("Create"))
            {
                Create();
            }
        }
    }

    private void Create()
    {
        var sourcePath = AssetDatabase.GetAssetPath(_sourceFont);
        var assetName = $"{Path.GetFileNameWithoutExtension(sourcePath)} TMP";
        var outputPath = $"{Path.GetDirectoryName(sourcePath).Replace('\\', '/')}/{assetName}.asset";

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath) != null)
        {
            Debug.LogWarning($"이미 존재하는 폰트 에셋: {outputPath}");
            return;
        }

        // 한글 글리프가 많으므로 Dynamic + 멀티 아틀라스로 필요한 글자만 채운다
        var fontAsset = TMP_FontAsset.CreateFontAsset(
            _sourceFont,
            _samplingPointSize,
            _atlasPadding,
            _isPixelFont ? GlyphRenderMode.RASTER_HINTED : GlyphRenderMode.SDFAA,
            _atlasSize,
            _atlasSize,
            AtlasPopulationMode.Dynamic,
            true);

        if (fontAsset == null)
        {
            Debug.LogError($"TMP 폰트 에셋 생성 실패: {sourcePath}");
            return;
        }

        fontAsset.name = assetName;
        AssetDatabase.CreateAsset(fontAsset, outputPath);

        var atlas = fontAsset.atlasTexture;
        atlas.name = $"{assetName} Atlas";
        atlas.filterMode = _isPixelFont ? FilterMode.Point : FilterMode.Bilinear;
        AssetDatabase.AddObjectToAsset(atlas, fontAsset);

        var material = fontAsset.material;
        material.name = $"{assetName} Material";
        AssetDatabase.AddObjectToAsset(material, fontAsset);

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(outputPath);

        EditorGUIUtility.PingObject(fontAsset);
        Debug.Log($"TMP 폰트 에셋 생성 완료: {outputPath}");
    }
}
