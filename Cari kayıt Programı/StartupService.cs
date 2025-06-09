using AutoUpdaterDotNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cari_kayıt_Programı
{
    public class StartupService
    {

        public StartupService()
        {
            // LogManager.Initialize(); // Loglama sistemi başlatılabilir
            AppUpdateCheck(); // Uygulama güncelleme kontrolü
        }

        public bool InitializeApplication()
        {
            if (!CheckDatabase())
            {
                var settingsWindow = new MySqlSettingsWindow();
                if (settingsWindow.ShowDialog() != true)
                    return false;

                if (!CheckDatabase())
                    return false;
            }
            return true;
        }

        private bool CheckDatabase()
        {
            try
            {
                var db = new DatabaseManager();
                db.InitializeDatabase();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void AppUpdateCheck()
        {
            try
            {
                AutoUpdater.Mandatory = true; // Zorunlu olmasın
                AutoUpdater.ShowRemindLaterButton = true; // Daha sonra hatırlat
                AutoUpdater.ShowSkipButton = false; // Güncellemeyi atla
                AutoUpdater.UpdateMode = Mode.Forced; // Kullanıcıya arayüz gösterilir
                AutoUpdater.Start("https://raw.githubusercontent.com/Pentoxin/CariKayitProgrami/refs/heads/master/update.xml");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "StartupService", methodName: "AppUpdateCheck()", stackTrace: ex.StackTrace);
            }
        }
    }
}
