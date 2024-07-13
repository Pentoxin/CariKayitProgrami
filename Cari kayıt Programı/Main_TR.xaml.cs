using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Cari_kayıt_Programı.Yazdir_TR;


namespace Cari_kayıt_Programı
{
    public partial class Main_TR : Page
    {
        public MainViewModel ViewModel { get; set; }

        public Main_TR()
        {
            InitializeComponent();

            ViewModel = new MainViewModel();
            DataContext = ViewModel;

            _ = GuncellemeKontrol();
            Check.AppDataCreate();
            Check.InitializeDatabase();
        }

        public static class Degiskenler
        {
            public static bool guncellemeOnay { get; set; } = false;
            public static bool guncel { get; set; }
            public static Business? selectedBusiness { get; set; }
        }


        int temizle = 0;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Admin.Visibility = Visibility.Hidden;

            dataGrid.ItemsSource = GetBusinesses();

            if (temizle == 0)
            {
                YazdirDegiskenler.ClearSecilenOzellikler();
                temizle++;
            }
        }

        public DataGrid MainDataGrid
        {
            get { return dataGrid; }
        }

        public class Check
        {
            private readonly Main_TR _mainTR;
            public Check(Main_TR mainTR)
            {
                _mainTR = mainTR;
            }

            public void CheckAndCreateIsletmeFolders()
            {
                try
                {
                    foreach (var item in _mainTR.MainDataGrid.Items)
                    {
                        if (item is Business business)
                        {
                            string? isletmeAdi = business.Isletme_Adi; // Odeme sınıfında Isletme_Adi özelliği varsa kullanılabilir
                            string? isletmePath = Path.Combine(Config.IsletmePath, isletmeAdi);

                            // Klasörü kontrol et veya oluştur
                            if (!Directory.Exists(isletmePath))
                            {
                                Directory.CreateDirectory(isletmePath);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
            }

            public void CheckAndUpdateTables()
            {
                try
                {
                    var idList = new List<int>();
                    foreach (var item in _mainTR.MainDataGrid.Items)
                    {
                        if (item is Business business)
                        {
                            idList.Add(business.ID);
                        }
                    }

                    foreach (int id in idList)
                    {
                        string tableName = $"Cari_{id}";
                        AddMissingColumns(tableName);
                    }

                    foreach (int id in idList)
                    {
                        string tableName = $"Cari_{id}";
                        UpdateDateFormatInTable(tableName);
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
            }

            public void UpdateDateFormatInTable(string tableName)
            {
                try
                {
                    using (SQLiteConnection connection = new SQLiteConnection(Config.ConnectionString))
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
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
            }

            public static List<string> MissingColumns(string tableName)
            {
                List<string> requiredColumns = new List<string> { "ID", "tarih", "tip", "evrakno", "aciklama", "vadetarihi", "borc", "alacak", "dosya" };
                List<string> missingColumns = new List<string>();
                try
                {
                    using (SQLiteConnection connection = new SQLiteConnection(Config.ConnectionString))
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
                    LogError(ex);
                    return missingColumns;
                }
            }

            public static void AddMissingColumns(string tableName)
            {
                List<string> missingColumns = MissingColumns(tableName);
                try
                {
                    if (missingColumns.Count > 0)
                    {
                        using (SQLiteConnection connection = new SQLiteConnection(Config.ConnectionString))
                        {
                            connection.Open();

                            foreach (string columnName in missingColumns)
                            {
                                string columnType = "TEXT";

                                // Sütun adlarına göre sütun tiplerini belirleyin
                                switch (columnName.ToLower())
                                {
                                    case "id":
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
                }
                catch (Exception ex)
                {
                    LogError(ex);
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
                    return ($"Error: {ex.Message}", string.Empty, string.Empty, string.Empty);
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
                catch (PingException)
                {
                    return false;
                }
            }

            public static void AppDataCreate()
            {
                try
                {
                    //Klasör oluşturma
                    if (!Directory.Exists(Config.AppDataPath))
                    {
                        Directory.CreateDirectory(Config.AppDataPath);
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
            }

            public static void InitializeDatabase()
            {
                try
                {
                    if (!File.Exists(Config.DatabaseFileName))
                    {
                        SQLiteConnection.CreateFile(Config.DatabaseFileName);
                    }

                    using (SQLiteConnection connection = new SQLiteConnection(Config.ConnectionString))
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
                                            isletmeadi TEXT NOT NULL UNIQUE,
                                            vergidairesi TEXT,
                                            vergino TEXT,
                                            banka TEXT,
                                            hesapno TEXT,
                                            adres TEXT,
                                            mail1 TEXT,
                                            mail2 TEXT,
                                            telefon1 TEXT,
                                            telefon2 TEXT);";

                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
            }
        }

        public async Task GuncellemeKontrol()
        {
            try
            {
                UygulamayıGuncelle.Visibility = Visibility.Hidden;

                if (Check.InternetErisimiKontrolEt())
                {
                    var releaseInfo = await Check.GetLatestReleaseInfoAsync();

                    int guncelVersiyon = Convert.ToInt32(releaseInfo.version.Replace("v", "").Replace(".", ""));

                    Assembly? assembly = Assembly.GetExecutingAssembly();
                    Version? version = assembly.GetName().Version;
                    string? versionString = $"{version.Major}.{version.Minor}.{version.Build}".Replace(".", "");

                    int v = Convert.ToInt32(versionString);

                    if (v < guncelVersiyon)
                    {
                        UygulamayıGuncelle.Visibility = Visibility.Visible;
                        Degiskenler.guncel = false;
                    }
                    else if (v >= guncelVersiyon)
                    {
                        UygulamayıGuncelle.Visibility = Visibility.Hidden;

                        if (File.Exists(Config.dosyaAdi))
                        {
                            int productVersion = 0;
                            try
                            {
                                FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(Config.dosyaAdi);
                                productVersion = Convert.ToInt32(fileInfo.ProductVersion.Replace(".", ""));
                                if (v >= productVersion)
                                {
                                    File.Delete(Config.dosyaAdi);
                                }
                            }
                            catch (Exception)
                            {
                                File.Delete(Config.dosyaAdi);
                            }

                        }
                        Degiskenler.guncel = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        public static void LogError(Exception ex)
        {
            try
            {
                // Log dosyasının varlığını kontrol et, yoksa oluştur
                if (!File.Exists(Config.LogFilePath))
                {
                    using (StreamWriter createFile = File.CreateText(Config.LogFilePath))
                    {
                        createFile.Close();
                    }
                }

                string errorMessage = $"{DateTime.Now}: {ex.Message}";

                // Hata mesajını log dosyasına yaz
                using (StreamWriter writer = new StreamWriter(Config.LogFilePath, true))
                {
                    writer.WriteLine(errorMessage);
                    writer.WriteLine();
                }

                // Kullanıcıya hata mesajını göster
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception Logex)
            {
                LogError(Logex);
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
                else if (openStatus == "D")
                {
                    window.Owner = App.Current.MainWindow;
                    window.Show();
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string searchTerm = txtSearch.Text;
                dataGrid.ItemsSource = Businesses(searchTerm).ToList();
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void KaydetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenWindow(new YeniKayit(), "SD");

                dataGrid.SelectedItem = null;
                dataGrid.ItemsSource = GetBusinesses();
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Business selectedBusinessItem = (Business)dataGrid.SelectedItem;

                if (selectedBusinessItem == null)
                {
                    MessageBox.Show("Önce silmek istediğiniz veriyi seçiniz", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                else
                {
                    if (MessageBox.Show("Seçilen veriyi silmek istediğinize emin misiniz?", "Uyarı", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        using (SQLiteConnection connection = new SQLiteConnection(Config.ConnectionString))
                        {
                            connection.Open();
                            string query = "DELETE FROM CariKayit WHERE ID=@Id";

                            using (SQLiteCommand command = new SQLiteCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@Id", selectedBusinessItem.ID);
                                command.ExecuteNonQuery();
                            }

                            // Önce Odeme tablosunu silme işlemi
                            string queryOdeme = $"DROP TABLE IF EXISTS Cari_{selectedBusinessItem.ID}";
                            using (SQLiteCommand commandOdeme = new SQLiteCommand(queryOdeme, connection))
                            {
                                commandOdeme.ExecuteNonQuery();
                            }
                        }
                        dataGrid.ItemsSource = GetBusinesses();

                        if (Directory.Exists(Path.Combine(Config.IsletmePath, selectedBusinessItem.Isletme_Adi)))
                        {
                            Directory.Delete(Path.Combine(Config.IsletmePath, selectedBusinessItem.Isletme_Adi));
                        }
                    }
                }
                isletmeadiTextBox.Clear();
                vergidairesiTextBox.Clear();
                verginoTextBox.Clear();
                bankaTextBox.Clear();
                hesapnoTextBox.Clear();
                adresTextBox.Clear();
                mail1TextBox.Clear();
                mail2TextBox.Clear();
                telefon1TextBox.Clear();
                telefon2TextBox.Clear();
                dataGrid.SelectedItem = null;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void Updatebutton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Degiskenler.selectedBusiness = (Business)dataGrid.SelectedItem;

                if (Degiskenler.selectedBusiness == null)
                {
                    MessageBox.Show("Önce değiştirmek istediğiniz veriyi seçiniz", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                OpenWindow(new Degistir(), "SD");

                isletmeadiTextBox.Clear();
                vergidairesiTextBox.Clear();
                verginoTextBox.Clear();
                hesapnoTextBox.Clear();
                bankaTextBox.Clear();
                adresTextBox.Clear();
                mail1TextBox.Clear();
                mail2TextBox.Clear();
                telefon1TextBox.Clear();
                telefon2TextBox.Clear();
                txtSearch.Clear();
                dataGrid.ItemsSource = GetBusinesses();
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void YazdırButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenWindow(new Yazdir(), "SD");

                if (YazdirDegiskenler.olustur)
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "PDF Dosyası|*.pdf";
                    saveFileDialog.FileName = "Cari Kayıt Rehberi.pdf";
                    saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        string path = saveFileDialog.FileName;

                        if (YazdirDegiskenler.olustur)
                        {
                            DataTable dt = new DataTable();
                            dt.Columns.Add("ID");
                            dt.Columns.Add("İşletme Adı");
                            dt.Columns.Add("Vergi Dairesi");
                            dt.Columns.Add("Vergi No");
                            dt.Columns.Add("Banka");
                            dt.Columns.Add("Hesap No / IBAN");
                            dt.Columns.Add("Adres");
                            dt.Columns.Add("E-Posta 1");
                            dt.Columns.Add("E-Posta 2");
                            dt.Columns.Add("Telefon 1");
                            dt.Columns.Add("Telefon 2");
                            foreach (var item in dataGrid.Items)
                            {
                                var business = item as Business;
                                if (business != null)
                                {
                                    dt.Rows.Add(business.ID, business.Isletme_Adi, business.VergiDairesi, business.VergiNo, business.Banka, business.HesapNo,
                                    business.Adres, business.EPosta1, business.EPosta2, business.Telefon1, business.Telefon2);
                                }
                            }

                            string fontPath = "C:\\WINDOWS\\Fonts\\arial.ttf";

                            if (File.Exists(fontPath))
                            {
                                var writer = new PdfWriter(path);
                                var pdf = new PdfDocument(writer);
                                var document = new Document(pdf);

                                PdfFont font = PdfFontFactory.CreateFont(fontPath);
                                var headerFont = new iText.Layout.Style().SetFont(font).SetFontSize(6).SetBold();
                                var dataFont = new iText.Layout.Style().SetFont(font).SetFontSize(6);

                                var SecilenOzellikler = YazdirDegiskenler.GetSecilenOzellikler();

                                float[] columnWidths = new float[SecilenOzellikler.Count];
                                Table table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();

                                // Sadece seçilen özellikleri ekleyin
                                foreach (DataColumn column in dt.Columns.Cast<DataColumn>().Where(col => SecilenOzellikler.Contains(col.ColumnName)))
                                {
                                    table.AddCell(new Cell().Add(new Paragraph(column.ColumnName).AddStyle(headerFont)));
                                }

                                foreach (DataRow row in dt.Rows)
                                {
                                    foreach (DataColumn column in dt.Columns.Cast<DataColumn>().Where(col => SecilenOzellikler.Contains(col.ColumnName)))
                                    {
                                        table.AddCell(new Cell().Add(new Paragraph(row[column].ToString()).AddStyle(dataFont)));
                                    }
                                }

                                document.Add(table);
                                document.Close();

                                MessageBoxResult result = MessageBox.Show("Belge Oluşturuldu. Dosyayı Açmak ister misiniz?", "Belge Oluşturuldu", MessageBoxButton.YesNo, MessageBoxImage.Asterisk);
                                if (result == MessageBoxResult.Yes)
                                {
                                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                                }
                            }
                            else
                            {
                                MessageBox.Show("Arial font dosyası bulunamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void FiltreleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenWindow(new Filtre(), "SD");

                var SecilenSutunlar = Filtre_TR.GetSecilenSutunlar();
                var SecilmeyenSutunlar = Filtre_TR.GetSecilmeyenSutunlar();

                if (SecilenSutunlar != null || SecilmeyenSutunlar != null)
                {
                    foreach (DataGridColumn column in dataGrid.Columns)
                    {
                        string? columnName = column.Header != null ? column.Header.ToString() : string.Empty;

                        if (SecilenSutunlar != null)
                        {
                            for (int i = 0; i < SecilenSutunlar.Count; i++)
                            {
                                if (columnName == SecilenSutunlar[i])
                                {
                                    column.Visibility = Visibility.Visible;
                                }
                            }
                        }
                        for (int i = 0; i < SecilmeyenSutunlar.Count; i++)
                        {
                            if (columnName == SecilmeyenSutunlar[i])
                            {
                                column.Visibility = Visibility.Hidden;
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedBusiness = dataGrid.SelectedItem as Business;
                if (selectedBusiness != null)
                {
                    isletmeadiTextBox.Text = selectedBusiness.Isletme_Adi;
                    vergidairesiTextBox.Text = selectedBusiness.VergiDairesi;
                    verginoTextBox.Text = selectedBusiness.VergiNo;
                    bankaTextBox.Text = selectedBusiness.Banka;
                    hesapnoTextBox.Text = selectedBusiness.HesapNo;
                    adresTextBox.Text = selectedBusiness.Adres;
                    mail1TextBox.Text = selectedBusiness.EPosta1;
                    mail2TextBox.Text = selectedBusiness.EPosta2;
                    telefon1TextBox.Text = selectedBusiness.Telefon1;
                    telefon2TextBox.Text = selectedBusiness.Telefon2;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void dataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Check check = new Check(this);
                check.CheckAndUpdateTables();
                check.CheckAndCreateIsletmeFolders();
                dataGrid.ItemsSource = GetBusinesses();
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void IceriAktarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Zip Dosyalası (*.zip)|*.zip";
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (openFileDialog.ShowDialog() == true)
                {
                    Check check = new Check(this);

                    string selectedFilePath = openFileDialog.FileName;

                    ZipFile.ExtractToDirectory(selectedFilePath, Config.AppDataPath, true);

                    check.CheckAndUpdateTables();
                    check.CheckAndCreateIsletmeFolders();
                    MessageBox.Show("Veritabanı başarıyla içe aktarıldı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);

                    dataGrid.ItemsSource = GetBusinesses();
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void DışarıAktarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Zip Dosyası (*.zip)|*.zip";
                saveFileDialog.FileName = "CariKayitDB Yedek.zip";
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (saveFileDialog.ShowDialog() == true)
                {
                    string destinationFilePath = saveFileDialog.FileName;

                    if (File.Exists(destinationFilePath))
                    {
                        File.Delete(destinationFilePath);
                    }
                    if (Directory.Exists(Config.IsletmePath))
                    {
                        ZipFile.CreateFromDirectory(Config.IsletmePath, destinationFilePath, CompressionLevel.Optimal, true);
                    }

                    // CariKayitDB dosyasını ekleme
                    if (File.Exists(Config.DatabaseFileName))
                    {
                        using (var zipArchive = ZipFile.Open(destinationFilePath, ZipArchiveMode.Update))
                        {
                            zipArchive.CreateEntryFromFile(Config.DatabaseFileName, Path.GetFileName(Config.DatabaseFileName), CompressionLevel.Optimal);
                        }
                    }

                    MessageBox.Show("Veritabanı başarıyla dışa aktarıldı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void UygulamayıGuncelle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void HareketlerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Degiskenler.selectedBusiness = (Business)dataGrid.SelectedItem;

                OpenWindow(new Hareketler(), "D");
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        public ObservableCollection<Business> Businesses(string searchTerm)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                viewModel.Businesses.Clear();

                using (SQLiteConnection connection = new SQLiteConnection(Config.ConnectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM CariKayit WHERE LOWER(isletmeadi) LIKE '%' || LOWER(@searchTerm) || '%' OR ID LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(vergidairesi) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(vergino) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(banka) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(hesapno) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(adres) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(mail1) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(mail2) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(telefon1) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(telefon2) LIKE '%' || LOWER(@searchTerm) || '%'";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@searchTerm", searchTerm);

                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id = reader.GetInt32(0);
                                string name = reader.GetString(1);
                                string vergidairesi = reader.GetString(2);
                                string vergino = reader.GetString(3);
                                string banka = reader.GetString(4);
                                string hesapno = reader.GetString(5);
                                string address = reader.GetString(6);
                                string mail1 = reader.GetString(7);
                                string mail2 = reader.GetString(8);
                                string phone1 = reader.GetString(9);
                                string phone2 = reader.GetString(10);

                                Business b = new Business
                                {
                                    ID = id,
                                    Isletme_Adi = name,
                                    VergiDairesi = vergidairesi,
                                    VergiNo = vergino,
                                    Banka = banka,
                                    HesapNo = hesapno,
                                    Adres = address,
                                    EPosta1 = mail1,
                                    EPosta2 = mail2,
                                    Telefon1 = phone1,
                                    Telefon2 = phone2
                                };
                                viewModel.Businesses.Add(b);
                            }
                        }
                    }
                }
                return viewModel.Businesses;
            }
            catch (Exception ex)
            {
                LogError(ex);

                return null;
            }
        }

        public ObservableCollection<Business> GetBusinesses()
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                viewModel.Businesses.Clear();

                using (SQLiteConnection connection = new SQLiteConnection(Config.ConnectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM CariKayit";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id = reader.GetInt32(0);
                                string name = reader.GetString(1);
                                string vergidairesi = reader.GetString(2);
                                string vergino = reader.GetString(3);
                                string banka = reader.GetString(4);
                                string hesapno = reader.GetString(5);
                                string address = reader.GetString(6);
                                string mail1 = reader.GetString(7);
                                string mail2 = reader.GetString(8);
                                string phone1 = reader.GetString(9);
                                string phone2 = reader.GetString(10);

                                Business b = new Business
                                {
                                    ID = id,
                                    Isletme_Adi = name,
                                    VergiDairesi = vergidairesi,
                                    VergiNo = vergino,
                                    Banka = banka,
                                    HesapNo = hesapno,
                                    Adres = address,
                                    EPosta1 = mail1,
                                    EPosta2 = mail2,
                                    Telefon1 = phone1,
                                    Telefon2 = phone2
                                };
                                viewModel.Businesses.Add(b);
                            }
                        }
                    }
                }
                return viewModel.Businesses;
            }
            catch (Exception ex)
            {
                LogError(ex);

                return null;
            }
        }

        public class Business
        {
            public int ID { get; set; }
            public string? Isletme_Adi { get; set; }
            public string? VergiDairesi { get; set; }
            public string? VergiNo { get; set; }
            public string? Banka { get; set; }
            public string? HesapNo { get; set; }
            public string? Adres { get; set; }
            public string? EPosta1 { get; set; }
            public string? EPosta2 { get; set; }
            public string? Telefon1 { get; set; }
            public string? Telefon2 { get; set; }
        }


        private void Admin_Click(object sender, RoutedEventArgs e)
        {
            // Method intentionally left empty.
        }

        //public static int selectedValue;

        private void DilComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*if (DilComboBox.SelectedIndex == 1)
            {
                // Seçilen öğeyi al
                selectedValue = DilComboBox.SelectedIndex;

                Reload reload = new Reload();

                reload.Show();

                Window mainWindow = Window.GetWindow(this);

                if (mainWindow != null)
                {
                    // Ana pencereyi kapat
                    mainWindow.Close();
                }
            }*/
        }

        private void Page_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!IsMouseOverDataGrid(e))
                {
                    isletmeadiTextBox.Clear();
                    vergidairesiTextBox.Clear();
                    verginoTextBox.Clear();
                    hesapnoTextBox.Clear();
                    bankaTextBox.Clear();
                    adresTextBox.Clear();
                    mail1TextBox.Clear();
                    mail2TextBox.Clear();
                    telefon1TextBox.Clear();
                    telefon2TextBox.Clear();
                    txtSearch.Clear();
                    dataGrid.ItemsSource = GetBusinesses();
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private bool IsMouseOverDataGrid(MouseButtonEventArgs e)
        {
            try
            {
                DependencyObject originalSource = e.OriginalSource as DependencyObject;

                // Tıklanan öğenin DataGrid, Button, TextBox, RadioButton veya GroupBox olup olmadığını kontrol et
                while (originalSource != null)
                {
                    if (originalSource == dataGrid)
                    {
                        return true;
                    }

                    if (originalSource is Button ||
                        originalSource is TextBox ||
                        originalSource is RadioButton ||
                        originalSource is GroupBox)
                    {
                        return true;
                    }

                    originalSource = VisualTreeHelper.GetParent(originalSource);
                }

                return false;
            }
            catch (Exception ex)
            {
                LogError(ex);

                return false;
            }
        }
    }
}
