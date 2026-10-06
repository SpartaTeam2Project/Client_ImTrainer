using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SpriteCollab sprite 폴더에서 몬스터 그림을 가져와 MonsterVisualData에 넣는 창.
/// 미리 보기로 바뀔 내용을 확인한 뒤 가져오기를 누른다. 처리 규칙은 MonsterSpriteImporter에 있다.
/// </summary>
public class MonsterSpriteImporterWindow : EditorWindow
{
    private const string SOURCE_ROOT_KEY = "MonsterSpriteImporter.SourceRoot";
    private const string TITLE = "SpriteCollab 가져오기";
    private const int MIN_DEX = 1;
    private const int MAX_DEX = 1025;
    private const int ANIM_COLUMNS = 4;
    private const int MAX_LISTED_LINES = 200;
    private const float BROWSE_BUTTON_WIDTH = 50f;
    private const float ANIM_TOGGLE_WIDTH = 100f;
    private const float RUN_BUTTON_HEIGHT = 28f;

    private static readonly string[] TARGET_MODE_LABELS = { "도감 번호 범위", "번호 목록", "선택한 SO" };
    private static readonly char[] DEX_SEPARATORS = { ',', ' ', ';', '\t', '\n', '\r' };

    private string _sourceRoot = string.Empty;
    private TargetMode _targetMode;
    private int _fromDex = MIN_DEX;
    private int _toDex = MAX_DEX;
    private string _dexList = string.Empty;
    private bool[] _anims;
    private bool _includeSkills = true;
    private bool _overwrite;
    private bool _forceReslice;
    private int _chunkSize = MonsterSpriteImporter.DEFAULT_CHUNK_SIZE;
    private MonsterSpriteImporter.Plan _plan;
    private MonsterSpriteImporter.Report _report;
    private Vector2 _scroll;

    private enum TargetMode
    {
        Range,
        List,
        Selection,
    }

    [MenuItem("Tools/Monster/SpriteCollab 스프라이트 가져오기")]
    private static void Open()
    {
        var window = GetWindow<MonsterSpriteImporterWindow>(TITLE);
        window.minSize = new Vector2(440, 560);
    }

    private void OnEnable()
    {
        _sourceRoot = EditorPrefs.GetString(SOURCE_ROOT_KEY, string.Empty);
        if (_anims == null || _anims.Length != MonsterSpriteImporter.ACTIONS.Length)
        {
            _anims = Enumerable.Repeat(true, MonsterSpriteImporter.ACTIONS.Length).ToArray();
        }
    }

    private void OnGUI()
    {
        DrawSource();
        DrawTargets();
        DrawAnims();
        DrawOptions();
        DrawButtons();
        DrawResult();
    }

    private void DrawSource()
    {
        EditorGUILayout.LabelField("원본", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            var root = EditorGUILayout.TextField("sprite 폴더", _sourceRoot);
            if (GUILayout.Button("찾기", GUILayout.Width(BROWSE_BUTTON_WIDTH)))
            {
                var picked = EditorUtility.OpenFolderPanel("SpriteCollab sprite 폴더", _sourceRoot, string.Empty);
                if (!string.IsNullOrEmpty(picked))
                {
                    // 입력 칸에 포커스가 남아 있으면 고른 경로 대신 이전 글자가 보인다.
                    GUI.FocusControl(null);
                    SetSourceRoot(NormalizeRoot(picked));
                }

                // 폴더 창이 열려 있는 동안 레이아웃이 끊겨서 이번 그리기를 끝낸다.
                GUIUtility.ExitGUI();
            }

            if (root != _sourceRoot)
            {
                SetSourceRoot(root);
            }
        }

        if (!Directory.Exists(_sourceRoot))
        {
            EditorGUILayout.HelpBox("SpriteCollab 저장소의 sprite 폴더(0001, 0002 … 폴더가 있는 곳)를 고른다.", MessageType.Info);
        }
    }

    private void DrawTargets()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("대상", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        _targetMode = (TargetMode)GUILayout.Toolbar((int)_targetMode, TARGET_MODE_LABELS);
        switch (_targetMode)
        {
            case TargetMode.Range:
                _fromDex = Mathf.Clamp(EditorGUILayout.IntField("시작 번호", _fromDex), MIN_DEX, MAX_DEX);
                _toDex = Mathf.Clamp(EditorGUILayout.IntField("끝 번호", _toDex), MIN_DEX, MAX_DEX);
                break;
            case TargetMode.List:
                _dexList = EditorGUILayout.TextField("번호 목록", _dexList);
                EditorGUILayout.HelpBox("쉼표나 공백으로 나눈다. 예: 1, 3, 6, 25. 형태 SO(_Mega, _VMAX)도 같은 번호로 들어간다.", MessageType.None);
                break;
            default:
                EditorGUILayout.HelpBox("Project 창에서 MonsterVisualData나 그 폴더를 고른다. 세대 폴더를 고르면 안에 있는 SO가 전부 대상이다.", MessageType.None);
                break;
        }

        if (EditorGUI.EndChangeCheck())
        {
            _plan = null;
        }
    }

    private void DrawAnims()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("넣을 동작 (* 게임에서 읽는 칸)", EditorStyles.boldLabel);
        var actions = MonsterSpriteImporter.ACTIONS;
        EditorGUI.BeginChangeCheck();
        for (var start = 0; start < actions.Length; start += ANIM_COLUMNS)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var end = Mathf.Min(start + ANIM_COLUMNS, actions.Length);
                for (var i = start; i < end; i++)
                {
                    var label = actions[i].UsedAtRuntime ? actions[i].Anim + " *" : actions[i].Anim;
                    _anims[i] = EditorGUILayout.ToggleLeft(label, _anims[i], GUILayout.Width(ANIM_TOGGLE_WIDTH));
                }
            }
        }

        _includeSkills = EditorGUILayout.ToggleLeft($"특수 기술 목록 ({MonsterSpriteImporter.SKILL_ANIMS.Length}종 중 그 포켓몬에 있는 것, 예: 피카츄 Shock)", _includeSkills);
        if (EditorGUI.EndChangeCheck())
        {
            _plan = null;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("전부"))
            {
                SetAnims(_ => true);
            }

            if (GUILayout.Button("* 칸만"))
            {
                SetAnims(action => action.UsedAtRuntime);
            }

            if (GUILayout.Button("없음"))
            {
                SetAnims(_ => false);
            }
        }
    }

    private void DrawOptions()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("옵션", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        _overwrite = EditorGUILayout.ToggleLeft("채워진 칸 덮어쓰기 (원본에 없는 애니 칸은 그대로 둔다)", _overwrite);
        _forceReslice = EditorGUILayout.ToggleLeft("시트 다시 자르기 (피벗이나 압축을 바꿨을 때만)", _forceReslice);
        _chunkSize = Mathf.Max(1, EditorGUILayout.IntField("묶음 크기 (SO 수)", _chunkSize));
        if (EditorGUI.EndChangeCheck())
        {
            _plan = null;
        }
    }

    private void DrawButtons()
    {
        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!Directory.Exists(_sourceRoot)))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("미리 보기", GUILayout.Height(RUN_BUTTON_HEIGHT)))
                {
                    Preview();
                }

                if (GUILayout.Button("가져오기", GUILayout.Height(RUN_BUTTON_HEIGHT)))
                {
                    Import();
                    // 확인 창과 긴 임포트 뒤에는 레이아웃이 끊겨서 이번 그리기를 끝낸다.
                    GUIUtility.ExitGUI();
                }
            }

            if (GUILayout.Button("크레딧 다시 만들기"))
            {
                var count = MonsterSpriteImporter.WriteCredits(CollectTargets(), NormalizeRoot(_sourceRoot));
                Debug.Log($"[MonsterSpriteImporter] 크레딧 {count}개 갱신: {MonsterSpriteImporter.CREDITS_PATH}");
            }
        }
    }

    private void DrawResult()
    {
        if (_plan == null && _report == null)
        {
            return;
        }

        EditorGUILayout.Space();
        if (_report != null)
        {
            EditorGUILayout.HelpBox(_report.Summary(), _report.Errors.Count > 0 ? MessageType.Warning : MessageType.Info);
        }

        if (_plan != null)
        {
            EditorGUILayout.HelpBox(_plan.Summary(), _plan.Errors.Count > 0 ? MessageType.Warning : MessageType.Info);
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        DrawLines("원본 없는 SO", _plan?.Missing);
        DrawLines("참고", _plan?.Notes);
        DrawLines("계획 오류", _plan?.Errors);
        DrawLines("실행 오류", _report?.Errors);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.LabelField($"전체 목록: {MonsterSpriteImporter.LOG_PATH}", EditorStyles.miniLabel);
    }

    private static void DrawLines(string title, List<string> lines)
    {
        if (lines == null || lines.Count == 0)
        {
            return;
        }

        EditorGUILayout.LabelField($"{title} {lines.Count}건", EditorStyles.boldLabel);
        var count = Mathf.Min(lines.Count, MAX_LISTED_LINES);
        for (var i = 0; i < count; i++)
        {
            EditorGUILayout.LabelField(lines[i], EditorStyles.wordWrappedMiniLabel);
        }

        if (lines.Count > count)
        {
            EditorGUILayout.LabelField($"… 외 {lines.Count - count}건", EditorStyles.miniLabel);
        }
    }

    private void Preview()
    {
        _report = null;
        _plan = MonsterSpriteImporter.BuildPlan(CollectTargets(), CreateOptions());
        MonsterSpriteImporter.WriteLog(_plan, null);
    }

    private void Import()
    {
        var options = CreateOptions();
        _report = null;
        _plan = MonsterSpriteImporter.BuildPlan(CollectTargets(), options);
        if (_plan.Monsters.Count == 0)
        {
            MonsterSpriteImporter.WriteLog(_plan, null);
            EditorUtility.DisplayDialog(TITLE, "넣을 칸이 없다.\n\n" + _plan.Summary(), "확인");
            return;
        }

        if (!EditorUtility.DisplayDialog(TITLE, _plan.Summary() + "\n\n가져올까?", "가져오기", "취소"))
        {
            return;
        }

        _report = MonsterSpriteImporter.Run(_plan, options);
    }

    private MonsterSpriteImporter.Options CreateOptions()
    {
        var options = new MonsterSpriteImporter.Options
        {
            SourceRoot = NormalizeRoot(_sourceRoot),
            Overwrite = _overwrite,
            ForceReslice = _forceReslice,
            IncludeSkills = _includeSkills,
            ChunkSize = _chunkSize,
        };

        for (var i = 0; i < _anims.Length; i++)
        {
            if (_anims[i])
            {
                options.Anims.Add(MonsterSpriteImporter.ACTIONS[i].Anim);
            }
        }

        return options;
    }

    private List<string> CollectTargets()
    {
        switch (_targetMode)
        {
            case TargetMode.Range:
            {
                var from = Mathf.Min(_fromDex, _toDex);
                var to = Mathf.Max(_fromDex, _toDex);
                return MonsterSpriteImporter.FindTargets(dex => dex >= from && dex <= to);
            }
            case TargetMode.List:
            {
                var dexes = ParseDexList(_dexList);
                return MonsterSpriteImporter.FindTargets(dexes.Contains);
            }
            default:
            {
                return Selection.GetFiltered<MonsterVisualData>(SelectionMode.DeepAssets)
                    .Select(data => AssetDatabase.GetAssetPath(data))
                    .ToList();
            }
        }
    }

    // 입력 중인 글자는 그대로 둔다. 경로 정리는 실행할 때만 한다.
    private void SetSourceRoot(string root)
    {
        _sourceRoot = root;
        EditorPrefs.SetString(SOURCE_ROOT_KEY, root);
        _plan = null;
    }

    private void SetAnims(Func<MonsterSpriteImporter.ActionField, bool> enabled)
    {
        for (var i = 0; i < _anims.Length; i++)
        {
            _anims[i] = enabled(MonsterSpriteImporter.ACTIONS[i]);
        }

        _plan = null;
    }

    private static HashSet<int> ParseDexList(string text)
    {
        var dexes = new HashSet<int>();
        foreach (var token in text.Split(DEX_SEPARATORS, StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(token, out var dex))
            {
                dexes.Add(dex);
            }
        }

        return dexes;
    }

    // 끝의 구분자와 상대 경로를 정리한다. sprite 폴더의 상위 폴더에서 credit_names.txt를 찾기 때문이다.
    private static string NormalizeRoot(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            return string.Empty;
        }

        try
        {
            return Path.GetFullPath(root.Trim()).TrimEnd('\\', '/');
        }
        catch (Exception)
        {
            return root.Trim();
        }
    }
}
