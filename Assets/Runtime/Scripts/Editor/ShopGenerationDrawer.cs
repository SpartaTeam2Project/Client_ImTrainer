using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 상점 세대 마스크를 그린다. 기본 Flags 드롭다운은 한 번 고르면 닫혀서, 레이어 마스크처럼 열린 채로 여러 세대를 고르는 창을 띄운다.
/// </summary>
[CustomPropertyDrawer(typeof(ShopGeneration))]
public class ShopGenerationDrawer : PropertyDrawer
{
    private const string NOTHING = "Nothing";
    private const string EVERYTHING = "Everything";
    private const string MIXED = "—";
    private const string GENERATION_SUFFIX = "세대";
    private const int GENERATION_BITS = (1 << ShopGenerationMask.MAX_GENERATION) - 1;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var field = EditorGUI.PrefixLabel(position, label);
        var text = property.hasMultipleDifferentValues ? MIXED : Summary(property.intValue);
        if (EditorGUI.DropdownButton(field, new GUIContent(text), FocusType.Keyboard))
        {
            PopupWindow.Show(field, new ShopGenerationPopup(property.serializedObject, property.propertyPath, field.width));
        }

        EditorGUI.EndProperty();
    }

    /// <summary>
    /// 버튼에 보일 글자. 하나도 없으면 Nothing, 전부면 Everything, 아니면 "1, 4세대"처럼 쓴다.
    /// </summary>
    private static string Summary(int mask)
    {
        var bits = mask & GENERATION_BITS;
        if (bits == 0)
        {
            return NOTHING;
        }

        if (bits == GENERATION_BITS)
        {
            return EVERYTHING;
        }

        var builder = new StringBuilder();
        for (var generation = ShopGenerationMask.MIN_GENERATION; generation <= ShopGenerationMask.MAX_GENERATION; generation++)
        {
            if (!ShopGenerationMask.Contains((ShopGeneration)mask, generation))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(generation);
        }

        return builder.Append(GENERATION_SUFFIX).ToString();
    }

    /// <summary>
    /// 바깥을 누를 때까지 열려 있는 세대 체크 창.
    /// </summary>
    private sealed class ShopGenerationPopup : PopupWindowContent
    {
        private const float MIN_WIDTH = 140f;
        private const float PADDING = 4f;
        private const int FIXED_ROWS = 2;

        private readonly SerializedObject _serializedObject;
        private readonly string _propertyPath;
        private readonly float _width;

        public ShopGenerationPopup(SerializedObject serializedObject, string propertyPath, float width)
        {
            _serializedObject = serializedObject;
            _propertyPath = propertyPath;
            _width = width;
        }

        public override Vector2 GetWindowSize()
        {
            var rows = FIXED_ROWS + ShopGenerationMask.MAX_GENERATION;
            var height = rows * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) + PADDING * 2f;
            return new Vector2(Mathf.Max(MIN_WIDTH, _width), height);
        }

        public override void OnGUI(Rect rect)
        {
            if (_serializedObject == null || _serializedObject.targetObject == null)
            {
                editorWindow.Close();
                return;
            }

            _serializedObject.Update();
            var property = _serializedObject.FindProperty(_propertyPath);
            if (property == null)
            {
                editorWindow.Close();
                return;
            }

            var bits = property.intValue & GENERATION_BITS;
            var next = bits;
            GUILayout.Space(PADDING);
            if (EditorGUILayout.ToggleLeft(NOTHING, bits == 0) && bits != 0)
            {
                next = 0;
            }

            if (EditorGUILayout.ToggleLeft(EVERYTHING, bits == GENERATION_BITS) && bits != GENERATION_BITS)
            {
                next = GENERATION_BITS;
            }

            for (var generation = ShopGenerationMask.MIN_GENERATION; generation <= ShopGenerationMask.MAX_GENERATION; generation++)
            {
                var bit = 1 << (generation - ShopGenerationMask.MIN_GENERATION);
                var on = (bits & bit) != 0;
                if (EditorGUILayout.ToggleLeft(generation + GENERATION_SUFFIX, on) != on)
                {
                    next ^= bit;
                }
            }

            if (next == bits)
            {
                return;
            }

            // 전부 켜면 인스펙터 기본 Everything과 같은 값으로 둔다.
            property.intValue = next == GENERATION_BITS ? (int)ShopGenerationMask.ALL : next;
            _serializedObject.ApplyModifiedProperties();
            RepaintInspectors();
        }

        private static void RepaintInspectors()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window.GetType().Name == "InspectorWindow")
                {
                    window.Repaint();
                }
            }
        }
    }
}
