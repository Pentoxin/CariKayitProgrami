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
    public partial class Stok : Window
    {
        public Stok()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Stok", methodName: "Main()", stackTrace: ex.StackTrace);
                MessageBox.Show("Beklenmeyen bir hata oluştu. Lütfen destek ekibiyle iletişime geçin.", "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                dataGrid.ItemsSource = GetStoklar();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Stok", methodName: "Window_Loaded()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void KaydetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(StokAdTextBox.Text) 
                    || string.IsNullOrWhiteSpace(StokKodTextBox.Text) 
                    || string.IsNullOrWhiteSpace(SatisKDVOranTextBox.Text)
                    || string.IsNullOrWhiteSpace(AlisKDVOranTextBox.Text)
                    || OlcuBirimi1ComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Lütfen zorunlu yerleri doldurunuz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (MySqlConnection connection = DatabaseManager.GetConnection())
                {
                    connection.Open();

                    // Aynı stok kodu veya adı var mı kontrol
                    string checkQuery = "SELECT COUNT(*) FROM Stoklar WHERE LOWER(StokKod) = @stokkod OR LOWER(StokAd) = @stokad";
                    using (MySqlCommand checkCommand = new MySqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@StokKod", StokKodTextBox.Text.ToLower(new CultureInfo("tr-TR")));
                        checkCommand.Parameters.AddWithValue("@StokAd", StokAdTextBox.Text.ToLower(new CultureInfo("tr-TR")));

                        int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                        if (count > 0)
                        {
                            MessageBox.Show("Bu stok kodu veya stok adı daha önce girilmiş.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    if (MessageBox.Show("Seçilen veriyi kaydetmek istediğinize emin misiniz?", "Kaydet", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        // Oranları al ve kontrol et
                        int oranPay = 1;
                        int oranPayda = 1;

                        int.TryParse(OlcuBirimiOran1.Text, out oranPay);
                        int.TryParse(OlcuBirimiOran2.Text, out oranPayda);
                        if (oranPay <= 0) oranPay = 1;
                        if (oranPayda <= 0) oranPayda = 1;

                        string insertQuery = @"INSERT INTO Stoklar 
                            (StokKod, StokAd, OlcuBirimi1Id, OlcuBirimi2Id, OlcuOranPay, OlcuOranPayda, KDVAlis, KDVSatis) 
                            VALUES (@StokKod, @StokAd, @OlcuBirimi1Id, @OlcuBirimi2Id, @OlcuOranPay, @OlcuOranPayda, @KDVAlis, @KDVSatis)";

                        using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@StokKod", StokKodTextBox.Text);
                            insertCommand.Parameters.AddWithValue("@StokAd", StokAdTextBox.Text);

                            int? olcuBirimi1Id = OlcuBirimi1ComboBox.SelectedValue as int?;
                            int? olcuBirimi2Id = OlcuBirimi2ComboBox.SelectedValue as int?;

                            insertCommand.Parameters.AddWithValue("@OlcuBirimi1Id", olcuBirimi1Id ?? (object)DBNull.Value);
                            insertCommand.Parameters.AddWithValue("@OlcuBirimi2Id", olcuBirimi2Id ?? (object)DBNull.Value);

                            insertCommand.Parameters.AddWithValue("@OlcuOranPay", oranPay);
                            insertCommand.Parameters.AddWithValue("@OlcuOranPayda", oranPayda);

                            double.TryParse(AlisKDVOranTextBox.Text, out double kdvAlis);
                            double.TryParse(SatisKDVOranTextBox.Text, out double kdvSatis);

                            insertCommand.Parameters.AddWithValue("@KDVAlis", kdvAlis);
                            insertCommand.Parameters.AddWithValue("@KDVSatis", kdvSatis);

                            insertCommand.ExecuteNonQuery();
                        }

                        MessageBox.Show("Stok bilgileri başarıyla kaydedildi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Alanları temizle
                        StokKodTextBox.Clear();
                        StokAdTextBox.Clear();
                        AlisKDVOranTextBox.Clear();
                        SatisKDVOranTextBox.Clear();
                        OlcuBirimi1ComboBox.SelectedIndex = -1;
                        OlcuBirimi2ComboBox.SelectedIndex = -1;
                        OlcuBirimiOran1.Text = "1";
                        OlcuBirimiOran2.Text = "1";

                        // Listeyi güncelle
                        dataGrid.ItemsSource = GetStoklar();

                        Keyboard.ClearFocus();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Stok", methodName: "KaydetButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dataGrid.SelectedItem is not Stoklar selectedStokItem)
                {
                    MessageBox.Show("Önce silmek istediğiniz veriyi seçiniz", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (MessageBox.Show("Seçilen veriyi silmek istediğinize emin misiniz?", "Uyarı", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    using (MySqlConnection connection = DatabaseManager.GetConnection())
                    {
                        connection.Open();
                        string query = "DELETE FROM Stoklar WHERE StokKod = @StokKod";

                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@StokKod", selectedStokItem.StokKod);
                            command.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Stok başarıyla silindi.", "Silindi", MessageBoxButton.OK, MessageBoxImage.Information);
                    dataGrid.ItemsSource = GetStoklar();
                }

                // Alanları temizle
                StokKodTextBox.Clear();
                StokAdTextBox.Clear();
                SatisKDVOranTextBox.Clear();
                AlisKDVOranTextBox.Clear();
                OlcuBirimi1ComboBox.SelectedIndex = -1;
                OlcuBirimi2ComboBox.SelectedIndex = -1;
                OlcuBirimiOran1.Text = "1";
                OlcuBirimiOran2.Text = "1";
                dataGrid.SelectedItem = null;
                Keyboard.ClearFocus();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Stok", methodName: "DeleteButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Updatebutton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dataGrid.SelectedItem is not Stoklar selectedStokItem)
                {
                    MessageBox.Show("Önce değiştirmek istediğiniz veriyi seçiniz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // TextBox boşluk kontrolü
                if (string.IsNullOrWhiteSpace(StokAdTextBox.Text)
                    || string.IsNullOrWhiteSpace(StokKodTextBox.Text)
                    || string.IsNullOrWhiteSpace(SatisKDVOranTextBox.Text)
                    || string.IsNullOrWhiteSpace(AlisKDVOranTextBox.Text)
                    || OlcuBirimi1ComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Lütfen zorunlu yerleri doldurunuz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }


                using (MySqlConnection connection = DatabaseManager.GetConnection())
                {
                    connection.Open();

                    List<Stoklar> stokList = new List<Stoklar>();

                    string query = "SELECT * FROM Stoklar";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Stoklar stok = new Stoklar
                                {
                                    StokKod = reader.GetString("StokKod"),
                                    StokAd = reader.GetString("StokAd"),
                                };
                                stokList.Add(stok);
                            }
                        }
                    }

                    foreach (var stok in stokList)
                    {
                        if (stok.StokKod != selectedStokItem.StokKod) // Seçilen stok harici kontrol
                        {
                            string stokAdiLower = stok.StokAd.ToLower(new CultureInfo("tr-TR"));
                            string normalizedStokAdi = StokAdTextBox.Text.ToLower(new CultureInfo("tr-TR"));
                            string stokKodLower = stok.StokKod.ToLower(new CultureInfo("tr-TR"));
                            string normalizedStokKod = StokKodTextBox.Text.ToLower(new CultureInfo("tr-TR"));

                            if (stokAdiLower == normalizedStokAdi || stokKodLower == normalizedStokKod)
                            {
                                MessageBox.Show("Bu stok adı veya stok kodu daha önce girilmiş.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                    }

                    if (MessageBox.Show("Seçili veriyi değiştirmek istediğinize emin misiniz?", "Veri Güncelleme", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        // Oranları kontrol et
                        int oranPay = 1;
                        int oranPayda = 1;
                        int.TryParse(OlcuBirimiOran1.Text, out oranPay);
                        int.TryParse(OlcuBirimiOran2.Text, out oranPayda);
                        if (oranPay <= 0) oranPay = 1;
                        if (oranPayda <= 0) oranPayda = 1;

                        // Ölçü birimi ID'lerini al
                        int? olcuBirimi1Id = OlcuBirimi1ComboBox.SelectedValue as int?;
                        int? olcuBirimi2Id = OlcuBirimi2ComboBox.SelectedValue as int?;

                        string updateSql = @"UPDATE Stoklar 
                            SET StokAd = @StokAd,
                                KDVSatis = @KDVSatis,
                                KDVAlis = @KDVAlis,
                                OlcuBirimi1Id = @OlcuBirimi1Id,
                                OlcuBirimi2Id = @OlcuBirimi2Id,
                                OlcuOranPay = @OranPay,
                                OlcuOranPayda = @OranPayda
                            WHERE StokKod = @StokKod";

                        using (MySqlCommand updateCommand = new MySqlCommand(updateSql, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@StokAd", StokAdTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@KDVSatis", double.TryParse(SatisKDVOranTextBox.Text, out var kdvs) ? kdvs : 0);
                            updateCommand.Parameters.AddWithValue("@KDVAlis", double.TryParse(AlisKDVOranTextBox.Text, out var kdva) ? kdva : 0);
                            updateCommand.Parameters.AddWithValue("@OlcuBirimi1Id", olcuBirimi1Id ?? (object)DBNull.Value);
                            updateCommand.Parameters.AddWithValue("@OlcuBirimi2Id", olcuBirimi2Id ?? (object)DBNull.Value);
                            updateCommand.Parameters.AddWithValue("@OranPay", oranPay);
                            updateCommand.Parameters.AddWithValue("@OranPayda", oranPayda);
                            updateCommand.Parameters.AddWithValue("@StokKod", selectedStokItem.StokKod);

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

                        // Alanları temizle ve verileri yenile
                        StokKodTextBox.Clear();
                        StokAdTextBox.Clear();
                        SatisKDVOranTextBox.Clear();
                        AlisKDVOranTextBox.Clear();
                        OlcuBirimi1ComboBox.SelectedIndex = -1;
                        OlcuBirimi2ComboBox.SelectedIndex = -1;
                        OlcuBirimiOran1.Text = "1";
                        OlcuBirimiOran2.Text = "1";
                        dataGrid.SelectedItem = null;
                        dataGrid.ItemsSource = GetStoklar();
                        Keyboard.ClearFocus();
                    }

                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Stok", methodName: "Updatebutton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void YazdırButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Excel Dosyası|*.xlsx";
                saveFileDialog.FileName = $"Stok.xlsx";
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (saveFileDialog.ShowDialog() == true)
                {

                    string path = saveFileDialog.FileName;

                    // DataGrid'deki verileri alın
                    var stoklar = dataGrid.ItemsSource as IEnumerable<Stoklar>;

                    // Yeni bir Excel iş kitabı oluşturun
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Stoklar");

                        // Başlık satırını ekle
                        var properties = typeof(Stoklar).GetProperties();
                        int colIndex = 1;
                        for (int col = 0; col < properties.Length; col++)
                        {
                            worksheet.Cell(1, colIndex).Value = properties[col].Name;
                            colIndex++;
                        }

                        // Veri satırlarını ekle
                        int row = 2;
                        if (stoklar != null)
                        {
                            foreach (var b in stoklar)
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
                LogManager.LogError(ex, className: "Stok", methodName: "YazdırButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedStok = dataGrid.SelectedItem as Stoklar;
                if (selectedStok != null)
                {
                    StokKodTextBox.Text = selectedStok.StokKod;
                    StokAdTextBox.Text = selectedStok.StokAd;
                    SatisKDVOranTextBox.Text = selectedStok.KdvSatis.ToString();
                    AlisKDVOranTextBox.Text = selectedStok.KdvAlis.ToString();
                    OlcuBirimi1ComboBox.SelectedValue = selectedStok.OlcuBirimi1Id;
                    OlcuBirimi2ComboBox.SelectedValue = selectedStok.OlcuBirimi2Id;
                    OlcuBirimiOran1.Text = selectedStok.OlcuOranPay.ToString();
                    OlcuBirimiOran2.Text = selectedStok.OlcuOranPayda.ToString();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Stok", methodName: "dataGrid_SelectionChanged()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StokKodTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;

                bool var = false;

                foreach (var item in viewModel.Stoklar)
                {
                    if (item is Stoklar stoklar)
                    {
                        string? stokKod = stoklar.StokKod;

                        if (StokKodTextBox.Text == stokKod)
                        {
                            StokAdTextBox.Text = stoklar.StokAd;
                            SatisKDVOranTextBox.Text = stoklar.KdvSatis.ToString();
                            AlisKDVOranTextBox.Text = stoklar.KdvAlis.ToString();
                            OlcuBirimi1ComboBox.SelectedValue = stoklar.OlcuBirimi1Id;
                            OlcuBirimi2ComboBox.SelectedValue = stoklar.OlcuBirimi2Id;


                            OlcuBirimiOran1.Text = stoklar.OlcuOranPay.ToString();
                            OlcuBirimiOran2.Text = stoklar.OlcuOranPayda.ToString();

                            dataGrid.SelectedItem = stoklar;
                            var = true;
                            break;
                        }
                        else
                        {
                            StokAdTextBox.Clear();
                            SatisKDVOranTextBox.Clear();
                            AlisKDVOranTextBox.Clear();
                            OlcuBirimi1ComboBox.SelectedValue = null;
                            OlcuBirimi2ComboBox.SelectedValue = null;
                            OlcuBirimiOran1.Text = "1";
                            OlcuBirimiOran2.Text = "1";
                            dataGrid.SelectedItem = null;
                        }
                    }
                }

                if (var)
                {
                    KaydetButton.IsEnabled = false;
                    DeleteButton.IsEnabled = true;
                    Updatebutton.IsEnabled = true;
                }
                else
                {
                    KaydetButton.IsEnabled = true;
                    DeleteButton.IsEnabled = false;
                    Updatebutton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Stok", methodName: "StokKodTextBox_TextChanged()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public ObservableCollection<Stoklar> GetStoklar()
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                viewModel.Stoklar.Clear();

                using (MySqlConnection connection = DatabaseManager.GetConnection())
                {
                    connection.Open();

                    string query = "SELECT * FROM Stoklar";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {

                                int olcu1Id = Convert.ToInt32(reader["OlcuBirimi1Id"]);
                                int? olcu2Id = reader["OlcuBirimi2Id"] != DBNull.Value ? Convert.ToInt32(reader["OlcuBirimi2Id"]) : null;

                                Stoklar s = new Stoklar()
                                {
                                    StokKod = reader.GetString("StokKod"),
                                    StokAd = reader.GetString("StokAd"),
                                    OlcuBirimi1Id = olcu1Id,
                                    OlcuBirimi2Id = olcu2Id,
                                    OlcuOranPay = reader["OlcuOranPay"] != DBNull.Value ? Convert.ToInt32(reader["OlcuOranPay"]) : 1,
                                    OlcuOranPayda = reader["OlcuOranPayda"] != DBNull.Value ? Convert.ToInt32(reader["OlcuOranPayda"]) : 1,
                                    KdvAlis = reader["KDVAlis"] != DBNull.Value ? Convert.ToInt32(reader["KDVAlis"]) : 0,
                                    KdvSatis = reader["KDVSatis"] != DBNull.Value ? Convert.ToInt32(reader["KDVSatis"]) : 0,

                                    OlcuBirimi1 = OlcuBirimleriService.GetById(olcu1Id),
                                    OlcuBirimi2 = OlcuBirimleriService.GetById(olcu2Id)
                                };
                                viewModel.Stoklar.Add(s);
                            }
                        }
                    }
                }
                return viewModel.Stoklar;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Stok", methodName: "GetStoklar()", stackTrace: ex.StackTrace);
                return null;
            }
        }

        private void OnlyNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }
    }
}
