/// <summary>
/// RecycledScrollView가 재사용하는 셀 프리팹이 구현해야 하는 인터페이스입니다.
/// 셀은 풀링되어 재사용되므로, Bind()가 호출될 때마다 이전 데이터의 흔적 없이
/// 현재 data/index 기준으로 UI를 완전히 갱신해야 합니다.
/// </summary>
/// <typeparam name="TData">이 셀이 표시할 데이터 타입입니다.</typeparam>
public interface IRecycledScrollCell<TData>
{
    /// <summary>
    /// 셀이 화면에 표시될 때(또는 재사용되어 다른 데이터로 바뀔 때) 호출됩니다.
    /// </summary>
    /// <param name="data">이 셀이 표시할 데이터입니다.</param>
    /// <param name="index">전체 데이터 리스트 기준 인덱스입니다. (선택 상태 비교 등에 사용 가능)</param>
    void Bind(TData data, int index);
}