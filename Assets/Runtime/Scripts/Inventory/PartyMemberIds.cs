using System.Collections.Generic;

/// <summary>
/// 장착 칸마다 판 안의 포켓몬 개체 번호를 붙인다. 가방 포켓몬은 쌓이므로 번호가 없다.
/// 새로 장착하면 새 번호, 칸끼리 바꾸면 번호도 따라가고, 합성 결과는 결과 칸의 번호를 이어 쓴다.
/// </summary>
internal sealed class PartyMemberIds
{
    private readonly int[] _ids;
    private int _nextId = DamageSource.NO_MEMBER + 1;

    public PartyMemberIds(int slotCount)
    {
        _ids = new int[slotCount];
    }

    /// <summary>
    /// 칸의 개체 번호. 비어 있거나 칸 밖이면 0이다.
    /// </summary>
    public int Get(int slot)
    {
        return IsValid(slot) ? _ids[slot] : DamageSource.NO_MEMBER;
    }

    /// <summary>
    /// 칸에 새 개체 번호를 붙인다.
    /// </summary>
    public void AssignNew(int slot)
    {
        if (!IsValid(slot))
        {
            return;
        }

        _ids[slot] = _nextId;
        _nextId++;
    }

    /// <summary>
    /// 두 칸의 번호를 맞바꾼다. 한쪽이 비어 있으면 옮긴 것과 같다.
    /// </summary>
    public void Swap(int a, int b)
    {
        if (!IsValid(a) || !IsValid(b))
        {
            return;
        }

        var temp = _ids[a];
        _ids[a] = _ids[b];
        _ids[b] = temp;
    }

    /// <summary>
    /// 칸의 번호를 지운다. 판매, 해제, 합성 재료가 된 칸이다.
    /// </summary>
    public void Release(int slot)
    {
        if (IsValid(slot))
        {
            _ids[slot] = DamageSource.NO_MEMBER;
        }
    }

    /// <summary>
    /// 합성 재료로 쓴 칸 중 결과가 놓인 첫 칸만 남기고 번호를 지운다.
    /// </summary>
    public void ReleaseMaterials(List<int> usedSlots)
    {
        for (var i = 1; i < usedSlots.Count; i++)
        {
            Release(usedSlots[i]);
        }
    }

    private bool IsValid(int slot)
    {
        return slot >= 0 && slot < _ids.Length;
    }
}
