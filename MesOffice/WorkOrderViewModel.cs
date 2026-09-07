using MesCore;
using System.Collections.ObjectModel;
using System.Windows;

namespace MesOffice
{
    internal class WorkOrderViewModel : ViewModelBase
    {
        // ------------------------------------------------------------
        // 조회 조건
        // ------------------------------------------------------------
        private DateTime _fromDate = DateTime.Today.AddDays(-7);
        public DateTime FromDate { get => _fromDate; set => Set(ref _fromDate, value); }

        private DateTime _toDate = DateTime.Today.AddDays(7);
        public DateTime ToDate { get => _toDate; set => Set(ref _toDate, value); }

        public string[] StatusFilters { get; } = ["전체", "WAIT", "RUN", "DONE", "CANCEL"];

        private string _statusFilter = "전체";
        public string StatusFilter { get => _statusFilter; set => Set(ref _statusFilter, value); }

        // ------------------------------------------------------------
        // 조회 결과
        // ------------------------------------------------------------
        public ObservableCollection<WorkOrderRow> Rows { get; } = [];

        private string _statusText = "";
        public string StatusText { get => _statusText; set => Set(ref _statusText, value); }

        // ------------------------------------------------------------
        // 등록 입력값
        // ------------------------------------------------------------
        public ObservableCollection<CodeItem> Items { get; } = [];
        public ObservableCollection<CodeItem> Equips { get; } = [];

        private CodeItem? _selectedItem;
        public CodeItem? SelectedItem { get => _selectedItem; set => Set(ref _selectedItem, value); }

        private CodeItem? _selectedEquip;
        public CodeItem? SelectedEquip { get => _selectedEquip; set => Set(ref _selectedEquip, value); }

        private string _woNo = "";
        public string WoNo { get => _woNo; set => Set(ref _woNo, value); }

        private DateTime _planDate = DateTime.Today;
        public DateTime PlanDate
        {
            get => _planDate;
            set { if (Set(ref _planDate, value)) MakeNextWoNo(); }   // 날짜가 바뀌면 번호 다시 채번
        }

        private int _orderQty = 1000;
        public int OrderQty { get => _orderQty; set => Set(ref _orderQty, value); }

        // ------------------------------------------------------------
        // 버튼
        // ------------------------------------------------------------
        public RelayCommand SearchCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand ResetCommand { get; }

        public WorkOrderViewModel()
        {
            SearchCommand = new RelayCommand(Search);
            ResetCommand = new RelayCommand(MakeNextWoNo);

            // 두 번째 인자가 false 를 돌려주면 버튼이 자동으로 흐려진다.
            // Enabled 를 직접 켜고 끄지 않아도 되는 것이 WPF 바인딩의 장점.
            SaveCommand = new RelayCommand(Save, () => SelectedItem != null && OrderQty > 0);

            LoadCombos();
            MakeNextWoNo();
            Search();
        }

        private void LoadCombos()
        {
            const string itemSql = @"
            SELECT ITEM_CD, ITEM_NM + ' (캐비티 ' + CAST(CAVITY AS NVARCHAR) + ')'
            FROM dbo.MST_ITEM WHERE USE_YN = 'Y' ORDER BY ITEM_CD";

            const string equipSql = @"
            SELECT EQUIP_CD, EQUIP_NM
            FROM dbo.MST_EQUIP WHERE USE_YN = 'Y' ORDER BY EQUIP_CD";

            try
            {
                foreach (var x in Db.Query(itemSql, r => new CodeItem { Code = r.GetString(0), Name = r.GetString(1) }))
                    Items.Add(x);
                foreach (var x in Db.Query(equipSql, r => new CodeItem { Code = r.GetString(0), Name = r.GetString(1) }))
                    Equips.Add(x);

                SelectedItem = Items.FirstOrDefault();
                SelectedEquip = Equips.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show("기준정보 조회 실패\n\n" + ex.Message);
            }
        }

        private void Search()
        {
            const string sql = @"
            SELECT  W.WO_NO,
                    W.PLAN_DT,
                    I.ITEM_NM,
                    ISNULL(E.EQUIP_NM, ''),
                    W.ORDER_QTY,
                    W.PROD_QTY,
                    W.ORDER_QTY - W.PROD_QTY,
                    W.ORDER_QTY / I.CAVITY,
                    W.WO_STATUS
            FROM        dbo.TB_WORK_ORDER W
            INNER JOIN  dbo.MST_ITEM      I ON I.ITEM_CD  = W.ITEM_CD
            LEFT  JOIN  dbo.MST_EQUIP     E ON E.EQUIP_CD = W.EQUIP_CD
            WHERE   W.PLAN_DT BETWEEN @From AND @To
              AND  (@Status = '전체' OR W.WO_STATUS = @Status)
            ORDER BY W.PLAN_DT DESC, W.WO_NO DESC";

            try
            {
                var rows = Db.Query(sql, r => new WorkOrderRow
                {
                    WoNo = r.GetString(0),
                    PlanDate = r.GetDateTime(1),
                    ItemName = r.GetString(2),
                    EquipName = r.GetString(3),
                    OrderQty = r.GetInt32(4),
                    ProdQty = r.GetInt32(5),
                    RemainQty = r.GetInt32(6),
                    ShotNeeded = r.GetInt32(7),
                    Status = r.GetString(8)
                },
                ("@From", FromDate.Date),
                ("@To", ToDate.Date),
                ("@Status", StatusFilter));

                Rows.Clear();
                foreach (var row in rows) Rows.Add(row);

                StatusText = $"{Rows.Count}건 조회됨";
            }
            catch (Exception ex)
            {
                MessageBox.Show("조회 실패\n\n" + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // 지시번호 자동 채번
        // ------------------------------------------------------------
        private void MakeNextWoNo()
        {
            var prefix = "WO" + PlanDate.ToString("yyyyMMdd");
            try
            {
                var next = Db.Scalar<int>(@"
                SELECT ISNULL(MAX(CAST(RIGHT(WO_NO, 3) AS INT)), 0) + 1
                FROM dbo.TB_WORK_ORDER
                WHERE WO_NO LIKE @Prefix + '%'", ("@Prefix", prefix));

                WoNo = prefix + next.ToString("D3");
            }
            catch (Exception ex)
            {
                MessageBox.Show("채번 실패\n\n" + ex.Message);
            }
        }

        private void Save()
        {
            const string sql = @"
            INSERT INTO dbo.TB_WORK_ORDER
                   (WO_NO, ITEM_CD, EQUIP_CD, PLAN_DT, ORDER_QTY, WO_STATUS, REG_USER)
            VALUES (@WoNo, @ItemCd, @EquipCd, @PlanDt, @OrderQty, 'WAIT', @RegUser)";

            try
            {
                Db.Execute(sql,
                    ("@WoNo", WoNo),
                    ("@ItemCd", SelectedItem!.Code),
                    ("@EquipCd", SelectedEquip?.Code),
                    ("@PlanDt", PlanDate.Date),
                    ("@OrderQty", OrderQty),
                    ("@RegUser", Environment.UserName));

                StatusText = $"{WoNo} 저장됨";
                MakeNextWoNo();
                Search();
            }
            catch (Exception ex)
            {
                MessageBox.Show("저장 실패\n\n" + ex.Message);
            }
        }
    }
}
