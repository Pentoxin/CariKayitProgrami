using System;
using System.IO;

namespace Cari_kayıt_Programı
{
    internal static class Config
    {
        public static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Cari Kayıt Programı");
        public static readonly string IsletmePath = Path.Combine(AppDataPath, "İşletmeler");
        public static readonly string DatabaseFileName = Path.Combine(AppDataPath, "CariKayitDB.db");
        public static readonly string LogFilePath = Path.Combine(AppDataPath, "log.txt");
        public static readonly string ConnectionString = $"Data Source={DatabaseFileName};Version=3;";
        public static readonly string dosyaAdi = Path.Combine(AppDataPath, "cari_kayit_programi_setup.exe");
    }
}
