using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


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
        }

        public static class Degiskenler
        {
            public static bool guncellemeOnay { get; set; } = false;
            public static bool guncel { get; set; }
            public static Business? selectedBusiness { get; set; }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dataGrid.ItemsSource = GetBusinesses();
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
                    using (SQLiteConnection connection = new SQLiteConnection(Config.ConnectionString))
                    {
                        connection.Open();
                        string selectSql = "SELECT COUNT(*) FROM CariKayit WHERE lower(cariisim) = lower(@cariisim)";
                        using (SQLiteCommand command = new SQLiteCommand(selectSql, connection))
                        {
                            command.Parameters.AddWithValue("@cariisim", CariIsimTextBox.Text);
                            int count = Convert.ToInt32(command.ExecuteScalar());
                            if (count > 0)
                            {
                                MessageBox.Show("Bu cari adı daha önce girilmiş.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            else
                            {
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

                                        // Yeni tabloyu oluştur
                                        string createTableSql = $"CREATE TABLE Cari_{kod} (" +
                                                                "ID INTEGER PRIMARY KEY AUTOINCREMENT, " +
                                                                "tarih TEXT, " +
                                                                "tip TEXT, " +
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
                                        Directory.CreateDirectory(Path.Combine(Config.IsletmePath, kod));

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
                }
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
                            string queryOdeme = $"DROP TABLE IF EXISTS Cari_{selectedBusinessItem.CariKod}";
                            using (SQLiteCommand commandOdeme = new SQLiteCommand(queryOdeme, connection))
                            {
                                commandOdeme.ExecuteNonQuery();
                            }
                        }
                        dataGrid.ItemsSource = GetBusinesses();

                        if (Directory.Exists(Path.Combine(Config.IsletmePath, selectedBusinessItem.CariKod)))
                        {
                            Directory.Delete(Path.Combine(Config.IsletmePath, selectedBusinessItem.CariKod));
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
                LogError(ex);
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

                using (SQLiteConnection connection = new SQLiteConnection(Config.ConnectionString))
                {
                    connection.Open();

                    string checkSql = "SELECT COUNT(*) FROM CariKayit WHERE lower(cariisim) = lower(@cariisim) AND ID != @ID";

                    using (SQLiteCommand checkCommand = new SQLiteCommand(checkSql, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@cariisim", CariIsimTextBox.Text);
                        checkCommand.Parameters.AddWithValue("@ID", Degiskenler.selectedBusiness.ID);

                        int result = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (result > 0)
                        {
                            MessageBox.Show("Bu işletme adı daha önce girilmiş!", "Veri Kaydetme Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
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
                LogError(ex);
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
                LogError(ex);
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
                    Anasayfa anasayfa = new Anasayfa();

                    string selectedFilePath = openFileDialog.FileName;

                    if (Directory.Exists(Config.IsletmePath))
                    {
                        Directory.Delete(Config.IsletmePath, true);
                    }

                    ZipFile.ExtractToDirectory(selectedFilePath, Config.AppDataPath, true);

                    anasayfa.CheckAtImport();
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

                    string query = "SELECT * FROM CariKayit WHERE ID LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(carikod) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(cariisim) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(adres) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(il) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(ilce) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(vergidairesi) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(vergino) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(banka) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(hesapno) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(adres) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(mail) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(tcno) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(telefon1) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(telefon2) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(postakodu) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(ulkekodu) LIKE '%' || LOWER(@searchTerm) || '%' OR LOWER(tip) LIKE '%' || LOWER(@searchTerm) || '%' ";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@searchTerm", searchTerm);

                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id = reader.GetInt32(0);
                                string? carikod = reader.IsDBNull(1) ? null : reader.GetString(1);
                                string cariisim = reader.GetString(2);
                                string adres = reader.GetString(3);
                                string? il = reader.IsDBNull(4) ? null : reader.GetString(4);
                                string? ilce = reader.IsDBNull(5) ? null : reader.GetString(5);
                                string telefon1 = reader.GetString(6);
                                string telefon2 = reader.GetString(7);
                                string? postakodu = reader.IsDBNull(8) ? null : reader.GetString(8);
                                string? ulkekodu = reader.IsDBNull(9) ? null : reader.GetString(9);
                                string vergidairesi = reader.GetString(10);
                                string vergino = reader.GetString(11);
                                string? tcno = reader.IsDBNull(12) ? null : reader.GetString(12);
                                string? tip = reader.IsDBNull(13) ? null : reader.GetString(13);
                                string mail = reader.GetString(14);
                                string banka = reader.GetString(15);
                                string hesapno = reader.GetString(16);

                                Business b = new Business
                                {
                                    ID = id,
                                    CariKod = carikod,
                                    CariIsim = cariisim,
                                    Adres = adres,
                                    Il = il,
                                    Ilce = ilce,
                                    Telefon1 = telefon1,
                                    Telefon2 = telefon2,
                                    PostaKodu = postakodu,
                                    UlkeKodu = ulkekodu,
                                    VergiDairesi = vergidairesi,
                                    VergiNo = vergino,
                                    TcNo = tcno,
                                    Tip = tip,
                                    EPosta = mail,
                                    Banka = banka,
                                    HesapNo = hesapno
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
                                string? carikod = reader.IsDBNull(1) ? null : reader.GetString(1);
                                string cariisim = reader.GetString(2);
                                string adres = reader.GetString(3);
                                string? il = reader.IsDBNull(4) ? null : reader.GetString(4);
                                string? ilce = reader.IsDBNull(5) ? null : reader.GetString(5);
                                string telefon1 = reader.GetString(6);
                                string telefon2 = reader.GetString(7);
                                string? postakodu = reader.IsDBNull(8) ? null : reader.GetString(8);
                                string? ulkekodu = reader.IsDBNull(9) ? null : reader.GetString(9);
                                string vergidairesi = reader.GetString(10);
                                string vergino = reader.GetString(11);
                                string? tcno = reader.IsDBNull(12) ? null : reader.GetString(12);
                                string? tip = reader.IsDBNull(13) ? null : reader.GetString(13);
                                string mail = reader.GetString(14);
                                string banka = reader.GetString(15);
                                string hesapno = reader.GetString(16);

                                Business b = new Business
                                {
                                    ID = id,
                                    CariKod = carikod,
                                    CariIsim = cariisim,
                                    Adres = adres,
                                    Il = il,
                                    Ilce = ilce,
                                    Telefon1 = telefon1,
                                    Telefon2 = telefon2,
                                    PostaKodu = postakodu,
                                    UlkeKodu = ulkekodu,
                                    VergiDairesi = vergidairesi,
                                    VergiNo = vergino,
                                    TcNo = tcno,
                                    Tip = tip,
                                    EPosta = mail,
                                    Banka = banka,
                                    HesapNo = hesapno
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
