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
    private const float SET_BUTTON_WIDTH = 70f;

    // SpriteCollab 시트 행 순서와 같다. MonsterSpriteImporter가 행을 방향 칸에 넣을 때 쓴다.
    internal static readonly string[] EIGHT_DIRECTION_FIELDS =
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
        "_walk", "_idle", "_sleep", "_hurt", "_attack", "_charge", "_shoot", "_strike", "_swing", "_rotate", "_hop",
    };

    private static readonly string[] EIGHT_DIRECTION_ACTION_LABELS =
    {
        "걷기 (Walk)", "아이들 (Idle)", "잠 (Sleep)", "피격 (Hurt)", "공격 (Attack)", "차지 (Charge)",
        "사격 (Shoot)", "타격 (Strike)", "휘두르기 (Swing)", "회전 (Rotate)", "점프 (Hop)",
    };

    private static GUIStyle _textAreaStyle;

    private Editor _animationEditor;

    /// <summary>
    /// 크기, 필드 애니메이션, 무기 능력, 타입, 진화, 도감 정보, 상점, UI 애니메이션 순으로 그린다.
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
        DrawShop();
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

    private void OnDisable()
    {
        if (_animationEditor != null)
        {
            DestroyImmediate(_animationEditor);
        }
    }

    // 동작 그림은 MonsterAnimationSet에 있다. 연결된 세트의 인스펙터를 이 안에 그대로 그려서 한 화면에서 고친다.
    private void DrawFieldAnimation()
    {
        var expanded = BeginCategory("Animation", "필드 애니메이션 (Animation)");
        if (expanded)
        {
            DrawField("_animations", "동작 그림 세트 (Animations)", "Addressables로 필요할 때만 불러온다. 형태는 기본형 세트를 같이 쓸 수 있다");
            var set = serializedObject.isEditingMultipleObjects ? null : MonsterAnimationAssets.Find((MonsterVisualData)target);
            if (set == null)
            {
                EditorGUILayout.HelpBox("연결된 세트가 없다. Tools/Monster/SpriteCollab 스프라이트 가져오기로 만든다.", MessageType.Info);
            }
            else
            {
                // 아래 칸은 이 SO가 아니라 세트 에셋의 내용이다. 여기서 고쳐도 세트 파일이 바뀐다.
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox($"아래 동작 칸은 별도 에셋 {set.name}의 내용이다. 이 SO에는 참조만 있다.", MessageType.None);
                    if (GUILayout.Button("세트 열기", GUILayout.Width(SET_BUTTON_WIDTH)))
                    {
                        EditorGUIUtility.PingObject(set);
                        Selection.activeObject = set;
                    }
                }

                CreateCachedEditor(set, typeof(MonsterAnimationSetEditor), ref _animationEditor);
                _animationEditor.OnInspectorGUI();
            }
        }

        EndCategory(expanded);
    }

    /// <summary>
    /// 8방향 동작 칸들과 기절, 포즈 칸을 그린다. MonsterAnimationSetEditor가 쓴다.
    /// </summary>
    internal static void DrawActions(SerializedObject serialized)
    {
        for (var i = 0; i < EIGHT_DIRECTION_ACTION_FIELDS.Length; i++)
        {
            DrawEightDirectionAction(serialized, EIGHT_DIRECTION_ACTION_FIELDS[i], EIGHT_DIRECTION_ACTION_LABELS[i]);
        }

        DrawTwoDirectionAction(serialized, "Faint", "기절 (Faint)", "_faintLeft", "_faintRight");
        DrawTwoDirectionAction(serialized, "Pose", "포즈 (Pose)", "_poseLeft", "_poseRight");
        DrawSkills(serialized);
    }

    // 종마다 다른 특수 기술(예: 피카츄 Shock). 이름은 SpriteCollab 애니 이름 그대로다.
    private static void DrawSkills(SerializedObject serialized)
    {
        var skills = serialized.FindProperty("_skills");
        if (skills == null)
        {
            return;
        }

        if (!BeginAction("Skills", $"특수 기술 (Skills)   {skills.arraySize}개"))
        {
            return;
        }

        for (var i = 0; i < skills.arraySize; i++)
        {
            var skill = skills.GetArrayElementAtIndex(i);
            var name = skill.FindPropertyRelative("_name").stringValue;
            DrawDirections(skill.FindPropertyRelative("_frames"), "Skill." + name, name);
        }

        EditorGUI.indentLevel--;
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
            DrawField("_megaEvolution", "메가진화 (Mega)", "이 종의 메가진화. 없으면 비운다. 지금은 정보창 표시에만 쓴다");
            DrawField("_vmaxEvolution", "거다이맥스 (V-Max)", "이 종의 거다이맥스. 없으면 비운다. 지금은 정보창 표시에만 쓴다");
        }

        EndCategory(expanded);
    }

    private void DrawStorage()
    {
        var expanded = BeginCategory("Storage", "도감 정보 (Storage)");
        if (expanded)
        {
            DrawField("_startable", "시작 가능 (Startable)", "켜면 스토리지에 나오고 시작 포켓몬으로 고를 수 있다");
            DrawField("_generation", "세대 (Generation)");
            DrawField("_monsterName", "이름 (Name)");
            DrawField("_dexNumber", "도감 번호 (Dex No.)");
            DrawTextArea("_unlockCondition", "해금 조건 (Unlock Condition)");
        }

        EndCategory(expanded);
    }

    private void DrawShop()
    {
        var expanded = BeginCategory("Shop", "상점 (Shop)");
        if (expanded)
        {
            DrawField("_shopPrice", "1성 구매가 (Price)", "상점 1성 가격. 2성은 3배, 3성은 9배. 판매가는 구매가에 판매 배율을 곱한다");
            DrawField("_shopCurrencyId", "화폐 (Currency)", "사고팔 때 쓰는 화폐 id. monster_ball, master_ball 등. 비우면 몬스터볼");
            DrawField("_baseStatTotal", "종족값 합 (BST)", "상점 등급을 나누는 종족값 합. 경계는 ShopOddsSettings에 있다");
            DrawField("_legendary", "전설·환상 (Legendary)", "켜면 종족값과 상관없이 상점 전설 등급으로 뽑힌다");
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

    private static void DrawEightDirectionAction(SerializedObject serialized, string field, string label)
    {
        DrawDirections(serialized.FindProperty(field), field, label);
    }

    private static void DrawDirections(SerializedProperty action, string field, string label)
    {
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

    private static void DrawTwoDirectionAction(SerializedObject serialized, string key, string label, string leftField, string rightField)
    {
        var left = serialized.FindProperty(leftField);
        var right = serialized.FindProperty(rightField);
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
