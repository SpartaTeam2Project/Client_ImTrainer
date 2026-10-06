using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// SpriteCollab 시트를 몬스터 스프라이트 폴더로 복사하고, 칸 크기 격자로 자른 뒤 종의 MonsterAnimationSet 동작 칸에 넣는다.
/// 세트가 없으면 만들어 Addressables에 등록하고 MonsterVisualData에 연결한다.
/// SO 칸 이름과 애니 이름이 1:1이고, 시트 행 순서(아래, 오른쪽 아래 … 왼쪽 아래)가 방향 칸 순서와 같다.
/// 결과가 이미 같은 단계는 건너뛰어서 중간에 멈춰도 다시 실행하면 이어진다. 창은 MonsterSpriteImporterWindow가 그린다.
/// </summary>
public static class MonsterSpriteImporter
{
    public const string MONSTER_FOLDER = "Assets/Runtime/SO/Monsters";
    public const string SPRITE_FOLDER = "Assets/Runtime/UI/Sprites/Monster/InGame";
    public const string CREDITS_PATH = SPRITE_FOLDER + "/SpriteCollab_Credits.txt";
    public const string LOG_PATH = "Logs/MonsterSpriteImporter.log";
    public const int DEFAULT_CHUNK_SIZE = 25;

    private const string LOG_PREFIX = "[MonsterSpriteImporter]";
    private const string WALK = "Walk";
    private const string IDLE = "Idle";
    private const int SINGLE_ROW = 1;
    private const int EIGHT_ROWS = 8;
    // 기절, 포즈 칸은 좌우뿐이라 시트의 왼쪽(6행), 오른쪽(2행) 방향 줄을 쓴다.
    private const int LEFT_ROW = 6;
    private const int RIGHT_ROW = 2;
    private const float BYTES_PER_MB = 1024f * 1024f;

    private static readonly Regex ASSET_NAME = new Regex(@"^Monster(\d{4})(?:_(.+))?$");
    private static readonly Regex DEX_FOLDER = new Regex(@"^\d{4}$");

    private static readonly string[] CREDITS_HEADER =
    {
        "# PMD SpriteCollab 스프라이트 크레딧",
        "# 출처: https://sprites.pmdcollab.org/ , https://github.com/PMDCollab/SpriteCollab",
        "# 괄호 안은 작가가 고른 라이선스다. CC_BY-NC_4 = CC BY-NC 4.0, PMDCollab_1/2는 원본 저장소 license_history를 따른다.",
        "# Tools/Monster/SpriteCollab 스프라이트 가져오기가 만든다. 직접 고치지 않는다.",
    };

    /// <summary>
    /// SO 동작 칸과 SpriteCollab 애니 이름. 순서는 인스펙터와 같다.
    /// </summary>
    public static readonly ActionField[] ACTIONS =
    {
        new ActionField("Walk", "_walk", true),
        new ActionField("Sleep", "_sleep", false),
        new ActionField("Hurt", "_hurt", true),
        new ActionField("Attack", "_attack", true),
        new ActionField("Charge", "_charge", false),
        new ActionField("Shoot", "_shoot", true),
        new ActionField("Strike", "_strike", false),
        new ActionField("Swing", "_swing", false),
        new ActionField("Rotate", "_rotate", false),
        new ActionField("Hop", "_hop", false),
        new ActionField("Faint", "_faintLeft", "_faintRight", true),
        new ActionField("Pose", "_poseLeft", "_poseRight", true),
    };

    // 형태 SO가 쓸 SpriteCollab 형태 폴더. 표에 없거나 폴더에 그림이 없으면 기본형 시트를 같이 쓴다.
    // 0003, 0009의 메가와 거다이맥스는 원본에 그림이 없다.
    /// <summary>
    /// 특수 기술 목록에 넣을 SpriteCollab 전투 동작. sprite_config.json의 dungeon_actions에서
    /// 동작 칸이 따로 있는 12개와 Idle을 뺐다. 종에 없는 기술은 건너뛰고, CopyOf 기술은 원본 시트를 같이 쓴다.
    /// </summary>
    public static readonly string[] SKILL_ANIMS =
    {
        "Double", "QuickStrike", "Chop", "Scratch", "Punch", "Slap", "Slice", "MultiScratch", "MultiStrike", "Uppercut",
        "Ricochet", "Bite", "Shake", "Jab", "Kick", "Lick", "Slam", "Stomp", "Appeal", "Dance", "Twirl", "TailWhip",
        "Sing", "Sound", "Rumble", "FlapAround", "Gas", "Shock", "Emit", "SpAttack", "Withdraw", "RearUp", "Swell", "Hover",
    };

    private const string SKILLS_FIELD = "_skills";
    private const string SKILL_NAME_FIELD = "_name";
    private const string SKILL_FRAMES_FIELD = "_frames";

    private static readonly Dictionary<string, string> FORM_FOLDERS = new Dictionary<string, string>
    {
        { "0006_Mega", "0001" },
        { "0006_VMAX", "0003" },
    };

    /// <summary>
    /// 형태 SO가 SpriteCollab 형태 폴더(자기 그림)를 갖는지. 없으면 기본형 그림과 세트를 같이 쓴다.
    /// </summary>
    internal static bool HasFormFolder(string dex, string form)
    {
        return FORM_FOLDERS.ContainsKey($"{dex}_{form}");
    }

    /// <summary>
    /// SO 동작 칸 하나. 8방향 칸이거나 좌우 칸 한 쌍이다.
    /// </summary>
    public class ActionField
    {
        public readonly string Anim;
        public readonly string Field;
        public readonly string LeftField;
        public readonly string RightField;
        public readonly bool UsedAtRuntime;

        public ActionField(string anim, string field, bool usedAtRuntime)
        {
            Anim = anim;
            Field = field;
            UsedAtRuntime = usedAtRuntime;
        }

        public ActionField(string anim, string leftField, string rightField, bool usedAtRuntime)
        {
            Anim = anim;
            LeftField = leftField;
            RightField = rightField;
            UsedAtRuntime = usedAtRuntime;
        }

        public bool IsSides => Field == null;
    }

    /// <summary>
    /// 창에서 고른 실행 옵션.
    /// </summary>
    public class Options
    {
        public readonly HashSet<string> Anims = new HashSet<string>();
        public string SourceRoot;
        public bool Overwrite;
        public bool ForceReslice;
        public bool IncludeSkills;
        public int ChunkSize = DEFAULT_CHUNK_SIZE;
    }

    /// <summary>
    /// 원본 시트 하나와 복사될 자리. 같은 시트를 여러 칸과 형태가 같이 쓴다.
    /// </summary>
    public class SheetEntry
    {
        public string SourcePath;
        public string AssetPath;
        public int Width;
        public int Height;
        public int FrameWidth;
        public int FrameHeight;
        public long Bytes;
        public bool Exists;

        public int Columns => Width / FrameWidth;

        public int Rows => Height / FrameHeight;
    }

    public class FieldEntry
    {
        public ActionField Action;
        public string SkillName;
        public SheetEntry Sheet;
    }

    public class MonsterEntry
    {
        public readonly List<FieldEntry> Fields = new List<FieldEntry>();
        public string AssetPath;
        public string Name;
        public string Dex;
    }

    /// <summary>
    /// 미리 보기 결과. Sprite나 Texture는 담지 않고 경로만 담아서 묶음마다 메모리를 비울 수 있다.
    /// </summary>
    public class Plan
    {
        public readonly List<MonsterEntry> Monsters = new List<MonsterEntry>();
        public readonly Dictionary<string, SheetEntry> Sheets = new Dictionary<string, SheetEntry>(StringComparer.OrdinalIgnoreCase);
        public readonly List<string> Missing = new List<string>();
        public readonly List<string> Notes = new List<string>();
        public readonly List<string> Errors = new List<string>();
        public string SourceRoot;
        public int TargetCount;
        public int KeptFields;
        public int MissingFields;

        public int FieldCount => Monsters.Sum(monster => monster.Fields.Count);

        public int NewSheetCount => Sheets.Values.Count(sheet => !sheet.Exists);

        public long NewSheetBytes => Sheets.Values.Where(sheet => !sheet.Exists).Sum(sheet => sheet.Bytes);

        public int SpriteCount => Sheets.Values.Sum(sheet => sheet.Columns * sheet.Rows);

        public string Summary()
        {
            return $"대상 SO {TargetCount}개 중 채울 SO {Monsters.Count}개, 칸 {FieldCount}개\n"
                + $"시트 {Sheets.Count}장(새로 복사 {NewSheetCount}장, {NewSheetBytes / BYTES_PER_MB:0.0}MB), 스프라이트 {SpriteCount}장\n"
                + $"이미 채워져 둔 칸 {KeptFields}개, 원본에 애니가 없어 둔 칸 {MissingFields}개\n"
                + $"원본 없는 SO {Missing.Count}개, 참고 {Notes.Count}건, 오류 {Errors.Count}건";
        }
    }

    /// <summary>
    /// 실행 결과.
    /// </summary>
    public class Report
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Processed = new List<string>();
        public int CopiedSheets;
        public int ReimportedSheets;
        public int WrittenMonsters;
        public int WrittenFields;
        public int UnchangedMonsters;
        public int CreditedMonsters;
        public bool Canceled;
        public double Seconds;

        public string Summary()
        {
            var state = Canceled ? "중간에 멈춤" : "완료";
            return $"{state}: 복사 {CopiedSheets}장, 자르기 {ReimportedSheets}장, SO {WrittenMonsters}개에 칸 {WrittenFields}개 씀, "
                + $"그대로 {UnchangedMonsters}개, 크레딧 {CreditedMonsters}개, 오류 {Errors.Count}건, {Seconds:0}초";
        }
    }

    /// <summary>
    /// 도감 번호 조건에 맞는 몬스터 SO 경로. 형태(_Mega, _VMAX)도 같은 번호로 따라온다.
    /// </summary>
    public static List<string> FindTargets(Func<int, bool> includeDex)
    {
        var targets = new List<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:MonsterVisualData", new[] { MONSTER_FOLDER }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var match = ASSET_NAME.Match(Path.GetFileNameWithoutExtension(path));
            if (match.Success && includeDex(int.Parse(match.Groups[1].Value)))
            {
                targets.Add(path);
            }
        }

        return targets;
    }

    /// <summary>
    /// 무엇을 복사하고 어느 칸에 넣을지 계산한다. 파일과 에셋은 바꾸지 않는다.
    /// </summary>
    public static Plan BuildPlan(IEnumerable<string> targetPaths, Options options)
    {
        var plan = new Plan { SourceRoot = options.SourceRoot };
        if (string.IsNullOrEmpty(options.SourceRoot) || !Directory.Exists(options.SourceRoot))
        {
            plan.Errors.Add($"원본 sprite 폴더가 없음: {options.SourceRoot}");
            return plan;
        }

        var dexFolders = FindDexFolders();
        var animDataCache = new Dictionary<string, SpriteCollabAnimData>(StringComparer.OrdinalIgnoreCase);
        // 이름순이면 기본형 바로 뒤에 형태가 온다. 묶음을 나눌 때 같은 번호가 갈라지지 않는다.
        var paths = targetPaths.Distinct().OrderBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.Ordinal).ToList();
        plan.TargetCount = paths.Count;
        foreach (var path in paths)
        {
            var monster = PlanMonster(path, options, dexFolders, animDataCache, plan);
            if (monster != null && monster.Fields.Count > 0)
            {
                plan.Monsters.Add(monster);
            }
        }

        return plan;
    }

    /// <summary>
    /// 묶음마다 복사, 자르기, 할당을 한다. 끝나면 크레딧 파일과 로그를 쓴다.
    /// </summary>
    public static Report Run(Plan plan, Options options)
    {
        var report = new Report();
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            report.Errors.Add("플레이 중이거나 컴파일 중이라 실행하지 않음");
            return report;
        }

        var startTime = EditorApplication.timeSinceStartup;
        var chunks = MakeChunks(plan.Monsters, Mathf.Max(1, options.ChunkSize));
        var factories = new SpriteDataProviderFactories();
        factories.Init();

        // 실행 중에 다른 변경이 끼어들어 임포트되거나 스크립트가 다시 불러와지지 않게 막는다.
        AssetDatabase.DisallowAutoRefresh();
        EditorApplication.LockReloadAssemblies();
        try
        {
            for (var i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                var info = $"{chunk[0].Name} ~ {chunk[chunk.Count - 1].Name}";
                if (EditorUtility.DisplayCancelableProgressBar($"SpriteCollab 가져오기 {i + 1}/{chunks.Count}", info, (float)i / chunks.Count))
                {
                    report.Canceled = true;
                    break;
                }

                // 긴 실행이 한 묶음의 파일 오류로 멈추지 않게 기록만 하고 다음 묶음으로 간다. 다시 실행하면 이어진다.
                try
                {
                    ProcessChunk(chunk, options, factories, report);
                }
                catch (Exception exception)
                {
                    report.Errors.Add($"{info}: {exception.Message}");
                    Debug.LogException(exception);
                }
            }
        }
        finally
        {
            EditorApplication.UnlockReloadAssemblies();
            AssetDatabase.AllowAutoRefresh();
            EditorUtility.ClearProgressBar();
        }

        report.Seconds = EditorApplication.timeSinceStartup - startTime;
        MonsterAnimationAssets.SaveSettings();
        report.CreditedMonsters = WriteCredits(report.Processed, plan.SourceRoot);
        WriteLog(plan, report);

        var message = $"{LOG_PREFIX} {report.Summary()}\n자세한 내용: {LOG_PATH}";
        if (report.Errors.Count > 0)
        {
            Debug.LogWarning(message);
        }
        else
        {
            Debug.Log(message);
        }

        return report;
    }

    /// <summary>
    /// 고른 SO가 쓰는 원본 폴더의 작가와 라이선스를 크레딧 파일에 합쳐 쓴다. 갱신한 SO 수를 돌려준다.
    /// </summary>
    public static int WriteCredits(IEnumerable<string> targetPaths, string spriteRoot)
    {
        if (string.IsNullOrEmpty(spriteRoot) || !Directory.Exists(spriteRoot))
        {
            return 0;
        }

        var names = SpriteCollabAnimData.LoadCreditNames(spriteRoot);
        var lines = ReadCreditLines();
        var written = 0;
        foreach (var path in targetPaths)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var match = ASSET_NAME.Match(name);
            if (!match.Success)
            {
                continue;
            }

            var form = match.Groups[2].Success ? match.Groups[2].Value : null;
            if (!TryFindSource(spriteRoot, match.Groups[1].Value, form, out var folder, out _, out _))
            {
                continue;
            }

            var credits = SpriteCollabAnimData.ReadCredits(folder, names);
            if (credits.Count == 0)
            {
                continue;
            }

            lines[name] = $"{name} (sprite/{RelativeSource(spriteRoot, folder)}): {string.Join(", ", credits)}";
            written++;
        }

        if (written == 0)
        {
            return 0;
        }

        var builder = new StringBuilder();
        foreach (var header in CREDITS_HEADER)
        {
            builder.AppendLine(header);
        }

        builder.AppendLine();
        foreach (var key in lines.Keys.OrderBy(key => key, StringComparer.Ordinal))
        {
            builder.AppendLine(lines[key]);
        }

        File.WriteAllText(CREDITS_PATH, builder.ToString(), new UTF8Encoding(false));
        AssetDatabase.ImportAsset(CREDITS_PATH);
        return written;
    }

    /// <summary>
    /// 계획과 결과의 전체 목록을 로그 파일에 쓴다. 창에는 앞부분만 보인다.
    /// </summary>
    public static void WriteLog(Plan plan, Report report)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"{LOG_PREFIX} {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"원본: {plan.SourceRoot}");
        builder.AppendLine(plan.Summary());
        if (report != null)
        {
            builder.AppendLine(report.Summary());
        }

        AppendList(builder, "원본 없는 SO", plan.Missing);
        AppendList(builder, "참고", plan.Notes);
        AppendList(builder, "계획 오류", plan.Errors);
        if (report != null)
        {
            AppendList(builder, "실행 오류", report.Errors);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(LOG_PATH));
        File.WriteAllText(LOG_PATH, builder.ToString(), new UTF8Encoding(false));
    }

    private static MonsterEntry PlanMonster(string path, Options options, Dictionary<string, string> dexFolders, Dictionary<string, SpriteCollabAnimData> animDataCache, Plan plan)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var match = ASSET_NAME.Match(name);
        if (!match.Success)
        {
            plan.Errors.Add($"{name}: 이름이 Monster0000 형식이 아님");
            return null;
        }

        var dex = match.Groups[1].Value;
        var form = match.Groups[2].Success ? match.Groups[2].Value : null;
        if (!dexFolders.TryGetValue(dex, out var targetFolder))
        {
            plan.Errors.Add($"{name}: {SPRITE_FOLDER} 아래에 {dex} 폴더가 없음");
            return null;
        }

        if (!TryFindSource(options.SourceRoot, dex, form, out var sourceFolder, out var prefix, out var note))
        {
            plan.Missing.Add(name);
            return null;
        }

        if (note != null)
        {
            plan.Notes.Add($"{name}: {note}");
        }

        if (!animDataCache.TryGetValue(sourceFolder, out var animData))
        {
            if (!SpriteCollabAnimData.TryLoad(sourceFolder, out animData, out var error))
            {
                plan.Errors.Add($"{name}: {error} ({sourceFolder})");
                return null;
            }

            animDataCache.Add(sourceFolder, animData);
        }

        var data = AssetDatabase.LoadAssetAtPath<MonsterVisualData>(path);
        if (data == null)
        {
            plan.Errors.Add($"{name}: MonsterVisualData를 불러오지 못함");
            return null;
        }

        var monster = new MonsterEntry { AssetPath = path, Name = name, Dex = dex };
        // 그림은 SO가 아니라 연결된 MonsterAnimationSet에 있다. 세트가 아직 없으면 모든 칸이 비어 있는 것으로 본다.
        var set = MonsterAnimationAssets.Find(data);
        using (var serialized = set != null ? new SerializedObject(set) : null)
        {
            foreach (var action in ACTIONS)
            {
                if (!options.Anims.Contains(action.Anim))
                {
                    continue;
                }

                if (!options.Overwrite && serialized != null && IsFilled(serialized, action))
                {
                    plan.KeptFields++;
                    continue;
                }

                if (!TryResolveAnim(animData, action, out var anim, out var usedIdle))
                {
                    plan.MissingFields++;
                    continue;
                }

                if (usedIdle)
                {
                    plan.Notes.Add($"{name}: Walk가 없어 Idle로 채움");
                }

                var sheet = PlanSheet(plan, animData, anim, targetFolder, prefix, name);
                if (sheet != null)
                {
                    monster.Fields.Add(new FieldEntry { Action = action, Sheet = sheet });
                }
            }

            if (options.IncludeSkills)
            {
                PlanSkills(plan, monster, serialized, options, animData, targetFolder, prefix);
            }
        }

        return monster;
    }

    // 종에 있는 기술만 넣는다. 대부분의 종은 기술이 몇 개뿐이라 없는 기술은 '원본에 없는 칸'으로 세지 않는다.
    private static void PlanSkills(Plan plan, MonsterEntry monster, SerializedObject serialized, Options options, SpriteCollabAnimData animData, string targetFolder, string prefix)
    {
        foreach (var skill in SKILL_ANIMS)
        {
            if (!animData.TryResolve(skill, out var anim))
            {
                continue;
            }

            if (!options.Overwrite && serialized != null && FindSkill(serialized, skill) is { } existing
                && HasAnyDirection(existing.FindPropertyRelative(SKILL_FRAMES_FIELD)))
            {
                plan.KeptFields++;
                continue;
            }

            var sheet = PlanSheet(plan, animData, anim, targetFolder, prefix, monster.Name);
            if (sheet != null)
            {
                monster.Fields.Add(new FieldEntry { SkillName = skill, Sheet = sheet });
            }
        }
    }

    // 형태 표에 있으면 그 형태 폴더를, 아니면 기본형 폴더를 쓴다. prefix는 복사할 시트 이름 앞부분이다.
    private static bool TryFindSource(string spriteRoot, string dex, string form, out string folder, out string prefix, out string note)
    {
        note = null;
        if (form != null && FORM_FOLDERS.TryGetValue($"{dex}_{form}", out var formFolder))
        {
            var formPath = Path.Combine(spriteRoot, dex, formFolder);
            if (File.Exists(Path.Combine(formPath, SpriteCollabAnimData.ANIM_DATA_FILE)))
            {
                folder = formPath;
                prefix = $"{dex}_{form}";
                note = $"형태 폴더 {dex}/{formFolder} 사용";
                return true;
            }
        }

        folder = SpriteCollabAnimData.FindSpeciesFolder(spriteRoot, dex);
        prefix = dex;
        if (folder == null)
        {
            return false;
        }

        if (form != null)
        {
            note = "형태 그림이 없어 기본형 시트를 같이 씀";
        }
        else if (RelativeSource(spriteRoot, folder) != dex)
        {
            note = $"기본 폴더가 비어 {RelativeSource(spriteRoot, folder)} 사용";
        }

        return true;
    }

    // Walk가 없는 종(0618, 거다이맥스 등)은 Idle로 걷는다. 걷기 칸이 비면 필드에서 그림이 안 보인다.
    private static bool TryResolveAnim(SpriteCollabAnimData animData, ActionField action, out SpriteCollabAnimData.Anim anim, out bool usedIdle)
    {
        usedIdle = false;
        if (animData.TryResolve(action.Anim, out anim))
        {
            return true;
        }

        if (action.Anim == WALK && animData.TryResolve(IDLE, out anim))
        {
            usedIdle = true;
            return true;
        }

        return false;
    }

    private static SheetEntry PlanSheet(Plan plan, SpriteCollabAnimData animData, SpriteCollabAnimData.Anim anim, string targetFolder, string prefix, string monsterName)
    {
        // CopyOf를 따라간 실제 애니 이름으로 짓는다. Strike가 Attack 시트를 같이 쓰면 파일도 하나다.
        // 대소문자만 다른 기존 파일(0001_walk.png)과 겹치지 않게 원본의 -Anim 꼬리를 남긴다.
        var assetPath = $"{targetFolder}/{prefix}_{anim.Name}{SpriteCollabAnimData.SHEET_SUFFIX}";
        if (plan.Sheets.TryGetValue(assetPath, out var sheet))
        {
            return sheet;
        }

        var sourcePath = animData.SheetPath(anim);
        if (!File.Exists(sourcePath) || !SpriteCollabAnimData.TryReadPngSize(sourcePath, out var width, out var height))
        {
            plan.Errors.Add($"{monsterName}: 시트를 읽지 못함 {sourcePath}");
            return null;
        }

        var rows = height / anim.FrameHeight;
        if (width != anim.FrameWidth * anim.FrameCount || height % anim.FrameHeight != 0 || (rows != EIGHT_ROWS && rows != SINGLE_ROW))
        {
            plan.Errors.Add($"{monsterName}: {anim.Name} 시트 {width}x{height}가 칸 {anim.FrameWidth}x{anim.FrameHeight}, {anim.FrameCount}장과 맞지 않음");
            return null;
        }

        sheet = new SheetEntry
        {
            SourcePath = sourcePath,
            AssetPath = assetPath,
            Width = width,
            Height = height,
            FrameWidth = anim.FrameWidth,
            FrameHeight = anim.FrameHeight,
            Bytes = new FileInfo(sourcePath).Length,
            Exists = File.Exists(assetPath),
        };
        plan.Sheets.Add(assetPath, sheet);
        return sheet;
    }

    private static bool IsFilled(SerializedObject serialized, ActionField action)
    {
        if (action.IsSides)
        {
            return HasReference(serialized.FindProperty(action.LeftField)) || HasReference(serialized.FindProperty(action.RightField));
        }

        var property = serialized.FindProperty(action.Field);
        foreach (var direction in MonsterVisualDataEditor.EIGHT_DIRECTION_FIELDS)
        {
            if (HasReference(property.FindPropertyRelative(direction)))
            {
                return true;
            }
        }

        return false;
    }

    // 참조 값을 꺼내면 시트 텍스처까지 불러오므로 EntityId로만 확인한다.
    private static bool HasReference(SerializedProperty frames)
    {
        if (frames == null || !frames.isArray)
        {
            return false;
        }

        for (var i = 0; i < frames.arraySize; i++)
        {
            if (frames.GetArrayElementAtIndex(i).objectReferenceEntityIdValue.IsValid())
            {
                return true;
            }
        }

        return false;
    }

    private static List<List<MonsterEntry>> MakeChunks(List<MonsterEntry> monsters, int size)
    {
        var chunks = new List<List<MonsterEntry>>();
        var current = new List<MonsterEntry>();
        string lastDex = null;
        foreach (var monster in monsters)
        {
            // 형태는 기본형 시트를 같이 쓸 수 있어서 같은 도감 번호는 한 묶음에 둔다.
            if (current.Count >= size && monster.Dex != lastDex)
            {
                chunks.Add(current);
                current = new List<MonsterEntry>();
            }

            current.Add(monster);
            lastDex = monster.Dex;
        }

        if (current.Count > 0)
        {
            chunks.Add(current);
        }

        return chunks;
    }

    private static void ProcessChunk(List<MonsterEntry> chunk, Options options, SpriteDataProviderFactories factories, Report report)
    {
        var sheets = chunk.SelectMany(monster => monster.Fields).Select(field => field.Sheet).Distinct().ToList();

        // 1. 없는 시트만 복사하고 한 번에 기본 임포트한다. Refresh는 프로젝트 전체를 훑어서 쓰지 않는다.
        var copied = new List<string>();
        foreach (var sheet in sheets)
        {
            if (File.Exists(sheet.AssetPath))
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(sheet.AssetPath));
            File.Copy(sheet.SourcePath, sheet.AssetPath);
            copied.Add(sheet.AssetPath);
        }

        if (copied.Count > 0)
        {
            using (new AssetDatabase.AssetEditingScope())
            {
                foreach (var path in copied)
                {
                    AssetDatabase.ImportAsset(path);
                }
            }
        }

        report.CopiedSheets += copied.Count;

        // 2. 설정과 격자를 맞춘다. 바뀐 시트만 모아서 한 번에 다시 임포트한다.
        var changed = new List<TextureImporter>();
        var failed = new HashSet<SheetEntry>();
        foreach (var sheet in sheets)
        {
            var importer = AssetImporter.GetAtPath(sheet.AssetPath) as TextureImporter;
            if (importer == null)
            {
                report.Errors.Add($"{sheet.AssetPath}: 텍스처 임포터가 없음");
                failed.Add(sheet);
                continue;
            }

            var settingsChanged = MonsterSpriteSheetSlicer.ApplyImportSettings(importer, sheet.Width, sheet.Height);
            var gridChanged = MonsterSpriteSheetSlicer.ApplyGrid(importer, factories, sheet.FrameWidth, sheet.FrameHeight, options.ForceReslice, out var error);
            if (settingsChanged || gridChanged)
            {
                changed.Add(importer);
            }

            if (error != null)
            {
                report.Errors.Add($"{sheet.AssetPath}: {error}");
                failed.Add(sheet);
            }
        }

        if (changed.Count > 0)
        {
            using (new AssetDatabase.AssetEditingScope())
            {
                foreach (var importer in changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        report.ReimportedSheets += changed.Count;

        // 3. SO 칸에 넣는다.
        var cache = new Dictionary<SheetEntry, Sprite[]>();
        foreach (var monster in chunk)
        {
            AssignMonster(monster, cache, failed, report);
            report.Processed.Add(monster.AssetPath);
        }

        // 4. 묶음마다 불러온 시트를 내려서 메모리가 쌓이지 않게 한다.
        cache.Clear();
        EditorUtility.UnloadUnusedAssetsImmediate();
    }

    private static void AssignMonster(MonsterEntry monster, Dictionary<SheetEntry, Sprite[]> cache, HashSet<SheetEntry> failed, Report report)
    {
        var data = AssetDatabase.LoadAssetAtPath<MonsterVisualData>(monster.AssetPath);
        if (data == null)
        {
            report.Errors.Add($"{monster.Name}: MonsterVisualData를 불러오지 못함");
            return;
        }

        var set = MonsterAnimationAssets.GetOrCreate(data, out _);
        if (set == null)
        {
            report.Errors.Add($"{monster.Name}: 동작 그림 세트를 만들지 못함");
            return;
        }

        var written = 0;
        using (var serialized = new SerializedObject(set))
        {
            foreach (var field in monster.Fields)
            {
                if (failed.Contains(field.Sheet))
                {
                    continue;
                }

                var sprites = LoadSprites(field.Sheet, cache, report);
                if (sprites != null && WriteField(serialized, field, sprites))
                {
                    written++;
                }
            }

            if (written > 0)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        if (written == 0)
        {
            report.UnchangedMonsters++;
            return;
        }

        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssetIfDirty(set);
        AssetDatabase.SaveAssetIfDirty(data);
        report.WrittenMonsters++;
        report.WrittenFields += written;
    }

    private static bool WriteField(SerializedObject serialized, FieldEntry field, Sprite[] sprites)
    {
        var sheet = field.Sheet;
        var singleRow = sheet.Rows == SINGLE_ROW;
        var changed = false;
        if (field.SkillName != null)
        {
            return WriteSkill(serialized, field.SkillName, sprites, sheet);
        }

        if (field.Action.IsSides)
        {
            changed |= WriteFrames(serialized.FindProperty(field.Action.LeftField), sprites, singleRow ? 0 : LEFT_ROW, sheet.Columns);
            changed |= WriteFrames(serialized.FindProperty(field.Action.RightField), sprites, singleRow ? 0 : RIGHT_ROW, sheet.Columns);
            return changed;
        }

        var action = serialized.FindProperty(field.Action.Field);
        var directions = MonsterVisualDataEditor.EIGHT_DIRECTION_FIELDS;
        for (var direction = 0; direction < directions.Length; direction++)
        {
            // 1줄 시트(잠 등)는 방향이 없어서 모든 방향 칸에 같은 줄을 넣는다.
            var row = singleRow ? 0 : direction;
            changed |= WriteFrames(action.FindPropertyRelative(directions[direction]), sprites, row, sheet.Columns);
        }

        return changed;
    }

    // 기술 목록에서 이름이 같은 칸을 찾아 8방향을 쓴다. 없으면 목록 끝에 새로 만든다.
    private static bool WriteSkill(SerializedObject serialized, string skill, Sprite[] sprites, SheetEntry sheet)
    {
        var changed = false;
        var element = FindSkill(serialized, skill);
        if (element == null)
        {
            var list = serialized.FindProperty(SKILLS_FIELD);
            list.arraySize++;
            element = list.GetArrayElementAtIndex(list.arraySize - 1);
            element.FindPropertyRelative(SKILL_NAME_FIELD).stringValue = skill;
            changed = true;
        }

        // 새 칸은 앞 칸 값을 복사해 생기지만, 아래에서 8방향을 모두 다시 쓴다.
        var frames = element.FindPropertyRelative(SKILL_FRAMES_FIELD);
        var directions = MonsterVisualDataEditor.EIGHT_DIRECTION_FIELDS;
        for (var direction = 0; direction < directions.Length; direction++)
        {
            var row = sheet.Rows == SINGLE_ROW ? 0 : direction;
            changed |= WriteFrames(frames.FindPropertyRelative(directions[direction]), sprites, row, sheet.Columns);
        }

        return changed;
    }

    private static SerializedProperty FindSkill(SerializedObject serialized, string skill)
    {
        var list = serialized.FindProperty(SKILLS_FIELD);
        if (list == null)
        {
            return null;
        }

        for (var i = 0; i < list.arraySize; i++)
        {
            var element = list.GetArrayElementAtIndex(i);
            if (element.FindPropertyRelative(SKILL_NAME_FIELD).stringValue == skill)
            {
                return element;
            }
        }

        return null;
    }

    private static bool HasAnyDirection(SerializedProperty frames)
    {
        foreach (var direction in MonsterVisualDataEditor.EIGHT_DIRECTION_FIELDS)
        {
            if (HasReference(frames.FindPropertyRelative(direction)))
            {
                return true;
            }
        }

        return false;
    }

    // 이미 같은 스프라이트가 같은 순서로 있으면 쓰지 않는다. 다시 실행해도 SO가 바뀌지 않는다.
    private static bool WriteFrames(SerializedProperty frames, Sprite[] sprites, int row, int columns)
    {
        var start = row * columns;
        var same = frames.arraySize == columns;
        for (var i = 0; same && i < columns; i++)
        {
            same = frames.GetArrayElementAtIndex(i).objectReferenceEntityIdValue == sprites[start + i].GetEntityId();
        }

        if (same)
        {
            return false;
        }

        frames.arraySize = columns;
        for (var i = 0; i < columns; i++)
        {
            frames.GetArrayElementAtIndex(i).objectReferenceValue = sprites[start + i];
        }

        return true;
    }

    private static Sprite[] LoadSprites(SheetEntry sheet, Dictionary<SheetEntry, Sprite[]> cache, Report report)
    {
        if (cache.TryGetValue(sheet, out var cached))
        {
            return cached;
        }

        var textureName = Path.GetFileNameWithoutExtension(sheet.AssetPath);
        var sprites = new Sprite[sheet.Columns * sheet.Rows];
        foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(sheet.AssetPath))
        {
            if (asset is Sprite sprite && TryParseIndex(sprite.name, textureName, out var index) && index < sprites.Length)
            {
                sprites[index] = sprite;
            }
        }

        if (sprites.Any(sprite => sprite == null))
        {
            report.Errors.Add($"{sheet.AssetPath}: 잘린 칸이 {sprites.Length}장보다 적음");
            sprites = null;
        }

        cache[sheet] = sprites;
        return sprites;
    }

    private static bool TryParseIndex(string spriteName, string textureName, out int index)
    {
        index = -1;
        var prefix = textureName + "_";
        return spriteName.StartsWith(prefix, StringComparison.Ordinal)
            && int.TryParse(spriteName.Substring(prefix.Length), out index)
            && index >= 0;
    }

    private static Dictionary<string, string> FindDexFolders()
    {
        var folders = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!Directory.Exists(SPRITE_FOLDER))
        {
            return folders;
        }

        foreach (var directory in Directory.GetDirectories(SPRITE_FOLDER, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(directory);
            if (DEX_FOLDER.IsMatch(name) && !folders.ContainsKey(name))
            {
                folders.Add(name, directory.Replace('\\', '/'));
            }
        }

        return folders;
    }

    // 다른 범위를 따로 실행해도 앞선 줄이 남도록 기존 크레딧 파일을 SO 이름으로 읽는다.
    private static Dictionary<string, string> ReadCreditLines()
    {
        var lines = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!File.Exists(CREDITS_PATH))
        {
            return lines;
        }

        foreach (var line in File.ReadAllLines(CREDITS_PATH, Encoding.UTF8))
        {
            var end = line.IndexOf(' ');
            if (line.StartsWith("#", StringComparison.Ordinal) || end <= 0)
            {
                continue;
            }

            lines[line.Substring(0, end)] = line;
        }

        return lines;
    }

    private static string RelativeSource(string spriteRoot, string folder)
    {
        var root = Path.GetFullPath(spriteRoot).TrimEnd('\\', '/');
        var full = Path.GetFullPath(folder);
        var relative = full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full.Substring(root.Length) : full;
        return relative.TrimStart('\\', '/').Replace('\\', '/');
    }

    private static void AppendList(StringBuilder builder, string title, List<string> lines)
    {
        if (lines.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine($"## {title} {lines.Count}건");
        foreach (var line in lines)
        {
            builder.AppendLine(line);
        }
    }
}
