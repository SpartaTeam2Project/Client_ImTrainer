using System.Collections.Generic;

/// <summary>
/// 한 플레이어의 가방 또는 장착 칸을 들고 있는 목록.
/// </summary>
public class InventoryHolder
{
    public enum HolderType
    {
        PlayerInventory,
        PlayerEquipment
    }

    public string Name = "Inventory";
    public HolderType Type;
    public List<InventoryStack> Stacks = new List<InventoryStack>();
    public int InventorySize = 10;

    /// <summary>
    /// 빈 칸을 크기만큼 만든다.
    /// </summary>
    public void EnsureSize()
    {
        if (Stacks == null)
        {
            Stacks = new List<InventoryStack>();
        }

        while (Stacks.Count < InventorySize)
        {
            var stack = new InventoryStack();
            stack.Delete();
            Stacks.Add(stack);
        }

        while (Stacks.Count > InventorySize)
        {
            Stacks.RemoveAt(Stacks.Count - 1);
        }
    }

    /// <summary>
    /// 채워진 칸 수.
    /// </summary>
    public int CountFilled()
    {
        var count = 0;
        for (var i = 0; i < Stacks.Count; i++)
        {
            if (!Stacks[i].Empty)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 같은 종과 성의 개수.
    /// </summary>
    public int GetItemNumber(int uid, int upgradeLevel)
    {
        var count = 0;
        for (var i = 0; i < Stacks.Count; i++)
        {
            if (Stacks[i].isSameItem(uid, upgradeLevel))
            {
                count += Stacks[i].Number;
            }
        }

        return count;
    }

    /// <summary>
    /// 같은 종과 성이 처음 있는 칸. 없으면 -1.
    /// </summary>
    public int FindIndex(int uid, int upgradeLevel)
    {
        for (var i = 0; i < Stacks.Count; i++)
        {
            if (Stacks[i].isSameItem(uid, upgradeLevel))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 첫 빈 칸. 없으면 -1.
    /// </summary>
    public int FindEmptyIndex()
    {
        for (var i = 0; i < Stacks.Count; i++)
        {
            if (Stacks[i].Empty)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 넣기 전에 공간이 있는지 본다.
    /// </summary>
    public bool CanAccept(Item item, int number)
    {
        if (item == null || number <= 0)
        {
            return false;
        }

        var remaining = number;
        var limit = item.maxiumStack > 0 ? item.maxiumStack : 1;
        for (var i = 0; i < Stacks.Count; i++)
        {
            var stack = Stacks[i];
            if (!stack.isSameItem(item.uid, item.upgradeLevel) || stack.Item.maxiumStack <= stack.Number)
            {
                continue;
            }

            remaining -= stack.Item.maxiumStack - stack.Number;
            if (remaining <= 0)
            {
                return true;
            }
        }

        for (var i = 0; i < Stacks.Count; i++)
        {
            if (!Stacks[i].Empty)
            {
                continue;
            }

            remaining -= limit;
            if (remaining <= 0)
            {
                return true;
            }
        }

        return remaining <= 0;
    }

    /// <summary>
    /// 같은 종과 성에 먼저 쌓고, 남으면 빈 칸에 둔다. 못 넣은 수량이 반환된다.
    /// </summary>
    public InventoryStack AddItem(Item item, int number)
    {
        var incoming = new InventoryStack();
        if (item == null || number <= 0)
        {
            incoming.Delete();
            return incoming;
        }

        incoming.SetItem(item, number);
        for (var i = 0; i < Stacks.Count; i++)
        {
            if (incoming.Number <= 0)
            {
                break;
            }

            var stack = Stacks[i];
            if (!stack.isSameItem(item.uid, item.upgradeLevel))
            {
                continue;
            }

            var room = stack.Item.maxiumStack - stack.Number;
            if (room <= 0)
            {
                continue;
            }

            var move = room < incoming.Number ? room : incoming.Number;
            stack.AddNumber(move);
            incoming.AddNumber(-move);
        }

        for (var i = 0; i < Stacks.Count; i++)
        {
            if (incoming.Number <= 0)
            {
                break;
            }

            if (!Stacks[i].Empty)
            {
                continue;
            }

            var limit = incoming.Item.maxiumStack > 0 ? incoming.Item.maxiumStack : 1;
            var move = limit < incoming.Number ? limit : incoming.Number;
            Stacks[i].SetItem(incoming.Item, move);
            incoming.AddNumber(-move);
        }

        if (incoming.Number <= 0)
        {
            incoming.Delete();
        }

        return incoming;
    }

    /// <summary>
    /// 같은 종과 성을 개수만큼 뺀다. 실제로 뺀 수를 돌려준다.
    /// </summary>
    public int RemoveItem(int uid, int upgradeLevel, int number)
    {
        if (number <= 0)
        {
            return 0;
        }

        var removed = 0;
        for (var i = 0; i < Stacks.Count; i++)
        {
            if (number <= 0)
            {
                break;
            }

            var stack = Stacks[i];
            if (!stack.isSameItem(uid, upgradeLevel))
            {
                continue;
            }

            var take = number < stack.Number ? number : stack.Number;
            stack.AddNumber(-take);
            number -= take;
            removed += take;
        }

        return removed;
    }

    /// <summary>
    /// 지정 칸에 한 마리를 둔다. 칸이 차 있으면 false.
    /// </summary>
    public bool TrySetSlot(int index, Item item)
    {
        if (item == null || index < 0 || index >= Stacks.Count || !Stacks[index].Empty)
        {
            return false;
        }

        Stacks[index].SetItem(item, 1);
        return true;
    }

    /// <summary>
    /// 지정 칸을 비운다.
    /// </summary>
    public void ClearSlot(int index)
    {
        if (index < 0 || index >= Stacks.Count)
        {
            return;
        }

        Stacks[index].Delete();
    }

    /// <summary>
    /// 들어 있는 칸을 앞으로 모은다. 종 번호가 작은 쪽이 앞이고, 같은 종은 성이 높은 쪽이 앞이다.
    /// </summary>
    public void Sort()
    {
        if (Stacks == null)
        {
            return;
        }

        Stacks.Sort(CompareStack);
    }

    /// <summary>
    /// 칸 상태를 복사한다.
    /// </summary>
    public List<InventoryStack> Capture()
    {
        var copy = new List<InventoryStack>(Stacks.Count);
        for (var i = 0; i < Stacks.Count; i++)
        {
            copy.Add(Stacks[i].Copy());
        }

        return copy;
    }

    /// <summary>
    /// 복사해 둔 칸으로 되돌린다.
    /// </summary>
    public void Restore(List<InventoryStack> stacks)
    {
        if (stacks == null)
        {
            return;
        }

        var count = stacks.Count < Stacks.Count ? stacks.Count : Stacks.Count;
        for (var i = 0; i < count; i++)
        {
            if (stacks[i] == null || stacks[i].Empty || stacks[i].Item == null)
            {
                Stacks[i].Delete();
                continue;
            }

            Stacks[i].SetItem(stacks[i].Item, stacks[i].Number);
        }
    }

    private static int CompareStack(InventoryStack a, InventoryStack b)
    {
        var emptyA = a == null || a.Empty || a.Item == null;
        var emptyB = b == null || b.Empty || b.Item == null;
        if (emptyA && emptyB)
        {
            return 0;
        }

        if (emptyA)
        {
            return 1;
        }

        if (emptyB)
        {
            return -1;
        }

        var uidOrder = a.Item.uid.CompareTo(b.Item.uid);
        if (uidOrder != 0)
        {
            return uidOrder;
        }

        return b.Item.upgradeLevel.CompareTo(a.Item.upgradeLevel);
    }
}
