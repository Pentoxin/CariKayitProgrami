﻿using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Win32;
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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Cari_kayıt_Programı.Yazdir_TR;


namespace Cari_kayıt_Programı
{
    public partial class Main_TR : Page
    {
        public Main_TR()
        {
            InitializeComponent();

            _ = GuncellemeKontrol();
            AppDataCreate();
            InitializeDatabase();
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

            dataGrid.ItemsSource = GetBusinesses(Config.ConnectionString);

            if (temizle == 0)
            {
                YazdirDegiskenler.ClearSecilenOzellikler();
                temizle++;
            }
        }

        public async Task<(string version, string notes, string downloadUrl, string title)> GetLatestReleaseInfoAsync()
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
                                // assets null değilse ve en az bir öğe içeriyorsa işlem yap
                            }
                            else
                            {
                                downloadUrl = "No assets found";
                                // assets null ise veya hiç öğe içermiyorsa işlem yap
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

        public async Task GuncellemeKontrol()
        {
            try
            {
                UygulamayıGuncelle.Visibility = Visibility.Hidden;

                if (InternetErisimiKontrolEt())
                {
                    HttpClient client = new HttpClient();
                    HttpResponseMessage response = await client.GetAsync(Config.VersiyonUrl);
                    response.EnsureSuccessStatusCode(); // İsteğin başarılı olup olmadığını kontrol edin

                    string html = await response.Content.ReadAsStringAsync();
                    int guncelVersiyon = Convert.ToInt32(html);

                    if (Config.version < guncelVersiyon)
                    {
                        UygulamayıGuncelle.Visibility = Visibility.Visible;
                        Degiskenler.guncel = false;
                    }
                    else if (Config.version >= guncelVersiyon)
                    {
                        UygulamayıGuncelle.Visibility = Visibility.Hidden;

                        if (File.Exists(Config.dosyaAdi))
                        {
                            FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(Config.dosyaAdi);
                            int productVersion = Convert.ToInt32(fileInfo.ProductVersion);
                            if (Config.version >= productVersion)
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

        private void OpenWindow(Window window)
        {
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        private void XButton_Click(object sender, RoutedEventArgs e)
        {
            try
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
                dataGrid.ItemsSource = GetBusinesses(Config.ConnectionString);
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
                OpenWindow(new YeniKayit());

                dataGrid.SelectedItem = null;
                dataGrid.ItemsSource = GetBusinesses(Config.ConnectionString);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void ListeleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                dataGrid.ItemsSource = GetBusinesses(Config.ConnectionString);
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
                        }
                        dataGrid.ItemsSource = GetBusinesses(Config.ConnectionString);
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
                Main_TR.Degiskenler.selectedBusiness = (Business)dataGrid.SelectedItem;

                if (Degiskenler.selectedBusiness == null)
                {
                    MessageBox.Show("Önce değiştirmek istediğiniz veriyi seçiniz", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                OpenWindow(new Degistir());

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
                dataGrid.ItemsSource = GetBusinesses(Config.ConnectionString);
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
                OpenWindow(new Yazdir());

                if (Yazdir_TR.YazdirDegiskenler.olustur)
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "PDF Dosyası|*.pdf";
                    saveFileDialog.FileName = "Cari Kayıt Rehberi.pdf";
                    saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        string path = saveFileDialog.FileName;

                        if (Yazdir_TR.YazdirDegiskenler.olustur)
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
                                    dt.Rows.Add(business.ID, business.İşletme_Adı, business.VergiDairesi, business.VergiNo, business.Banka, business.HesapNo,
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

                                // İkinci belgeyi oluşturun ve içeriği kopyalayın
                                var writer2 = new PdfWriter(Config.PDFPath2);
                                var pdf2 = new PdfDocument(writer2);
                                var document2 = new Document(pdf2);

                                PdfFont font2 = PdfFontFactory.CreateFont(fontPath);
                                var headerFont2 = new iText.Layout.Style().SetFont(font2).SetFontSize(6).SetBold();
                                var dataFont2 = new iText.Layout.Style().SetFont(font2).SetFontSize(6);

                                Table table2 = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();

                                // Sadece seçilen özellikleri ekleyin
                                foreach (DataColumn column in dt.Columns.Cast<DataColumn>().Where(col => SecilenOzellikler.Contains(col.ColumnName)))
                                {
                                    table2.AddCell(new Cell().Add(new Paragraph(column.ColumnName).AddStyle(headerFont2)));
                                }


                                foreach (DataRow row in dt.Rows)
                                {
                                    foreach (DataColumn column in dt.Columns.Cast<DataColumn>().Where(col => SecilenOzellikler.Contains(col.ColumnName)))
                                    {
                                        table2.AddCell(new Cell().Add(new Paragraph(row[column].ToString()).AddStyle(dataFont2)));
                                    }
                                }

                                document2.Add(table2);
                                document2.Close();

                                MessageBoxResult result = MessageBox.Show("Belge Oluşturuldu. Dosyayı Açmak ister misiniz?", "Belge Oluşturuldu", MessageBoxButton.YesNo, MessageBoxImage.Asterisk);
                                if (result == MessageBoxResult.Yes)
                                {
                                    Process.Start(new ProcessStartInfo(Config.PDFPath2) { UseShellExecute = true });
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
                OpenWindow(new Filtre());

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
                    isletmeadiTextBox.Text = selectedBusiness.İşletme_Adı;
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

        private void IceriAktarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "DB Dosyaları (*.db)|*.db";
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (openFileDialog.ShowDialog() == true)
                {
                    string selectedFilePath = openFileDialog.FileName;

                    // Mevcut CariKayitDB.db dosyasını sil
                    if (File.Exists(Config.DatabaseFileName))
                    {
                        File.Delete(Config.DatabaseFileName);
                    }

                    // Seçilen dosyayı AppData dizinine kopyala
                    File.Copy(selectedFilePath, Config.DatabaseFileName, true);

                    // Yeni dosyanın adını CariKayitDB.db olarak değiştir
                    File.Move(Config.DatabaseFileName, Config.DatabaseFileName);

                    MessageBox.Show("Veritabanı başarıyla içe aktarıldı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);

                    dataGrid.ItemsSource = GetBusinesses(Config.ConnectionString);
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
                saveFileDialog.Filter = "Veritabanı Dosyası (*.db)|*.db";
                saveFileDialog.FileName = "CariKayitDB Yedek.db";
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (saveFileDialog.ShowDialog() == true)
                {
                    string destinationFilePath = saveFileDialog.FileName;

                    File.Copy(Config.DatabaseFileName, destinationFilePath, true);

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
                    OpenWindow(new YuklemeEkrani());

                    if (Degiskenler.guncellemeOnay)
                    {
                        OpenWindow(new YuklemeEkrani());
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private static List<Business> Businesses(string searchTerm)
        {
            List<Business> businesses = new List<Business>();

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
                                İşletme_Adı = name,
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
                            businesses.Add(b);
                        }
                    }
                }
            }
            return businesses;
        }

        public static List<Business> GetBusinesses(string connectionString)
        {
            List<Business> businesses = new List<Business>();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
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
                                İşletme_Adı = name,
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
                            businesses.Add(b);
                        }
                    }
                }
            }
            return businesses;
        }

        public class Business
        {
            public int ID { get; set; }
            public string? İşletme_Adı { get; set; }
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
            /*
            // FileVersionInfo kullanarak dosya bilgilerini alın
            FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(Config.dosyaAdi);

            // Ürün sürümü bilgisini alın
            string productVersion = fileInfo.ProductVersion;
            MessageBox.Show(productVersion);
            */
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
    }
}
