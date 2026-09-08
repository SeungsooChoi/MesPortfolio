using System.Windows.Controls;

namespace MesOffice
{
    /// <summary>
    /// BaseInfoView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BaseInfoView : UserControl
    {
        public BaseInfoView()
        {
            InitializeComponent();

            ItemTab.DataContext = new ItemViewModel();
            EquipTab.DataContext = new EquipViewModel();
        }
    }
}
