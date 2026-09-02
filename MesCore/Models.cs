namespace MesCore;

/* ============================================================
    화면에 표시할 데이터의 모양을 정의한다.
    DB 컬럼과 1:1 대응이 아니라, "화면이 필요로 하는 것"만 담는다.

    이 파일에 나오는 약어
        Cd   Code       코드   (예: ItemCd = 품목코드)
        Nm   Name       이름
        No   Number     번호
        Qty  Quantity   수량
        Dt   DateTime   일시
        WO   Work Order 작업지시
    ============================================================ */


/// <summary>
/// 콤보박스에 쓰는 "코드 + 이름" 한 쌍.
/// 품목, 설비, 정지사유, 불량종류에 모두 재사용한다.
/// </summary>
public class CodeItem
{
    /// <summary>
    /// 코드(Code). DB 에 실제로 저장되는 값.
    /// 예) 품목 IT001 / 설비 EQ01 / 정지사유 DT03 / 작업지시 WO20260814001
    /// </summary>
    public string Code { get; init; } = "";

    /// <summary>
    /// 이름(Name). 화면에 보이는 글자. 사람이 읽는 쪽.
    /// 예) 도어트림 클립 / 1호 사출기 / 자재대기
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// 콤보박스나 목록에 이 객체를 넣었을 때 보일 글자.
    /// 이걸 정의하지 않으면 "MesCore.CodeItem" 이라는 클래스 이름이 그대로 보인다.
    /// </summary>
    public override string ToString() => Name;
}


/* ============================================================
    WorkOrderRow - 작업지시 그리드 한 줄
    ------------------------------------------------------------
    Work Order = 작업지시.
    "8월 14일에 1호 사출기에서 도어트림 클립 4000개를 만들어라"
    라는 명령서 한 장이 이 클래스 하나에 해당한다.

    이 명령서가 실행되면 실적(WorkResultRow)이 만들어진다.
        지시 1장  ->  실적 여러 건 (교대가 바뀌면 실적이 나뉜다)
    ============================================================ */
public class WorkOrderRow
{
    /// <summary>
    /// 작업지시번호. WO = Work Order(작업지시), No = Number(번호).
    /// 형식: WO + 생산예정일(8자리) + 그날의 일련번호(3자리)
    /// 예: WO20260814001  =  2026년 8월 14일의 첫 번째 지시
    /// </summary>
    public string WoNo { get; init; } = "";

    /// <summary>
    /// 생산 예정일. Plan(계획) + Date(날짜).
    /// </summary>
    public DateTime PlanDate { get; init; }

    /// <summary>만들 제품 이름. 예: 도어트림 클립</summary>
    public string ItemName { get; init; } = "";

    /// <summary>
    /// 배정된 기계 이름. 예: 1호 사출기.
    /// 아직 어느 기계에서 할지 안 정했으면 빈 문자열이다.
    /// </summary>
    public string EquipName { get; init; } = "";

    /// <summary>
    /// 지시수량. Order(지시) + Qty = Quantity(수량).
    /// </summary>
    public int OrderQty { get; init; }

    /// <summary>
    /// 누적 생산수량. Prod = Production(생산).
    /// 이 지시로 지금까지 실제로 만들어낸 양품의 합계.
    /// 이 값은 마감할 때만 늘어난다.
    /// </summary>
    public int ProdQty { get; init; }

    /// <summary>
    /// 잔량. 지시수량 - 누적 생산수량. 앞으로 더 만들어야 할 수.
    /// 이 값이 0 이 되면 지시 상태가 DONE(완료)으로 바뀐다.
    /// </summary>
    public int RemainQty { get; init; }

    /// <summary>
    /// 필요타수. 지시수량을 채우려면 기계를 몇 번 찍어야 하는지.
    /// </summary>
    public int ShotNeeded { get; init; }

    /// <summary>
    /// 지시 상태 코드. DB 에 저장되는 영문 값.
    ///   WAIT   대기   - 아직 아무도 시작하지 않음
    ///   RUN    진행중 - 어느 설비에서 작업이 돌고 있음
    ///   DONE   완료   - 지시수량을 다 채움
    ///   CANCEL 취소
    /// </summary>
    public string Status { get; init; } = "";

    /// <summary>
    /// 화면에 보여줄 한글 상태명.
    /// </summary>
    public string StatusName => Status switch
    {
        "WAIT" => "대기",
        "RUN" => "진행중",
        "DONE" => "완료",
        "CANCEL" => "취소",
        _ => Status
    };
}
