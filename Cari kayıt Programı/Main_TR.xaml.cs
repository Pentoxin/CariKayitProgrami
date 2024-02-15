using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Data;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using static Cari_kayıt_Programı.Yazdir_TR;
using System.Data.SQLite;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout;
using iText.Kernel.Font;
using iText.Layout.Properties;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;


namespace Cari_kayıt_Programı
{
    public partial class Main_TR : Page
    {
        public Main_TR()
        {
            InitializeComponent();

            GuncellemeKontrol();
            AppDataCreate();
            InitializeDatabase();
        }

        public static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Cari Kayıt Programı");
        private static readonly string DatabaseFileName = Path.Combine(AppDataPath, "CariKayitDB.db");
        private static readonly string LogFilePath = Path.Combine(AppDataPath, "log.txt");
        public static readonly string ConnectionString = $"Data Source={DatabaseFileName};Version=3;";
        public static readonly string PDFPath2 = Path.Combine(AppDataPath, "Cari Kayıt Rehberi.pdf");
        public static readonly string version = "1.3.0.0";
        public static readonly string verisonUrl = "https://raw.githubusercontent.com/Pentoxin/CariKayitProgrami/master/Version.txt";
        public static readonly string dosyaAdi = Path.Combine(AppDataPath, "cari_kayit_programi_setup.exe");

        int temizle = 0;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dataGrid.ItemsSource = GetBusinesses(ConnectionString);

            if (temizle == 0)
            {
                degiskenler.secilenOzellikler.Clear();
                temizle++;
            }
        }

        public bool guncel;
        public void GuncellemeKontrol()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string html = client.DownloadString(verisonUrl);

                    if (version != html)
                    {
                        UygulamayıGuncelle.Visibility = Visibility.Visible;
                        guncel = false;
                    }
                    else if (version == html)
                    {
                        UygulamayıGuncelle.Visibility = Visibility.Hidden;

                        UygulamayıGuncelle.Visibility = Visibility.Hidden;
                        if (File.Exists(dosyaAdi))
                        {
                            File.Delete(dosyaAdi);
                        }
                        guncel = true;
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
                if (!Directory.Exists(AppDataPath))
                {
                    Directory.CreateDirectory(AppDataPath);
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
                if (!File.Exists(LogFilePath))
                {
                    using (StreamWriter createFile = File.CreateText(LogFilePath))
                    {
                        createFile.Close();
                    }
                }

                string errorMessage = $"{DateTime.Now}: {ex.Message}";

                // Hata mesajını log dosyasına yaz
                using (StreamWriter writer = new StreamWriter(LogFilePath, true))
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
                if (!File.Exists(DatabaseFileName))
                {
                    SQLiteConnection.CreateFile(DatabaseFileName);
                }

                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
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
                dataGrid.ItemsSource = GetBusinesses(ConnectionString);
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
                YeniKayit yeniKayit = new YeniKayit();
                yeniKayit.ShowDialog();

                dataGrid.SelectedItem = null;
                dataGrid.ItemsSource = GetBusinesses(ConnectionString);
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
                dataGrid.ItemsSource = GetBusinesses(ConnectionString);
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
                Business selectedBusiness = (Business)dataGrid.SelectedItem;
                if (selectedBusiness != null)
                {
                    if (MessageBox.Show("Seçilen Veriyi Silmek İstediğinize Emin Misiniz?", "Uyarı", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                        {
                            connection.Open();
                            string query = "DELETE FROM CariKayit WHERE ID=@Id";

                            using (SQLiteCommand command = new SQLiteCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@Id", selectedBusiness.ID);
                                command.ExecuteNonQuery();
                            }
                        }
                        dataGrid.ItemsSource = GetBusinesses(ConnectionString);
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

        public static Business selectedBusiness;
        private void Updatebutton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Main_TR.selectedBusiness = (Business)dataGrid.SelectedItem;

                if (selectedBusiness == null)
                {
                    MessageBox.Show("Önce Değiştirmek İstediğiniz Veriyi Seçiniz: ", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Degistir degistir = new Degistir();
                degistir.ShowDialog();

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
                dataGrid.ItemsSource = GetBusinesses(ConnectionString);
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
                Yazdir yazdir = new Yazdir();
                yazdir.ShowDialog();

                if (Yazdir_TR.degiskenler.olustur == true)
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "PDF Dosyası|*.pdf";
                    saveFileDialog.FileName = "Cari Kayıt Rehberi.pdf";
                    saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        string path = saveFileDialog.FileName;

                        if (Yazdir_TR.degiskenler.olustur == true)
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

                                float[] columnWidths = new float[Yazdir_TR.degiskenler.secilenOzellikler.Count];
                                Table table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();

                                // Sadece seçilen özellikleri ekleyin
                                foreach (DataColumn column in dt.Columns)
                                {
                                    if (Yazdir_TR.degiskenler.secilenOzellikler.Contains(column.ColumnName))
                                    {

                                        table.AddCell(new Cell().Add(new Paragraph(column.ColumnName).AddStyle(headerFont)));
                                    }
                                }

                                foreach (DataRow row in dt.Rows)
                                {
                                    foreach (DataColumn column in dt.Columns)
                                    {
                                        if (Yazdir_TR.degiskenler.secilenOzellikler.Contains(column.ColumnName))
                                        {
                                            table.AddCell(new Cell().Add(new Paragraph(row[column].ToString()).AddStyle(dataFont)));
                                        }
                                    }
                                }

                                document.Add(table);
                                document.Close();

                                // İkinci belgeyi oluşturun ve içeriği kopyalayın
                                var writer2 = new PdfWriter(PDFPath2);
                                var pdf2 = new PdfDocument(writer2);
                                var document2 = new Document(pdf2);

                                PdfFont font2 = PdfFontFactory.CreateFont(fontPath);
                                var headerFont2 = new iText.Layout.Style().SetFont(font2).SetFontSize(6).SetBold();
                                var dataFont2 = new iText.Layout.Style().SetFont(font2).SetFontSize(6);

                                Table table2 = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();

                                // Sadece seçilen özellikleri ekleyin
                                foreach (DataColumn column in dt.Columns)
                                {
                                    if (Yazdir_TR.degiskenler.secilenOzellikler.Contains(column.ColumnName))
                                    {
                                        table2.AddCell(new Cell().Add(new Paragraph(column.ColumnName).AddStyle(headerFont2)));
                                    }
                                }

                                foreach (DataRow row in dt.Rows)
                                {
                                    foreach (DataColumn column in dt.Columns)
                                    {
                                        if (Yazdir_TR.degiskenler.secilenOzellikler.Contains(column.ColumnName))
                                        {
                                            table2.AddCell(new Cell().Add(new Paragraph(row[column].ToString()).AddStyle(dataFont2)));
                                        }
                                    }
                                }

                                document2.Add(table2);
                                document2.Close();

                                MessageBoxResult result = MessageBox.Show("Belge Oluşturuldu. Dosyayı Açmak İster Misiniz?", "Belge Oluşturuldu", MessageBoxButton.YesNo, MessageBoxImage.Asterisk);
                                if (result == MessageBoxResult.Yes)
                                {
                                    Process.Start(new ProcessStartInfo(PDFPath2) { UseShellExecute = true });
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
                Filtre filtre = new Filtre();
                filtre.ShowDialog();

                if (Filtre_TR.SecilenSutunlar != null || Filtre_TR.SecilmeyenSutunlar != null)
                {
                    foreach (DataGridColumn column in dataGrid.Columns)
                    {
                        string columnName = column.Header.ToString();


                        for (int i = 0; i < Filtre_TR.SecilenSutunlar.Count; i++)
                        {
                            if (columnName == Filtre_TR.SecilenSutunlar[i])
                            {
                                column.Visibility = Visibility.Visible;
                            }
                        }
                        for (int i = 0; i < Filtre_TR.SecilmeyenSutunlar.Count; i++)
                        {
                            if (columnName == Filtre_TR.SecilmeyenSutunlar[i])
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
                    if (File.Exists(DatabaseFileName))
                    {
                        File.Delete(DatabaseFileName);
                    }

                    // Seçilen dosyayı AppData dizinine kopyala
                    File.Copy(selectedFilePath, DatabaseFileName, true);

                    // Yeni dosyanın adını CariKayitDB.db olarak değiştir
                    File.Move(DatabaseFileName, DatabaseFileName);

                    MessageBox.Show("Veritabanı başarıyla içe aktarıldı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);

                    dataGrid.ItemsSource = GetBusinesses(ConnectionString);
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

                    File.Copy(DatabaseFileName, destinationFilePath, true);
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
                using (WebClient client = new WebClient())
                {
                    if (guncel == false)
                    {
                        YuklemeEkrani yuklemeEkrani = new YuklemeEkrani();
                        yuklemeEkrani.ShowDialog();
                    }
                    else if (guncel == true)
                    {
                        UygulamayıGuncelle.Visibility = Visibility.Hidden;
                        if (File.Exists(dosyaAdi))
                        {
                            File.Delete(dosyaAdi);
                        }
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

            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
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
            public string İşletme_Adı { get; set; }
            public string VergiDairesi { get; set; }
            public string VergiNo { get; set; }
            public string Banka { get; set; }
            public string HesapNo { get; set; }
            public string Adres { get; set; }
            public string EPosta1 { get; set; }
            public string EPosta2 { get; set; }
            public string Telefon1 { get; set; }
            public string Telefon2 { get; set; }
        }


        private void Admin_Click(object sender, RoutedEventArgs e)
        {

        }

        public static int selectedValue;

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
