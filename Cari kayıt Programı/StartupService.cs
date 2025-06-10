using AutoUpdaterDotNET;
using System;
using System.IO;
using System.Windows;

namespace Cari_kayıt_Programı
{
    public class StartupService
    {

        public StartupService()
        {
            try
            {
                AppUpdateCheck(); // Uygulama güncelleme kontrolü

                CheckAndCreateAppDataFolders();

                if (!InitializeApplication())
                {
                    MessageBox.Show("Program başlatılamadı. Bağlantı yapılamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(1);
                    return;
                }

                LogManager.LogInformation(message: "Uygulama başlatılıyor...", className: "StartupService", methodName: "StartupService()");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "StartupService", methodName: "StartupService()", stackTrace: ex.StackTrace);
                throw;
            }
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
                AutoUpdater.Mandatory = false; // Zorunlu olmasın
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

        public static void CheckAndCreateAppDataFolders()
        {
            try
            {
                // Önce AppData klasörü varsa kontrol et yoksa oluştur
                if (!Directory.Exists(ConfigManager.AppDataPath))
                {
                    Directory.CreateDirectory(ConfigManager.AppDataPath);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "StartupService", methodName: "CheckAndCreateAppDataFolders()", stackTrace: ex.StackTrace);
                throw;
            }
        }
    }
}
