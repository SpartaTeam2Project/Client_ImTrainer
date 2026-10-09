/// <summary>
/// 가방이나 장착 칸 하나와 그 칸에 있던 포켓몬. 끌기와 키보드 합성이 출발 칸을 기억할 때 쓴다.
/// 가방은 바뀔 때마다 정렬돼서 번호가 어긋나면 종과 성으로 다시 찾는다.
/// </summary>
public struct InventorySlotRef
{
    public static readonly InventorySlotRef None = new InventorySlotRef(InventorySlotDrag.SlotKind.None, -1, -1, 0);

    public InventorySlotDrag.SlotKind Kind;
    public int Index;
    public int Uid;
    public int Star;

    public InventorySlotRef(InventorySlotDrag.SlotKind kind, int index, int uid, int star)
    {
        Kind = kind;
        Index = index;
        Uid = uid;
        Star = star;
    }

    public bool IsEmpty => Kind == InventorySlotDrag.SlotKind.None;
}
