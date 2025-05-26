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
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cari_kayıt_Programı
{
    public partial class CariHareketKayitlari : Window
    {
        public MainViewModel ViewModel { get; set; }
        private static Cariler? SelectedCariler { get; set; }

        public CariHareketKayitlari()
        {
            try
            {
                InitializeComponent();

                ViewModel = new MainViewModel();
                DataContext = ViewModel;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "Main()", stackTrace: ex.StackTrace);
                MessageBox.Show("Beklenmeyen bir hata oluştu. Lütfen destek ekibiyle iletişime geçin.", "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static class FiltreDegiskenler
        {
            public static DateTime? baslangicTarihi { get; set; }
            public static DateTime? bitisTarihi { get; set; }
            public static string? tip { get; set; } = "";
            public static bool filtrele { get; set; } = false;
        }

        private void YeniHareketButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(AciklamaTextbox.Text) &&
                    !string.IsNullOrWhiteSpace(TutarTextbox.Text) &&
                    (AlacakRadioButton.IsChecked == true || BorcRadioButton.IsChecked == true) &&
                    TarihDatePicker.SelectedDate.HasValue &&
                    VadeDatePicker.SelectedDate.HasValue)
                {
                    Cariler? selectedBusiness = SelectedCariler;
                    string? cariKod = selectedBusiness?.CariKod;

                    if (string.IsNullOrWhiteSpace(cariKod))
                    {
                        MessageBox.Show("Cari bilgisi bulunamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    DateTime selectedDate = TarihDatePicker.SelectedDate.Value;
                    DateTime selectedVadeDate = VadeDatePicker.SelectedDate.Value;

                    string tip = AlacakRadioButton.IsChecked == true ? "A" : "B";
                    string durum = AcikRadioButton.IsChecked == true ? "Açık" : "Kapalı";

                    double alacak = tip == "A" ? Convert.ToDouble(TutarTextbox.Text) : 0.0;
                    double borc = tip == "B" ? Convert.ToDouble(TutarTextbox.Text) : 0.0;

                    Odeme yeniOdeme = new Odeme
                    {
                        CariKod = cariKod,
                        Tarih = selectedDate.ToString("yyyy-MM-dd"),
                        Tip = tip,
                        Durum = durum,
                        EvrakNo = EvrakNoTextbox.Text,
                        Aciklama = AciklamaTextbox.Text,
                        VadeTarihi = selectedVadeDate.ToString("yyyy-MM-dd"),
                        Borc = borc,
                        Alacak = alacak
                    };

                    if (MessageBox.Show("Veriyi kaydetmek istediğinize emin misiniz?", "Kaydet", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                        return;

                    using (MySqlConnection connection = DatabaseManager.GetConnection())
                    {
                        connection.Open();

                        if (!string.IsNullOrWhiteSpace(yeniOdeme.EvrakNo))
                        {
                            string checkQuery = "SELECT COUNT(*) FROM CariHareketler WHERE CariKod = @CariKod AND LOWER(EvrakNo) = @EvrakNo";
                            using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection))
                            {
                                checkCmd.Parameters.AddWithValue("@CariKod", cariKod);
                                checkCmd.Parameters.AddWithValue("@EvrakNo", yeniOdeme.EvrakNo.ToLower(new CultureInfo("tr-TR")));

                                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                                if (count > 0)
                                {
                                    MessageBox.Show("Bu evrak numarası daha önce kaydedilmiş.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                            }
                        }

                        string insertQuery = @"
                    INSERT INTO CariHareketler (CariKod, Tarih, Tip, Durum, EvrakNo, Aciklama, VadeTarihi, Borc, Alacak)
                    VALUES (@CariKod, @Tarih, @Tip, @Durum, @EvrakNo, @Aciklama, @VadeTarihi, @Borc, @Alacak)";

                        using (MySqlCommand cmd = new MySqlCommand(insertQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@CariKod", yeniOdeme.CariKod);
                            cmd.Parameters.AddWithValue("@Tarih", yeniOdeme.Tarih);
                            cmd.Parameters.AddWithValue("@Tip", yeniOdeme.Tip);
                            cmd.Parameters.AddWithValue("@Durum", yeniOdeme.Durum);
                            cmd.Parameters.AddWithValue("@EvrakNo", yeniOdeme.EvrakNo);
                            cmd.Parameters.AddWithValue("@Aciklama", yeniOdeme.Aciklama);
                            cmd.Parameters.AddWithValue("@VadeTarihi", yeniOdeme.VadeTarihi);
                            cmd.Parameters.AddWithValue("@Borc", yeniOdeme.Borc);
                            cmd.Parameters.AddWithValue("@Alacak", yeniOdeme.Alacak);

                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Arayüz temizliği
                    TarihDatePicker.SelectedDate = DateTime.Now;
                    EvrakNoTextbox.Clear();
                    TutarTextbox.Clear();
                    AciklamaTextbox.Clear();
                    VadeDatePicker.SelectedDate = DateTime.Now;
                    dataGrid.SelectedItems.Clear();
                    BorcRadioButton.IsChecked = true;
                    AcikRadioButton.IsChecked = true;

                    MessageBox.Show("İşletme hareket bilgileri veritabanına kaydedildi.", "Kaydedildi", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadDataGrid();
                }
                else
                {
                    MessageBox.Show("Lütfen gerekli yerleri doldurunuz", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "Window_Loaded()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void SilHareketButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cariler? selectedBusiness = SelectedCariler;
                string? cariKod = selectedBusiness?.CariKod;

                if (string.IsNullOrWhiteSpace(cariKod))
                {
                    MessageBox.Show("Lütfen bir işletme seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Seçilen öğenin ID'sini alın
                int selectedOdemeId = 0; // varsayılan olarak 0 ayarlandı
                if (dataGrid.SelectedItem != null)
                {
                    // Seçilen öğenin ID'sini alma
                    if (dataGrid.SelectedItem is Odeme selectedOdeme)
                    {
                        selectedOdemeId = selectedOdeme.HareketID;
                    }
                }

                // Seçilen öğenin ID'si 0'dan büyükse silme işlemini gerçekleştir
                if (selectedOdemeId > 0)
                {
                    if (MessageBox.Show("Veriyi silmek istediğinize emin misiniz?", "Sil", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        // Veritabanı bağlantısını açın ve silme işlemini gerçekleştirin
                        using (MySqlConnection connection = DatabaseManager.GetConnection())
                        {
                            connection.Open();

                            string deleteQuery = "DELETE FROM CariHareketler WHERE HareketID = @OdemeId AND CariKod = @CariKod";

                            using (MySqlCommand deleteCommand = new MySqlCommand(deleteQuery, connection))
                            {
                                deleteCommand.Parameters.AddWithValue("@OdemeId", selectedOdemeId);
                                deleteCommand.Parameters.AddWithValue("@CariKod", cariKod);

                                int rowsAffected = deleteCommand.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    MessageBox.Show("Hareket başarıyla silindi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    MessageBox.Show("Hareket silinemedi.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }

                        LoadDataGrid();
                        TarihDatePicker.SelectedDate = DateTime.Now;
                        EvrakNoTextbox.Clear();
                        TutarTextbox.Clear();
                        AciklamaTextbox.Clear();
                        VadeDatePicker.SelectedDate = DateTime.Now;
                        dataGrid.SelectedItems.Clear();
                        BorcRadioButton.IsChecked = true;

                        TarihDatePicker.IsEnabled = true;
                        EvrakNoTextbox.IsEnabled = true;
                        AciklamaTextbox.IsEnabled = true;
                        VadeDatePicker.IsEnabled = true;
                        BAGroupBox.IsEnabled = true;
                        TutarTextbox.IsEnabled = true;
                    }
                }
                else
                {
                    MessageBox.Show("Lütfen silmek istediğiniz hareketi seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "SilHareketButton_Click()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void DegistirHareketButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cariler? selectedBusiness = SelectedCariler;
                string? cariKod = selectedBusiness?.CariKod;

                if (string.IsNullOrWhiteSpace(cariKod))
                {
                    MessageBox.Show("Lütfen bir işletme seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int selectedOdemeId = 0;
                if (dataGrid.SelectedItem is Odeme selectedOdeme)
                {
                    selectedOdemeId = selectedOdeme.HareketID;
                }
                else
                {
                    MessageBox.Show("Lütfen değiştirmek istediğiniz hareketi seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(AciklamaTextbox.Text) &&
                    !string.IsNullOrWhiteSpace(TutarTextbox.Text) &&
                    (AlacakRadioButton.IsChecked == true || BorcRadioButton.IsChecked == true) &&
                    TarihDatePicker.SelectedDate.HasValue &&
                    VadeDatePicker.SelectedDate.HasValue)
                {
                    DateTime selectedDate = TarihDatePicker.SelectedDate.Value;
                    DateTime selectedVadeDate = VadeDatePicker.SelectedDate.Value;

                    string tip = AlacakRadioButton.IsChecked == true ? "A" : "B";
                    string durum = AcikRadioButton.IsChecked == true ? "Açık" : "Kapalı";

                    double alacak = tip == "A" ? Convert.ToDouble(TutarTextbox.Text) : 0.0;
                    double borc = tip == "B" ? Convert.ToDouble(TutarTextbox.Text) : 0.0;

                    Odeme yeniOdeme = new Odeme
                    {
                        Tarih = selectedDate.ToString("yyyy-MM-dd"),
                        Tip = tip,
                        Durum = durum,
                        EvrakNo = EvrakNoTextbox.Text,
                        Aciklama = AciklamaTextbox.Text,
                        VadeTarihi = selectedVadeDate.ToString("yyyy-MM-dd"),
                        Borc = borc,
                        Alacak = alacak
                    };

                    if (selectedOdemeId > 0)
                    {
                        using (MySqlConnection connection = DatabaseManager.GetConnection())
                        {
                            connection.Open();

                            // EvrakNo kontrolü (CariKod içinde benzersiz mi?)
                            if (!string.IsNullOrWhiteSpace(yeniOdeme.EvrakNo))
                            {
                                string kontrolQuery = @"SELECT EvrakNo FROM CariHareketler 
                                                WHERE CariKod = @CariKod AND HareketID != @HareketID";
                                using (MySqlCommand kontrolCmd = new MySqlCommand(kontrolQuery, connection))
                                {
                                    kontrolCmd.Parameters.AddWithValue("@CariKod", cariKod);
                                    kontrolCmd.Parameters.AddWithValue("@HareketID", selectedOdemeId);

                                    using (MySqlDataReader reader = kontrolCmd.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            string mevcutEvrakNo = reader.GetString("EvrakNo")?.ToLower(new CultureInfo("tr-TR")) ?? "";
                                            string girilenEvrakNo = yeniOdeme.EvrakNo.ToLower(new CultureInfo("tr-TR"));
                                            if (mevcutEvrakNo == girilenEvrakNo)
                                            {
                                                MessageBox.Show("Bu evrak numarası daha önce kaydedilmiş.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }

                            if (MessageBox.Show("Veriyi değiştirmek istediğinize emin misiniz?", "Kaydet", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                string updateQuery = @"UPDATE CariHareketler
                                               SET Tarih = @Tarih,
                                                   Tip = @Tip,
                                                   Durum = @Durum,
                                                   EvrakNo = @EvrakNo,
                                                   Aciklama = @Aciklama,
                                                   VadeTarihi = @VadeTarihi,
                                                   Borc = @Borc,
                                                   Alacak = @Alacak
                                               WHERE HareketID = @HareketID AND CariKod = @CariKod";

                                using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, connection))
                                {
                                    updateCmd.Parameters.AddWithValue("@Tarih", yeniOdeme.Tarih);
                                    updateCmd.Parameters.AddWithValue("@Tip", yeniOdeme.Tip);
                                    updateCmd.Parameters.AddWithValue("@Durum", yeniOdeme.Durum);
                                    updateCmd.Parameters.AddWithValue("@EvrakNo", yeniOdeme.EvrakNo);
                                    updateCmd.Parameters.AddWithValue("@Aciklama", yeniOdeme.Aciklama);
                                    updateCmd.Parameters.AddWithValue("@VadeTarihi", yeniOdeme.VadeTarihi);
                                    updateCmd.Parameters.AddWithValue("@Borc", yeniOdeme.Borc);
                                    updateCmd.Parameters.AddWithValue("@Alacak", yeniOdeme.Alacak);
                                    updateCmd.Parameters.AddWithValue("@HareketID", selectedOdemeId);
                                    updateCmd.Parameters.AddWithValue("@CariKod", cariKod);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // Formu temizle
                        LoadDataGrid();
                        TarihDatePicker.SelectedDate = DateTime.Now;
                        EvrakNoTextbox.Clear();
                        TutarTextbox.Clear();
                        AciklamaTextbox.Clear();
                        VadeDatePicker.SelectedDate = DateTime.Now;
                        dataGrid.SelectedItems.Clear();
                        BorcRadioButton.IsChecked = true;
                        AcikRadioButton.IsChecked = true;

                        YeniHareketButton.IsEnabled = true;
                    }
                }
                else
                {
                    MessageBox.Show("Lütfen zorunlu yerleri doldurun!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "DegistirHareketButton_Click()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void YazdırButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cariler? selectedBusiness = SelectedCariler;
                string? carikod = "", cariisim = "", telefonNo1 = "", telefonNo2 = "", adres = "", vergiNo = "", vergiDairesi = "";
                if (selectedBusiness != null)
                {
                    carikod = selectedBusiness.CariKod != "" ? selectedBusiness.CariKod : "-";
                    cariisim = selectedBusiness.Unvan != "" ? selectedBusiness.Unvan : "-";
                    telefonNo1 = selectedBusiness.Telefon1 != "" ? selectedBusiness.Telefon1 : "-";
                    telefonNo2 = selectedBusiness.Telefon2 != "" ? selectedBusiness.Telefon2 : "-";
                    adres = selectedBusiness.Adres != "" ? selectedBusiness.Adres : "-";
                    vergiNo = selectedBusiness.VergiNo != "" ? selectedBusiness.VergiNo : "-";
                    vergiDairesi = selectedBusiness.VergiDairesi != "" ? selectedBusiness.VergiDairesi : "-";
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Excel Dosyası|*.xlsx";
                saveFileDialog.FileName = $"{cariisim} Hareketleri.xlsx";
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (saveFileDialog.ShowDialog() == true)
                {

                    string path = saveFileDialog.FileName;

                    // DataGrid'deki verileri alın
                    var odemeler = dataGrid.ItemsSource as IEnumerable<Odeme>;

                    // Yeni bir Excel iş kitabı oluşturun
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Odemeler");


                        worksheet.Cell(1, 1).Value = "Cari Kod";
                        worksheet.Cell(1, 2).Value = carikod;
                        worksheet.Cell(1, 3).Value = "İşletme Bilgileri";
                        worksheet.Cell(1, 4).Value = cariisim;
                        worksheet.Cell(2, 3).Value = $"Telefon 1: {telefonNo1} | Telefon 2: {telefonNo2}";
                        worksheet.Cell(3, 3).Value = $"Adres: {adres}";
                        worksheet.Cell(4, 3).Value = $"V.No: {vergiNo}";
                        worksheet.Cell(4, 4).Value = $"V.Dairesi: {vergiDairesi}";

                        // Başlık satırını ekle
                        var properties = typeof(Odeme).GetProperties();
                        int colIndex = 1;
                        for (int col = 0; col < properties.Length; col++)
                        {
                            if (properties[col].Name != "ID") // ID sütununu atla
                            {
                                worksheet.Cell(5, colIndex).Value = properties[col].Name;
                                colIndex++;
                            }
                        }

                        // Veri satırlarını ekle
                        int row = 6;
                        if (odemeler != null)
                        {
                            foreach (var odeme in odemeler)
                            {
                                colIndex = 1;
                                for (int col = 0; col < properties.Length; col++)
                                {
                                    if (properties[col].Name != "ID") // ID sütununu atla
                                    {
                                        var value = properties[col].GetValue(odeme);
                                        if (properties[col].Name == "Borc" || properties[col].Name == "Alacak")
                                        {
                                            // Sayısal değerleri dönüştür
                                            double numericValue;
                                            if (double.TryParse(value?.ToString(), out numericValue))
                                            {
                                                var cell = worksheet.Cell(row, colIndex);
                                                cell.SetValue(numericValue);
                                                cell.Style.NumberFormat.Format = "₺#,##0.00"; // Para birimi biçimlendirme
                                            }
                                            else
                                            {
                                                worksheet.Cell(row, colIndex).SetValue(0); // Geçerli bir sayı değilse 0 yaz
                                            }
                                        }
                                        else if (properties[col].Name == "Bakiye")
                                        {
                                            // Bakiye sütunundaki hücre değerlerini ayarla ve renklendir
                                            double numericValue;
                                            var cell = worksheet.Cell(row, colIndex);
                                            if (double.TryParse(value?.ToString(), out numericValue))
                                            {
                                                cell.SetValue(numericValue);
                                                cell.Style.NumberFormat.Format = "₺#,##0.00"; // Para birimi biçimlendirme
                                                if (numericValue > 0)
                                                {
                                                    cell.Style.Font.FontColor = XLColor.Green;
                                                }
                                                else if (numericValue < 0)
                                                {
                                                    cell.Style.Font.FontColor = XLColor.Red;
                                                }
                                            }
                                            else
                                            {
                                                cell.SetValue(0);
                                                cell.Style.Font.FontColor = XLColor.Black;
                                            }
                                        }
                                        else
                                        {
                                            // Diğer hücre değerlerini ayarla
                                            worksheet.Cell(row, colIndex).SetValue(value != null ? value.ToString() : "");
                                        }
                                        colIndex++;
                                    }
                                }
                                row++;
                            }
                        }

                        // Borç, Alacak ve Bakiye değerlerini en alt satıra yazdır
                        int lastRow = row; // Son satırın indeksi
                        if (double.TryParse(BorcTopTextbox.Text.Replace("₺", ""), out double borcTop))
                        {
                            var cell = worksheet.Cell(lastRow + 1, colIndex - 4);
                            cell.SetValue(borcTop);
                            cell.Style.NumberFormat.Format = "₺#,##0.00"; // Para birimi biçimlendirme
                        }
                        else
                        {
                            worksheet.Cell(lastRow + 1, colIndex - 3).SetValue(0);
                        }

                        if (double.TryParse(AlacakTopTextbox.Text.Replace("₺", ""), out double alacakTop))
                        {
                            var cell = worksheet.Cell(lastRow + 1, colIndex - 3);
                            cell.SetValue(alacakTop);
                            cell.Style.NumberFormat.Format = "₺#,##0.00"; // Para birimi biçimlendirme
                        }
                        else
                        {
                            worksheet.Cell(lastRow + 1, colIndex - 2).SetValue(0);
                        }

                        if (double.TryParse(BakiyeTopTextbox.Text.Replace("₺", ""), out double bakiyeTop))
                        {
                            var cell = worksheet.Cell(lastRow + 1, colIndex - 2);
                            cell.SetValue(bakiyeTop);
                            cell.Style.NumberFormat.Format = "₺#,##0.00"; // Para birimi biçimlendirme
                            if (bakiyeTop > 0)
                            {
                                cell.Style.Font.FontColor = XLColor.Green;
                            }
                            else if (bakiyeTop < 0)
                            {
                                cell.Style.Font.FontColor = XLColor.Red;
                            }
                        }
                        else
                        {
                            var cell = worksheet.Cell(lastRow + 1, colIndex - 2);
                            cell.SetValue(0);
                            cell.Style.Font.FontColor = XLColor.Black;
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
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "YazdırButton_Click()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedOdeme = dataGrid.SelectedItem as Odeme;
                if (selectedOdeme != null && TarihDatePicker.SelectedDate.HasValue && VadeDatePicker.SelectedDate.HasValue)
                {
                    Cariler? selectedBusiness = SelectedCariler;
                    string? kod = "";
                    if (selectedBusiness != null)
                    {
                        kod = selectedBusiness.CariKod;
                    }

                    TarihDatePicker.SelectedDate = DateTime.Parse(selectedOdeme.Tarih);
                    EvrakNoTextbox.Text = selectedOdeme.EvrakNo;
                    AciklamaTextbox.Text = selectedOdeme.Aciklama;

                    VadeDatePicker.SelectedDate = DateTime.Parse(selectedOdeme.VadeTarihi);

                    if (selectedOdeme.Tip == "A")
                    {
                        AlacakRadioButton.IsChecked = true;
                        TutarTextbox.Text = selectedOdeme.Alacak.ToString();
                    }
                    else if (selectedOdeme.Tip == "B")
                    {
                        BorcRadioButton.IsChecked = true;
                        TutarTextbox.Text = selectedOdeme.Borc.ToString();
                    }

                    if (selectedOdeme.Durum == "Açık")
                    {
                        AcikRadioButton.IsChecked = true;
                    }
                    else if (selectedOdeme.Durum == "Kapalı")
                    {
                        KapaliRadioButton.IsChecked = true;
                    }

                    YeniHareketButton.IsEnabled = false;
                    DegistirHareketButton.IsEnabled = true;
                }
                else
                {
                    YeniHareketButton.IsEnabled = true;
                    DegistirHareketButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "dataGrid_SelectionChanged()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void CariKartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selector = new EntitySelectorView(typeof(Cariler));
                if (selector.ShowDialog() == true)
                {
                    var selectedBusiness = selector.SelectedItem as Cariler;
                    CariKodTextbox.Text = selectedBusiness.CariKod;
                    IsletmeAdiLabel.Content = selectedBusiness.Unvan;
                    BorcTopTextbox.Clear();
                    AlacakTopTextbox.Clear();
                    BakiyeTopTextbox.Clear();
                    LoadDataGrid();

                    TarihDatePicker.IsEnabled = true;
                    EvrakNoTextbox.IsEnabled = true;
                    AciklamaTextbox.IsEnabled = true;
                    VadeDatePicker.IsEnabled = true;
                    BAGroupBox.IsEnabled = true;
                    AKGroupBox.IsEnabled = true;
                    TutarTextbox.IsEnabled = true;
                    YeniHareketButton.IsEnabled = true;
                    SilHareketButton.IsEnabled = true;
                    DegistirHareketButton.IsEnabled = false;
                    YazdırButton.IsEnabled = true;
                    txtSearch.IsEnabled = true;
                    FiltreleButton.IsEnabled = true;

                    TarihDatePicker.SelectedDate = DateTime.Now;
                    EvrakNoTextbox.Clear();
                    TutarTextbox.Clear();
                    AciklamaTextbox.Clear();
                    VadeDatePicker.SelectedDate = DateTime.Now;
                    dataGrid.SelectedItems.Clear();
                    txtSearch.Clear();

                }
                else
                {
                    IsletmeAdiLabel.Content = "-";
                    CariKodTextbox.Text = "";
                    TarihDatePicker.IsEnabled = false;
                    EvrakNoTextbox.IsEnabled = false;
                    AciklamaTextbox.IsEnabled = false;
                    VadeDatePicker.IsEnabled = false;
                    BAGroupBox.IsEnabled = false;
                    AKGroupBox.IsEnabled = false;
                    TutarTextbox.IsEnabled = false;
                    YeniHareketButton.IsEnabled = false;
                    SilHareketButton.IsEnabled = false;
                    DegistirHareketButton.IsEnabled = false;
                    YazdırButton.IsEnabled = false;
                    txtSearch.IsEnabled = false;
                    FiltreleButton.IsEnabled = false;

                    TarihDatePicker.SelectedDate = DateTime.Now;
                    EvrakNoTextbox.Clear();
                    TutarTextbox.Clear();
                    AciklamaTextbox.Clear();
                    VadeDatePicker.SelectedDate = DateTime.Now;
                    dataGrid.SelectedItems.Clear();
                    txtSearch.Clear();
                    BorcTopTextbox.Clear();
                    AlacakTopTextbox.Clear();
                    BakiyeTopTextbox.Clear();
                    BakiyeTopTextbox.Foreground = Brushes.Black;

                    MainViewModel? viewModel = (MainViewModel)this.DataContext;
                    viewModel.Odemeler.Clear();
                    BorcTopTextbox.Text = "";
                    AlacakTopTextbox.Text = "";
                    BakiyeTopTextbox.Text = "";
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "CariKartButton_Click()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                Cariler? selectedBusiness = SelectedCariler;
                if (selectedBusiness != null)
                {
                    string searchTerm = txtSearch.Text;
                    SearchGetOdemeler(selectedBusiness.CariKod, searchTerm);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "txtSearch_TextChanged()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void FiltreleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                OpenWindow(new HareketlerFiltre());

                if (FiltreDegiskenler.filtrele)
                {
                    DateTime? startDate = FiltreDegiskenler.baslangicTarihi;
                    DateTime? endDate = FiltreDegiskenler.bitisTarihi;

                    if (startDate.HasValue && endDate.HasValue)
                    {
                        LoadDataGrid();
                        var filteredData = viewModel.Odemeler.Where(x => DateTime.TryParse(x.Tarih, out DateTime tarih) && tarih >= startDate.Value && tarih <= endDate.Value).ToList();

                        if (FiltreDegiskenler.tip != "T")
                        {
                            filteredData = filteredData.Where(x => x.Tip.Equals(FiltreDegiskenler.tip, StringComparison.OrdinalIgnoreCase)).ToList();
                        }

                        double bakiye = 0, borcTop = 0, alacakTop = 0;
                        foreach (var odeme in filteredData)
                        {
                            bakiye += odeme.Borc - odeme.Alacak;
                            odeme.Bakiye = bakiye;
                            borcTop += odeme.Borc;
                            alacakTop += odeme.Alacak;
                        }

                        BorcTopTextbox.Text = borcTop.ToString("C");
                        AlacakTopTextbox.Text = alacakTop.ToString("C");
                        BakiyeTopTextbox.Text = bakiye.ToString("C");

                        if (bakiye < 0)
                        {
                            BakiyeTopTextbox.Foreground = Brushes.Red;
                        }
                        else if (bakiye > 0)
                        {
                            BakiyeTopTextbox.Foreground = Brushes.Green;
                        }
                        else
                        {
                            BakiyeTopTextbox.Foreground = Brushes.Black;
                        }

                        viewModel.Odemeler = new ObservableCollection<Odeme>(filteredData);
                        viewModel.Odemeler = new ObservableCollection<Odeme>(viewModel.Odemeler.OrderBy(o => DateTime.Parse(o.Tarih)));

                        TarihDatePicker.SelectedDate = DateTime.Now;
                        EvrakNoTextbox.Clear();
                        TutarTextbox.Clear();
                        AciklamaTextbox.Clear();
                        VadeDatePicker.SelectedDate = DateTime.Now;
                        dataGrid.SelectedItems.Clear();
                        txtSearch.Clear();

                        BorcRadioButton.IsChecked = true;
                        TarihDatePicker.IsEnabled = true;
                        EvrakNoTextbox.IsEnabled = true;
                        AciklamaTextbox.IsEnabled = true;
                        VadeDatePicker.IsEnabled = true;
                        BAGroupBox.IsEnabled = true;
                        TutarTextbox.IsEnabled = true;
                        YeniHareketButton.IsEnabled = true;
                        SilHareketButton.IsEnabled = true;
                        YazdırButton.IsEnabled = true;
                        FiltreleButton.IsEnabled = true;

                        Keyboard.ClearFocus();

                        startDate = null;
                        endDate = null;
                        FiltreDegiskenler.filtrele = false;
                        FiltreDegiskenler.baslangicTarihi = null;
                        FiltreDegiskenler.bitisTarihi = null;
                        FiltreDegiskenler.tip = null;

                        filteredData = null;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "FiltreleButton_Click()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void CariKodTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;

                CariHesapKayitlari cariHesapKayitlari = new CariHesapKayitlari();
                var businesses = cariHesapKayitlari.GetBusinesses();

                bool var = false;

                foreach (var item in businesses)
                {
                    if (item is Cariler business)
                    {
                        string? carikod = business.CariKod;

                        if (CariKodTextbox.Text == carikod)
                        {
                            SelectedCariler = business;

                            IsletmeAdiLabel.Content = business.Unvan;
                            BorcTopTextbox.Clear();
                            AlacakTopTextbox.Clear();
                            BakiyeTopTextbox.Clear();
                            LoadDataGrid();

                            TarihDatePicker.IsEnabled = true;
                            EvrakNoTextbox.IsEnabled = true;
                            AciklamaTextbox.IsEnabled = true;
                            VadeDatePicker.IsEnabled = true;
                            BAGroupBox.IsEnabled = true;
                            AKGroupBox.IsEnabled = true;
                            TutarTextbox.IsEnabled = true;
                            YeniHareketButton.IsEnabled = true;
                            SilHareketButton.IsEnabled = true;
                            DegistirHareketButton.IsEnabled = false;
                            YazdırButton.IsEnabled = true;
                            txtSearch.IsEnabled = true;
                            FiltreleButton.IsEnabled = true;

                            TarihDatePicker.SelectedDate = DateTime.Now;
                            EvrakNoTextbox.Clear();
                            TutarTextbox.Clear();
                            AciklamaTextbox.Clear();
                            VadeDatePicker.SelectedDate = DateTime.Now;
                            dataGrid.SelectedItems.Clear();
                            txtSearch.Clear();

                            var = true;
                            break;
                        }
                        else
                        {
                            IsletmeAdiLabel.Content = "-";
                            TarihDatePicker.IsEnabled = false;
                            EvrakNoTextbox.IsEnabled = false;
                            AciklamaTextbox.IsEnabled = false;
                            VadeDatePicker.IsEnabled = false;
                            BAGroupBox.IsEnabled = false;
                            AKGroupBox.IsEnabled = false;
                            TutarTextbox.IsEnabled = false;
                            YeniHareketButton.IsEnabled = false;
                            SilHareketButton.IsEnabled = false;
                            DegistirHareketButton.IsEnabled = false;
                            YazdırButton.IsEnabled = false;
                            txtSearch.IsEnabled = false;
                            FiltreleButton.IsEnabled = false;

                            TarihDatePicker.SelectedDate = DateTime.Now;
                            EvrakNoTextbox.Clear();
                            TutarTextbox.Clear();
                            AciklamaTextbox.Clear();
                            VadeDatePicker.SelectedDate = DateTime.Now;
                            dataGrid.SelectedItems.Clear();
                            txtSearch.Clear();
                            BorcTopTextbox.Clear();
                            AlacakTopTextbox.Clear();
                            BakiyeTopTextbox.Clear();
                            BakiyeTopTextbox.Foreground = Brushes.Black;

                            viewModel.Odemeler.Clear();
                            BorcTopTextbox.Text = "";
                            AlacakTopTextbox.Text = "";
                            BakiyeTopTextbox.Text = "";
                        }
                    }
                }

                if (var)
                {
                    YeniHareketButton.IsEnabled = true;
                    DegistirHareketButton.IsEnabled = false;
                }
                else
                {
                    YeniHareketButton.IsEnabled = false;
                    DegistirHareketButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "CariKodTextbox_TextChanged()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void OpenWindow(Window window)
        {
            try
            {
                window.Owner = App.Current.MainWindow;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "OpenWindow()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        public void LoadDataGrid()
        {
            try
            {
                Cariler? selectedBusiness = SelectedCariler;
                if (selectedBusiness != null)
                {
                    GetOdemeler(selectedBusiness.CariKod);

                    // Her sütunun genişliğini içerdiği metne göre ayarla
                    foreach (var column in dataGrid.Columns)
                    {
                        column.Width = DataGridLength.Auto;

                        // Sütunu yeniden boyutlandırmak için
                        column.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);
                    }
                }
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                if (viewModel.Odemeler.Count == 0)
                {
                    // Her sütunun genişliğini içerdiği Sütuna göre ayarla
                    foreach (var column in dataGrid.Columns)
                    {
                        column.Width = DataGridLength.Auto;
                        // Sütunu yeniden boyutlandırmak için
                        column.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToHeader);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "LoadDataGrid()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        public void SearchGetOdemeler(string? businessKod, string? searchTerm)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                viewModel.Odemeler.Clear();

                using (MySqlConnection connection = DatabaseManager.GetConnection())
                {
                    connection.Open();

                    string query = @"
                SELECT * FROM CariHareketler 
                WHERE CariKod = @CariKod AND (
                    LOWER(Tarih) LIKE CONCAT('%', LOWER(@searchTerm), '%') OR
                    LOWER(EvrakNo) LIKE CONCAT('%', LOWER(@searchTerm), '%') OR
                    LOWER(Aciklama) LIKE CONCAT('%', LOWER(@searchTerm), '%') OR
                    LOWER(VadeTarihi) LIKE CONCAT('%', LOWER(@searchTerm), '%') OR
                    CAST(Borc AS CHAR) LIKE CONCAT('%', @searchTerm, '%') OR
                    CAST(Alacak AS CHAR) LIKE CONCAT('%', @searchTerm, '%')
                )";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CariKod", businessKod);
                        command.Parameters.AddWithValue("@searchTerm", searchTerm ?? "");

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Odeme odeme = new Odeme
                                {
                                    HareketID = reader.GetInt32("HareketID"),
                                    Tarih = reader.GetString("Tarih"),
                                    Tip = reader.GetString("Tip"),
                                    Durum = reader.GetString("Durum"),
                                    EvrakNo = reader.GetString("EvrakNo"),
                                    Aciklama = reader.GetString("Aciklama"),
                                    VadeTarihi = reader.GetString("VadeTarihi"),
                                    Borc = reader.GetDouble("Borc"),
                                    Alacak = reader.GetDouble("Alacak")
                                };
                                viewModel.Odemeler.Add(odeme);
                            }
                        }
                    }

                    // Bakiye hesaplama ve görünüm güncelleme
                    double bakiye = 0, borcTop = 0, alacakTop = 0;
                    viewModel.Odemeler = new ObservableCollection<Odeme>(viewModel.Odemeler.OrderBy(o => DateTime.Parse(o.Tarih)));
                    foreach (var odeme in viewModel.Odemeler)
                    {
                        bakiye += odeme.Borc - odeme.Alacak;
                        odeme.Bakiye = bakiye;
                        borcTop += odeme.Borc;
                        alacakTop += odeme.Alacak;
                    }

                    BorcTopTextbox.Text = borcTop.ToString("C");
                    AlacakTopTextbox.Text = alacakTop.ToString("C");
                    BakiyeTopTextbox.Text = bakiye.ToString("C");

                    BakiyeTopTextbox.Foreground = bakiye < 0 ? Brushes.Red : (bakiye > 0 ? Brushes.Green : Brushes.Black);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "SearchGetOdemeler()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        public void GetOdemeler(string? businessKod)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                viewModel.Odemeler.Clear();

                using (MySqlConnection connection = DatabaseManager.GetConnection())
                {
                    connection.Open();

                    string query = "SELECT * FROM CariHareketler WHERE CariKod = @CariKod";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CariKod", businessKod);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Odeme odeme = new Odeme
                                {
                                    HareketID = reader.GetInt32("HareketID"),
                                    Tarih = reader.GetDateTime("Tarih").ToString("dd.MM.yyyy"),
                                    Tip = reader.GetString("Tip"),
                                    Durum = reader.GetString("Durum"),
                                    EvrakNo = reader.GetString("EvrakNo"),
                                    Aciklama = reader.GetString("Aciklama"),
                                    VadeTarihi = reader.GetDateTime("VadeTarihi").ToString("dd.MM.yyyy"),
                                    Borc = reader.GetDouble("Borc"),
                                    Alacak = reader.GetDouble("Alacak")
                                };
                                viewModel.Odemeler.Add(odeme);
                            }
                        }
                    }
                }

                // Bakiye ve görünüm hesaplamaları
                double bakiye = 0, borcTop = 0, alacakTop = 0;
                viewModel.Odemeler = new ObservableCollection<Odeme>(viewModel.Odemeler.OrderBy(o => DateTime.Parse(o.Tarih)));
                foreach (var odeme in viewModel.Odemeler)
                {
                    bakiye += odeme.Borc - odeme.Alacak;
                    odeme.Bakiye = bakiye;
                    borcTop += odeme.Borc;
                    alacakTop += odeme.Alacak;
                }

                BorcTopTextbox.Text = borcTop.ToString("C");
                AlacakTopTextbox.Text = alacakTop.ToString("C");
                BakiyeTopTextbox.Text = bakiye.ToString("C");

                BakiyeTopTextbox.Foreground = bakiye < 0 ? Brushes.Red : (bakiye > 0 ? Brushes.Green : Brushes.Black);
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "GetOdemeler()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        public class Odeme
        {
            public int HareketID { get; set; }
            public string? CariKod { get; set; }
            public required string Tarih { get; set; }
            public required string Tip { get; set; }
            public required string Durum { get; set; }
            public string? EvrakNo { get; set; }
            public required string Aciklama { get; set; }
            public string? VadeTarihi { get; set; }
            public required double Borc { get; set; }
            public required double Alacak { get; set; }
            public double Bakiye { get; set; }
        }

        private void Page_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Eğer tıklanan öğe DataGrid değilse veya DataGrid'in bir çocuğu değilse seçimi temizle
                if (!IsMouseOverDataGrid(e))
                {
                    Cariler? selectedBusiness = SelectedCariler;
                    var selectedOdeme = dataGrid.SelectedItem as Odeme;
                    if (selectedBusiness != null && selectedOdeme != null)
                    {
                        LoadDataGrid();
                        TarihDatePicker.SelectedDate = DateTime.Now;
                        EvrakNoTextbox.Clear();
                        TutarTextbox.Clear();
                        AciklamaTextbox.Clear();
                        VadeDatePicker.SelectedDate = DateTime.Now;
                        dataGrid.SelectedItems.Clear();
                        txtSearch.Clear();

                        BorcRadioButton.IsChecked = true;
                        AcikRadioButton.IsChecked = true;

                        Keyboard.ClearFocus();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "Page_PreviewMouseDown()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private bool IsMouseOverDataGrid(MouseButtonEventArgs e)
        {
            try
            {
                DependencyObject? originalSource = e.OriginalSource as DependencyObject;

                // Tıklanan öğenin DataGrid, Button, TextBox, RadioButton veya GroupBox olup olmadığını kontrol et
                while (originalSource != null)
                {
                    if (originalSource == dataGrid)
                    {
                        return true;
                    }

                    if (originalSource is Button || originalSource is TextBox || originalSource is RadioButton || originalSource is GroupBox)
                    {
                        return true;
                    }

                    originalSource = VisualTreeHelper.GetParent(originalSource);
                }

                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "IsMouseOverDataGrid()", stackTrace: ex.StackTrace);
                return false;
                throw;
            }
        }

        // Yardımcı metot: Bir ebeveyni belirli bir türde bulmak için
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            try
            {
                DependencyObject parentObject = VisualTreeHelper.GetParent(child);
                if (parentObject == null) return null;

                T parent = parentObject as T;
                return parent ?? FindParent<T>(parentObject);
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "FindParent()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
                throw;
            }
        }

        private void TutarTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                e.Handled = !IsTextAllowed(e.Text);
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "TutarTextBox_PreviewTextInput()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void TutarTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            try
            {
                if (e.DataObject.GetDataPresent(typeof(string)))
                {
                    string text = (string)e.DataObject.GetData(typeof(string));
                    if (!IsTextAllowed(text))
                    {
                        e.CancelCommand();
                    }
                }
                else
                {
                    e.CancelCommand();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "TutarTextBox_Pasting()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private static bool IsTextAllowed(string text)
        {
            try
            {
                Regex regex = new Regex("[^0-9,]+"); // Sadece sayısal karakterlere izin ver
                return !regex.IsMatch(text);
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "IsTextAllowed()", stackTrace: ex.StackTrace);
                return false;
                throw;
            }
        }
    }
}