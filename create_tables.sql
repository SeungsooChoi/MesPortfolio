/* ============================================================
   명명 규칙
     MST_  기준정보
     TB_   거래/이력 데이터
     PK_   기본키 / FK_ 외래키 / UQ_ 유니크 / CK_ 체크 / IX_ 인덱스
   ============================================================ */

/* ============================================================
   MST_CODE - 공통코드
   ------------------------------------------------------------
   비가동 사유, 불량 종류처럼 "드롭다운에 뜰 목록"을 한 테이블에 모음.
   GROUP_CD 로 종류를 구분한다.
 
   코드 종류가 늘어도 테이블을 하나로 사용하도록 함
   ============================================================ */
CREATE TABLE dbo.MST_CODE
(
    GROUP_CD    NVARCHAR(20)    NOT NULL,               -- 코드 그룹 (DOWNTIME, DEFECT)
    CODE        NVARCHAR(20)    NOT NULL,               -- 코드값 (DT01, DF01 ...)
    CODE_NM     NVARCHAR(100)   NOT NULL,               -- 화면에 보일 이름
    SORT_NO     INT             NOT NULL DEFAULT 0,     -- 드롭다운 정렬 순서
    ATTR1       NVARCHAR(50)    NULL,                   -- 그룹별 추가 속성
                                                        --   DOWNTIME 그룹: 'PLAN'이면 계획정지
    USE_YN      CHAR(1)         NOT NULL DEFAULT 'Y',   -- 사용여부 (논리삭제용)
 
    REG_USER    NVARCHAR(20)    NOT NULL DEFAULT 'SYSTEM',
    REG_DT      DATETIME2(0)    NOT NULL DEFAULT SYSDATETIME(),
    UPD_USER    NVARCHAR(20)    NULL,
    UPD_DT      DATETIME2(0)    NULL,
 
    CONSTRAINT PK_MST_CODE PRIMARY KEY (GROUP_CD, CODE),
    CONSTRAINT CK_MST_CODE_USE_YN CHECK (USE_YN IN ('Y','N'))
);


/* ============================================================
   MST_ITEM - 품목 (만들 제품)
   ============================================================ */
CREATE TABLE dbo.MST_ITEM
(
    ITEM_CD         NVARCHAR(20)    NOT NULL,               -- 품목코드
    ITEM_NM         NVARCHAR(100)   NOT NULL,               -- 품목명
    ITEM_TYPE       NVARCHAR(10)    NOT NULL DEFAULT 'FG',
                                                            -- FG 완제품 / SF 반제품 / RM 원자재
    UNIT            NVARCHAR(10)    NOT NULL DEFAULT 'EA',  -- 단위
 
    CAVITY          INT             NOT NULL DEFAULT 1,
        -- 금형에 제품이 몇 개 들어가는지.
        -- 실적수량 = 찍은 횟수(타수) x CAVITY
 
    STD_CYCLE_SEC   DECIMAL(6,2)    NOT NULL DEFAULT 30.00,
        -- 표준 사이클타임(초). 한 번 찍는 데 걸리는 정상 시간.
        -- OEE의 '성능가동률' 계산에 쓰인다.
 
    USE_YN          CHAR(1)         NOT NULL DEFAULT 'Y',
 
    REG_USER        NVARCHAR(20)    NOT NULL DEFAULT 'SYSTEM',
    REG_DT          DATETIME2(0)    NOT NULL DEFAULT SYSDATETIME(),
    UPD_USER        NVARCHAR(20)    NULL,
    UPD_DT          DATETIME2(0)    NULL,
 
    CONSTRAINT PK_MST_ITEM PRIMARY KEY (ITEM_CD),
    CONSTRAINT CK_MST_ITEM_TYPE     CHECK (ITEM_TYPE IN ('FG','SF','RM')),
    CONSTRAINT CK_MST_ITEM_CAVITY   CHECK (CAVITY >= 1),
    CONSTRAINT CK_MST_ITEM_CYCLE    CHECK (STD_CYCLE_SEC > 0),
    CONSTRAINT CK_MST_ITEM_USE_YN   CHECK (USE_YN IN ('Y','N'))
);
 
 
/* ============================================================
   MST_EQUIP - 설비 (기계)
   ============================================================ */
CREATE TABLE dbo.MST_EQUIP
(
    EQUIP_CD    NVARCHAR(20)    NOT NULL,   -- 설비코드
    EQUIP_NM    NVARCHAR(100)   NOT NULL,   -- 설비명
    LINE_CD     NVARCHAR(20)    NULL,       -- 라인 구분 (A라인, B라인)
    TONNAGE     INT             NULL,       -- 사출기 형체력(톤). 사출기 크기 표현
    USE_YN      CHAR(1)         NOT NULL DEFAULT 'Y',
 
    REG_USER    NVARCHAR(20)    NOT NULL DEFAULT 'SYSTEM',
    REG_DT      DATETIME2(0)    NOT NULL DEFAULT SYSDATETIME(),
    UPD_USER    NVARCHAR(20)    NULL,
    UPD_DT      DATETIME2(0)    NULL,
 
    CONSTRAINT PK_MST_EQUIP PRIMARY KEY (EQUIP_CD),
    CONSTRAINT CK_MST_EQUIP_USE_YN CHECK (USE_YN IN ('Y','N'))
);
 
 
/* ============================================================
   TB_WORK_ORDER - 작업지시
   ============================================================ */
CREATE TABLE dbo.TB_WORK_ORDER
(
    WO_NO       NVARCHAR(20)    NOT NULL,               -- 작업지시번호 (예: WO20260814001)
    ITEM_CD     NVARCHAR(20)    NOT NULL,               -- 만들 품목
    EQUIP_CD    NVARCHAR(20)    NULL,                   -- 배정 설비 (미정이면 NULL)
    PLAN_DT     DATE            NOT NULL,               -- 생산 예정일
    ORDER_QTY   INT             NOT NULL,               -- 지시 수량
    PROD_QTY    INT             NOT NULL DEFAULT 0,     -- 누적 생산 수량
 
    WO_STATUS   NVARCHAR(10)    NOT NULL DEFAULT 'WAIT',
        -- 상태. 정해진 값만 들어가도록 CHECK로 막는다.
        --   WAIT   대기 (아직 시작 안 함)
        --   RUN    진행중
        --   DONE   완료
        --   CANCEL 취소
 
    REMARK      NVARCHAR(200)   NULL,
 
    REG_USER    NVARCHAR(20)    NOT NULL DEFAULT 'SYSTEM',
    REG_DT      DATETIME2(0)    NOT NULL DEFAULT SYSDATETIME(),
    UPD_USER    NVARCHAR(20)    NULL,
    UPD_DT      DATETIME2(0)    NULL,
 
    CONSTRAINT PK_TB_WORK_ORDER PRIMARY KEY (WO_NO),
    CONSTRAINT FK_TB_WORK_ORDER_ITEM
        FOREIGN KEY (ITEM_CD)  REFERENCES dbo.MST_ITEM(ITEM_CD),
    CONSTRAINT FK_TB_WORK_ORDER_EQUIP
        FOREIGN KEY (EQUIP_CD) REFERENCES dbo.MST_EQUIP(EQUIP_CD),
    CONSTRAINT CK_TB_WORK_ORDER_STATUS
        CHECK (WO_STATUS IN ('WAIT','RUN','DONE','CANCEL')),
    CONSTRAINT CK_TB_WORK_ORDER_ORDER_QTY CHECK (ORDER_QTY > 0),
    CONSTRAINT CK_TB_WORK_ORDER_PROD_QTY  CHECK (PROD_QTY >= 0)
);
 
 
/* ============================================================
   TB_WORK_RESULT - 생산실적
   ============================================================ */
CREATE TABLE dbo.TB_WORK_RESULT
(
    RESULT_ID       BIGINT          NOT NULL IDENTITY(1,1),

    WO_NO           NVARCHAR(20)    NOT NULL,   -- 어느 작업지시의 실적인지
    EQUIP_CD        NVARCHAR(20)    NOT NULL,   -- 어느 기계에서
 
    START_DT        DATETIME2(0)    NOT NULL,   -- 작업 시작 시각
    END_DT          DATETIME2(0)    NULL,       -- 작업 종료 시각 (진행중이면 NULL)
 
    SHOT_QTY        INT             NOT NULL DEFAULT 0,  -- 찍은 횟수(타수)
    PROD_QTY        INT             NOT NULL DEFAULT 0,  -- 총 생산수량 = 타수 x CAVITY
    DEFECT_QTY      INT             NOT NULL DEFAULT 0,  -- 불량수량
    GOOD_QTY        INT             NOT NULL DEFAULT 0,  -- 양품수량 = 총생산 - 불량
 
    LOT_NO          NVARCHAR(30)    NULL,
        -- 마감할 때 발급되는 번호. 마감 전에는 NULL.
 
    RESULT_STATUS   NVARCHAR(10)    NOT NULL DEFAULT 'RUN',
        --   RUN    작업중
        --   CLOSED 마감됨 (수정 불가)
        --   CANCEL 마감 취소됨
 
    WORKER          NVARCHAR(20)    NULL,       -- 작업자 이름
 
    REG_USER        NVARCHAR(20)    NOT NULL DEFAULT 'SYSTEM',
    REG_DT          DATETIME2(0)    NOT NULL DEFAULT SYSDATETIME(),
    UPD_USER        NVARCHAR(20)    NULL,
    UPD_DT          DATETIME2(0)    NULL,
 
    CONSTRAINT PK_TB_WORK_RESULT PRIMARY KEY (RESULT_ID),
    CONSTRAINT FK_TB_WORK_RESULT_WO
        FOREIGN KEY (WO_NO)    REFERENCES dbo.TB_WORK_ORDER(WO_NO),
    CONSTRAINT FK_TB_WORK_RESULT_EQUIP
        FOREIGN KEY (EQUIP_CD) REFERENCES dbo.MST_EQUIP(EQUIP_CD),
    CONSTRAINT CK_TB_WORK_RESULT_STATUS
        CHECK (RESULT_STATUS IN ('RUN','CLOSED','CANCEL')),
    CONSTRAINT CK_TB_WORK_RESULT_END_DT
        CHECK (END_DT IS NULL OR END_DT >= START_DT),
    CONSTRAINT CK_TB_WORK_RESULT_QTY
        CHECK (SHOT_QTY >= 0 AND PROD_QTY >= 0 AND DEFECT_QTY >= 0 AND GOOD_QTY >= 0),
    CONSTRAINT CK_TB_WORK_RESULT_CLOSED_LOT
        -- 마감된 실적은 반드시 LOT 번호가 있어야 한다
        CHECK (RESULT_STATUS <> 'CLOSED' OR LOT_NO IS NOT NULL)
);


/* ------------------------------------------------------------
   한 설비에서 '작업중'인 실적은 1건만 허용
   ------------------------------------------------------------
   WHERE 조건이 붙은 유니크 인덱스 = 필터드 인덱스.
   RESULT_STATUS = 'RUN' 인 행들 사이에서만 EQUIP_CD 중복을 막는다.
   ------------------------------------------------------------ */
CREATE UNIQUE INDEX UQ_TB_WORK_RESULT_RUNNING
    ON dbo.TB_WORK_RESULT (EQUIP_CD)
    WHERE RESULT_STATUS = 'RUN';
 
CREATE INDEX IX_TB_WORK_RESULT_WO      ON dbo.TB_WORK_RESULT (WO_NO);
CREATE INDEX IX_TB_WORK_RESULT_START   ON dbo.TB_WORK_RESULT (START_DT);
 
 
/* ============================================================
   TB_SHOT_LOG - 샷 로그 (기계가 보내는 원시 신호)
   ------------------------------------------------------------
   기계가 한 번 찍을 때마다 1줄씩 쌓인다.
   설비 6대 x 25초 주기 x 8시간 = 하루 약 7천 건.
   ============================================================ */
CREATE TABLE dbo.TB_SHOT_LOG
(
    SHOT_ID     BIGINT          NOT NULL IDENTITY(1,1),
    EQUIP_CD    NVARCHAR(20)    NOT NULL,   -- 어느 기계가 보낸 신호인지
 
    RESULT_ID   BIGINT          NULL,
        -- 어느 실적에 속하는지.
        -- 작업 시작 전에 온 신호는 실적이 없으므로 NULL 허용.
 
    SHOT_DT     DATETIME2(0)    NOT NULL,   -- 찍은 시각
    CYCLE_SEC   DECIMAL(6,2)    NULL,       -- 이번 사이클에 걸린 시간(초)
 
    REG_DT      DATETIME2(0)    NOT NULL DEFAULT SYSDATETIME(),
 
    CONSTRAINT PK_TB_SHOT_LOG PRIMARY KEY (SHOT_ID),
    CONSTRAINT FK_TB_SHOT_LOG_EQUIP
        FOREIGN KEY (EQUIP_CD)  REFERENCES dbo.MST_EQUIP(EQUIP_CD),
    CONSTRAINT FK_TB_SHOT_LOG_RESULT
        FOREIGN KEY (RESULT_ID) REFERENCES dbo.TB_WORK_RESULT(RESULT_ID)
);
 
-- 조회는 거의 항상 "특정 기계의 특정 기간"이므로 이 순서로 인덱스를 만든다
CREATE INDEX IX_TB_SHOT_LOG_EQUIP_DT ON dbo.TB_SHOT_LOG (EQUIP_CD, SHOT_DT);
CREATE INDEX IX_TB_SHOT_LOG_RESULT   ON dbo.TB_SHOT_LOG (RESULT_ID);
 
 
/* ============================================================
   TB_DOWNTIME - 비가동 이력 (기계가 멈춘 구간)
   ============================================================ */
CREATE TABLE dbo.TB_DOWNTIME
(
    DOWNTIME_ID     BIGINT          NOT NULL IDENTITY(1,1),
    EQUIP_CD        NVARCHAR(20)    NOT NULL,
    RESULT_ID       BIGINT          NULL,       -- 작업중 멈춘 거면 실적과 연결
 
    DOWNTIME_CD     NVARCHAR(20)    NOT NULL,   -- 멈춘 사유 (MST_CODE의 DOWNTIME 그룹)
 
    CODE_GROUP      AS CAST(N'DOWNTIME' AS NVARCHAR(20)) PERSISTED,
        -- MST_CODE의 기본키가 (GROUP_CD, CODE) 두 개라서,
        -- FK를 걸려면 이쪽에도 컬럼이 두 개 필요하다.
        -- 이렇게 하면 DOWNTIME 그룹의 코드만 들어갈 수 있게 DB가 강제한다.
 
    START_DT        DATETIME2(0)    NOT NULL,   -- 멈춘 시각
    END_DT          DATETIME2(0)    NULL,       -- 재가동 시각 (아직 멈춰있으면 NULL)
    REMARK          NVARCHAR(200)   NULL,
 
    REG_USER        NVARCHAR(20)    NOT NULL DEFAULT 'SYSTEM',
    REG_DT          DATETIME2(0)    NOT NULL DEFAULT SYSDATETIME(),
    UPD_USER        NVARCHAR(20)    NULL,
    UPD_DT          DATETIME2(0)    NULL,
 
    CONSTRAINT PK_TB_DOWNTIME PRIMARY KEY (DOWNTIME_ID),
    CONSTRAINT FK_TB_DOWNTIME_EQUIP
        FOREIGN KEY (EQUIP_CD)  REFERENCES dbo.MST_EQUIP(EQUIP_CD),
    CONSTRAINT FK_TB_DOWNTIME_RESULT
        FOREIGN KEY (RESULT_ID) REFERENCES dbo.TB_WORK_RESULT(RESULT_ID),
    CONSTRAINT FK_TB_DOWNTIME_CODE
        FOREIGN KEY (CODE_GROUP, DOWNTIME_CD) REFERENCES dbo.MST_CODE(GROUP_CD, CODE),
    CONSTRAINT CK_TB_DOWNTIME_END_DT
        CHECK (END_DT IS NULL OR END_DT > START_DT)
);
 
/* ------------------------------------------------------------
   한 설비에서 '아직 안 끝난' 비가동은 1건만 허용
   ------------------------------------------------------------
   이게 없으면 같은 기계에 겹치는 정지 구간이 두 개 생겨서
   나중에 가동률 계산이 100%를 넘거나 음수가 된다.
   ------------------------------------------------------------ */
CREATE UNIQUE INDEX UQ_TB_DOWNTIME_ACTIVE
    ON dbo.TB_DOWNTIME (EQUIP_CD)
    WHERE END_DT IS NULL;
 
CREATE INDEX IX_TB_DOWNTIME_EQUIP_DT ON dbo.TB_DOWNTIME (EQUIP_CD, START_DT);
 
 
/* ============================================================
   TB_DEFECT - 불량 등록
   ------------------------------------------------------------
   실적 1건에 불량 종류별로 여러 줄이 들어간다.
   ============================================================ */
CREATE TABLE dbo.TB_DEFECT
(
    DEFECT_ID   BIGINT          NOT NULL IDENTITY(1,1),
    RESULT_ID   BIGINT          NOT NULL,   -- 어느 실적의 불량인지
    DEFECT_CD   NVARCHAR(20)    NOT NULL,   -- 불량 종류 (MST_CODE의 DEFECT 그룹)
 
    CODE_GROUP  AS CAST(N'DEFECT' AS NVARCHAR(20)) PERSISTED,
 
    DEFECT_QTY  INT             NOT NULL,   -- 불량 수량
    REMARK      NVARCHAR(200)   NULL,
 
    REG_USER    NVARCHAR(20)    NOT NULL DEFAULT 'SYSTEM',
    REG_DT      DATETIME2(0)    NOT NULL DEFAULT SYSDATETIME(),
 
    CONSTRAINT PK_TB_DEFECT PRIMARY KEY (DEFECT_ID),
    CONSTRAINT FK_TB_DEFECT_RESULT
        FOREIGN KEY (RESULT_ID) REFERENCES dbo.TB_WORK_RESULT(RESULT_ID),
    CONSTRAINT FK_TB_DEFECT_CODE
        FOREIGN KEY (CODE_GROUP, DEFECT_CD) REFERENCES dbo.MST_CODE(GROUP_CD, CODE),
    CONSTRAINT CK_TB_DEFECT_QTY CHECK (DEFECT_QTY > 0)
);
 
CREATE INDEX IX_TB_DEFECT_RESULT ON dbo.TB_DEFECT (RESULT_ID);


/* ============================================================
   샘플 데이터
   ============================================================ */
 
-- 비가동 사유 코드
INSERT INTO dbo.MST_CODE (GROUP_CD, CODE, CODE_NM, SORT_NO, ATTR1) VALUES
 (N'DOWNTIME', N'DT01', N'설비고장',        1, NULL),
 (N'DOWNTIME', N'DT02', N'금형교체',        2, NULL),
 (N'DOWNTIME', N'DT03', N'자재대기',        3, NULL),
 (N'DOWNTIME', N'DT04', N'품질문제',        4, NULL),
 (N'DOWNTIME', N'DT05', N'계획정지(식사)',  5, N'PLAN'),
 (N'DOWNTIME', N'DT99', N'기타',           99, NULL);
 
-- 불량 종류 코드 (사출 성형에서 실제로 쓰는 용어)
INSERT INTO dbo.MST_CODE (GROUP_CD, CODE, CODE_NM, SORT_NO) VALUES
 (N'DEFECT', N'DF01', N'미성형',   1),   -- 재료가 덜 차서 모양이 안 나옴
 (N'DEFECT', N'DF02', N'버',       2),   -- 금형 틈으로 재료가 삐져나옴
 (N'DEFECT', N'DF03', N'은줄',     3),   -- 표면에 은색 줄무늬
 (N'DEFECT', N'DF04', N'변형',     4),   -- 식으면서 휘어짐
 (N'DEFECT', N'DF05', N'이물',     5),
 (N'DEFECT', N'DF99', N'기타',    99);
 
-- 품목
INSERT INTO dbo.MST_ITEM (ITEM_CD, ITEM_NM, CAVITY, STD_CYCLE_SEC) VALUES
 (N'IT001', N'도어트림 클립',   4, 22.50),
 (N'IT002', N'콘솔 커버',       2, 35.00),
 (N'IT003', N'하우징 브라켓',   8, 18.00);
 
-- 설비 6대
INSERT INTO dbo.MST_EQUIP (EQUIP_CD, EQUIP_NM, LINE_CD, TONNAGE) VALUES
 (N'EQ01', N'1호 사출기', N'A', 150),
 (N'EQ02', N'2호 사출기', N'A', 150),
 (N'EQ03', N'3호 사출기', N'A', 250),
 (N'EQ04', N'4호 사출기', N'B', 250),
 (N'EQ05', N'5호 사출기', N'B', 350),
 (N'EQ06', N'6호 사출기', N'B', 350);
 
-- 작업지시 3건
INSERT INTO dbo.TB_WORK_ORDER (WO_NO, ITEM_CD, EQUIP_CD, PLAN_DT, ORDER_QTY, WO_STATUS) VALUES
 (N'WO20260814001', N'IT001', N'EQ01', '2026-08-14', 4000, N'WAIT'),
 (N'WO20260814002', N'IT002', N'EQ03', '2026-08-14', 1200, N'WAIT'),
 (N'WO20260814003', N'IT003', N'EQ05', '2026-08-14', 8000, N'WAIT');
 
 
/* ============================================================
   [검증]
   ============================================================ */
 
-- (1) 테이블 8개가 만들어졌는가
SELECT name AS 테이블명
FROM sys.tables
ORDER BY name;
-- 기대 결과: 8행 (MST_CODE, MST_EQUIP, MST_ITEM, TB_DEFECT,
--                TB_DOWNTIME, TB_SHOT_LOG, TB_WORK_ORDER, TB_WORK_RESULT)
 
 
-- (2) 샘플 데이터가 들어갔는가
SELECT '공통코드' AS 구분, COUNT(*) AS 건수 FROM dbo.MST_CODE
UNION ALL SELECT '품목',      COUNT(*) FROM dbo.MST_ITEM
UNION ALL SELECT '설비',      COUNT(*) FROM dbo.MST_EQUIP
UNION ALL SELECT '작업지시',  COUNT(*) FROM dbo.TB_WORK_ORDER;
-- 기대 결과: 12 / 3 / 6 / 3
 
 
-- (3) 작업지시를 사람이 보기 좋게 조회 (JOIN 연습)
SELECT  W.WO_NO           AS 지시번호,
        I.ITEM_NM         AS 품목명,
        E.EQUIP_NM        AS 설비명,
        W.ORDER_QTY       AS 지시수량,
        I.CAVITY          AS 캐비티,
        W.ORDER_QTY / I.CAVITY AS 필요타수,
        W.WO_STATUS       AS 상태
FROM        dbo.TB_WORK_ORDER W
INNER JOIN  dbo.MST_ITEM      I ON I.ITEM_CD  = W.ITEM_CD
LEFT  JOIN  dbo.MST_EQUIP     E ON E.EQUIP_CD = W.EQUIP_CD
ORDER BY W.WO_NO;
 
 
-- (4) [제약조건 테스트] 

-- 4-1. 없는 상태값 -> CK_TB_WORK_ORDER_STATUS 위반
 --UPDATE dbo.TB_WORK_ORDER SET WO_STATUS = N'HELLO' WHERE WO_NO = N'WO20260814001';
 
-- 4-2. 없는 품목코드 -> FK_TB_WORK_ORDER_ITEM 위반
 --INSERT INTO dbo.TB_WORK_ORDER (WO_NO, ITEM_CD, PLAN_DT, ORDER_QTY)
 --VALUES (N'WO_TEST', N'없는품목', '2026-08-14', 100);
 
-- 4-3. 같은 설비에 진행중 실적 2건 -> UQ_TB_WORK_RESULT_RUNNING 위반
--      (첫 줄은 성공, 둘째 줄에서 오류가 나면 정상)
 --INSERT INTO dbo.TB_WORK_RESULT (WO_NO, EQUIP_CD, START_DT)
 --VALUES (N'WO20260814001', N'EQ01', SYSDATETIME());
 --INSERT INTO dbo.TB_WORK_RESULT (WO_NO, EQUIP_CD, START_DT)
 --VALUES (N'WO20260814001', N'EQ01', SYSDATETIME());
 
-- 4-4. 불량코드 자리에 비가동코드 -> FK_TB_DEFECT_CODE 위반
--      (계산 컬럼 CODE_GROUP 덕분에 DEFECT 그룹 코드만 허용됨)
 --INSERT INTO dbo.TB_DEFECT (RESULT_ID, DEFECT_CD, DEFECT_QTY)
 --VALUES (1, N'DT01', 5);