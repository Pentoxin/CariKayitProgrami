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

        // MySQL ayarlarının ini dosya yolu
        public static string MySqlIniPath
        {
            get
            {
                return Path.Combine(AppDataPath, "mysqlsettings.ini");
            }
        }

        private static IniFile Ini => new IniFile();

        public static string Server => Ini.Read("Server", "Connection");
        public static string Port => Ini.Read("Port", "Connection");
        public static string User => Ini.Read("User", "Connection");
        public static string Password => Ini.ReadDecrypted("Password", "Connection");

        public static string ServerConnection =>
            $"server={Server};user={User};password={Password};port={Port};";

        public static string DbConnection =>
            $"server={Server};user={User};password={Password};port={Port};database=CariTakipDB;AllowPublicKeyRetrieval=True;SslMode=Preferred;";
    }
}
