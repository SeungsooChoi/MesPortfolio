using MesCore;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Windows;

namespace MesOffice
{
    internal class ItemViewModel : ViewModelBase
    {
        // 우선 로그인 기능 없으므로 고정.
        private const string LoginUser = "admin";

        // 조회 결과
        public ObservableCollection<ItemRow> Rows { get; } = [];

        private ItemRow? _selectedRow;
        public ItemRow? SelectedRow
        {
            get => _selectedRow;
            set
            {
                if (_selectedRow == value) return;
                _selectedRow = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ToggleUseText));
                if (value != null) LoadToForm(value);
            }
        }

        private string _statusText = "";
        public string StatusText 
        { 
            get => _statusText; 
            set 
            { 
                if(_statusText == value) return;
                _statusText = value;
                OnPropertyChanged();
            } 
        }

        // 입력값
        public string[] ItemTypes { get; } = ["FG", "SF", "RM"];

        private string _itemCd = "";
        public string ItemCd { get => _itemCd; set => Set(ref _itemCd, value); }

        private string _itemNm = "";
        public string ItemNm { get => _itemNm; set => Set(ref _itemNm, value); }

        private string _itemType = "FG";
        public string ItemType { get => _itemType; set => Set(ref _itemType, value); }

        private string _unit = "EA";
        public string Unit { get => _unit; set => Set(ref _unit, value); }

        private int _cavity = 1;
        public int Cavity { get => _cavity; set => Set(ref _cavity, value); }

        private decimal _stdCycleSec = 20m;
        public decimal StdCycleSec { get => _stdCycleSec; set => Set(ref _stdCycleSec, value); }

        // 신규 등록 모드인지 확인
        private bool _isNew = true;
        public bool IsNew
        {
            get => _isNew;
            set { if (Set(ref _isNew, value)) OnPropertyChanged(nameof(IsCodeLocked)); }
        }

        public bool IsCodeLocked => !IsNew;

        public string ToggleUseText => SelectedRow is { UseYn: "N" } ? "사용재개" : "사용중지";

        // ------------------------------------------------------------
        // 버튼
        // ------------------------------------------------------------
        public RelayCommand SearchCommand { get; }
        public RelayCommand NewCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand ToggleUseCommand { get; }
        public ItemViewModel()
        {
            SearchCommand = new RelayCommand(Search);
            NewCommand = new RelayCommand(ClearForm);

            SaveCommand = new RelayCommand(Save, () =>
                !string.IsNullOrWhiteSpace(ItemCd) &&
                !string.IsNullOrWhiteSpace(ItemNm) &&
                Cavity > 0 && StdCycleSec > 0);

            ToggleUseCommand = new RelayCommand(ToggleUse, () => SelectedRow != null);

            Search();
        }

        // 조회
        private void Search()
        {
            // 중지된 품목도 보여야 함.
            // 작업지시 화면의 콤보에서만 USE_YN = 'Y' 로 걸러낸다.
            const string sql = """
            SELECT ITEM_CD, ITEM_NM, ITEM_TYPE, UNIT, CAVITY, STD_CYCLE_SEC, USE_YN
              FROM dbo.MST_ITEM
             ORDER BY ITEM_CD
            """;

            try
            {
                var list = Db.Query(sql, r => new ItemRow
                {
                    ItemCd = r.GetString(0),
                    ItemNm = r.GetString(1),
                    ItemType = r.GetString(2),
                    Unit = r.IsDBNull(3) ? "EA" : r.GetString(3),
                    Cavity = r.GetInt32(4),
                    StdCycleSec = r.GetDecimal(5),
                    UseYn = r.GetString(6)
                });

                Rows.Clear();
                foreach (var row in list) Rows.Add(row);

                StatusText = $"{Rows.Count}건 (사용중 {Rows.Count(x => x.IsUsed)}건)";
            }
            catch (Exception ex)
            {
                Fail("조회 실패", ex);
            }
        }

        // 저장 - 신규면 INSERT, 수정이면 UPDATE
        private void Save()
        {
            const string insert = """
            INSERT INTO dbo.MST_ITEM
                   (ITEM_CD, ITEM_NM, ITEM_TYPE, UNIT, CAVITY, STD_CYCLE_SEC,
                    USE_YN, REG_USER, REG_DT)
            VALUES (@Cd, @Nm, @Type, @Unit, @Cavity, @Cycle,
                    'Y', @User, SYSDATETIME())
            """;

            // ITEM_CD 는 SET 절에 없다. PK 는 바꾸지 않는다.
            const string update = """
            UPDATE dbo.MST_ITEM
               SET ITEM_NM       = @Nm,
                   ITEM_TYPE     = @Type,
                   UNIT          = @Unit,
                   CAVITY        = @Cavity,
                   STD_CYCLE_SEC = @Cycle,
                   UPD_USER      = @User,
                   UPD_DT        = SYSDATETIME()
             WHERE ITEM_CD = @Cd
            """;

            var code = ItemCd.Trim();

            try
            {
                Db.Execute(IsNew ? insert : update,
                    ("@Cd", code),
                    ("@Nm", ItemNm.Trim()),
                    ("@Type", ItemType),
                    ("@Unit", Unit.Trim()),
                    ("@Cavity", Cavity),
                    ("@Cycle", StdCycleSec),
                    ("@User", LoginUser));
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                MessageBox.Show($"이미 등록된 품목코드입니다: {code}",
                    "저장 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            catch (Exception ex)
            {
                Fail("저장 실패", ex);
                return;
            }

            Search();
            SelectedRow = Rows.FirstOrDefault(x => x.ItemCd == code);
            StatusText = $"{code} 저장됨";
        }

        // 사용중지 / 사용재개 - 삭제 대신 사용
        private void ToggleUse()
        {
            if (SelectedRow is null) return;

            var code = SelectedRow.ItemCd;
            var nextYn = SelectedRow.IsUsed ? "N" : "Y";
            var word = nextYn == "N" ? "사용중지" : "사용재개";

            var answer = MessageBox.Show(
                $"{code} 을(를) {word} 하시겠습니까?\n\n" +
                (nextYn == "N"
                    ? "삭제가 아니라 상태만 바꿉니다.\n과거 실적 조회는 그대로 유지되고, 새 작업지시에서만 선택할 수 없게 됩니다."
                    : "다시 작업지시에서 선택할 수 있게 됩니다."),
                word, MessageBoxButton.OKCancel, MessageBoxImage.Question);

            if (answer != MessageBoxResult.OK) return;

            const string sql = """
            UPDATE dbo.MST_ITEM
               SET USE_YN   = @Yn,
                   UPD_USER = @User,
                   UPD_DT   = SYSDATETIME()
             WHERE ITEM_CD = @Cd
            """;

            try
            {
                Db.Execute(sql, ("@Yn", nextYn), ("@Cd", code), ("@User", LoginUser));
            }
            catch (Exception ex)
            {
                Fail(word + " 실패", ex);
                return;
            }

            Search();
            SelectedRow = Rows.FirstOrDefault(x => x.ItemCd == code);
            StatusText = $"{code} {word}됨";
        }

        private void LoadToForm(ItemRow row)
        {
            ItemCd = row.ItemCd;
            ItemNm = row.ItemNm;
            ItemType = row.ItemType;
            Unit = row.Unit;
            Cavity = row.Cavity;
            StdCycleSec = row.StdCycleSec;
            IsNew = false;          // 코드 칸이 잠금
        }

        private void ClearForm()
        {
            SelectedRow = null;
            ItemCd = "";
            ItemNm = "";
            ItemType = "FG";
            Unit = "EA";
            Cavity = 1;
            StdCycleSec = 20m;
            IsNew = true;
        }

        private void Fail(string title, Exception ex)
        {
            StatusText = title;
            MessageBox.Show(ex.Message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
