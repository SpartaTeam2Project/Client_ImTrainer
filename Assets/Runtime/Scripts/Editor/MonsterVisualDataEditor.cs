using UnityEditor;
using UnityEngine;

/// <summary>
/// 포켓몬 그림 에셋을 기획자가 보기 쉽게 카테고리별로 접어서 보여 준다.
/// 접힘 상태는 에디터를 끌 때까지 유지된다.
/// </summary>
[CustomEditor(typeof(MonsterVisualData)), CanEditMultipleObjects]
public class MonsterVisualDataEditor : Editor
{
    private const string FOLD_KEY_PREFIX = "MonsterVisualDataEditor.";
    private const float LABEL_WIDTH = 170f;
    private const float UNLOCK_CONDITION_MIN_HEIGHT = 40f;

    private static readonly string[] EIGHT_DIRECTION_FIELDS =
    {
        "_down", "_downRight", "_right", "_upRight", "_up", "_upLeft", "_left", "_downLeft",
    };

    private static readonly string[] EIGHT_DIRECTION_LABELS =
    {
        "아래 (Down)", "오른쪽 아래 (Down Right)", "오른쪽 (Right)", "오른쪽 위 (Up Right)",
        "위 (Up)", "왼쪽 위 (Up Left)", "왼쪽 (Left)", "왼쪽 아래 (Down Left)",
    };

    private static readonly string[] EIGHT_DIRECTION_ACTION_FIELDS =
    {
        "_walk", "_sleep", "_hurt", "_attack", "_charge", "_shoot", "_strike", "_swing", "_rotate", "_hop",
    };

    private static readonly string[] EIGHT_DIRECTION_ACTION_LABELS =
    {
        "걷기 (Walk)", "잠 (Sleep)", "피격 (Hurt)", "공격 (Attack)", "차지 (Charge)",
        "사격 (Shoot)", "타격 (Strike)", "휘두르기 (Swing)", "회전 (Rotate)", "점프 (Hop)",
    };

    private static GUIStyle _textAreaStyle;

    /// <summary>
    /// 크기, 필드 애니메이션, 무기 능력, 타입, 진화, 도감 정보, UI 애니메이션 순으로 그린다.
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var previousLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = LABEL_WIDTH;

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        }

        EditorGUILayout.Space();
        DrawSize();
        DrawFieldAnimation();
        DrawWeaponAbility();
        DrawType();
        DrawEvolution();
        DrawStorage();
        DrawUiAnimation();

        EditorGUIUtility.labelWidth = previousLabelWidth;
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSize()
    {
        var expanded = BeginCategory("Size", "크기 (Size)");
        if (expanded)
        {
            DrawField("_scale", "크기 (Scale)", "씬 뷰에서 적용될 스프라이트 크기");
        }

        EndCategory(expanded);
    }

    private void DrawFieldAnimation()
    {
        var expanded = BeginCategory("Animation", "필드 애니메이션 (Animation)");
        if (expanded)
        {
            for (var i = 0; i < EIGHT_DIRECTION_ACTION_FIELDS.Length; i++)
            {
                DrawEightDirectionAction(EIGHT_DIRECTION_ACTION_FIELDS[i], EIGHT_DIRECTION_ACTION_LABELS[i]);
            }

            DrawTwoDirectionAction("Faint", "기절 (Faint)", "_faintLeft", "_faintRight");
            DrawTwoDirectionAction("Pose", "포즈 (Pose)", "_poseLeft", "_poseRight");
        }

        EndCategory(expanded);
    }

    private void DrawWeaponAbility()
    {
        var expanded = BeginCategory("WeaponAbility", "무기 능력 (Weapon Ability)");
        if (expanded)
        {
            DrawField("_ability", "무기 능력 (Ability)");
        }

        EndCategory(expanded);
    }

    private void DrawType()
    {
        var expanded = BeginCategory("Type", "타입 (Type)");
        if (expanded)
        {
            DrawField("_primaryType", "메인 타입 (Primary)");
            var hasSecondary = serializedObject.FindProperty("_hasSecondaryType");
            EditorGUILayout.PropertyField(hasSecondary, new GUIContent("서브 타입 사용 (Has Secondary)"));
            if (hasSecondary.hasMultipleDifferentValues || hasSecondary.boolValue)
            {
                EditorGUI.indentLevel++;
                DrawField("_secondaryType", "서브 타입 (Secondary)", "메인과 같으면 서브 없음으로 처리한다");
                EditorGUI.indentLevel--;
            }
        }

        EndCategory(expanded);
    }

    private void DrawEvolution()
    {
        var expanded = BeginCategory("Evolution", "진화 (Evolution)");
        if (expanded)
        {
            DrawField("_evolution", "다음 진화 (Evolution)", "3성 세 마리를 합성하면 나오는 다음 종. 최종 진화는 비운다");
        }

        EndCategory(expanded);
    }

    private void DrawStorage()
    {
        var expanded = BeginCategory("Storage", "도감 정보 (Storage)");
        if (expanded)
        {
            DrawField("_generation", "세대 (Generation)");
            DrawField("_monsterName", "이름 (Name)");
            DrawField("_dexNumber", "도감 번호 (Dex No.)");
            DrawTextArea("_unlockCondition", "해금 조건 (Unlock Condition)");
        }

        EndCategory(expanded);
    }

    private void DrawUiAnimation()
    {
        var expanded = BeginCategory("UiAnimation", "UI 애니메이션 (UI)");
        if (expanded)
        {
            DrawField("_icon", "아이콘 (Icon)", "스토리지 칸, 엔트리, 상점, 인벤토리에 쓰는 아이콘 애니메이션 프레임");
            DrawField("_iconSize", "아이콘 크기 (Icon Size)", "스토리지 칸 크기 기준. 0이면 프리팹 크기를 그대로 쓴다");
            DrawField("_infoAnimation", "정보창 애니메이션 (Info Animation)", "스토리지 정보창에 쓰는 애니메이션 프레임");
        }

        EndCategory(expanded);
    }

    private void DrawEightDirectionAction(string field, string label)
    {
        var action = serializedObject.FindProperty(field);
        var filled = 0;
        for (var i = 0; i < EIGHT_DIRECTION_FIELDS.Length; i++)
        {
            if (HasSprites(action.FindPropertyRelative(EIGHT_DIRECTION_FIELDS[i])))
            {
                filled++;
            }
        }

        var title = $"{label}   8방향 중 {filled}칸";
        if (!BeginAction(field, title))
        {
            return;
        }

        for (var i = 0; i < EIGHT_DIRECTION_FIELDS.Length; i++)
        {
            var frames = action.FindPropertyRelative(EIGHT_DIRECTION_FIELDS[i]);
            EditorGUILayout.PropertyField(frames, new GUIContent(EIGHT_DIRECTION_LABELS[i]), true);
        }

        EditorGUI.indentLevel--;
    }

    private void DrawTwoDirectionAction(string key, string label, string leftField, string rightField)
    {
        var left = serializedObject.FindProperty(leftField);
        var right = serializedObject.FindProperty(rightField);
        var filled = (HasSprites(left) ? 1 : 0) + (HasSprites(right) ? 1 : 0);
        var title = $"{label}   좌우 중 {filled}칸";
        if (!BeginAction(key, title))
        {
            return;
        }

        EditorGUILayout.PropertyField(left, new GUIContent("왼쪽 (Left)"), true);
        EditorGUILayout.PropertyField(right, new GUIContent("오른쪽 (Right)"), true);
        EditorGUI.indentLevel--;
    }

    // 펼쳐졌으면 들여쓰기를 한 단계 올린다. 호출 쪽이 내용을 그린 뒤 되돌린다.
    private static bool BeginAction(string key, string title)
    {
        var foldKey = FOLD_KEY_PREFIX + "Action." + key;
        var expanded = EditorGUILayout.Foldout(SessionState.GetBool(foldKey, false), title, true);
        SessionState.SetBool(foldKey, expanded);
        if (expanded)
        {
            EditorGUI.indentLevel++;
        }

        return expanded;
    }

    // BeginFoldoutHeaderGroup은 안에 배열이 있으면 중첩 에러가 나서 헤더 스타일 Foldout으로 그린다.
    private static bool BeginCategory(string key, string title)
    {
        var foldKey = FOLD_KEY_PREFIX + key;
        var expanded = EditorGUILayout.Foldout(SessionState.GetBool(foldKey, true), title, true, EditorStyles.foldoutHeader);
        SessionState.SetBool(foldKey, expanded);
        if (expanded)
        {
            EditorGUI.indentLevel++;
        }

        return expanded;
    }

    private static void EndCategory(bool expanded)
    {
        if (expanded)
        {
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(2f);
    }

    private void DrawField(string field, string label, string tooltip = null)
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty(field), new GUIContent(label, tooltip), true);
    }

    private void DrawTextArea(string field, string label)
    {
        var property = serializedObject.FindProperty(field);
        _textAreaStyle ??= new GUIStyle(EditorStyles.textArea) { wordWrap = true };

        EditorGUILayout.LabelField(label);
        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        var text = EditorGUILayout.TextArea(property.stringValue, _textAreaStyle, GUILayout.MinHeight(UNLOCK_CONDITION_MIN_HEIGHT));
        if (EditorGUI.EndChangeCheck())
        {
            property.stringValue = text;
        }

        EditorGUI.showMixedValue = false;
    }

    private static bool HasSprites(SerializedProperty frames)
    {
        if (frames == null || !frames.isArray)
        {
            return false;
        }

        for (var i = 0; i < frames.arraySize; i++)
        {
            if (frames.GetArrayElementAtIndex(i).objectReferenceValue != null)
            {
                return true;
            }
        }

        return false;
    }
}
