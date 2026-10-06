using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

/// <summary>
/// SpriteCollab sprite 폴더 하나의 AnimData.xml과 원본 폴더를 찾는 규칙.
/// 시트는 가로가 프레임, 세로가 방향(8줄 또는 1줄)이다. CopyOf 애니는 시트가 없어서 원본 애니를 따라간다.
/// </summary>
public class SpriteCollabAnimData
{
    public const string ANIM_DATA_FILE = "AnimData.xml";
    public const string SHEET_SUFFIX = "-Anim.png";

    private const string CREDITS_FILE = "credits.txt";
    private const string CREDIT_NAMES_FILE = "credit_names.txt";
    private const string CURRENT_CREDIT = "CUR";
    private const string BASE_COLOR_FOLDER = "0000";
    // 폴더 단계는 도감/형태/이로치/성별이다. 이로치 단계에서는 기본 색(0000)만 따라 내려간다.
    private const int SHINY_DEPTH = 2;
    // CopyOf는 원본에서 한 단계뿐이다. 잘못된 데이터가 돌지 않게만 막는다.
    private const int MAX_COPY_DEPTH = 4;
    private const int PNG_HEADER_LENGTH = 24;
    private const int PNG_WIDTH_OFFSET = 16;
    private const int PNG_HEIGHT_OFFSET = 20;

    private static readonly byte[] PNG_SIGNATURE = { 137, 80, 78, 71, 13, 10, 26, 10 };
    private static readonly byte[] IHDR = { (byte)'I', (byte)'H', (byte)'D', (byte)'R' };

    private readonly Dictionary<string, Anim> _anims;

    /// <summary>
    /// 애니 하나. CopyOf가 있으면 크기와 프레임 수가 비어 있다.
    /// </summary>
    public class Anim
    {
        public string Name;
        public string CopyOf;
        public int FrameWidth;
        public int FrameHeight;
        public int FrameCount;
    }

    private SpriteCollabAnimData(string folder, Dictionary<string, Anim> anims)
    {
        Folder = folder;
        _anims = anims;
    }

    /// <summary>
    /// AnimData.xml이 있는 폴더의 전체 경로.
    /// </summary>
    public string Folder { get; }

    /// <summary>
    /// 폴더의 AnimData.xml을 읽는다. 파일마다 XML 선언과 들여쓰기가 달라서 XML 파서로 읽는다.
    /// </summary>
    public static bool TryLoad(string folder, out SpriteCollabAnimData data, out string error)
    {
        data = null;
        error = null;
        var path = Path.Combine(folder, ANIM_DATA_FILE);
        if (!File.Exists(path))
        {
            error = $"{ANIM_DATA_FILE} 없음";
            return false;
        }

        XDocument document;
        try
        {
            document = XDocument.Load(path);
        }
        catch (Exception exception)
        {
            error = $"{ANIM_DATA_FILE} 읽기 실패: {exception.Message}";
            return false;
        }

        var animsElement = document.Root?.Element("Anims");
        if (animsElement == null)
        {
            error = $"{ANIM_DATA_FILE}에 Anims 없음";
            return false;
        }

        var anims = new Dictionary<string, Anim>(StringComparer.Ordinal);
        foreach (var element in animsElement.Elements("Anim"))
        {
            var name = ReadText(element, "Name");
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            anims[name] = new Anim
            {
                Name = name,
                CopyOf = ReadText(element, "CopyOf"),
                FrameWidth = ReadInt(element, "FrameWidth"),
                FrameHeight = ReadInt(element, "FrameHeight"),
                FrameCount = element.Element("Durations")?.Elements("Duration").Count() ?? 0,
            };
        }

        data = new SpriteCollabAnimData(folder, anims);
        return true;
    }

    /// <summary>
    /// CopyOf를 따라가 시트가 있는 애니를 찾는다. 대상은 파일 뒤쪽에 정의돼 있을 수도 있다.
    /// 애니가 없거나 크기 정보가 비면 false.
    /// </summary>
    public bool TryResolve(string name, out Anim anim)
    {
        anim = null;
        var current = name;
        for (var depth = 0; depth < MAX_COPY_DEPTH; depth++)
        {
            if (!_anims.TryGetValue(current, out var found))
            {
                return false;
            }

            if (string.IsNullOrEmpty(found.CopyOf))
            {
                anim = found;
                return found.FrameWidth > 0 && found.FrameHeight > 0 && found.FrameCount > 0;
            }

            current = found.CopyOf;
        }

        return false;
    }

    /// <summary>
    /// 애니 시트 png의 전체 경로.
    /// </summary>
    public string SheetPath(Anim anim)
    {
        return Path.Combine(Folder, anim.Name + SHEET_SUFFIX);
    }

    /// <summary>
    /// png 헤더만 읽어 크기를 얻는다. 임포트 전에도 미리 보기를 계산할 수 있다.
    /// </summary>
    public static bool TryReadPngSize(string path, out int width, out int height)
    {
        width = 0;
        height = 0;
        var header = new byte[PNG_HEADER_LENGTH];
        using (var stream = File.OpenRead(path))
        {
            if (stream.Read(header, 0, header.Length) != header.Length)
            {
                return false;
            }
        }

        // 서명 뒤 첫 청크는 늘 IHDR이다. 너비와 높이는 빅엔디언 4바이트씩이다.
        if (!StartsWith(header, 0, PNG_SIGNATURE) || !StartsWith(header, 12, IHDR))
        {
            return false;
        }

        width = ReadBigEndian(header, PNG_WIDTH_OFFSET);
        height = ReadBigEndian(header, PNG_HEIGHT_OFFSET);
        return width > 0 && height > 0;
    }

    /// <summary>
    /// 도감 번호의 기본형 폴더를 찾는다. 기본 폴더에 AnimData.xml이 없으면 하위 폴더를 이름순 너비 우선으로 찾고,
    /// 이로치 폴더로는 내려가지 않는다. 예: 0668은 암컷 폴더(0000/0000/0002)만 있다. 없으면 null.
    /// </summary>
    public static string FindSpeciesFolder(string spriteRoot, string dex)
    {
        var root = Path.Combine(spriteRoot, dex);
        if (!Directory.Exists(root))
        {
            return null;
        }

        var queue = new Queue<(string Path, int Depth)>();
        queue.Enqueue((root, 0));
        while (queue.Count > 0)
        {
            var (path, depth) = queue.Dequeue();
            if (File.Exists(Path.Combine(path, ANIM_DATA_FILE)))
            {
                return path;
            }

            var children = Directory.GetDirectories(path);
            Array.Sort(children, StringComparer.Ordinal);
            foreach (var child in children)
            {
                var childDepth = depth + 1;
                if (childDepth == SHINY_DEPTH && Path.GetFileName(child) != BASE_COLOR_FOLDER)
                {
                    continue;
                }

                queue.Enqueue((child, childDepth));
            }
        }

        return null;
    }

    /// <summary>
    /// credit_names.txt에서 Discord 표기를 작가 이름으로 바꾸는 표를 만든다. sprite 폴더의 상위 폴더에 있다.
    /// </summary>
    public static Dictionary<string, string> LoadCreditNames(string spriteRoot)
    {
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        var parent = Directory.GetParent(spriteRoot);
        var path = parent == null ? null : Path.Combine(parent.FullName, CREDIT_NAMES_FILE);
        if (path == null || !File.Exists(path))
        {
            return names;
        }

        // 첫 줄은 Name, Discord, Contact 머리글이다.
        foreach (var line in File.ReadLines(path, Encoding.UTF8).Skip(1))
        {
            var columns = line.Split('\t');
            if (columns.Length >= 2 && !string.IsNullOrWhiteSpace(columns[1]))
            {
                names[columns[1].Trim()] = columns[0].Trim();
            }
        }

        return names;
    }

    /// <summary>
    /// 폴더 credits.txt에서 지금 쓰는(CUR) 작가를 "이름 (라이선스)"로 모은다.
    /// 줄 형식은 탭으로 나뉜 날짜, 작가, CUR/OLD, 라이선스, 애니 목록이다.
    /// </summary>
    public static List<string> ReadCredits(string folder, Dictionary<string, string> names)
    {
        var credits = new List<string>();
        var path = Path.Combine(folder, CREDITS_FILE);
        if (!File.Exists(path))
        {
            return credits;
        }

        foreach (var line in File.ReadLines(path, Encoding.UTF8))
        {
            var columns = line.Split('\t');
            if (columns.Length < 4 || columns[2].Trim() != CURRENT_CREDIT)
            {
                continue;
            }

            var author = columns[1].Trim();
            var name = names.TryGetValue(author, out var known) ? known : author;
            var entry = $"{name} ({columns[3].Trim()})";
            if (!credits.Contains(entry))
            {
                credits.Add(entry);
            }
        }

        return credits;
    }

    private static string ReadText(XElement parent, string name)
    {
        return ((string)parent.Element(name))?.Trim();
    }

    private static int ReadInt(XElement parent, string name)
    {
        return int.TryParse(ReadText(parent, name), out var value) ? value : 0;
    }

    private static bool StartsWith(byte[] bytes, int offset, byte[] expected)
    {
        for (var i = 0; i < expected.Length; i++)
        {
            if (bytes[offset + i] != expected[i])
            {
                return false;
            }
        }

        return true;
    }

    private static int ReadBigEndian(byte[] bytes, int offset)
    {
        return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
    }
}
