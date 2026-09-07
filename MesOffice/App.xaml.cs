using MesCore;
using Microsoft.Extensions.Configuration;
using System.Windows;

namespace MesOffice
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            var connStr = config.GetConnectionString("Mes");
            if (string.IsNullOrWhiteSpace(connStr))
            {
                MessageBox.Show("appsettings.json 에 ConnectionStrings:Mes 가 없습니다.");
                Shutdown();
                return;
            }

            Db.Init(connStr);

            try
            {
                Db.Scalar<int>("SELECT 1");
            }
            catch (Exception ex)
            {
                MessageBox.Show("DB 접속 실패\n\n" + ex.Message, "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            new MainWindow().Show();
        }
    }

}
