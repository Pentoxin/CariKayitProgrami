using Cari_kayıt_Programı.Models;
using ClosedXML.Excel;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cari_kayıt_Programı
{
    public partial class CariHesapKayitlari : Window
    {
        public MainViewModel ViewModel { get; set; }
        private static Cariler? SelectedCariler { get; set; }

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
                if (string.IsNullOrWhiteSpace(UnvanTextBox.Text) || string.IsNullOrWhiteSpace(CariKodTextBox.Text))
                {
                    MessageBox.Show("Lütfen zorunlu yerleri doldurunuz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (MySqlConnection connection = DatabaseManager.GetConnection())
                {
                    connection.Open();

                    List<Cariler> businessesList = new List<Cariler>();
                    string query = "SELECT * FROM Cariler";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                businessesList.Add(new Cariler
                                {
                                    Unvan = reader.GetString("Unvan")
                                });
                            }
                        }
                    }

                    foreach (var business in businessesList)
                    {
                        string unvanLower = business.Unvan?.ToLower(new CultureInfo("tr-TR")) ?? "";
                        string inputUnvanLower = UnvanTextBox.Text.ToLower(new CultureInfo("tr-TR"));

                        if (unvanLower == inputUnvanLower)
                        {
                            MessageBox.Show("Bu cari isim daha önce kaydedilmiş.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    if (MessageBox.Show("Seçilen veriyi kaydetmek istediğinize emin misiniz?", "Kaydet", MessageBoxButton.YesNo, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
                    {
                        string tip = AliciRadioButton.IsChecked == true ? "A" :
                                     SaticiRadioButton.IsChecked == true ? "S" :
                                     ToptanciRadioButton.IsChecked == true ? "T" :
                                     KefilRadioButton.IsChecked == true ? "K" :
                                     MuhtahsilRadioButton.IsChecked == true ? "M" : "D";

                        string insertSql = "INSERT INTO Cariler (Carikod, Unvan, Adres, Il, Ilce, Telefon1, Telefon2, PostaKodu, UlkeKodu, VergiDairesi, VergiNo, TcNo, Tip, Email, Banka, HesapNo) " +
                                           "VALUES (@Carikod, @Unvan, @Adres, @Il, @Ilce, @Telefon1, @Telefon2, @PostaKodu, @UlkeKodu, @VergiDairesi, @VergiNo, @TcNo, @Tip, @Email, @Banka, @HesapNo)";

                        using (MySqlCommand insertCommand = new MySqlCommand(insertSql, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@CariKod", CariKodTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@Unvan", UnvanTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@Adres", AdresTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@Il", ilTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@Ilce", ilceTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@Telefon1", Telefon1TextBox.Text);
                            insertCommand.Parameters.AddWithValue("@Telefon2", Telefon2TextBox.Text);
                            insertCommand.Parameters.AddWithValue("@PostaKodu", PostaKoduTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@UlkeKodu", UlkeKoduTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@VergiDairesi", VergiDairesiTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@VergiNo", VergiNoTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@TcNo", TcKimlikNoTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@Tip", tip);
                            insertCommand.Parameters.AddWithValue("@Email", EMailTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@Banka", BankaTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@HesapNo", HesapNoTextBox.Text);
                            insertCommand.ExecuteNonQuery();
                            MessageBox.Show("Cari bilgiler veritabanına kaydedildi.", "Kaydedildi", MessageBoxButton.OK, MessageBoxImage.Information);
                        }

                        // Form temizleme
                        CariKodTextBox.Clear();
                        UnvanTextBox.Clear();
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
                        EMailTextBox.Clear();
                        BankaTextBox.Clear();
                        HesapNoTextBox.Clear();
                        dataGrid.ItemsSource = GetBusinesses();
                        Keyboard.ClearFocus();
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
                Cariler selectedBusinessItem = (Cariler)dataGrid.SelectedItem;

                if (selectedBusinessItem == null)
                {
                    MessageBox.Show("Önce silmek istediğiniz veriyi seçiniz", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (MessageBox.Show("Seçilen veriyi silmek istediğinize emin misiniz?", "Uyarı", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    using (MySqlConnection connection = DatabaseManager.GetConnection())
                    {
                        connection.Open();

                        // Kayıt silme sorgusu (CariKod üzerinden)
                        string deleteQuery = "DELETE FROM Cariler WHERE CariKod = @CariKod";

                        using (MySqlCommand command = new MySqlCommand(deleteQuery, connection))
                        {
                            command.Parameters.AddWithValue("@CariKod", selectedBusinessItem.CariKod);
                            command.ExecuteNonQuery();
                        }
                    }

                    // Arayüz temizleme
                    dataGrid.ItemsSource = GetBusinesses();
                    UnvanTextBox.Clear();
                    VergiDairesiTextBox.Clear();
                    VergiNoTextBox.Clear();
                    BankaTextBox.Clear();
                    HesapNoTextBox.Clear();
                    AdresTextBox.Clear();
                    EMailTextBox.Clear();
                    Telefon1TextBox.Clear();
                    Telefon2TextBox.Clear();
                    dataGrid.SelectedItem = null;

                    Keyboard.ClearFocus();
                }
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
                if (SelectedCariler == null)
                {
                    MessageBox.Show("Önce değiştirmek istediğiniz veriyi seçiniz", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (MySqlConnection connection = DatabaseManager.GetConnection())
                {
                    connection.Open();

                    List<Cariler> businessesList = new List<Cariler>();

                    string query = "SELECT CariKod, Unvan FROM Cariler";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Cariler b = new Cariler
                                {
                                    CariKod = reader.GetString("CariKod"),
                                    Unvan = reader.GetString("Unvan")
                                };
                                businessesList.Add(b);
                            }
                        }
                    }

                    foreach (var business in businessesList)
                    {
                        if (SelectedCariler.CariKod != business.CariKod)
                        {
                            string UnvanLower = business.Unvan.ToLower(new CultureInfo("tr-TR"));
                            string inputUnvan = UnvanTextBox.Text.ToLower(new CultureInfo("tr-TR"));

                            if (UnvanLower == inputUnvan)
                            {
                                MessageBox.Show("Bu cari isim daha önce kaydedilmiş.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                    }

                    if (MessageBox.Show("Seçili veriyi değiştirmek istediğinize emin misiniz?", "Veri Güncelleme", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        string tip = "D";
                        if (AliciRadioButton.IsChecked == true) tip = "A";
                        else if (SaticiRadioButton.IsChecked == true) tip = "S";
                        else if (ToptanciRadioButton.IsChecked == true) tip = "T";
                        else if (KefilRadioButton.IsChecked == true) tip = "K";
                        else if (MuhtahsilRadioButton.IsChecked == true) tip = "M";

                        string updateSql = @"
                        UPDATE Cariler SET 
                            Unvan=@Unvan, 
                            Adres=@Adres, 
                            Il=@Il, 
                            Ilce=@Ilce, 
                            Telefon1=@Telefon1, 
                            Telefon2=@Telefon2, 
                            PostaKodu=@PostaKodu, 
                            UlkeKodu=@UlkeKodu, 
                            VergiDairesi=@VergiDairesi, 
                            VergiNo=@VergiNo, 
                            TcNo=@TcNo, 
                            Tip=@Tip, 
                            Email=@Email, 
                            Banka=@Banka, 
                            HesapNo=@HesapNo 
                        WHERE CariKod=@CariKod";

                        using (MySqlCommand updateCommand = new MySqlCommand(updateSql, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@CariKod", SelectedCariler.CariKod);
                            updateCommand.Parameters.AddWithValue("@Unvan", UnvanTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@Adres", AdresTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@Il", ilTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@Ilce", ilceTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@Telefon1", Telefon1TextBox.Text);
                            updateCommand.Parameters.AddWithValue("@Telefon2", Telefon2TextBox.Text);
                            updateCommand.Parameters.AddWithValue("@PostaKodu", PostaKoduTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@UlkeKodu", UlkeKoduTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@VergiDairesi", VergiDairesiTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@VergiNo", VergiNoTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@TcNo", TcKimlikNoTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@Tip", tip);
                            updateCommand.Parameters.AddWithValue("@Email", EMailTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@Banka", BankaTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@HesapNo", HesapNoTextBox.Text);

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

                        // Temizlik ve yenileme
                        CariKodTextBox.Clear();
                        UnvanTextBox.Clear();
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
                        EMailTextBox.Clear();
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
                    var businesses = dataGrid.ItemsSource as IEnumerable<Cariler>;

                    // Yeni bir Excel iş kitabı oluşturun
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Cari Hesap Kayıtları");

                        // Başlık satırını ekle
                        var properties = typeof(Cariler).GetProperties();
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
                var selectedBusiness = dataGrid.SelectedItem as Cariler;
                if (selectedBusiness != null)
                {
                    CariKodTextBox.Text = selectedBusiness.CariKod;
                    UnvanTextBox.Text = selectedBusiness.Unvan;
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
                    EMailTextBox.Text = selectedBusiness.Email;
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

        private void CariKodTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;

                bool var = false;

                foreach (var item in viewModel.Cariler)
                {
                    if (item is Cariler business)
                    {
                        string? carikod = business.CariKod;

                        if (CariKodTextBox.Text == carikod)
                        {
                            SelectedCariler = business;

                            UnvanTextBox.Text = business.Unvan;
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
                            EMailTextBox.Text = business.Email;
                            BankaTextBox.Text = business.Banka;
                            HesapNoTextBox.Text = business.HesapNo;
                            dataGrid.SelectedItem = business;

                            var = true;
                            break;
                        }
                        else
                        {
                            UnvanTextBox.Clear();
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
                            EMailTextBox.Clear();
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
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                string userInput = CariKodTextBox.Text;

                if (string.IsNullOrWhiteSpace(userInput))
                {
                    MessageBox.Show("Lütfen bir CariKod yazınız.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // MVVM'deki Cariler listesini kullan
                var maxCari = viewModel.Cariler
                    .Where(c => c.CariKod != null && c.CariKod.StartsWith(userInput, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(c => c.CariKod)
                    .FirstOrDefault();

                string newCariKod;

                if (maxCari != null)
                {
                    newCariKod = GetNextCariKod(maxCari.CariKod);
                }
                else
                {
                    newCariKod = userInput + "001";
                }

                CariKodTextBox.Text = newCariKod;
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
                if (string.IsNullOrWhiteSpace(currentCariKod))
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

        public ObservableCollection<Cariler> GetBusinesses()
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                viewModel.Cariler.Clear();

                using (MySqlConnection connection = DatabaseManager.GetConnection())
                {
                    connection.Open();

                    string query = "SELECT * FROM Cariler";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Cariler b = new Cariler
                                {
                                    CariKod = reader.GetString("CariKod"),
                                    Unvan = reader.GetString("Unvan"),
                                    Adres = reader.IsDBNull(reader.GetOrdinal("Adres")) ? null : reader.GetString("Adres"),
                                    Il = reader.IsDBNull(reader.GetOrdinal("Il")) ? null : reader.GetString("Il"),
                                    Ilce = reader.IsDBNull(reader.GetOrdinal("Ilce")) ? null : reader.GetString("Ilce"),
                                    Telefon1 = reader.IsDBNull(reader.GetOrdinal("Telefon1")) ? null : reader.GetString("Telefon1"),
                                    Telefon2 = reader.IsDBNull(reader.GetOrdinal("Telefon2")) ? null : reader.GetString("Telefon2"),
                                    PostaKodu = reader.IsDBNull(reader.GetOrdinal("PostaKodu")) ? null : reader.GetString("PostaKodu"),
                                    UlkeKodu = reader.IsDBNull(reader.GetOrdinal("UlkeKodu")) ? null : reader.GetString("UlkeKodu"),
                                    VergiDairesi = reader.IsDBNull(reader.GetOrdinal("VergiDairesi")) ? null : reader.GetString("VergiDairesi"),
                                    VergiNo = reader.IsDBNull(reader.GetOrdinal("VergiNo")) ? null : reader.GetString("VergiNo"),
                                    TcNo = reader.IsDBNull(reader.GetOrdinal("TcNo")) ? null : reader.GetString("TcNo"),
                                    Tip = reader.IsDBNull(reader.GetOrdinal("Tip")) ? null : reader.GetString("Tip"),
                                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString("Email"),
                                    Banka = reader.IsDBNull(reader.GetOrdinal("Banka")) ? null : reader.GetString("Banka"),
                                    HesapNo = reader.IsDBNull(reader.GetOrdinal("HesapNo")) ? null : reader.GetString("HesapNo"),
                                };
                                viewModel.Cariler.Add(b);
                            }
                        }
                    }
                }
                return viewModel.Cariler;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHesapKayitlari", methodName: "GetBusinesses()", stackTrace: ex.StackTrace);
                return null;
                throw;
            }
        }
    }
}
