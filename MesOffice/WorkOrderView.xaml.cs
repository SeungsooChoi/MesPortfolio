using System.Windows.Controls;

namespace MesOffice
{
    /// <summary>
    /// WorkOrderView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WorkOrderView : UserControl
    {
        public WorkOrderView()
        {
            InitializeComponent();

            DataContext = new WorkOrderViewModel();
        }
    }
}
