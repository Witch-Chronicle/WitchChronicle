/// <summary>
/// Inventory 그리드용 RecycledScrollView 구체 클래스입니다.
/// ScrollRect가 붙은 오브젝트(또는 그 부모)에 부착하세요.
/// Column Count를 그리드 열 수(예: 5)로 설정하면 그리드로, 1이면 세로 리스트로 동작합니다.
/// </summary>
public class InventoryScrollView : RecycledScrollView<InventorySlotEntry, InventoryItemSlot>
{
}