using System;
using System.Collections.Generic;

/// <summary>
/// uid 사이의 일반 진화 관계. 다음 진화는 갈래마다 하나씩, 이전 종은 uid마다 하나다.
/// 메가진화와 거다이맥스는 다루지 않는다.
/// </summary>
public class EvolutionGraph
{
    private const int MAX_DEPTH = 8;

    private readonly int[][] _next;
    private readonly int[] _previous;
    private readonly int[] _generations;

    /// <summary>
    /// next는 uid별 다음 진화 uid 목록, generations는 uid별 세대다. 둘 다 uid 순서다.
    /// </summary>
    public EvolutionGraph(int[][] next, int[] generations)
    {
        _next = next ?? new int[0][];
        _generations = generations ?? new int[0];
        _previous = new int[_next.Length];
        for (var i = 0; i < _previous.Length; i++)
        {
            _previous[i] = EvolutionLine.NONE;
        }

        for (var i = 0; i < _next.Length; i++)
        {
            var targets = GetNext(i);
            for (var j = 0; j < targets.Length; j++)
            {
                var target = targets[j];
                if (target >= 0 && target < _previous.Length && _previous[target] < 0)
                {
                    _previous[target] = i;
                }
            }
        }
    }

    /// <summary>
    /// 이 종으로 진화하는 이전 종. 없으면 -1.
    /// </summary>
    public int FindPrevious(int uid)
    {
        return uid >= 0 && uid < _previous.Length ? _previous[uid] : EvolutionLine.NONE;
    }

    /// <summary>
    /// 다음 진화 uid 목록. 최종 진화면 빈 배열이다.
    /// </summary>
    public int[] GetNext(int uid)
    {
        if (uid < 0 || uid >= _next.Length || _next[uid] == null)
        {
            return Array.Empty<int>();
        }

        return _next[uid];
    }

    /// <summary>
    /// 거슬러 올라간 계통의 첫 종.
    /// </summary>
    public int FindBasic(int uid)
    {
        var basic = uid;
        for (var depth = 0; depth < MAX_DEPTH; depth++)
        {
            var previous = FindPrevious(basic);
            if (previous < 0)
            {
                break;
            }

            basic = previous;
        }

        return basic;
    }

    /// <summary>
    /// 기본, 1진화, 2진화 칸과 갈래 종을 채운다.
    /// 고른 종까지는 실제 경로를 쓰고, 그 뒤는 첫 갈래를 따라간다.
    /// 갈래는 경로에서 처음 나뉘는 곳의 나머지 종이다.
    /// </summary>
    public void FillLine(ref EvolutionLine line, int uid)
    {
        var path = new List<int> { uid };
        for (var depth = 0; depth < MAX_DEPTH; depth++)
        {
            var previous = FindPrevious(path[0]);
            if (previous < 0)
            {
                break;
            }

            path.Insert(0, previous);
        }

        for (var depth = 0; depth < MAX_DEPTH; depth++)
        {
            var next = GetNext(path[path.Count - 1]);
            if (next.Length == 0)
            {
                break;
            }

            path.Add(next[0]);
        }

        for (var stage = EvolutionStage.Basic; stage <= EvolutionStage.Stage2; stage++)
        {
            var index = (int)stage;
            line.Set(stage, index < path.Count ? path[index] : EvolutionLine.NONE);
        }

        for (var i = 0; i < path.Count - 1; i++)
        {
            var next = GetNext(path[i]);
            if (next.Length < 2)
            {
                continue;
            }

            for (var j = 0; j < next.Length; j++)
            {
                if (next[j] != path[i + 1])
                {
                    line.AddBranch(next[j]);
                }
            }

            break;
        }
    }

    /// <summary>
    /// 이 종부터 갈래를 모두 따라간 계통의 세대 비트. 1세대가 1번 비트다.
    /// </summary>
    public int GetLineGenerationMask(int uid)
    {
        return CollectGenerationMask(uid, 0);
    }

    private int CollectGenerationMask(int uid, int depth)
    {
        if (uid < 0 || uid >= _generations.Length || depth >= MAX_DEPTH)
        {
            return 0;
        }

        var mask = 1 << (_generations[uid] - ShopGenerationMask.MIN_GENERATION);
        var next = GetNext(uid);
        for (var i = 0; i < next.Length; i++)
        {
            mask |= CollectGenerationMask(next[i], depth + 1);
        }

        return mask;
    }
}
