/// <summary>
/// 가방이나 장착 칸 하나. 같은 종과 같은 성만 한 칸에 쌓인다.
/// </summary>
public class InventoryStack
{
    public Item Item;
    public int Number;
    public bool Empty = true;

    /// <summary>
    /// 종과 성이 같은지 본다.
    /// </summary>
    public bool isSameItem(int uid, int upgradeLevel)
    {
        return !Empty && Item != null && Item.uid == uid && Item.upgradeLevel == upgradeLevel;
    }

    /// <summary>
    /// 칸을 비운다.
    /// </summary>
    public void Delete()
    {
        Item = null;
        Number = 0;
        Empty = true;
    }

    /// <summary>
    /// 복사한 아이템을 칸에 둔다.
    /// </summary>
    public void SetItem(Item item, int number)
    {
        if (item == null || number <= 0)
        {
            Delete();
            return;
        }

        Item = item.Copy();
        Number = number;
        Empty = false;
    }

    /// <summary>
    /// 개수를 더한다. 0 이하면 칸을 비운다.
    /// </summary>
    public void AddNumber(int number)
    {
        if (Empty)
        {
            return;
        }

        Number += number;
        if (Number <= 0)
        {
            Delete();
        }
    }

    /// <summary>
    /// 칸을 복사한다.
    /// </summary>
    public InventoryStack Copy()
    {
        var copy = new InventoryStack();
        if (Empty || Item == null)
        {
            copy.Delete();
            return copy;
        }

        copy.SetItem(Item, Number);
        return copy;
    }
}
