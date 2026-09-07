using System.Windows;
using System.Windows.Controls;

namespace MesOffice
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Host.Content = new WorkOrderView();
        }

        /// <summary>
        /// 화면 전환
        /// 이 방식으로 만든 후 MVVM 적용 예정
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button)?.Tag as string;

            Host.Content = tag switch
            {
                "WO" => new WorkOrderView(),
                "BASE" => Placeholder("품목 / 설비 관리"),
                "RESULT" => Placeholder("생산실적 조회"),
                _ => Host.Content
            };
        }
        private static UIElement Placeholder(string text) => new TextBlock
        {
            Text = text,
            FontSize = 16,
            Foreground = System.Windows.Media.Brushes.Gray,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };
    }
}