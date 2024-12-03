using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Cari_kayıt_Programı.CariHesapKayitlari;

namespace Cari_kayıt_Programı
{
    public partial class CariHareketKayitlari : Window
    {
        public MainViewModel ViewModel { get; set; }

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

        private string? DosyaPath = "";

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Business? selectedBusiness = Degiskenler.selectedBusiness;
                if (selectedBusiness == null)
                {
                    IsletmeAdiLabel.Content = "-";
                    TarihDatePicker.IsEnabled = false;
                    EvrakNoTextbox.IsEnabled = false;
                    AciklamaTextbox.IsEnabled = false;
                    VadeDatePicker.IsEnabled = false;
                    BAGroupBox.IsEnabled = false;
                    AKGroupBox.IsEnabled = false;
                    YukleGroupBox.IsEnabled = false;
                    TutarTextbox.IsEnabled = false;
                    YeniHareketButton.IsEnabled = false;
                    SilHareketButton.IsEnabled = false;
                    DegistirHareketButton.IsEnabled = false;
                    YazdırButton.IsEnabled = false;
                    txtSearch.IsEnabled = false;
                    FiltreleButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "Window_Loaded()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void YeniHareketButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(AciklamaTextbox.Text) && !string.IsNullOrWhiteSpace(TutarTextbox.Text) && (AlacakRadioButton.IsChecked == true || BorcRadioButton.IsChecked == true) && TarihDatePicker.SelectedDate.HasValue && VadeDatePicker.SelectedDate.HasValue)
                {
                    Business? selectedBusiness = Degiskenler.selectedBusiness;
                    string? kod = "";
                    if (selectedBusiness != null)
                    {
                        kod = selectedBusiness.CariKod;
                    }

                    DateTime selectedDate = TarihDatePicker.SelectedDate.HasValue ? TarihDatePicker.SelectedDate.Value : DateTime.Now;
                    string tarih = selectedDate.ToString("dd.MM.yyyy");

                    DateTime selectedVadeDate = VadeDatePicker.SelectedDate.HasValue ? VadeDatePicker.SelectedDate.Value : DateTime.Now;
                    string vadeTarih = selectedVadeDate.ToString("dd.MM.yyyy");

                    string tip = AlacakRadioButton.IsChecked == true ? "A" : "B";
                    string durum = AcikRadioButton.IsChecked == true ? "Açık" : "Kapalı";

                    double A = 0.00, B = 0.00;
                    if (tip == "A")
                    {
                        A = Convert.ToDouble(TutarTextbox.Text);
                        B = 0.00;
                    }
                    else if (tip == "B")
                    {
                        A = 0.00;
                        B = Convert.ToDouble(TutarTextbox.Text);
                    }

                    Odeme yeniOdeme = new Odeme
                    {
                        Tarih = tarih,
                        Tip = tip,
                        Durum = durum,
                        EvrakNo = EvrakNoTextbox.Text,
                        Aciklama = AciklamaTextbox.Text,
                        VadeTarihi = vadeTarih,
                        Borc = B,
                        Alacak = A,
                        Dosya = DosyaPath
                    };

                    using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                    {
                        connection.Open();

                        string tableName = $"Cari_{kod}";
                        string escapedTableName = $"\"{tableName}\"";

                        if (!string.IsNullOrEmpty(EvrakNoTextbox.Text))
                        {
                            List<Odeme> OdemeList = new List<Odeme>();

                            string query = $"SELECT * FROM {escapedTableName}";

                            using (SQLiteCommand command = new SQLiteCommand(query, connection))
                            {
                                using (SQLiteDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        Odeme odeme = new Odeme
                                        {
                                            EvrakNo = reader.GetString(4),
                                        };
                                        OdemeList.Add(odeme);
                                    }
                                }
                            }

                            foreach (var item in OdemeList)
                            {
                                if (item is Odeme odeme)
                                {
                                    string? evrakno = odeme.EvrakNo;

                                    string EvrakNoLower = evrakno.ToLower(new CultureInfo("tr-TR"));
                                    string normalizedEvrakNo = EvrakNoTextbox.Text.ToLower(new CultureInfo("tr-TR"));
                                    if (EvrakNoLower == normalizedEvrakNo)
                                    {
                                        MessageBox.Show("Bu evrak numarası daha önce kaydedilmiş.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                                        return;
                                    }
                                }
                            }
                        }

                        if (MessageBox.Show("Veriyi kaydetmek istediğinize emin misiniz?", "Kaydet", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            string query = $"INSERT INTO {escapedTableName} (Tarih, Tip, Durum, EvrakNo, Aciklama, VadeTarihi, Borc, Alacak, Dosya) " +
                                           "VALUES (@Tarih, @Tip, @Durum, @EvrakNo, @Aciklama, @VadeTarihi, @Borc, @Alacak, @Dosya)";

                            using (SQLiteCommand command = new SQLiteCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@Tarih", yeniOdeme.Tarih);
                                command.Parameters.AddWithValue("@Tip", yeniOdeme.Tip);
                                command.Parameters.AddWithValue("@Durum", yeniOdeme.Durum);
                                command.Parameters.AddWithValue("@EvrakNo", yeniOdeme.EvrakNo);
                                command.Parameters.AddWithValue("@Aciklama", yeniOdeme.Aciklama);
                                command.Parameters.AddWithValue("@VadeTarihi", yeniOdeme.VadeTarihi);
                                command.Parameters.AddWithValue("@Borc", yeniOdeme.Borc);
                                command.Parameters.AddWithValue("@Alacak", yeniOdeme.Alacak);
                                command.Parameters.AddWithValue("@Dosya", yeniOdeme.Dosya);

                                command.ExecuteNonQuery();
                            }

                            if (!string.IsNullOrWhiteSpace(selectedFilePath) && !string.IsNullOrWhiteSpace(DosyaPath))
                            {
                                File.Copy(selectedFilePath, DosyaPath, true);
                            }

                            TarihDatePicker.SelectedDate = DateTime.Now;
                            EvrakNoTextbox.Clear();
                            TutarTextbox.Clear();
                            AciklamaTextbox.Clear();
                            VadeDatePicker.SelectedDate = DateTime.Now;
                            dataGrid.SelectedItems.Clear();
                            BorcRadioButton.IsChecked = true;
                            AcikRadioButton.IsChecked = true;
                            DosyaPath = "";
                            DosyaIslem = "Saved";
                            ChangeButtonContent("");
                            MessageBox.Show("İşletme hareket bilgileri veritabanına kaydedildi.", "Kaydedildi", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
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
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SilHareketButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Business? selectedBusiness = Degiskenler.selectedBusiness;
                string? kod = "";
                if (selectedBusiness != null)
                {
                    kod = selectedBusiness.CariKod;
                }

                // Seçilen öğenin ID'sini alın
                int selectedOdemeId = 0; // varsayılan olarak 0 ayarlandı
                string? selectedOdemeDosya = "";
                if (dataGrid.SelectedItem != null)
                {
                    // Seçilen öğenin ID'sini alma
                    if (dataGrid.SelectedItem is Odeme selectedOdeme)
                    {
                        selectedOdemeId = selectedOdeme.ID;
                        selectedOdemeDosya = selectedOdeme.Dosya;
                    }
                }

                // Seçilen öğenin ID'si 0'dan büyükse silme işlemini gerçekleştir
                if (selectedOdemeId > 0)
                {
                    if (MessageBox.Show("Veriyi silmek istediğinize emin misiniz?", "Sil", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        // Veritabanı bağlantısını açın ve silme işlemini gerçekleştirin
                        using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                        {
                            connection.Open();

                            string tableName = $"Cari_{kod}";
                            string escapedTableName = $"\"{tableName}\"";

                            string deleteQuery = $"DELETE FROM {escapedTableName} WHERE ID = @OdemeId";
                            using (SQLiteCommand deleteCommand = new SQLiteCommand(deleteQuery, connection))
                            {
                                deleteCommand.Parameters.AddWithValue("@OdemeId", selectedOdemeId);
                                int rowsAffected = deleteCommand.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    MessageBox.Show("Hareket başarıyla silindi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);

                                    if (File.Exists(selectedOdemeDosya))
                                    {
                                        File.Delete(selectedOdemeDosya);
                                    }
                                    DosyaIslem = "Saved";
                                    ChangeButtonContent("");
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
                        DosyaPath = "";
                        BorcRadioButton.IsChecked = true;
                        DosyaIslem = "Saved";
                        ChangeButtonContent("");

                        TarihDatePicker.IsEnabled = true;
                        EvrakNoTextbox.IsEnabled = true;
                        AciklamaTextbox.IsEnabled = true;
                        VadeDatePicker.IsEnabled = true;
                        BAGroupBox.IsEnabled = true;
                        YukleGroupBox.IsEnabled = true;
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
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DegistirHareketButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Business? selectedBusiness = Degiskenler.selectedBusiness;
                int ID = 0;
                string? kod = "";
                if (selectedBusiness != null)
                {
                    ID = selectedBusiness.ID;
                    kod = selectedBusiness.CariKod;
                }

                int selectedOdemeId = 0;
                string? selectedOdemeDosya = "";
                if (dataGrid.SelectedItem != null)
                {
                    if (dataGrid.SelectedItem is Odeme selectedOdeme)
                    {
                        selectedOdemeId = selectedOdeme.ID;
                        selectedOdemeDosya = selectedOdeme.Dosya;
                    }
                }
                else
                {
                    MessageBox.Show("Lütfen değiştirmek istediğiniz hareketi seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(AciklamaTextbox.Text) && !string.IsNullOrWhiteSpace(TutarTextbox.Text) && (AlacakRadioButton.IsChecked == true || BorcRadioButton.IsChecked == true) && TarihDatePicker.SelectedDate.HasValue && VadeDatePicker.SelectedDate.HasValue)
                {
                    DateTime selectedDate = TarihDatePicker.SelectedDate.HasValue ? TarihDatePicker.SelectedDate.Value : DateTime.Now;
                    string tarih = selectedDate.ToString("dd.MM.yyyy");

                    DateTime selectedVadeDate = VadeDatePicker.SelectedDate.HasValue ? VadeDatePicker.SelectedDate.Value : DateTime.Now;
                    string vadeTarih = selectedVadeDate.ToString("dd.MM.yyyy");

                    string tip = AlacakRadioButton.IsChecked == true ? "A" : "B";
                    string durum = AcikRadioButton.IsChecked == true ? "Açık" : "Kapalı";

                    double A = 0.00, B = 0.00;
                    if (tip == "A")
                    {
                        A = Convert.ToDouble(TutarTextbox.Text);
                        B = 0.00;
                    }
                    else if (tip == "B")
                    {
                        A = 0.00;
                        B = Convert.ToDouble(TutarTextbox.Text);
                    }

                    Odeme yeniOdeme = new Odeme
                    {
                        Tarih = tarih,
                        Tip = tip,
                        Durum = durum,
                        EvrakNo = EvrakNoTextbox.Text,
                        Aciklama = AciklamaTextbox.Text,
                        VadeTarihi = vadeTarih,
                        Borc = B,
                        Alacak = A,
                        Dosya = DosyaPath
                    };

                    if (selectedOdemeId > 0)
                    {

                        using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                        {
                            connection.Open();

                            string tableName = $"Cari_{kod}";
                            string escapedTableName = $"\"{tableName}\"";

                            if (!string.IsNullOrEmpty(EvrakNoTextbox.Text))
                            {
                                List<Odeme> OdemeList = new List<Odeme>();

                                string query = $"SELECT * FROM {escapedTableName}";

                                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                                {
                                    using (SQLiteDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            Odeme odeme = new Odeme
                                            {
                                                ID = reader.GetInt32(0),
                                                EvrakNo = reader.GetString(4),
                                            };
                                            OdemeList.Add(odeme);
                                        }
                                    }
                                }

                                foreach (var item in OdemeList)
                                {
                                    if (item is Odeme odeme && selectedOdemeId != odeme.ID)
                                    {
                                        string? evrakno = odeme.EvrakNo;

                                        string EvrakNoLower = evrakno.ToLower(new CultureInfo("tr-TR"));
                                        string normalizedEvrakNo = EvrakNoTextbox.Text.ToLower(new CultureInfo("tr-TR"));
                                        if (EvrakNoLower == normalizedEvrakNo)
                                        {
                                            MessageBox.Show("Bu evrak numarası daha önce kaydedilmiş.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                                            return;
                                        }
                                    }
                                }
                            }

                            if (MessageBox.Show("Veriyi değiştirmek istediğinize emin misiniz?", "Kaydet", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                string query = $"UPDATE {escapedTableName} " +
                                       "SET Tarih = @Tarih, " +
                                       "    Tip = @Tip, " +
                                       "    Durum = @Durum, " +
                                       "    EvrakNo = @EvrakNo, " +
                                       "    Aciklama = @Aciklama, " +
                                       "    VadeTarihi = @VadeTarihi, " +
                                       "    Borc = @Borc, " +
                                       "    Alacak = @Alacak, " +
                                       "    Dosya = @Dosya " +
                                       "WHERE ID = @ID";

                                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                                {
                                    command.Parameters.AddWithValue("@Tarih", yeniOdeme.Tarih);
                                    command.Parameters.AddWithValue("@Tip", yeniOdeme.Tip);
                                    command.Parameters.AddWithValue("@Durum", yeniOdeme.Durum);
                                    command.Parameters.AddWithValue("@EvrakNo", yeniOdeme.EvrakNo);
                                    command.Parameters.AddWithValue("@Aciklama", yeniOdeme.Aciklama);
                                    command.Parameters.AddWithValue("@VadeTarihi", yeniOdeme.VadeTarihi);
                                    command.Parameters.AddWithValue("@Borc", yeniOdeme.Borc);
                                    command.Parameters.AddWithValue("@Alacak", yeniOdeme.Alacak);
                                    command.Parameters.AddWithValue("@Dosya", yeniOdeme.Dosya);
                                    command.Parameters.AddWithValue("@ID", selectedOdemeId);
                                    command.ExecuteNonQuery();
                                }

                                if (DosyaSil)
                                {
                                    if (File.Exists(selectedOdemeDosya))
                                    {
                                        File.Delete(selectedOdemeDosya);
                                    }
                                    DosyaIslem = "Saved";
                                    ChangeButtonContent("");
                                }

                                if (selectedFilePath != "" && DosyaPath != "")
                                {
                                    File.Copy(selectedFilePath, DosyaPath, true);
                                }
                            }
                            else
                            {
                                DosyaIslem = "Clear";
                                ChangeButtonContent("");
                            }

                            LoadDataGrid();
                            TarihDatePicker.SelectedDate = DateTime.Now;
                            EvrakNoTextbox.Clear();
                            TutarTextbox.Clear();
                            AciklamaTextbox.Clear();
                            VadeDatePicker.SelectedDate = DateTime.Now;
                            dataGrid.SelectedItems.Clear();
                            DosyaPath = "";
                            BorcRadioButton.IsChecked = true;
                            AcikRadioButton.IsChecked = true;
                            DosyaIslem = "Saved";
                            ChangeButtonContent("");

                            YeniHareketButton.IsEnabled = true;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Lütfen zorunlu yerleri doldurun!!!.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "DegistirHareketButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void YazdırButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Business? selectedBusiness = Degiskenler.selectedBusiness;
                string? carikod = "", cariisim = "", telefonNo1 = "", telefonNo2 = "", adres = "", vergiNo = "", vergiDairesi = "";
                if (selectedBusiness != null)
                {
                    carikod = selectedBusiness.CariKod != "" ? selectedBusiness.CariKod : "-";
                    cariisim = selectedBusiness.CariIsim != "" ? selectedBusiness.CariIsim : "-";
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
                                        else if (properties[col].Name.Equals("Dosya", StringComparison.OrdinalIgnoreCase) && value != null && value.ToString() != "")
                                        {
                                            string? dosyaYolu = Path.Combine(ConfigManager.IsletmePath, carikod, Path.GetFileName(value.ToString()));
                                            if (!string.IsNullOrEmpty(dosyaYolu))
                                            {
                                                var cell = worksheet.Cell(row, colIndex);
                                                string? link = dosyaYolu;
                                                cell.Value = Path.GetFileName(link);
                                                cell.SetHyperlink(new XLHyperlink(link));
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
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void YukleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Business? selectedBusiness = Degiskenler.selectedBusiness;
                string? kod = "";
                if (selectedBusiness != null)
                {
                    kod = selectedBusiness.CariKod;
                }

                var selectedOdeme = dataGrid.SelectedItem as Odeme;
                if (selectedOdeme != null && !string.IsNullOrEmpty(selectedOdeme.Dosya))
                {
                    DosyaIslem = "Uploaded";
                }

                if (DosyaIslem != "Uploaded")
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Filter = "PDF Dosyaları (*.pdf)|*.pdf|Excel Dosyaları (*.xlsx)|*.xlsx|Word Dosyaları (*.docx)|*.docx";
                    openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                    if (openFileDialog.ShowDialog() == true)
                    {
                        selectedFilePath = openFileDialog.FileName;
                        string fileName = Path.GetFileName(selectedFilePath);

                        DosyaPath = Path.Combine(ConfigManager.IsletmePath, kod, fileName);

                        DosyaIslem = "Uploaded";
                        ChangeButtonContent(fileName);
                    }
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show("Dosyayı silmek istediğinize emin misiniz?", "Dosya Silme", MessageBoxButton.YesNo, MessageBoxImage.Asterisk);
                    if (result == MessageBoxResult.Yes)
                    {
                        DosyaIslem = "Saved";
                        ChangeButtonContent("");
                        DosyaSil = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "YukleButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedOdeme = dataGrid.SelectedItem as Odeme;
                if (selectedOdeme != null && TarihDatePicker.SelectedDate.HasValue && VadeDatePicker.SelectedDate.HasValue)
                {
                    Business? selectedBusiness = Degiskenler.selectedBusiness;
                    string? kod = "";
                    if (selectedBusiness != null)
                    {
                        kod = selectedBusiness.CariKod;
                    }

                    TarihDatePicker.SelectedDate = DateTime.Parse(selectedOdeme.Tarih);
                    EvrakNoTextbox.Text = selectedOdeme.EvrakNo;
                    AciklamaTextbox.Text = selectedOdeme.Aciklama;
                    DosyaPath = Path.Combine(ConfigManager.IsletmePath, kod, Path.GetFileName(selectedOdeme.Dosya));
                    DosyaIslem = "View";
                    ChangeButtonContent();

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
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button? button = sender as Button;
                if (button != null)
                {
                    var selectedOdeme = dataGrid.SelectedItem as Odeme;

                    Business? selectedBusiness = Degiskenler.selectedBusiness;
                    string? kod = "";
                    if (selectedBusiness != null)
                    {
                        kod = selectedBusiness.CariKod;
                    }
                    string dosyaYolu = Path.Combine(ConfigManager.IsletmePath, kod, Path.GetFileName(selectedOdeme.Dosya));

                    if (selectedOdeme != null && !string.IsNullOrEmpty(dosyaYolu))
                    {
                        MessageBoxResult result = MessageBox.Show("Dosyayı incelemek ister misiniz?", "Dosya İnceleme", MessageBoxButton.YesNo, MessageBoxImage.Asterisk);
                        if (result == MessageBoxResult.Yes)
                        {
                            Process.Start(new ProcessStartInfo(dosyaYolu) { UseShellExecute = true });
                        }
                    }
                    else if (string.IsNullOrEmpty(dosyaYolu))
                    {
                        MessageBox.Show("Ekli dosya yok", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "Button_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        string DosyaIslem = "", selectedFilePath = "";
        bool DosyaSil = false;
        private void ChangeButtonContent(string? filename = "")
        {
            try
            {
                if (DosyaIslem == "Uploaded")
                {
                    YukleButton.Visibility = Visibility.Hidden;

                    YukleTextbox.Text = filename;

                    var currentMargin = YukleBorder.Margin;
                    YukleBorder.Margin = new Thickness(currentMargin.Left, 7, currentMargin.Right, currentMargin.Bottom);
                }
                else if (DosyaIslem == "Saved")
                {
                    var currentMargin = YukleBorder.Margin;
                    YukleBorder.Margin = new Thickness(currentMargin.Left, 45, currentMargin.Right, currentMargin.Bottom);

                    YukleTextbox.Text = "";
                    YukleButton.Visibility = Visibility.Visible;

                    DosyaIslem = "";
                    DosyaPath = "";
                    selectedFilePath = "";
                }
                else if (DosyaIslem == "View")
                {
                    var selectedOdeme = dataGrid.SelectedItem as Odeme;
                    if (selectedOdeme.Dosya != null && selectedOdeme.Dosya != "")
                    {
                        YukleButton.Visibility = Visibility.Hidden;

                        filename = Path.GetFileName(DosyaPath);
                        YukleTextbox.Text = filename;

                        var currentMargin = YukleBorder.Margin;
                        YukleBorder.Margin = new Thickness(currentMargin.Left, 7, currentMargin.Right, currentMargin.Bottom);
                    }
                    else
                    {
                        var currentMargin = YukleBorder.Margin;
                        YukleBorder.Margin = new Thickness(currentMargin.Left, 45, currentMargin.Right, currentMargin.Bottom);

                        YukleTextbox.Text = "";

                        YukleButton.Visibility = Visibility.Visible;
                    }
                    DosyaIslem = "";
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "ChangeButtonContent()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void CariKartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenWindow(new CariKart());

                Business? selectedBusiness = Degiskenler.selectedBusiness;
                if (selectedBusiness != null)
                {
                    CariKodTextbox.Text = selectedBusiness.CariKod;
                    IsletmeAdiLabel.Content = selectedBusiness.CariIsim;
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
                    YukleGroupBox.IsEnabled = true;
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
                    DosyaPath = "";
                    DosyaIslem = "Saved";
                    ChangeButtonContent("");

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
                    YukleGroupBox.IsEnabled = false;
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
                    DosyaPath = "";
                    DosyaIslem = "Saved";
                    ChangeButtonContent("");

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
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                Business? selectedBusiness = Degiskenler.selectedBusiness;
                if (selectedBusiness != null)
                {
                    string searchTerm = txtSearch.Text;
                    SearchGetOdemeler(selectedBusiness.CariKod, searchTerm);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "txtSearch_TextChanged()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        int dosyaSayac = 0;
                        foreach (var odeme in filteredData)
                        {
                            bakiye += odeme.Borc - odeme.Alacak;
                            odeme.Bakiye = bakiye;
                            borcTop += odeme.Borc;
                            alacakTop += odeme.Alacak;
                            if (!string.IsNullOrEmpty(odeme.Dosya))
                            {
                                dosyaSayac++;
                            }
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

                        if (dosyaSayac == 0)
                        {
                            DosyaColumn.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            DosyaColumn.Visibility = Visibility.Visible;
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
                        DosyaPath = "";
                        DosyaIslem = "Saved";
                        ChangeButtonContent("");

                        BorcRadioButton.IsChecked = true;
                        TarihDatePicker.IsEnabled = true;
                        EvrakNoTextbox.IsEnabled = true;
                        AciklamaTextbox.IsEnabled = true;
                        VadeDatePicker.IsEnabled = true;
                        BAGroupBox.IsEnabled = true;
                        YukleGroupBox.IsEnabled = true;
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
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CariKodTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                GetBusinesses();
                MainViewModel viewModel = (MainViewModel)this.DataContext;

                bool var = false;

                foreach (var item in viewModel.Businesses)
                {
                    if (item is Business business)
                    {
                        string? carikod = business.CariKod;

                        if (CariKodTextbox.Text == carikod)
                        {
                            Degiskenler.selectedBusiness = business;

                            IsletmeAdiLabel.Content = business.CariIsim;
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
                            YukleGroupBox.IsEnabled = true;
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
                            DosyaPath = "";
                            DosyaIslem = "Saved";
                            ChangeButtonContent("");

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
                            YukleGroupBox.IsEnabled = false;
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
                            DosyaPath = "";
                            DosyaIslem = "Saved";
                            ChangeButtonContent("");

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
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadDataGrid()
        {
            try
            {
                Business? selectedBusiness = Degiskenler.selectedBusiness;
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
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SearchGetOdemeler(string? businessKod, string? searchTerm)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                viewModel.Odemeler.Clear();

                using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                {
                    connection.Open();

                    string tableName = $"Cari_{businessKod}";
                    string escapedTableName = $"\"{tableName}\"";

                    string query = $@"SELECT * FROM {escapedTableName} 
                             WHERE LOWER(tarih) LIKE '%' || LOWER(@searchTerm) || '%'                   
                             OR LOWER(evrakno) LIKE '%' || LOWER(@searchTerm) || '%' 
                             OR LOWER(aciklama) LIKE '%' || LOWER(@searchTerm) || '%' 
                             OR LOWER(vadetarihi) LIKE '%' || LOWER(@searchTerm) || '%' 
                             OR LOWER(borc) LIKE '%' || LOWER(@searchTerm) || '%' 
                             OR LOWER(alacak) LIKE '%' || LOWER(@searchTerm) || '%'";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@searchTerm", searchTerm);

                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Odeme odeme = new Odeme
                                {
                                    ID = reader.GetInt32(0),
                                    Tarih = reader.GetString(1),
                                    Tip = reader.GetString(2),
                                    Durum = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    EvrakNo = reader.GetString(4),
                                    Aciklama = reader.GetString(5),
                                    VadeTarihi = reader.GetString(6),
                                    Borc = reader.GetDouble(7),
                                    Alacak = reader.GetDouble(8),
                                    Dosya = reader.IsDBNull(9) ? null : reader.GetString(9),
                                };
                                viewModel.Odemeler.Add(odeme);
                            }
                        }
                    }

                    double bakiye = 0, borcTop = 0, alacakTop = 0;
                    int dosyaSayac = 0;
                    viewModel.Odemeler = new ObservableCollection<Odeme>(viewModel.Odemeler.OrderBy(o => DateTime.Parse(o.Tarih)));
                    foreach (var odeme in viewModel.Odemeler)
                    {
                        bakiye += odeme.Borc - odeme.Alacak;
                        odeme.Bakiye = bakiye;
                        borcTop += odeme.Borc;
                        alacakTop += odeme.Alacak;
                        if (!string.IsNullOrEmpty(odeme.Dosya))
                        {
                            dosyaSayac++;
                        }
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

                    if (dosyaSayac == 0)
                    {
                        DosyaColumn.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        DosyaColumn.Visibility = Visibility.Visible;
                    }
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

                using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                {
                    connection.Open();

                    string tableName = $"Cari_{businessKod}";
                    string escapedTableName = $"\"{tableName}\"";

                    string query = $"SELECT * FROM {escapedTableName}";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Odeme odeme = new Odeme
                                {
                                    ID = reader.GetInt32(0),
                                    Tarih = reader.GetString(1),
                                    Tip = reader.GetString(2),
                                    Durum = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    EvrakNo = reader.GetString(4),
                                    Aciklama = reader.GetString(5),
                                    VadeTarihi = reader.GetString(6),
                                    Borc = reader.GetDouble(7),
                                    Alacak = reader.GetDouble(8),
                                    Dosya = reader.IsDBNull(9) ? null : reader.GetString(9),
                                };
                                viewModel.Odemeler.Add(odeme);
                            }
                        }
                    }
                }

                // Bakiye hesaplama
                double bakiye = 0, borcTop = 0, alacakTop = 0;
                int dosyaSayac = 0;
                viewModel.Odemeler = new ObservableCollection<Odeme>(viewModel.Odemeler.OrderBy(o => DateTime.Parse(o.Tarih)));
                foreach (var odeme in viewModel.Odemeler)
                {
                    bakiye += odeme.Borc - odeme.Alacak;
                    odeme.Bakiye = bakiye;
                    borcTop += odeme.Borc;
                    alacakTop += odeme.Alacak;
                    if (!string.IsNullOrEmpty(odeme.Dosya))
                    {
                        dosyaSayac++;
                    }
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

                if (dosyaSayac == 0)
                {
                    DosyaColumn.Visibility = Visibility.Hidden;
                }
                else
                {
                    DosyaColumn.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "GetOdemeler()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        public class Odeme
        {
            public int ID { get; set; }
            public string? Tarih { get; set; }
            public string? Tip { get; set; }
            public string? Durum { get; set; }
            public string? EvrakNo { get; set; }
            public string? Aciklama { get; set; }
            public string? VadeTarihi { get; set; }
            public double Borc { get; set; }
            public double Alacak { get; set; }
            public double Bakiye { get; set; }
            public string? Dosya { get; set; }
        }

        private void Page_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Eğer tıklanan öğe DataGrid değilse veya DataGrid'in bir çocuğu değilse seçimi temizle
                if (!IsMouseOverDataGrid(e))
                {
                    Business? selectedBusiness = Degiskenler.selectedBusiness;
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
                        DosyaPath = "";
                        DosyaIslem = "Saved";
                        ChangeButtonContent("");

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

        public void GetBusinesses()
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
                LogManager.LogError(ex, className: "CariHareketKayitlari", methodName: "GetBusinesses()", stackTrace: ex.StackTrace);
                throw;
            }
        }
    }
}
