using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using static Cari_kayıt_Programı.CariHesapKayitlari;

namespace Cari_kayıt_Programı
{
    public partial class Anasayfa : Window
    {
        public MainViewModel ViewModel { get; set; }

        public Anasayfa()
        {
            try
            {
                InitializeComponent();

                ViewModel = new MainViewModel();
                DataContext = ViewModel;

                LogManager.LogInformation(message: "Program Başlatıldı", className: "Program", methodName: "Main");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Program", methodName: "Main", stackTrace: ex.StackTrace);
                MessageBox.Show("Beklenmeyen bir hata oluştu. Lütfen destek ekibiyle iletişime geçin.", "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckAtStartup();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Anasayfa", methodName: "Window_Loaded()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        public void CheckAtStartup()
        {
            try
            {
                Check check = new Check(this);

                //_ = GuncellemeKontrol();
                Check.AppDataCreate();
                Check.InitializeDatabase();
                Check.CariRenameColumnAndRemoveMail2();
                BusinessesAtStartupCheck();
                check.CheckAndUpdateTables();
                check.CheckAndCreateOrRenameIsletmeFolders();
                Check.CheckAndCreateStokTable();

                LogManager.LogInformation(message: "Tüm başlangıç kontrolleri tamamlandı.", className: "Anasayfa", methodName: "CheckAtStartup()");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Anasayfa", methodName: "CheckAtStartup()", stackTrace: ex.StackTrace);
                throw;
            }

        }

        public void CheckAtImport()
        {
            try
            {
                Check check = new Check(this);

                Check.AppDataCreate();
                Check.InitializeDatabase();
                Check.CariRenameColumnAndRemoveMail2();
                BusinessesAtStartupCheck();
                check.CheckAndUpdateTables();
                check.CheckAndCreateOrRenameIsletmeFolders();
                Check.CheckAndCreateStokTable();

                LogManager.LogInformation(message: "Tüm içe aktarma kontrolleri tamamlandı.", className: "Anasayfa", methodName: "CheckAtStartup()");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Anasayfa", methodName: "CheckAtImport()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        public class Check
        {
            private readonly Anasayfa _anasayfa;
            public Check(Anasayfa anasayfa)
            {
                _anasayfa = anasayfa;
            }

            public void CheckAndUpdateTables()
            {
                try
                {
                    MainViewModel viewModel = (MainViewModel)_anasayfa.DataContext;

                    var cariKodList = new List<string?>();
                    var missingCariKod = new List<int>();

                    foreach (var item in viewModel.Businesses)
                    {
                        if (item is Business business && string.IsNullOrEmpty(business.CariKod))
                        {
                            missingCariKod.Add(business.ID);
                        }
                    }

                    if (missingCariKod.Count > 0)
                    {
                        CheckAndPromptForMissingCariKod();
                    }

                    ManageCariKodTables();

                    foreach (var item in viewModel.Businesses)
                    {
                        if (item is Business business)
                        {
                            cariKodList.Add(business.CariKod);
                        }
                    }

                    foreach (string? kod in cariKodList)
                    {
                        string tableName = $"Cari_{kod}";
                        string escapedTableName = $"\"{tableName}\"";
                        AddHareketlerMissingColumns(escapedTableName);
                    }

                    AddCariMissingColumns();

                    foreach (string? kod in cariKodList)
                    {
                        string tableName = $"Cari_{kod}";
                        string escapedTableName = $"\"{tableName}\"";
                        UpdateDateFormatInTable(escapedTableName);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "CheckAndUpdateTables()", stackTrace: ex.StackTrace);
                    throw;
                }
            }

            public static void CheckAndCreateStokTable()
            {
                try
                {
                    using (var connection = new SQLiteConnection(ConfigManager.ConnectionString))
                    {
                        connection.Open();

                        // Stok tablosu var mı kontrol et
                        string checkTableQuery = "SELECT name FROM sqlite_master WHERE type='table' AND name='Stok';";
                        using (var command = new SQLiteCommand(checkTableQuery, connection))
                        {
                            var result = command.ExecuteScalar();
                            if (result == null)
                            {
                                // Stok tablosu yoksa oluştur
                                string createTableQuery = @"
                                    CREATE TABLE IF NOT EXISTS 'Stok' (
	                                    'ID' INTEGER PRIMARY KEY AUTOINCREMENT,
	                                    'stokkodu' TEXT,
	                                    'stokadi' TEXT,
	                                    'kdvsatis' INTEGER,
	                                    'kdvalis' INTEGER,
	                                    'olcubirimi1' TEXT,
	                                    'olcubirimi2' TEXT,
	                                    'olcubirimi2oran' TEXT,
	                                    'stokmiktar' INTEGER,
	                                    'gelenmiktar' INTEGER,
	                                    'gelentutar' INTEGER,
	                                    'gidenmiktar' INTEGER,
	                                    'gidentutar' INTEGER);";

                                using (var createCommand = new SQLiteCommand(createTableQuery, connection))
                                {
                                    createCommand.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                    LogManager.LogInformation(message: "Stok tablosunun kontrolü yapıldı.", className: "Anasayfa / Check", methodName: "CheckAndCreateStokTable");
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "CheckAndCreateStokTable()", stackTrace: ex.StackTrace);
                    throw;
                }
            }

            public void ManageCariKodTables()
            {
                try
                {
                    using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                    {
                        connection.Open();

                        MainViewModel viewModel = (MainViewModel)_anasayfa.DataContext;

                        // Mevcut tablo isimlerini kontrol et
                        string query = "SELECT name FROM sqlite_master WHERE type='table' AND name LIKE 'Cari_%'";
                        using (SQLiteCommand command = new SQLiteCommand(query, connection))
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            List<string> tablesToRename = new List<string>();
                            while (reader.Read())
                            {
                                string tableName = reader.GetString(0);
                                if (tableName.StartsWith("Cari_") && int.TryParse(tableName.Substring(5), out _))
                                {
                                    tablesToRename.Add(tableName);
                                }
                            }

                            foreach (string oldTableName in tablesToRename)
                            {
                                string idPart = oldTableName.Substring(5);
                                string newCariKod = GetCariKodFromId(idPart);
                                if (!string.IsNullOrEmpty(newCariKod))
                                {
                                    string newTableName = $"Cari_{newCariKod}";
                                    string escapedTableName = $"\"{newTableName}\"";
                                    RenameTable(connection, oldTableName, escapedTableName);
                                }
                            }
                        }

                        LogManager.LogInformation(message: "Eksik tablolar kontrol ediliyor.", className: "Anasayfa / Check", methodName: "ManageCariKodTables()");

                        // Her bir iş için CariKod tablosunu oluştur veya var olup olmadığını kontrol et
                        foreach (var business in viewModel.Businesses)
                        {
                            if (business is Business b && b.CariKod != null)
                            {
                                string tableName = $"Cari_{b.CariKod}";

                                // Tablo adını çift tırnak içine alarak SQL için güvenli hale getirme
                                string escapedTableName = $"\"{tableName}\"";

                                // Tablo var mı kontrol et
                                string checkTableQuery = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';";
                                using (SQLiteCommand checkCommand = new SQLiteCommand(checkTableQuery, connection))
                                {
                                    var result = checkCommand.ExecuteScalar();

                                    // Eğer tablo yoksa oluştur
                                    if (result == null)
                                    {
                                        string createTableQuery = $@"
                                            CREATE TABLE {escapedTableName} (
                                                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                                                tarih TEXT,
                                                tip TEXT,
                                                evrakno TEXT,
                                                aciklama TEXT,
                                                vadetarihi TEXT,
                                                borc INTEGER,
                                                alacak INTEGER,
                                                dosya TEXT);";
                                        using (SQLiteCommand createCommand = new SQLiteCommand(createTableQuery, connection))
                                        {
                                            createCommand.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                        }

                        LogManager.LogInformation(message: "Eksik tablolar oluşturuldu.", className: "Anasayfa / Check", methodName: "ManageCariKodTables()");
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "ManageCariKodTables()", stackTrace: ex.StackTrace);
                    throw;
                }
            }

            private string GetCariKodFromId(string id)
            {
                try
                {
                    // ID'ye göre CariKod elde etme mantığını buraya yazın
                    // Örneğin:
                    MainViewModel viewModel = (MainViewModel)_anasayfa.DataContext;
                    var business = viewModel.Businesses.FirstOrDefault(b => b.ID.ToString() == id);
                    return business?.CariKod;
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "GetCariKodFromId()", stackTrace: ex.StackTrace);
                    throw;
                }
            }

            private static void RenameTable(SQLiteConnection connection, string oldTableName, string newTableName)
            {
                try
                {
                    string renameTableQuery = $"ALTER TABLE {oldTableName} RENAME TO {newTableName};";
                    using (SQLiteCommand renameCommand = new SQLiteCommand(renameTableQuery, connection))
                    {
                        renameCommand.ExecuteNonQuery();
                    }
                    LogManager.LogInformation(message: "Tablolar adları değiştirildi.", className: "Anasayfa / Check", methodName: "RenameTable()");
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "GetCariKodFromId()", stackTrace: ex.StackTrace);
                    throw;
                }
            }

            public void CheckAndPromptForMissingCariKod()
            {
                try
                {
                    MainViewModel viewModel = (MainViewModel)_anasayfa.DataContext;
                    var missingCariKodBusinesses = viewModel.Businesses.Where(b => string.IsNullOrEmpty(b.CariKod)).ToList();

                    foreach (var business in missingCariKodBusinesses)
                    {
                        CariKodOlusturma cariKodOlusturmaWindow = new CariKodOlusturma(_anasayfa);
                        cariKodOlusturmaWindow.Business = business;
                        if (cariKodOlusturmaWindow.ShowDialog() == true)
                        {
                            if (cariKodOlusturmaWindow.OtomatikGir)
                            {
                                return;
                            }
                            string? newCariKod = cariKodOlusturmaWindow.NewCariKod;
                            if (!string.IsNullOrEmpty(newCariKod))
                            {
                                UpdateCariKodInDatabase(business.ID, newCariKod);
                                business.CariKod = newCariKod;  // Güncellenmiş veriyi ViewModel'de de güncelle
                            }
                        }
                    }

                    // Eksik CariKod değerleri tekrar kontrol et
                    missingCariKodBusinesses = viewModel.Businesses.Where(b => string.IsNullOrEmpty(b.CariKod)).ToList();
                    if (missingCariKodBusinesses.Count > 0)
                    {
                        MessageBox.Show("Bazı kayıtlar hala eksik CariKod değerlerine sahip. Program kapanıyor.", "Eksik Veri", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "CheckAndPromptForMissingCariKod()", stackTrace: ex.StackTrace);
                    throw;
                }
            }

            public static void UpdateCariKodInDatabase(int businessId, string? newCariKod)
            {
                try
                {
                    string? tip = "D";
                    using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                    {
                        connection.Open();
                        string updateSql = "UPDATE CariKayit SET CariKod = @CariKod, tip = @tip WHERE ID = @ID";
                        using (SQLiteCommand command = new SQLiteCommand(updateSql, connection))
                        {
                            command.Parameters.AddWithValue("@CariKod", newCariKod);
                            command.Parameters.AddWithValue("@tip", tip);
                            command.Parameters.AddWithValue("@ID", businessId);
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "UpdateCariKodInDatabase()", stackTrace: ex.StackTrace);
                    throw;
                }
            }

            public static void UpdateDateFormatInTable(string tableName)
            {
                try
                {
                    using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                    {
                        connection.Open();

                        string query = $"SELECT ID, tarih, vadetarihi FROM {tableName}";
                        using (SQLiteCommand command = new SQLiteCommand(query, connection))
                        {
                            using (SQLiteDataReader reader = command.ExecuteReader())
                            {
                                var updateQueries = new List<string>();

                                while (reader.Read())
                                {
                                    int recordId = reader.GetInt32(0);
                                    string? tarihStr = reader.IsDBNull(1) ? null : reader.GetString(1);
                                    string? vadetarihiStr = reader.IsDBNull(2) ? null : reader.GetString(2);

                                    DateTime tarih;
                                    DateTime vadetarihi;

                                    bool tarihGuncelle = false;
                                    bool vadetarihiGuncelle = false;

                                    // Tarih formatlarını değiştir
                                    if (DateTime.TryParseExact(tarihStr, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out tarih))
                                    {
                                        tarihStr = tarih.ToString("dd.MM.yyyy");
                                        tarihGuncelle = true;
                                    }

                                    if (DateTime.TryParseExact(vadetarihiStr, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out vadetarihi))
                                    {
                                        vadetarihiStr = vadetarihi.ToString("dd.MM.yyyy");
                                        vadetarihiGuncelle = true;
                                    }

                                    // Güncelleme sorgularını sadece gerekli olduğunda ekle
                                    if (tarihGuncelle || vadetarihiGuncelle)
                                    {
                                        // Güncelleme sorgularını listeye ekle
                                        updateQueries.Add($"UPDATE {tableName} SET tarih = '{tarihStr}', vadetarihi = '{vadetarihiStr}' WHERE ID = {recordId}");
                                    }
                                }

                                // Güncelleme sorgularını çalıştır
                                foreach (var updateQuery in updateQueries)
                                {
                                    using (SQLiteCommand updateCommand = new SQLiteCommand(updateQuery, connection))
                                    {
                                        updateCommand.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                    }
                    LogManager.LogInformation(message: "Veritabanındaki tarih formatları düzeltiliyor.", className: "Anasayfa / Check", methodName: "UpdateDateFormatInTable()");
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "UpdateDateFormatInTable()", stackTrace: ex.StackTrace);
                    throw;
                }
            }

            public static List<string> HareketlerMissingColumns(string tableName)
            {
                List<string> requiredColumns = new List<string> { "ID", "tarih", "tip", "durum", "evrakno", "aciklama", "vadetarihi", "borc", "alacak", "dosya" };
                List<string> missingColumns = new List<string>();
                try
                {
                    using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                    {
                        connection.Open();

                        // PRAGMA table_info komutu ile sütunları sorgula
                        using (SQLiteCommand command = new SQLiteCommand($"PRAGMA table_info({tableName})", connection))
                        {
                            using (SQLiteDataReader reader = command.ExecuteReader())
                            {
                                List<string> existingColumns = new List<string>();
                                while (reader.Read())
                                {
                                    string existingColumnName = reader.GetString(1);
                                    existingColumns.Add(existingColumnName);
                                }

                                // Gerekli sütunların var olup olmadığını kontrol et
                                foreach (string column in requiredColumns)
                                {
                                    if (!existingColumns.Contains(column, StringComparer.OrdinalIgnoreCase))
                                    {
                                        missingColumns.Add(column);
                                    }
                                }
                            }
                        }
                    }
                    return missingColumns;
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "HareketlerMissingColumns()", stackTrace: ex.StackTrace);
                    return missingColumns;
                    throw;
                }
            }

            public static void AddHareketlerMissingColumns(string tableName)
            {
                List<string> missingColumns = HareketlerMissingColumns(tableName);
                try
                {
                    if (missingColumns.Count > 0)
                    {
                        using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                        {
                            connection.Open();

                            foreach (string columnName in missingColumns)
                            {
                                string columnType;

                                // Sütun adlarına göre sütun tiplerini belirleyin
                                if (columnName.ToLower() == "durum")
                                {
                                    AddColumnAtSpecificPosition(tableName, "durum", "TEXT", 4, "Açık");
                                    break;
                                }

                                switch (columnName.ToLower())
                                {
                                    case "ID":
                                        columnType = "INTEGER";
                                        break;
                                    case "tarih":
                                    case "vadetarihi":
                                        columnType = "TEXT";
                                        break;
                                    case "borc":
                                    case "alacak":
                                        columnType = "REAL";
                                        break;
                                    default:
                                        columnType = "TEXT";
                                        break;
                                }

                                // ALTER TABLE komutu ile sütunu ekleyin
                                string query = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType}";

                                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                                {
                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                    LogManager.LogInformation(message: "Hareketler tablosundaki eksikler belirlendi ve eksik sütunlar oluşturuldu.", className: "Anasayfa / Check", methodName: "AddHareketlerMissingColumns()");
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "AddHareketlerMissingColumns()", stackTrace: ex.StackTrace);
                    throw;
                }
            }

            public static void AddColumnAtSpecificPosition(string tableName, string newColumnName, string newColumnType, int position, string? defaultValue = null)
            {
                try
                {
                    using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                    {
                        connection.Open();

                        // Mevcut sütunları al
                        List<(string Name, string Type)> columns = new List<(string, string)>();
                        using (SQLiteCommand command = new SQLiteCommand($"PRAGMA table_info({tableName})", connection))
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                columns.Add((reader.GetString(1), reader.GetString(2))); // Sütun adı ve tipi
                            }
                        }

                        // Yeni sütunu belirtilen pozisyona ekle
                        columns.Insert(position - 1, (newColumnName, newColumnType));

                        // Geçici tablo oluşturma sorgusu
                        string temptableName = $"{tableName}_temp\"".Replace("\"", "");

                        string escapedTempTableName = $"\"{temptableName}\"";

                        string createTempTableQuery = $@" CREATE TABLE {escapedTempTableName} ({string.Join(", ", columns.Select((c, index) => index == 0 && c.Name.Equals("ID", StringComparison.OrdinalIgnoreCase)
                                        ? $"{c.Name} {c.Type} PRIMARY KEY AUTOINCREMENT" // İlk sütun ID ise PRIMARY KEY AUTOINCREMENT yap
                                        : $"{c.Name} {c.Type}"))})";

                        using (SQLiteCommand command = new SQLiteCommand(createTempTableQuery, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        // Verileri geçici tabloya kopyalama
                        string existingColumnNames = string.Join(", ", columns.Where(c => c.Name != newColumnName).Select(c => c.Name));
                        string newColumnDefaultValue = defaultValue != null ? $"'{defaultValue}'" : "NULL";

                        // Yeni sütunun pozisyonuna göre verileri sıralayın
                        var orderedColumnValues = columns.Select(c =>
                        {
                            if (c.Name == newColumnName)
                                return newColumnDefaultValue; // Yeni sütun için varsayılan değer ekle
                            return c.Name; // Mevcut sütunlar için orijinal sütun adı
                        });

                        string copyDataQuery = $@"INSERT INTO {escapedTempTableName} ({string.Join(", ", columns.Select(c => c.Name))}) SELECT {string.Join(", ", orderedColumnValues)} FROM {tableName}";
                        using (SQLiteCommand command = new SQLiteCommand(copyDataQuery, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        // Eski tabloyu sil ve geçici tabloyu yeniden adlandır
                        using (SQLiteCommand command = new SQLiteCommand($"DROP TABLE {tableName}", connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        using (SQLiteCommand command = new SQLiteCommand($"ALTER TABLE {escapedTempTableName} RENAME TO {tableName}", connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "AddColumnAtSpecificPosition()", stackTrace: ex.StackTrace);
                    throw;
                }
            }

            public static void CariRenameColumnAndRemoveMail2()
            {
                try
                {
                    using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                    {
                        connection.Open();

                        // Mevcut tablo yapısını kontrol et
                        bool tableNeedsUpdate = false;
                        using (SQLiteCommand command = new SQLiteCommand($"PRAGMA table_info(CariKayit)", connection))
                        {
                            using (SQLiteDataReader reader = command.ExecuteReader())
                            {
                                List<string> existingColumns = new List<string>();
                                while (reader.Read())
                                {
                                    string existingColumnName = reader.GetString(1);
                                    existingColumns.Add(existingColumnName);
                                }

                                // İstenen sütun adlarını kontrol et
                                if (!existingColumns.Contains("cariisim", StringComparer.OrdinalIgnoreCase) || existingColumns.Contains("isletmeadi", StringComparer.OrdinalIgnoreCase) || existingColumns.Contains("mail2", StringComparer.OrdinalIgnoreCase))
                                {
                                    tableNeedsUpdate = true;
                                }
                            }
                        }

                        if (tableNeedsUpdate)
                        {
                            // Geçici tablo oluşturma
                            string createTempTableQuery = @"
                                CREATE TABLE CariKayit_temp (
                                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                                carikod TEXT,
                                cariisim TEXT,
                                adres TEXT,
                                il TEXT,
                                ilce TEXT,
                                telefon1 TEXT,
                                telefon2 TEXT,
                                postakodu TEXT,
                                ulkekodu TEXT,
                                vergidairesi TEXT,
                                vergino TEXT,
                                tcno TEXT,
                                tip TEXT,
                                mail TEXT,
                                banka TEXT,
                                hesapno TEXT);";
                            using (SQLiteCommand command = new SQLiteCommand(createTempTableQuery, connection))
                            {
                                command.ExecuteNonQuery();
                            }

                            // Verileri geçici tabloya taşıma
                            string copyDataQuery = @"INSERT INTO CariKayit_temp (ID, carikod, cariisim, adres, il, ilce, telefon1, telefon2, postakodu, ulkekodu, vergidairesi, vergino, tcno, tip, mail, banka, hesapno)
                                SELECT ID,NULL as carikod, isletmeadi AS cariisim, adres, NULL AS il, NULL AS ilce, telefon1, telefon2, NULL AS postakodu, NULL AS ulkekodu, vergidairesi, vergino, NULL AS tcno, NULL AS tip, mail1 AS mail, banka, hesapno
                                FROM CariKayit;";
                            using (SQLiteCommand command = new SQLiteCommand(copyDataQuery, connection))
                            {
                                command.ExecuteNonQuery();
                            }

                            // Orijinal tabloyu silme
                            string dropOriginalTableQuery = "DROP TABLE CariKayit;";
                            using (SQLiteCommand command = new SQLiteCommand(dropOriginalTableQuery, connection))
                            {
                                command.ExecuteNonQuery();
                            }

                            // Geçici tabloyu yeniden adlandırma
                            string renameTempTableQuery = "ALTER TABLE CariKayit_temp RENAME TO CariKayit;";
                            using (SQLiteCommand command = new SQLiteCommand(renameTempTableQuery, connection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "CariRenameColumnAndRemoveMail2()", stackTrace: ex.StackTrace);
                    throw;
                }
            }

            public static List<string> CariMissingColumns()
            {
                List<string> requiredColumns = new List<string> { "ID", "carikod", "cariisim", "adres", "il", "ilce", "telefon1", "telefon2", "postakodu", "ulkekodu", "vergidairesi", "vergino", "tcno", "tip", "mail", "banka" };
                List<string> missingColumns = new List<string>();
                try
                {
                    using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                    {
                        connection.Open();

                        // PRAGMA table_info komutu ile sütunları sorgula
                        using (SQLiteCommand command = new SQLiteCommand($"PRAGMA table_info(CariKayit)", connection))
                        {
                            using (SQLiteDataReader reader = command.ExecuteReader())
                            {
                                List<string> existingColumns = new List<string>();
                                while (reader.Read())
                                {
                                    string existingColumnName = reader.GetString(1);
                                    existingColumns.Add(existingColumnName);
                                }

                                // Gerekli sütunların var olup olmadığını kontrol et
                                foreach (string column in requiredColumns)
                                {
                                    if (!existingColumns.Contains(column, StringComparer.OrdinalIgnoreCase))
                                    {
                                        missingColumns.Add(column);
                                    }
                                }
                            }
                        }
                    }

                    return missingColumns;
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "CariMissingColumns()", stackTrace: ex.StackTrace);
                    return missingColumns;
                    throw;
                }
            }

            public static void AddCariMissingColumns()
            {
                List<string> missingColumns = CariMissingColumns();
                try
                {
                    if (missingColumns.Count > 0)
                    {
                        using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                        {
                            connection.Open();

                            foreach (string columnName in missingColumns)
                            {
                                string columnType = "TEXT";

                                // Sütun adlarına göre sütun tiplerini belirleyin
                                switch (columnName.ToLower())
                                {
                                    case "ID":
                                        columnType = "INTEGER";
                                        break;
                                    default:
                                        columnType = "TEXT";
                                        break;
                                }

                                // ALTER TABLE komutu ile sütunu ekleyin
                                string query = $"ALTER TABLE CariKayit ADD COLUMN {columnName} {columnType}";

                                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                                {
                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "AddCariMissingColumns()", stackTrace: ex.StackTrace);
                    throw;
                }
            }

            public void CheckAndCreateOrRenameIsletmeFolders()
            {
                try
                {
                    MainViewModel viewModel = (MainViewModel)_anasayfa.DataContext;
                    var missingCariKod = new List<int>();

                    foreach (var item in viewModel.Businesses)
                    {
                        if (item is Business business && string.IsNullOrEmpty(business.CariKod))
                        {
                            missingCariKod.Add(business.ID);
                        }
                    }

                    if (missingCariKod.Count <= 0)
                    {
                        foreach (var item in viewModel.Businesses)
                        {
                            if (item is Business business)
                            {
                                string? baseDir = ConfigManager.IsletmePath;

                                string? oldDirName = Path.Combine(baseDir, business.CariIsim);
                                string? newDirName = Path.Combine(baseDir, business.CariKod);

                                // Eğer eski isimle klasör varsa ve yeni isimle klasör yoksa, yeniden adlandır
                                if (Directory.Exists(oldDirName) && !Directory.Exists(newDirName))
                                {
                                    Directory.Move(oldDirName, newDirName);
                                    //Directory.Delete(oldDirName, true);
                                }

                                string? isletmeKod = business.CariKod;
                                string? isletmePath = Path.Combine(ConfigManager.IsletmePath, isletmeKod);

                                // Klasörü kontrol et yoksa oluştur
                                if (!Directory.Exists(isletmePath))
                                {
                                    Directory.CreateDirectory(isletmePath);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "CheckAndCreateOrRenameIsletmeFolders()", stackTrace: ex.StackTrace);
                    throw;
                }
            }

            public static async Task<(string version, string notes, string downloadUrl, string title)> GetLatestReleaseInfoAsync()
            {
                try
                {
                    if (InternetErisimiKontrolEt())
                    {
                        string owner = "Pentoxin";
                        string repo = "CariKayitProgrami";
                        using (HttpClient client = new())
                        {
                            client.DefaultRequestHeaders.UserAgent.TryParseAdd("request");

                            string url = $"https://api.github.com/repos/{owner}/{repo}/releases";
                            HttpResponseMessage response = await client.GetAsync(url);
                            response.EnsureSuccessStatusCode();

                            string responseBody = await response.Content.ReadAsStringAsync();
                            JArray releases = JArray.Parse(responseBody);
                            var latestRelease = releases[0];


                            if (latestRelease != null)
                            {
                                string title = latestRelease["name"]?.ToString() ?? "";
                                string version = latestRelease["tag_name"]?.ToString() ?? "";
                                string notes = latestRelease["body"]?.ToString() ?? "";
                                var assets = latestRelease["assets"];
                                string downloadUrl;
                                if (assets != null && assets.Any())
                                {
                                    downloadUrl = assets[0]?["browser_download_url"]?.ToString() ?? "No assets found";
                                }
                                else
                                {
                                    downloadUrl = "No assets found";
                                }
                                return (version, notes, downloadUrl, title);
                            }
                            else
                            {
                                return ("", "", "", "");
                            }
                        }
                    }
                    else
                    {
                        return ("", "", "", "");
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "GetLatestReleaseInfoAsync()", stackTrace: ex.StackTrace);
                    return ($"Error: {ex.Message}", string.Empty, string.Empty, string.Empty);
                    throw;
                }
            }

            public static bool InternetErisimiKontrolEt()
            {
                string hedefAdres = "www.google.com";
                try
                {
                    Ping ping = new Ping();
                    PingReply cevap = ping.Send(hedefAdres);
                    return (cevap.Status == IPStatus.Success);
                }
                catch (PingException ex)
                {
                    LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "InternetErisimiKontrolEt()", stackTrace: ex.StackTrace);
                    return false;
                    throw;
                }
            }

            public static void AppDataCreate()
            {
                try
                {
                    //Klasör oluşturma
                    if (!Directory.Exists(ConfigManager.AppDataPath))
                    {
                        Directory.CreateDirectory(ConfigManager.AppDataPath);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "AppDataCreate()", stackTrace: ex.StackTrace);
                    throw;
                }
            }

            public static void InitializeDatabase()
            {
                try
                {
                    if (!File.Exists(ConfigManager.DatabaseFileName))
                    {
                        SQLiteConnection.CreateFile(ConfigManager.DatabaseFileName);
                    }

                    using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                    {
                        connection.Open();

                        using (SQLiteCommand command = new SQLiteCommand(connection))
                        {
                            // Tablo var mı kontrol et
                            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='CariKayit'";
                            var result = command.ExecuteScalar();

                            if (result == null || result.ToString() != "CariKayit")
                            {
                                // Tablo yoksa oluştur
                                command.CommandText = @"CREATE TABLE CariKayit (
                                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                                            carikod,
                                            cariisim TEXT,
                                            adres TEXT,
                                            il TEXT,
                                            ilce TEXT,
                                            telefon1 TEXT,
                                            telefon2 TEXT,
                                            postakodu TEXT,
                                            ulkekodu TEXT,
                                            vergidairesi TEXT,
                                            vergino TEXT,
                                            tcno TEXT,
                                            tip TEXT,
                                            mail TEXT,
                                            banka TEXT,
                                            hesapno TEXT);";

                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "InitializeDatabase()", stackTrace: ex.StackTrace);
                    throw;
                }
            }
        }

        private static Timer? timer;
        public static void StartTimer()
        {
            try
            {
                // Timer'ı 1 saat (3600000 milisaniye) aralıklarla ayarlıyoruz
                timer = new Timer(3600000);
                timer.Elapsed += OnTimedEvent;
                timer.AutoReset = true;
                timer.Enabled = true;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Anasayfa", methodName: "StartTimer()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private static async void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            try
            {
                await GuncellemeKontrol();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Anasayfa", methodName: "OnTimedEvent()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        public static async Task GuncellemeKontrol()
        {
            try
            {
                if (Check.InternetErisimiKontrolEt())
                {
                    // Başlangıçta Timer'ı başlatıyoruz
                    StartTimer();

                    var releaseInfo = await Check.GetLatestReleaseInfoAsync();

                    int guncelVersiyon = Convert.ToInt32(releaseInfo.version.Replace("v", "").Replace(".", ""));

                    Assembly? assembly = Assembly.GetExecutingAssembly();
                    Version? version = assembly.GetName().Version;
                    string? versionString = $"{version.Major}.{version.Minor}.{version.Build}".Replace(".", "");

                    int v = Convert.ToInt32(versionString);

                    if (v < guncelVersiyon)
                    {
                        Degiskenler.guncel = false;
                    }
                    else if (v >= guncelVersiyon)
                    {
                        if (File.Exists(ConfigManager.ExecutableFileName))
                        {
                            int productVersion = 0;
                            try
                            {
                                FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(ConfigManager.ExecutableFileName);
                                productVersion = Convert.ToInt32(fileInfo.ProductVersion.Replace(".", ""));
                                if (v >= productVersion)
                                {
                                    File.Delete(ConfigManager.ExecutableFileName);
                                }
                            }
                            catch (Exception)
                            {
                                File.Delete(ConfigManager.ExecutableFileName);
                            }

                        }
                        Degiskenler.guncel = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Anasayfa", methodName: "GuncellemeKontrol()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void OpenWindow(Window window, string openStatus)
        {
            try
            {
                if (openStatus == "SD")
                {
                    window.Owner = App.Current.MainWindow;
                    window.ShowDialog();
                }
                else if (openStatus == "S")
                {
                    window.Owner = App.Current.MainWindow;
                    window.Show();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Anasayfa", methodName: "OpenWindow()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StokButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogManager.LogInformation(message: "Stok penceresi açıldı.", className: "Anasayfa", methodName: "StokButton_Click()");
                OpenWindow(new Stok(), "S");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Anasayfa", methodName: "StokButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CariHesapKayitlariButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogManager.LogInformation(message: "Cari hesap kayıtları penceresi açıldı.", className: "Anasayfa", methodName: "CariHesapKayitlariButton_Click()");
                OpenWindow(new CariHesapKayitlari(), "S");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Anasayfa", methodName: "CariHesapKayitlariButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CariHareketKayitlariButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogManager.LogInformation(message: "Cari hareket kayıtları penceresi açıldı.", className: "Anasayfa", methodName: "CariHareketKayitlariButton_Click()");
                Degiskenler.selectedBusiness = null;
                OpenWindow(new CariHareketKayitlari(), "S");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Anasayfa", methodName: "CariHareketKayitlariButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UygulamayıGuncelle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogManager.LogInformation(message: "Uygulamanın güncelliği manuel olarak kontrol ediliyor.", className: "Anasayfa", methodName: "CariHareketKayitlariButton_Click()");

                Degiskenler.guncellemeOnay = false;
                _ = GuncellemeKontrol();

                if (!Degiskenler.guncel)
                {
                    OpenWindow(new YuklemeEkrani(), "SD");

                    if (Degiskenler.guncellemeOnay)
                    {
                        OpenWindow(new YuklemeEkrani(), "SD");
                    }
                }
                else
                {
                    MessageBox.Show("Uygulama Güncel", "Güncellemeleri Denetle", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Anasayfa", methodName: "UygulamayıGuncelle_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SurumNotlariButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string owner = "Pentoxin";
                string repo = "CariKayitProgrami";
                Assembly? assembly = Assembly.GetExecutingAssembly();
                Version? version = assembly.GetName().Version;
                string? versionString = $"{version.Major}.{version.Minor}.{version.Build}";

                string url = $"https://github.com/{owner}/{repo}/releases/tag/v{versionString}";

                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                LogManager.LogInformation(message: "Sürüm notları açıldı.", className: "Anasayfa", methodName: "SurumNotlariButton_Click()");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Anasayfa", methodName: "SurumNotlariButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void BusinessesAtStartupCheck()
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                viewModel.Businesses.Clear();

                using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM CariKayit";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Business b = new Business
                                {
                                    ID = reader.GetInt32(0),
                                    CariKod = reader.IsDBNull(1) ? null : reader.GetString(1),
                                    CariIsim = reader.GetString(2),
                                    Adres = reader.GetString(3),
                                    Il = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    Ilce = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    Telefon1 = reader.GetString(6),
                                    Telefon2 = reader.GetString(7),
                                    PostaKodu = reader.IsDBNull(8) ? null : reader.GetString(8),
                                    UlkeKodu = reader.IsDBNull(9) ? null : reader.GetString(9),
                                    VergiDairesi = reader.GetString(10),
                                    VergiNo = reader.GetString(11),
                                    TcNo = reader.IsDBNull(12) ? null : reader.GetString(12),
                                    Tip = reader.IsDBNull(13) ? null : reader.GetString(13),
                                    EPosta = reader.GetString(14),
                                    Banka = reader.GetString(15),
                                    HesapNo = reader.GetString(16),
                                };
                                viewModel.Businesses.Add(b);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Anasayfa", methodName: "BusinessesAtStartupCheck()", stackTrace: ex.StackTrace);
                throw;
            }
        }
    }
}