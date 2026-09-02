namespace MesCore;

/* ============================================================
   WorkResultRow - 실적 마감 화면의 그리드 한 줄
   ------------------------------------------------------------
   이름 규칙
     DB 컬럼은 RESULT_ID 처럼 대문자+밑줄(스네이크),
     C# 속성은 ResultId 처럼 파스칼 표기를 쓴다.
     둘을 잇는 것은 WorkResultViewModel.Search() 안의 매핑 코드다.

   이 파일에 나오는 약어
     Id    Identifier  식별번호
     No    Number      번호
     Qty   Quantity    수량
     Dt    DateTime    일시
     Nm    Name        이름
     WO    Work Order  작업지시
     Prod  Production  생산
     Shot  1회 사출    타수 (금형을 한 번 찍는 것)

   수량의 의미가 상태에 따라 다르다
     마감 전(END)    샷 로그를 지금 세어본 값 - 아직 확정 아님
     마감 후(CLOSED) 마감 프로시저가 확정해 테이블에 박아둔 값
   ============================================================ */
public class WorkResultRow
{
    /// <summary>
    /// 실적 고유번호. Result(실적) + Id = Identifier(식별번호).
    /// </summary>
    public long ResultId { get; init; }

    /// <summary>
    /// 작업지시번호. WO = Work Order(작업지시), No = Number(번호).
    /// 예: WO20260814001
    /// </summary>
    public string WoNo { get; init; } = "";

    /// <summary>만든 제품 이름. 예: 도어트림 클립</summary>
    public string ItemName { get; init; } = "";

    /// <summary>작업한 기계 이름. 예: 1호 사출기</summary>
    public string EquipName { get; init; } = "";

    /// <summary>
    /// 작업 시작 시각. Start(시작) + Dt = DateTime(일시).
    /// POP 단말에서 작업자가 '작업 시작'을 누른 때.
    /// LOT 번호의 날짜 부분도 이 값에서 뽑아 쓴다.
    /// </summary>
    public DateTime StartDt { get; init; }

    /// <summary>
    /// 작업 종료 시각. End(종료) + Dt = DateTime(일시).
    /// POP 단말에서 작업자가 '작업 종료'를 누른 때.
    /// </summary>
    public DateTime? EndDt { get; init; }

    /// <summary>
    /// 타수. Shot(1회 사출) + Qty = Quantity(수량).
    /// </summary>
    public int ShotQty { get; init; }

    /// <summary>
    /// 생산수량. Prod = Production(생산) + Qty = Quantity(수량).
    ///
    ///   생산수량 = 타수 x 캐비티
    /// </summary>
    public int ProdQty { get; init; }

    /// <summary>
    /// 불량수량. Defect(불량) + Qty = Quantity(수량).
    /// POP 단말에서 작업자가 등록한 불량의 합계.
    ///
    /// TB_DEFECT 에는 종류별로 여러 줄이 들어간다.
    ///   미성형 5개 / 버 3개 / 변형 4개  ->  여기서는 12 로 합쳐서 보여준다.
    /// 종류별 내역은 불량 분석 화면에서 따로 본다.
    /// </summary>
    public int DefectQty { get; init; }

    /// <summary>
    /// 양품수량. Good(양품, 정상품) + Qty = Quantity(수량).
    ///
    ///   양품수량 = 생산수량 - 불량수량
    ///
    /// 실제로 창고에 들어가고 고객에게 나갈 수량이다.
    /// 작업지시의 누적 생산수량(ProdQty)에 더해지는 것도 이 값이다.
    ///
    /// 이 값은 마감한 뒤에만 채워진다.
    ///   마감 전에는 0 으로 보인다. 아직 확정된 숫자가 아니기 때문.
    ///   마감 전 예상치가 필요하면 ProdQty - DefectQty 로 직접 계산한다.
    /// </summary>
    public int GoodQty { get; init; }

    /// <summary>
    /// LOT 번호. LOT(로트) = 같은 조건에서 같이 만들어진 제품 묶음.
    /// 형식: L + 시작일(8자리) + 설비(4자리) + 일련번호(3자리)
    /// 예: L20260814EQ01001
    ///
    /// 마감할 때 발급되므로 마감 전에는 빈 문자열이다.
    /// </summary>
    public string LotNo { get; init; } = "";

    /// <summary>
    /// 실적 상태 코드. DB 에 저장되는 영문 값.
    ///   RUN    작업중         - 현장에서 시작 버튼을 누른 상태
    ///   END    종료(마감대기)  - 현장 작업은 끝났고 사무실 마감을 기다림
    ///   CLOSED 마감완료       - 수량 확정 + LOT 발급까지 끝
    ///   CANCEL 취소
    /// </summary>
    public string Status { get; init; } = "";

    /// <summary>
    /// 화면에 보여줄 한글 상태명.
    /// DB 에는 영문 코드를 저장하고 사람에게는 한글로 보여준다.
    /// 이런 계산 결과는 저장하지 않고 읽을 때마다 만든다.
    /// </summary>
    public string StatusName => Status switch
    {
        "RUN" => "작업중",
        "END" => "종료(마감대기)",
        "CLOSED" => "마감완료",
        "CANCEL" => "취소",
        _ => Status
    };

    /// <summary>
    /// 마감 버튼을 누를 수 있는 줄인지.
    /// 그리드에서 이 값이 true 인 줄은 노란색으로 칠하고,
    /// 마감 버튼의 활성화 조건으로도 쓴다.
    /// </summary>
    public bool CanClose => Status == "END";
}