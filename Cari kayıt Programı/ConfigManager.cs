using System;
using System.Configuration;
using System.IO;

namespace Cari_kayıt_Programı
{
    public static class ConfigManager
    {
        // Uygulama verileri için tam yol
        public static string AppDataPath
        {
            get
            {
                string appDataPath = ConfigurationManager.AppSettings["AppDataPath"];
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appDataPath);
            }
        }

        // İşletme dosyasının tam yolu
        public static string IsletmePath
        {
            get
            {
                string isletmePath = ConfigurationManager.AppSettings["IsletmePath"];
                return Path.Combine(AppDataPath, isletmePath);
            }
        }

        // Veritabanı dosyasının tam yolu
        public static string DatabaseFileName
        {
            get
            {
                string databaseFileName = ConfigurationManager.AppSettings["DatabaseFileName"];
                return Path.Combine(AppDataPath, databaseFileName);
            }
        }

        // Log dosyasının tam yolu
        public static string LogFilePath
        {
            get
            {
                string logFilePath = ConfigurationManager.AppSettings["LogFilePath"];
                return Path.Combine(AppDataPath, logFilePath);
            }
        }

        // Connection String tam yolu
        public static string ConnectionString
        {
            get
            {
                return $"Data Source={DatabaseFileName};Version=3;";
            }
        }

        // ExecutableFileName tam yolu
        public static string ExecutableFileName
        {
            get
            {
                string executableFileName = ConfigurationManager.AppSettings["ExecutableFileName"];
                return Path.Combine(AppDataPath, executableFileName);
            }
        }
        
        public static string BackupPath
        {
            get
            {
                string backupPath = ConfigurationManager.AppSettings["BackupPath"];
                return Path.Combine(AppDataPath, backupPath);
            }
        }
    }
}
