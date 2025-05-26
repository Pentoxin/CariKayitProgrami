using AutoUpdaterDotNET;
using Serilog;
using System.Windows;

namespace Cari_kayıt_Programı
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AutoUpdater.Mandatory = false; // Zorunlu olmasın
            AutoUpdater.ShowRemindLaterButton = true; // Daha sonra hatırlat
            AutoUpdater.ShowSkipButton = true; // Güncellemeyi atla
            AutoUpdater.UpdateMode = Mode.Normal; // Kullanıcıya arayüz gösterilir
            AutoUpdater.Start("https://raw.githubusercontent.com/Pentoxin/CariKayitProgrami/main/update.xml");

            var db = new DatabaseManager();
            db.InitializeDatabase();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
