using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 결과 화면 생성기가 같이 쓰는 위젯 생성과 배치. 폰트와 별·패널 스프라이트를 들고 있다.
/// </summary>
public sealed class StageResultUiFactory
{
    private const string UI_SPRITE = "UI/Skin/UISprite.psd";
    private const string INVENTORY_ITEM_PREFAB = "Assets/Runtime/Prefabs/UI/InventoryItem.prefab";

    public static readonly Color PANEL_DARK = new Color(0.06f, 0.07f, 0.1f, 0.92f);
    public static readonly Color HEADER_DARK = new Color(0.12f, 0.14f, 0.2f, 1f);
    public static readonly Color SLOT_DARK = new Color(0.1f, 0.16f, 0.26f, 1f);
    public static readonly Color VALUE_YELLOW = new Color(1f, 0.85f, 0.25f, 1f);
    public static readonly Color VALUE_GREEN = new Color(0.55f, 1f, 0.35f, 1f);
    public static readonly Color BAR_BLUE = new Color(0.2f, 0.55f, 1f, 1f);

    public readonly TMP_FontAsset Font;
    public readonly Material FontMaterial;
    public readonly Sprite PanelSprite;
    public readonly Sprite StarSprite;

    public StageResultUiFactory(TMP_Text fontSource)
    {
        Font = fontSource != null ? fontSource.font : null;
        FontMaterial = fontSource != null ? fontSource.fontSharedMaterial : null;
        PanelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(UI_SPRITE);
        StarSprite = LoadStarSprite();
    }

    public bool IsValid => Font != null;

    public static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent != null ? parent.gameObject.layer : LayerMask.NameToLayer("UI");
        var rect = (RectTransform)go.transform;
        if (parent != null)
        {
            rect.SetParent(parent, false);
        }

        return rect;
    }

    /// <summary>
    /// 부모 비율 영역에 붙인다. min/max는 0~1 앵커다.
    /// </summary>
    public static RectTransform CreateArea(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = CreateRect(name, parent);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    /// <summary>
    /// 부모 기준 앵커 한 점에 고정 크기로 놓는다.
    /// </summary>
    public static RectTransform CreateFixed(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var rect = CreateRect(name, parent);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    /// <summary>
    /// 위쪽에 고정 높이로 가로를 채운다.
    /// </summary>
    public static RectTransform CreateTopBar(string name, Transform parent, float top, float height)
    {
        var rect = CreateRect(name, parent);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -top);
        rect.sizeDelta = new Vector2(0f, height);
        return rect;
    }

    public static void Inset(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    public Image AddImage(GameObject go, Color color, bool panelSprite)
    {
        go.AddComponent<CanvasRenderer>();
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        if (panelSprite && PanelSprite != null)
        {
            image.sprite = PanelSprite;
            image.type = Image.Type.Sliced;
        }

        return image;
    }

    public Image AddIcon(GameObject go)
    {
        var image = AddImage(go, Color.white, false);
        image.preserveAspect = true;
        return image;
    }

    public TextMeshProUGUI CreateLabel(string name, Transform parent, float size, TextAlignmentOptions alignment, string text, Color color)
    {
        var rect = CreateRect(name, parent);
        rect.gameObject.AddComponent<CanvasRenderer>();
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = Font;
        label.fontSharedMaterial = FontMaterial;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.text = text;
        return label;
    }

    /// <summary>
    /// 별 세 개를 가로로 깐다.
    /// </summary>
    public Image[] CreateStars(Transform parent, Vector2 anchor, Vector2 position, float size)
    {
        var stars = new Image[Item.STAR_MAX];
        var start = -(Item.STAR_MAX - 1) * size * 0.5f;
        for (var i = 0; i < stars.Length; i++)
        {
            var rect = CreateFixed("Star" + (i + 1), parent, anchor, position + new Vector2(start + size * i, 0f), new Vector2(size, size));
            stars[i] = AddIcon(rect.gameObject);
            stars[i].sprite = StarSprite;
        }

        return stars;
    }

    public static void SetObjects(SerializedObject so, string property, Object[] values)
    {
        var array = so.FindProperty(property);
        array.arraySize = values.Length;
        for (var i = 0; i < values.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static Sprite LoadStarSprite()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(INVENTORY_ITEM_PREFAB);
        var item = prefab != null ? prefab.GetComponentInChildren<InventoryItem>(true) : null;
        if (item == null)
        {
            return null;
        }

        var stars = new SerializedObject(item).FindProperty("_starImages");
        for (var i = 0; stars != null && i < stars.arraySize; i++)
        {
            if (stars.GetArrayElementAtIndex(i).objectReferenceValue is Image image && image.sprite != null)
            {
                return image.sprite;
            }
        }

        return null;
    }
}
