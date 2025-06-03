using Cari_kayıt_Programı.Models;
using ClosedXML.Excel;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

                    string faturaTip = AlacakRadioButton.IsChecked == true ? "Satış" : "Alış";
                    string durum = AcikRadioButton.IsChecked == true ? "Açık" : "Kapalı";

                    CariHareketGorunum yeniHareket = new CariHareketGorunum
                    {
                        CariKod = cariKod,
                        Tarih = TarihDatePicker.SelectedDate ?? DateTime.Now,
                        FaturaTip = faturaTip,
                        Tip = durum,
                        Numara = EvrakNoTextbox.Text,
                        Aciklama = AciklamaTextbox.Text,
                        VadeTarih = VadeDatePicker.SelectedDate ?? DateTime.Now,
                        Tutar = Convert.ToDecimal(TutarTextbox.Text)
                    };

                    if (MessageBox.Show("Veriyi kaydetmek istediğinize emin misiniz?", "Kaydet", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                        return;

                    MainViewModel viewModel = (MainViewModel)this.DataContext;

                    var girilenNumara = EvrakNoTextbox.Text.Trim();
                    var faturaNumarasi = viewModel.Hareketler.FirstOrDefault(f => string.Equals(f.Numara?.Trim(), girilenNumara, StringComparison.OrdinalIgnoreCase));

                    if (faturaNumarasi != null)
                    {
                        MessageBox.Show("Bu fatura numarası zaten mevcut.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    using (MySqlConnection connection = DatabaseManager.GetConnection())
                    {
                        connection.Open();

                        string insertQuery = @"INSERT INTO Faturalar (EvrakNo, CariKod, Tarih, VadeTarih, Tip, Durum, Aciklama, ToplamTutar)
                            VALUES (@EvrakNo, @CariKod, @Tarih, @VadeTarih, @Tip, @Durum, @Aciklama, @ToplamTutar)";

                        using (MySqlCommand cmd = new MySqlCommand(insertQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@CariKod", yeniHareket.CariKod);
                            cmd.Parameters.AddWithValue("@Tarih", yeniHareket.Tarih);
                            cmd.Parameters.AddWithValue("@Tip", yeniHareket.FaturaTip);
                            cmd.Parameters.AddWithValue("@Durum", yeniHareket.Tip);
                            cmd.Parameters.AddWithValue("@EvrakNo", yeniHareket.Numara);
                            cmd.Parameters.AddWithValue("@Aciklama", yeniHareket.Aciklama);
                            cmd.Parameters.AddWithValue("@VadeTarih", yeniHareket.VadeTarih);
                            cmd.Parameters.AddWithValue("@ToplamTutar", yeniHareket.Tutar);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    // Yeni hareketi ViewModel'e ekle
                    viewModel.Hareketler.Add(yeniHareket);

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

                int? selectedFaturaId = null;
                if (dataGrid.SelectedItem != null)
                {
                    if (dataGrid.SelectedItem is CariHareketGorunum selectedHareket)
                    {
                        selectedFaturaId = selectedHareket.FaturaID;
                    }
                }

                // Seçilen öğenin ID'si 0'dan büyükse silme işlemini gerçekleştir
                if (selectedFaturaId != null)
                {
                    if (MessageBox.Show("Veriyi silmek istediğinize emin misiniz?", "Sil", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        int? ID = selectedFaturaId;

                        try
                        {
                            using (MySqlConnection connection = DatabaseManager.GetConnection())
                            {
                                connection.Open();

                                // İşlemi tek bir transaction içinde yapalım
                                using (var transaction = connection.BeginTransaction())
                                {
                                    try
                                    {
                                        // Önce FaturaDetay tablosundan sil
                                        string deleteDetailsQuery = "DELETE FROM FaturaDetay WHERE FaturaID = @FaturaID";
                                        using (var deleteDetailsCmd = new MySqlCommand(deleteDetailsQuery, connection, transaction))
                                        {
                                            deleteDetailsCmd.Parameters.AddWithValue("@FaturaID", ID);
                                            deleteDetailsCmd.ExecuteNonQuery();
                                        }

                                        // Sonra Faturalar tablosundan sil
                                        string deleteFaturaQuery = "DELETE FROM Faturalar WHERE FaturaID = @FaturaID";
                                        using (var deleteFaturaCmd = new MySqlCommand(deleteFaturaQuery, connection, transaction))
                                        {
                                            deleteFaturaCmd.Parameters.AddWithValue("@FaturaID", ID);
                                            int rowsAffected = deleteFaturaCmd.ExecuteNonQuery();

                                            if (rowsAffected > 0)
                                            {
                                                transaction.Commit();
                                                MessageBox.Show("Fatura ve detayları başarıyla silindi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                                            }
                                            else
                                            {
                                                transaction.Rollback();
                                                MessageBox.Show("Fatura bulunamadı veya silinemedi.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        transaction.Rollback();
                                        MessageBox.Show("Silme işlemi sırasında bir hata oluştu: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                                        LogManager.LogError(ex, "FaturaSil", "DeleteFaturaWithDetails", ex.StackTrace);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Veritabanı bağlantı hatası: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                            LogManager.LogError(ex, "FaturaSil", "OuterConnection", ex.StackTrace);
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
                MainViewModel viewModel = (MainViewModel)this.DataContext;

                Cariler? selectedBusiness = SelectedCariler;
                string? cariKod = selectedBusiness?.CariKod;

                if (string.IsNullOrWhiteSpace(cariKod))
                {
                    MessageBox.Show("Lütfen bir işletme seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string selectedNumara = "";
                if (dataGrid.SelectedItem != null)
                {
                    if (dataGrid.SelectedItem is CariHareketGorunum selectedHareket)
                    {
                        if (selectedHareket.FaturaDetay)
                        {
                            MessageBox.Show("Seçilen hareketin fatura detayı mevcut, işlem yapılamaz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return; // İşlemi durdur
                        }

                        selectedNumara = selectedHareket.Numara;
                    }
                }
                else
                {
                    MessageBox.Show("Lütfen düzenlemek istediğiniz hareketi seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(AciklamaTextbox.Text) &&
                    !string.IsNullOrWhiteSpace(TutarTextbox.Text) &&
                    (AlacakRadioButton.IsChecked == true || BorcRadioButton.IsChecked == true) &&
                    TarihDatePicker.SelectedDate.HasValue &&
                    VadeDatePicker.SelectedDate.HasValue)
                {
                    string faturaTip = AlacakRadioButton.IsChecked == true ? "Satış" : "Alış";
                    string durum = AcikRadioButton.IsChecked == true ? "Açık" : "Kapalı";

                    CariHareketGorunum yeniHareket = new CariHareketGorunum
                    {
                        CariKod = cariKod,
                        Tarih = TarihDatePicker.SelectedDate ?? DateTime.Now,
                        FaturaTip = faturaTip,
                        Tip = durum,
                        Numara = EvrakNoTextbox.Text,
                        Aciklama = AciklamaTextbox.Text,
                        VadeTarih = VadeDatePicker.SelectedDate ?? DateTime.Now,
                        Tutar = Convert.ToDecimal(TutarTextbox.Text)
                    };

                    if (string.IsNullOrWhiteSpace(selectedNumara))
                    {
                        using (MySqlConnection connection = DatabaseManager.GetConnection())
                        {
                            connection.Open();

                            var girilenNumara = EvrakNoTextbox.Text.Trim();
                            var faturaNumarasi = viewModel.Hareketler.FirstOrDefault(f => string.Equals(f.Numara?.Trim(), girilenNumara, StringComparison.OrdinalIgnoreCase));

                            if (faturaNumarasi != null)
                            {
                                MessageBox.Show("Bu fatura numarası zaten mevcut.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            if (MessageBox.Show("Veriyi değiştirmek istediğinize emin misiniz?", "Kaydet", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                string updateQuery = @"UPDATE Faturalar
                                               SET Tarih = @Tarih,
                                                   Tip = @Tip,
                                                   Durum = @Durum,
                                                   Numara = @Numara,
                                                   Aciklama = @Aciklama,
                                                   VadeTarih = @VadeTarih,
                                                   ToplamTutar = @ToplamTutar
                                               WHERE FaturaID = @FaturaID";

                                using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, connection))
                                {
                                    updateCmd.Parameters.AddWithValue("@FaturaID", yeniHareket.FaturaID);
                                    updateCmd.Parameters.AddWithValue("@Tarih", yeniHareket.Tarih);
                                    updateCmd.Parameters.AddWithValue("@Tip", yeniHareket.FaturaTip);
                                    updateCmd.Parameters.AddWithValue("@Durum", yeniHareket.Tip);
                                    updateCmd.Parameters.AddWithValue("@Numara", yeniHareket.Numara);
                                    updateCmd.Parameters.AddWithValue("@Aciklama", yeniHareket.Aciklama);
                                    updateCmd.Parameters.AddWithValue("@VadeTarih", yeniHareket.VadeTarih);
                                    updateCmd.Parameters.AddWithValue("@ToplamTutar", yeniHareket.Tutar);
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
                    var odemeler = dataGrid.ItemsSource as IEnumerable<CariHareketGorunum>;

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
                        var properties = typeof(CariHareketGorunum).GetProperties();
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
                var selectedHareket = dataGrid.SelectedItem as CariHareketGorunum;
                if (selectedHareket != null && TarihDatePicker.SelectedDate.HasValue && VadeDatePicker.SelectedDate.HasValue)
                {
                    Cariler? selectedBusiness = SelectedCariler;
                    string? kod = "";
                    if (selectedBusiness != null)
                    {
                        kod = selectedBusiness.CariKod;
                    }

                    TarihDatePicker.SelectedDate = selectedHareket.Tarih;
                    EvrakNoTextbox.Text = selectedHareket.Numara;
                    AciklamaTextbox.Text = selectedHareket.Aciklama;

                    VadeDatePicker.SelectedDate = selectedHareket.VadeTarih;

                    AlacakRadioButton.IsChecked = true;
                    TutarTextbox.Text = selectedHareket.Tutar.ToString();

                    if (selectedHareket.Tip == "Açık")
                    {
                        AcikRadioButton.IsChecked = true;
                    }
                    else if (selectedHareket.Tip == "Kapalı")
                    {
                        KapaliRadioButton.IsChecked = true;
                    }

                    YeniHareketButton.IsEnabled = false;

                    if (selectedHareket.FaturaDetay)
                    {
                        DegistirHareketButton.IsEnabled = false;
                    }
                    else
                    {
                        DegistirHareketButton.IsEnabled = true;
                    }
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
                    viewModel.Hareketler.Clear();
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
                if (DataContext is MainViewModel vm)
                {
                    vm.FilterHareketler(txtSearch.Text);
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
                        var filteredData = viewModel.Hareketler.Where(x => x.Tarih >= startDate.Value && x.Tarih <= endDate.Value).ToList();

                        if (FiltreDegiskenler.tip != "T")
                        {
                            filteredData = filteredData.Where(x => x.FaturaTip.Equals(FiltreDegiskenler.tip, StringComparison.OrdinalIgnoreCase)).ToList();
                        }

                        decimal bakiye = 0, borcTop = 0, alacakTop = 0;
                        foreach (var hareket in filteredData)
                        {
                            if (hareket.Tip == "Alış") // para giriyor benden alıyor
                            {
                                borcTop += hareket.Tutar;
                                bakiye += hareket.Tutar;
                            }
                            else if (hareket.Tip == "Satış") // para çıkıyor bana satıyor
                            {
                                alacakTop += hareket.Tutar;
                                bakiye -= hareket.Tutar;
                            }
                            hareket.Bakiye = bakiye;
                        }

                        viewModel.CariBorcToplam = borcTop;
                        viewModel.CariAlacakToplam = alacakTop;
                        viewModel.CariBakiye = bakiye;

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

                        viewModel.Hareketler = new ObservableCollection<CariHareketGorunum>(filteredData);
                        viewModel.Hareketler = new ObservableCollection<CariHareketGorunum>(viewModel.Hareketler.OrderBy(o => o.Tarih));

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

                            viewModel.Hareketler.Clear();
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

        public async Task LoadDataGrid()
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;

                // Seçili cari varsa hareketleri yükle
                if (SelectedCariler is Cariler selectedBusiness)
                {
                    await viewModel.GetCariHareketlerAsync(selectedBusiness.CariKod);
                }

                // Sütun genişliklerini ayarla
                foreach (var column in dataGrid.Columns)
                {
                    column.Width = DataGridLength.Auto;
                    column.Width = new DataGridLength(1,
                        viewModel.Hareketler != null && viewModel.Hareketler.Count > 0
                        ? DataGridLengthUnitType.SizeToCells
                        : DataGridLengthUnitType.SizeToHeader);
                }

                if (viewModel.CariBakiye < 0)
                    BakiyeTopTextbox.Foreground = Brushes.Red;
                else if (viewModel.CariBakiye > 0)
                    BakiyeTopTextbox.Foreground = Brushes.Green;
                else
                    BakiyeTopTextbox.Foreground = Brushes.Black;

            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "LoadDataGrid()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void Page_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Eğer tıklanan öğe DataGrid değilse veya DataGrid'in bir çocuğu değilse seçimi temizle
                if (!IsMouseOverDataGrid(e))
                {
                    Cariler? selectedBusiness = SelectedCariler;
                    var selectedOdeme = dataGrid.SelectedItem as CariHareketGorunum;
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