using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cari_kayıt_Programı
{
    public partial class CariHesapKayitlari : Window
    {
        public MainViewModel ViewModel { get; set; }

        public CariHesapKayitlari()
        {
            try
            {
                InitializeComponent();

                ViewModel = new MainViewModel();
                DataContext = ViewModel;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHesapKayitlari", methodName: "Main()", stackTrace: ex.StackTrace);
                MessageBox.Show("Beklenmeyen bir hata oluştu. Lütfen destek ekibiyle iletişime geçin.", "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static class Degiskenler
        {
            public static bool guncellemeOnay { get; set; } = false;
            public static bool guncel { get; set; }
            public static Business? selectedBusiness { get; set; }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                dataGrid.ItemsSource = GetBusinesses();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHesapKayitlari", methodName: "Window_Loaded()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void KaydetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CariIsimTextBox.Text == "" || CariKodTextBox.Text == "")
                {
                    MessageBox.Show("Lütfen zorunlu yerleri doldurunuz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                    {
                        connection.Open();

                        List<Business> businessesList = new List<Business>();

                        string query = "SELECT * FROM CariKayit";

                        using (SQLiteCommand command = new SQLiteCommand(query, connection))
                        {
                            using (SQLiteDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    Business b = new Business
                                    {
                                        CariIsim = reader.GetString(2),
                                    };
                                    businessesList.Add(b);
                                }
                            }
                        }

                        foreach (var item in businessesList)
                        {
                            if (item is Business business)
                            {
                                string? cariisim = business.CariIsim;

                                string CariIsimLower = cariisim.ToLower(new CultureInfo("tr-TR"));
                                string normalizedCariIsım = CariIsimTextBox.Text.ToLower(new CultureInfo("tr-TR"));
                                if (CariIsimLower == normalizedCariIsım)
                                {
                                    MessageBox.Show("Bu cari isim daha önce kaydedilmiş.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                            }
                        }

                        if (MessageBox.Show("Seçilen veriyi kaydetmek istediğinize emin misiniz?", "Kaydet", MessageBoxButton.YesNo, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
                        {
                            string tip = "";

                            if (AliciRadioButton.IsChecked == true)
                            {
                                tip = "A";
                            }
                            else if (SaticiRadioButton.IsChecked == true)
                            {
                                tip = "S";
                            }
                            else if (ToptanciRadioButton.IsChecked == true)
                            {
                                tip = "T";
                            }
                            else if (KefilRadioButton.IsChecked == true)
                            {
                                tip = "K";
                            }
                            else if (MuhtahsilRadioButton.IsChecked == true)
                            {
                                tip = "M";
                            }
                            else
                            {
                                tip = "D";
                            }

                            string insertSql = "INSERT INTO CariKayit (carikod, cariisim, adres, il, ilce, telefon1, telefon2, postakodu, ulkekodu, vergidairesi, vergino, tcno, tip, mail, banka, hesapno) VALUES (@carikod, @cariisim, @adres, @il, @ilce, @telefon1, @telefon2, @postakodu, @ulkekodu, @vergidairesi, @vergino, @tcno , @tip, @mail, @banka, @hesapno)";
                            using (SQLiteCommand insertCommand = new SQLiteCommand(insertSql, connection))
                            {
                                insertCommand.Parameters.AddWithValue("@carikod", CariKodTextBox.Text);
                                insertCommand.Parameters.AddWithValue("@cariisim", CariIsimTextBox.Text);
                                insertCommand.Parameters.AddWithValue("@adres", AdresTextBox.Text);
                                insertCommand.Parameters.AddWithValue("@il", ilTextBox.Text);
                                insertCommand.Parameters.AddWithValue("@ilce", ilceTextBox.Text);
                                insertCommand.Parameters.AddWithValue("@telefon1", Telefon1TextBox.Text);
                                insertCommand.Parameters.AddWithValue("@telefon2", Telefon2TextBox.Text);
                                insertCommand.Parameters.AddWithValue("@postakodu", PostaKoduTextBox.Text);
                                insertCommand.Parameters.AddWithValue("@ulkekodu", UlkeKoduTextBox.Text);
                                insertCommand.Parameters.AddWithValue("@vergidairesi", VergiDairesiTextBox.Text);
                                insertCommand.Parameters.AddWithValue("@vergino", VergiNoTextBox.Text);
                                insertCommand.Parameters.AddWithValue("@tcno", TcKimlikNoTextBox.Text);
                                insertCommand.Parameters.AddWithValue("@tip", tip);
                                insertCommand.Parameters.AddWithValue("@mail", MailTextBox.Text);
                                insertCommand.Parameters.AddWithValue("@banka", BankaTextBox.Text);
                                insertCommand.Parameters.AddWithValue("@hesapno", HesapNoTextBox.Text);
                                insertCommand.ExecuteNonQuery();

                                string kod = CariKodTextBox.Text;

                                string tableName = $"Cari_{kod}";

                                // Tablo adını çift tırnak içine alarak SQL için güvenli hale getirme
                                string escapedTableName = $"\"{tableName}\"";

                                // Yeni tabloyu oluştur
                                string createTableSql = $"CREATE TABLE {escapedTableName} (" +
                                                        "ID INTEGER PRIMARY KEY AUTOINCREMENT, " +
                                                        "tarih TEXT, " +
                                                        "tip TEXT, " +
                                                        "durum TEXT, " +
                                                        "evrakno TEXT, " +
                                                        "aciklama TEXT, " +
                                                        "vadetarihi TEXT, " +
                                                        "borc INTEGER, " +
                                                        "alacak INTEGER," +
                                                        "dosya TEXT)";
                                using (SQLiteCommand createTableCommand = new SQLiteCommand(createTableSql, connection))
                                {
                                    createTableCommand.ExecuteNonQuery();
                                }
                                Directory.CreateDirectory(Path.Combine(ConfigManager.IsletmePath, kod));

                                MessageBox.Show("Cari bilgiler veritabanına kaydedildi.", "Kaydedildi", MessageBoxButton.OK, MessageBoxImage.Information);
                            }

                            CariKodTextBox.Clear();
                            CariIsimTextBox.Clear();
                            AdresTextBox.Clear();
                            ilTextBox.Clear();
                            ilceTextBox.Clear();
                            Telefon1TextBox.Clear();
                            Telefon2TextBox.Clear();
                            PostaKoduTextBox.Clear();
                            UlkeKoduTextBox.Clear();
                            VergiDairesiTextBox.Clear();
                            VergiNoTextBox.Clear();
                            TcKimlikNoTextBox.Clear();
                            AliciRadioButton.IsChecked = true;
                            MailTextBox.Clear();
                            BankaTextBox.Clear();
                            HesapNoTextBox.Clear();
                            dataGrid.ItemsSource = GetBusinesses();

                            Keyboard.ClearFocus();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHesapKayitlari", methodName: "KaydetButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                        {
                            connection.Open();
                            string query = "DELETE FROM CariKayit WHERE ID=@Id";

                            using (SQLiteCommand command = new SQLiteCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@Id", selectedBusinessItem.ID);
                                command.ExecuteNonQuery();
                            }

                            string tableName = $"Cari_{selectedBusinessItem.CariKod}";
                            string escapedTableName = $"\"{tableName}\"";

                            // Önce Odeme tablosunu silme işlemi
                            string queryOdeme = $"DROP TABLE IF EXISTS {escapedTableName}";
                            using (SQLiteCommand commandOdeme = new SQLiteCommand(queryOdeme, connection))
                            {
                                commandOdeme.ExecuteNonQuery();
                            }
                        }
                        dataGrid.ItemsSource = GetBusinesses();

                        if (Directory.Exists(Path.Combine(ConfigManager.IsletmePath, selectedBusinessItem.CariKod)))
                        {
                            Directory.Delete(Path.Combine(ConfigManager.IsletmePath, selectedBusinessItem.CariKod));
                        }
                    }
                }
                CariIsimTextBox.Clear();
                VergiDairesiTextBox.Clear();
                VergiNoTextBox.Clear();
                BankaTextBox.Clear();
                HesapNoTextBox.Clear();
                AdresTextBox.Clear();
                MailTextBox.Clear();
                Telefon1TextBox.Clear();
                Telefon2TextBox.Clear();
                dataGrid.SelectedItem = null;

                Keyboard.ClearFocus();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHesapKayitlari", methodName: "DeleteButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Updatebutton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Degiskenler.selectedBusiness == null)
                {
                    MessageBox.Show("Önce değiştirmek istediğiniz veriyi seçiniz", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                {
                    connection.Open();

                    List<Business> businessesList = new List<Business>();

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
                                    CariIsim = reader.GetString(2),
                                };
                                businessesList.Add(b);
                            }
                        }
                    }

                    foreach (var item in businessesList)
                    {
                        if (item is Business business && Degiskenler.selectedBusiness.ID != business.ID)
                        {
                            string? cariisim = business.CariIsim;

                            string CariIsimLower = cariisim.ToLower(new CultureInfo("tr-TR"));
                            string normalizedCariIsım = CariIsimTextBox.Text.ToLower(new CultureInfo("tr-TR"));
                            if (CariIsimLower == normalizedCariIsım)
                            {
                                MessageBox.Show("Bu cari isim daha önce kaydedilmiş.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                    }

                    if (MessageBox.Show("Seçili veriyi değiştirmek istediğinize emin misiniz?", "Veri Güncelleme", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        string tip = "";

                        if (AliciRadioButton.IsChecked == true)
                        {
                            tip = "A";
                        }
                        else if (SaticiRadioButton.IsChecked == true)
                        {
                            tip = "S";
                        }
                        else if (ToptanciRadioButton.IsChecked == true)
                        {
                            tip = "T";
                        }
                        else if (KefilRadioButton.IsChecked == true)
                        {
                            tip = "K";
                        }
                        else if (MuhtahsilRadioButton.IsChecked == true)
                        {
                            tip = "M";
                        }
                        else
                        {
                            tip = "D";
                        }

                        string updateSql = "UPDATE CariKayit SET cariisim=@cariisim, adres=@adres, il=@il, ilce=@ilce, telefon1=@telefon1, telefon2=@telefon2, postakodu=@postakodu, ulkekodu=@ulkekodu, vergidairesi=@vergidairesi, vergino=@vergino, tcno=@tcno, tip=@tip, mail=@mail, banka=@banka, hesapno=@hesapno  WHERE ID=@ID";

                        using (SQLiteCommand updateCommand = new SQLiteCommand(updateSql, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@ID", Degiskenler.selectedBusiness.ID);
                            updateCommand.Parameters.AddWithValue("@cariisim", CariIsimTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@adres", AdresTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@il", ilTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@ilce", ilceTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@telefon1", Telefon1TextBox.Text);
                            updateCommand.Parameters.AddWithValue("@telefon2", Telefon2TextBox.Text);
                            updateCommand.Parameters.AddWithValue("@postakodu", PostaKoduTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@ulkekodu", UlkeKoduTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@vergidairesi", VergiDairesiTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@vergino", VergiNoTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@tcno", TcKimlikNoTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@tip", tip);
                            updateCommand.Parameters.AddWithValue("@mail", MailTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@banka", BankaTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@hesapno", HesapNoTextBox.Text);

                            int rowsAffected = updateCommand.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Veri güncellendi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBox.Show("Veri güncelleme hatası!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }

                        CariKodTextBox.Clear();
                        CariIsimTextBox.Clear();
                        AdresTextBox.Clear();
                        ilTextBox.Clear();
                        ilceTextBox.Clear();
                        Telefon1TextBox.Clear();
                        Telefon2TextBox.Clear();
                        PostaKoduTextBox.Clear();
                        UlkeKoduTextBox.Clear();
                        VergiDairesiTextBox.Clear();
                        VergiNoTextBox.Clear();
                        TcKimlikNoTextBox.Clear();
                        AliciRadioButton.IsChecked = true;
                        MailTextBox.Clear();
                        BankaTextBox.Clear();
                        HesapNoTextBox.Clear();
                        dataGrid.ItemsSource = GetBusinesses();

                        Keyboard.ClearFocus();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHesapKayitlari", methodName: "Updatebutton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void YazdırButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Excel Dosyası|*.xlsx";
                saveFileDialog.FileName = $"Cari Hesap Kayıtları.xlsx";
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (saveFileDialog.ShowDialog() == true)
                {

                    string path = saveFileDialog.FileName;

                    // DataGrid'deki verileri alın
                    var businesses = dataGrid.ItemsSource as IEnumerable<Business>;

                    // Yeni bir Excel iş kitabı oluşturun
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Cari Hesap Kayıtları");

                        // Başlık satırını ekle
                        var properties = typeof(Business).GetProperties();
                        int colIndex = 1;
                        for (int col = 0; col < properties.Length; col++)
                        {
                            worksheet.Cell(1, colIndex).Value = properties[col].Name;
                            colIndex++;
                        }

                        // Veri satırlarını ekle
                        int row = 2;
                        if (businesses != null)
                        {
                            foreach (var b in businesses)
                            {
                                colIndex = 1;
                                for (int col = 0; col < properties.Length; col++)
                                {

                                    var value = properties[col].GetValue(b);

                                    // Diğer hücre değerlerini ayarla
                                    worksheet.Cell(row, colIndex).SetValue(value != null ? value.ToString() : "");

                                    colIndex++;

                                }
                                row++;
                            }
                        }

                        // Tüm sütunları otomatik genişliğe ayarla
                        worksheet.Columns().AdjustToContents(4);

                        // Excel dosyasını kaydet
                        workbook.SaveAs(path);
                    }
                    MessageBoxResult result = MessageBox.Show("Belge Oluşturuldu. Dosyayı Açmak ister misiniz?", "Belge Oluşturuldu", MessageBoxButton.YesNo, MessageBoxImage.Asterisk);
                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHesapKayitlari", methodName: "YazdırButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedBusiness = dataGrid.SelectedItem as Business;
                if (selectedBusiness != null)
                {
                    CariKodTextBox.Text = selectedBusiness.CariKod;
                    CariIsimTextBox.Text = selectedBusiness.CariIsim;
                    AdresTextBox.Text = selectedBusiness.Adres;
                    ilTextBox.Text = selectedBusiness.Il;
                    ilceTextBox.Text = selectedBusiness.Ilce;
                    Telefon1TextBox.Text = selectedBusiness.Telefon1;
                    Telefon2TextBox.Text = selectedBusiness.Telefon2;
                    PostaKoduTextBox.Text = selectedBusiness.PostaKodu;
                    UlkeKoduTextBox.Text = selectedBusiness.UlkeKodu;
                    VergiDairesiTextBox.Text = selectedBusiness.VergiDairesi;
                    VergiNoTextBox.Text = selectedBusiness.VergiNo;
                    TcKimlikNoTextBox.Text = selectedBusiness.TcNo;

                    if (selectedBusiness.Tip == "A")
                    {
                        AliciRadioButton.IsChecked = true;
                    }
                    else if (selectedBusiness.Tip == "S")
                    {
                        SaticiRadioButton.IsChecked = true;
                    }
                    else if (selectedBusiness.Tip == "T")
                    {
                        ToptanciRadioButton.IsChecked = true;
                    }
                    else if (selectedBusiness.Tip == "K")
                    {
                        KefilRadioButton.IsChecked = true;
                    }
                    else if (selectedBusiness.Tip == "M")
                    {
                        MuhtahsilRadioButton.IsChecked = true;
                    }
                    else
                    {
                        DigerRadioButton.IsChecked = true;
                    }
                    MailTextBox.Text = selectedBusiness.EPosta;
                    BankaTextBox.Text = selectedBusiness.Banka;
                    HesapNoTextBox.Text = selectedBusiness.HesapNo;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHesapKayitlari", methodName: "DataGrid_SelectionChanged()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                dataGrid.ItemsSource = GetBusinesses();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHesapKayitlari", methodName: "dataGrid_Loaded()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void IceriAktarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Zip Dosyalası (*.zip)|*.zip";
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (openFileDialog.ShowDialog() == true && MessageBox.Show("Varolan veriler silinecek onaylıyor musunuz?", "Uyarı", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Anasayfa anasayfa = new Anasayfa();

                    string selectedFilePath = openFileDialog.FileName;

                    if (Directory.Exists(ConfigManager.IsletmePath))
                    {
                        Directory.Delete(ConfigManager.IsletmePath, true);
                    }

                    ZipFile.ExtractToDirectory(selectedFilePath, ConfigManager.AppDataPath, true);

                    anasayfa.CheckAtImport();
                    MessageBox.Show("Veritabanı başarıyla içe aktarıldı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);

                    dataGrid.ItemsSource = GetBusinesses();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHesapKayitlari", methodName: "IceriAktarButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisariAktarButton_Click(object sender, RoutedEventArgs e)
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
                    if (Directory.Exists(ConfigManager.IsletmePath))
                    {
                        ZipFile.CreateFromDirectory(ConfigManager.IsletmePath, destinationFilePath, CompressionLevel.Optimal, true);
                    }

                    // CariKayitDB dosyasını ekleme
                    if (File.Exists(ConfigManager.DatabaseFileName))
                    {
                        using (var zipArchive = ZipFile.Open(destinationFilePath, ZipArchiveMode.Update))
                        {
                            zipArchive.CreateEntryFromFile(ConfigManager.DatabaseFileName, Path.GetFileName(ConfigManager.DatabaseFileName), CompressionLevel.Optimal);
                        }
                    }

                    MessageBox.Show("Veritabanı başarıyla dışa aktarıldı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHesapKayitlari", methodName: "DisariAktarButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CariKodTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;

                bool var = false;

                foreach (var item in viewModel.Businesses)
                {
                    if (item is Business business)
                    {
                        string? carikod = business.CariKod;

                        if (CariKodTextBox.Text == carikod)
                        {
                            Degiskenler.selectedBusiness = business;

                            CariIsimTextBox.Text = business.CariIsim;
                            AdresTextBox.Text = business.Adres;
                            ilTextBox.Text = business.Il;
                            ilceTextBox.Text = business.Ilce;
                            Telefon1TextBox.Text = business.Telefon1;
                            Telefon2TextBox.Text = business.Telefon2;
                            PostaKoduTextBox.Text = business.PostaKodu;
                            UlkeKoduTextBox.Text = business.UlkeKodu;
                            VergiDairesiTextBox.Text = business.VergiDairesi;
                            VergiNoTextBox.Text = business.VergiNo;
                            TcKimlikNoTextBox.Text = business.TcNo;

                            if (business.Tip == "A")
                            {
                                AliciRadioButton.IsChecked = true;
                            }
                            else if (business.Tip == "S")
                            {
                                SaticiRadioButton.IsChecked = true;
                            }
                            else if (business.Tip == "T")
                            {
                                ToptanciRadioButton.IsChecked = true;
                            }
                            else if (business.Tip == "K")
                            {
                                KefilRadioButton.IsChecked = true;
                            }
                            else if (business.Tip == "M")
                            {
                                MuhtahsilRadioButton.IsChecked = true;
                            }
                            else
                            {
                                DigerRadioButton.IsChecked = true;
                            }
                            MailTextBox.Text = business.EPosta;
                            BankaTextBox.Text = business.Banka;
                            HesapNoTextBox.Text = business.HesapNo;
                            dataGrid.SelectedItem = business;

                            var = true;
                            break;
                        }
                        else
                        {
                            CariIsimTextBox.Clear();
                            AdresTextBox.Clear();
                            ilTextBox.Clear();
                            ilceTextBox.Clear();
                            Telefon1TextBox.Clear();
                            Telefon2TextBox.Clear();
                            PostaKoduTextBox.Clear();
                            UlkeKoduTextBox.Clear();
                            VergiDairesiTextBox.Clear();
                            VergiNoTextBox.Clear();
                            TcKimlikNoTextBox.Clear();
                            AliciRadioButton.IsChecked = true;
                            MailTextBox.Clear();
                            BankaTextBox.Clear();
                            HesapNoTextBox.Clear();
                            dataGrid.SelectedItem = null;
                        }
                    }
                }

                if (var)
                {
                    KaydetButton.IsEnabled = false;
                    Updatebutton.IsEnabled = true;
                }
                else
                {
                    KaydetButton.IsEnabled = true;
                    Updatebutton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHesapKayitlari", methodName: "CariKodTextBox_TextChanged()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnCariKodSuggestionClick(object sender, RoutedEventArgs e)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                {
                    connection.Open();

                    string userInput = CariKodTextBox.Text;

                    if (string.IsNullOrWhiteSpace(userInput))
                    {
                        MessageBox.Show("Lütfen bir CariKod yazınız.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Kullanıcının yazdığı değere göre sorgu oluştur
                    string query = "SELECT MAX(CariKod) FROM CariKayit WHERE CariKod LIKE @UserInput";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        // Kullanıcının yazdığı değere '%' ekleyerek başlangıcına uygun değerleri getir
                        command.Parameters.AddWithValue("@UserInput", userInput + "%");
                        var result = command.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            string maxCariKod = result.ToString();

                            // Bir sonraki CariKod'u belirle
                            string newCariKod = GetNextCariKod(maxCariKod);

                            // TextBox'a yeni CariKod'u yaz
                            CariKodTextBox.Text = newCariKod;
                        }
                        else
                        {
                            // Eğer uygun bir sonuç yoksa kullanıcı girdisine ilk numarayı ekle
                            CariKodTextBox.Text = userInput + "001";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHesapKayitlari", methodName: "OnCariKodSuggestionClick()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetNextCariKod(string currentCariKod)
        {
            try
            {
                if (string.IsNullOrEmpty(currentCariKod))
                    return "D001";

                // Eğer cari kod 'D001' formatındaysa
                if (currentCariKod.StartsWith("D") && int.TryParse(currentCariKod.Substring(1), out int number))
                {
                    return $"D{(number + 1):D3}";
                }

                // Eğer cari kod '126-001-001' formatındaysa
                var parts = currentCariKod.Split('-');
                if (parts.Length > 0 && int.TryParse(parts[^1], out int lastNumber))
                {
                    parts[^1] = (lastNumber + 1).ToString("D3");
                    return string.Join("-", parts);
                }

                // Diğer durumlar için varsayılan değer
                return "D001";
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHesapKayitlari", methodName: "GetNextCariKod()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }
        }

        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            try
            {
                if (sender is TextBox textBox && textBox.ContextMenu != null)
                {
                    // Dinamik olarak "Header" güncelle
                    var menuItem = textBox.ContextMenu.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "DynamicMenuItem");
                    if (menuItem != null)
                    {
                        menuItem.Header = $"({textBox.Text}) Başlayanların Sonuncusu";
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHesapKayitlari", methodName: "OnContextMenuOpening()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        public ObservableCollection<Business> Businesses(string searchTerm)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                viewModel.Businesses.Clear();

                using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM CariKayit WHERE ID LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(carikod) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(cariisim) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(adres) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(il) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(ilce) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(vergidairesi) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(vergino) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(banka) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(hesapno) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(adres) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(mail) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(tcno) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(telefon1) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(telefon2) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(postakodu) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(ulkekodu) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(tip) LIKE '%' || LOWER(@searchTerm) || '%' ";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@searchTerm", searchTerm);

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
                return viewModel.Businesses;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHesapKayitlari", methodName: "Businesses()", stackTrace: ex.StackTrace);
                return null;
                throw;
            }
        }

        public ObservableCollection<Business> GetBusinesses()
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
                return viewModel.Businesses;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHesapKayitlari", methodName: "GetBusinesses()", stackTrace: ex.StackTrace);
                return null;
                throw;
            }
        }

        public class Business
        {
            public int ID { get; set; }
            public string? CariKod { get; set; }
            public string? CariIsim { get; set; }
            public string? Adres { get; set; }
            public string? Il { get; set; }
            public string? Ilce { get; set; }
            public string? Telefon1 { get; set; }
            public string? Telefon2 { get; set; }
            public string? PostaKodu { get; set; }
            public string? UlkeKodu { get; set; }
            public string? VergiDairesi { get; set; }
            public string? VergiNo { get; set; }
            public string? TcNo { get; set; }
            public string? Tip { get; set; }
            public string? EPosta { get; set; }
            public string? Banka { get; set; }
            public string? HesapNo { get; set; }
        }

        private void Page_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!IsMouseOverDataGrid(e))
                {
                    dataGrid.SelectedItem = null;

                    Keyboard.ClearFocus();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHesapKayitlari", methodName: "Page_PreviewMouseDown()", stackTrace: ex.StackTrace);
                throw;
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
                LogManager.LogError(ex, className: "CariHesapKayitlari", methodName: "IsMouseOverDataGrid()", stackTrace: ex.StackTrace);
                return false;
                throw;
            }
        }
    }
}
