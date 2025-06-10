using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Cari_kayıt_Programı
{
    public class IniFile
    {
        public string Path { get; }

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def,
            StringBuilder retVal, int size, string filePath);

        public IniFile()
        {
            Path = ConfigManager.MySqlIniPath;

            var dir = System.IO.Path.GetDirectoryName(Path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public void Write(string key, string value, string section)
        {
            WritePrivateProfileString(section, key, value, Path);
        }

        public string Read(string key, string section)
        {
            var retVal = new StringBuilder(255);
            GetPrivateProfileString(section, key, "", retVal, 255, Path);
            return retVal.ToString();
        }

        // Yeni: Base64 ile Şifreli Yaz
        public void WriteEncrypted(string key, string plainValue, string section)
        {
            string encrypted = Convert.ToBase64String(Encoding.UTF8.GetBytes(plainValue));
            Write(key, encrypted, section);
        }

        // Yeni: Base64 ile Çözerek Oku
        public string ReadDecrypted(string key, string section)
        {
            string encrypted = Read(key, section);
            try
            {
                byte[] bytes = Convert.FromBase64String(encrypted);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        public bool Exists => File.Exists(Path);
    }
}
