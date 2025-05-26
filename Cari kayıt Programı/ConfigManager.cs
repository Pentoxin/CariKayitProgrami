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
        public static string ServerConnection
        {
            get
            {
                return "server=localhost;user=root;password=11521152;";
            }
        }
        
        public static string DbConnection
        {
            get
            {
                return "server=localhost;user=root;password=11521152;database=CariTakipDB;";
            }
        }
    }
}
