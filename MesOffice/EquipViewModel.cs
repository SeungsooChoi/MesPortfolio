using MesCore;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Windows;

namespace MesOffice
{
    internal class EquipViewModel : ViewModelBase
    {
        // 우선 로그인 기능 없으므로 고정.
        private const string LoginUser = "admin";

        // 조회 결과
        public ObservableCollection<EquipRow> Rows { get; } = [];

        private EquipRow? _selectedRow;
        public EquipRow? SelectedRow
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
                if (_statusText == value) return;
                _statusText = value;
                OnPropertyChanged();
            }
        }

        // 입력값
        private string _equipCd = "";
        public string EquipCd { get => _equipCd; set => Set(ref _equipCd, value); }

        private string _equipNm = "";
        public string EquipNm { get => _equipNm; set => Set(ref _equipNm, value); }

        private string _lineCd = "A";
        public string LineCd { get => _lineCd; set => Set(ref _lineCd, value); }

        private int _tonnage = 150;
        public int Tonnage { get => _tonnage; set => Set(ref _tonnage, value); }

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
        public EquipViewModel()
        {
            SearchCommand = new RelayCommand(Search);
            NewCommand = new RelayCommand(ClearForm);

            SaveCommand = new RelayCommand(Save, () =>
                !string.IsNullOrWhiteSpace(EquipCd) &&
                !string.IsNullOrWhiteSpace(EquipNm) &&
                Tonnage > 0);

            ToggleUseCommand = new RelayCommand(ToggleUse, () => SelectedRow != null);

            Search();
        }

        // 조회
        private void Search()
        {
            const string sql = """
            SELECT EQUIP_CD, EQUIP_NM, LINE_CD, TONNAGE, USE_YN
              FROM dbo.MST_EQUIP
             ORDER BY EQUIP_CD
            """;

            try
            {
                // LINE_CD 와 TONNAGE 는 NOT NULL 이 아니다. IsDBNull 확인이 필요함
                var list = Db.Query(sql, r => new EquipRow
                {
                    EquipCd = r.GetString(0),
                    EquipNm = r.GetString(1),
                    LineCd = r.IsDBNull(2) ? null : r.GetString(2),
                    Tonnage = r.IsDBNull(3) ? null : r.GetInt32(3),
                    UseYn = r.GetString(4)
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

        // 저장 
        private void Save()
        {
           const string insert = """
            INSERT INTO dbo.MST_EQUIP
                   (EQUIP_CD, EQUIP_NM, LINE_CD, TONNAGE, USE_YN, REG_USER, REG_DT)
            VALUES (@Cd, @Nm, @Line, @Ton, 'Y', @User, SYSDATETIME())
            """;

           const string update = """
            UPDATE dbo.MST_EQUIP
               SET EQUIP_NM = @Nm,
                   LINE_CD  = @Line,
                   TONNAGE  = @Ton,
                   UPD_USER = @User,
                   UPD_DT   = SYSDATETIME()
             WHERE EQUIP_CD = @Cd
            """;

            var code = EquipCd.Trim();

            try
            {
                Db.Execute(IsNew ? insert : update,
                    ("@Cd", code),
                    ("@Nm", EquipNm.Trim()),
                    ("@Line", LineCd.Trim()),
                    ("@Ton", Tonnage),
                    ("@User", LoginUser));
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                MessageBox.Show($"이미 등록된 설비코드입니다: {code}",
                    "저장 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            catch (Exception ex)
            {
                Fail("저장 실패", ex);
                return;
            }

            Search();
            SelectedRow = Rows.FirstOrDefault(x => x.EquipCd == code);
            StatusText = $"{code} 저장됨";
        }

        // 사용중지 / 사용재개 
        private void ToggleUse()
        {
            if (SelectedRow is null) return;

            var code = SelectedRow.EquipCd;
            var nextYn = SelectedRow.IsUsed ? "N" : "Y";
            var word = nextYn == "N" ? "사용중지" : "사용재개";

            var answer = MessageBox.Show(
                $"{code} 을(를) {word} 하시겠습니까?\n\n" +
                (nextYn == "N"
                    ? "이 설비로 설정된 POP 단말이 있으면 새 작업지시를 배정할 수 없게 됩니다."
                    : "다시 작업지시에 배정할 수 있게 됩니다."),
                word, MessageBoxButton.OKCancel, MessageBoxImage.Question);

            if (answer != MessageBoxResult.OK) return;

            const string sql = """
            UPDATE dbo.MST_EQUIP
               SET USE_YN   = @Yn,
                   UPD_USER = @User,
                   UPD_DT   = SYSDATETIME()
             WHERE EQUIP_CD = @Cd
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
            SelectedRow = Rows.FirstOrDefault(x => x.EquipCd == code);
            StatusText = $"{code} {word}됨";
        }

        private void LoadToForm(EquipRow row)
        {
            EquipCd = row.EquipCd;
            EquipNm = row.EquipNm;
            LineCd = row.LineCd ?? "";
            Tonnage = row.Tonnage ?? 0;
            IsNew = false;
        }

        private void ClearForm()
        {
            SelectedRow = null;
            EquipCd = "";
            EquipNm = "";
            LineCd = "A";
            Tonnage = 150;
            IsNew = true;
        }

        private void Fail(string title, Exception ex)
        {
            StatusText = title;
            MessageBox.Show(ex.Message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
