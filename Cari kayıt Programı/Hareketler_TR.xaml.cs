using ClosedXML.Excel;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Cari_kayıt_Programı.Main_TR;

namespace Cari_kayıt_Programı
{
    public partial class Hareketler_TR : Page
    {
        public MainViewModel ViewModel { get; set; }

        public Hareketler_TR()
        {
            InitializeComponent();

            ViewModel = new MainViewModel();
            DataContext = ViewModel;
        }

        private string? DosyaPath = "";


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Business? selectedBusiness = Degiskenler.selectedBusiness;
            if (selectedBusiness != null)
            {
                CariKodLabel.Content = selectedBusiness.ID;
                IsletmeAdiLabel.Content = selectedBusiness.Isletme_Adi;
                LoadDataGrid();

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
                DegistirHareketButton.IsEnabled = true;
                YazdırButton.IsEnabled = true;
                txtSearch.IsEnabled = true;
            }
            else
            {
                IsletmeAdiLabel.Content = "-";
                BorcRadioButton.IsChecked = false;
                TarihDatePicker.IsEnabled = false;
                EvrakNoTextbox.IsEnabled = false;
                AciklamaTextbox.IsEnabled = false;
                VadeDatePicker.IsEnabled = false;
                BAGroupBox.IsEnabled = false;
                YukleGroupBox.IsEnabled = false;
                TutarTextbox.IsEnabled = false;
                YeniHareketButton.IsEnabled = false;
                SilHareketButton.IsEnabled = false;
                DegistirHareketButton.IsEnabled = false;
                YazdırButton.IsEnabled = false;
                txtSearch.IsEnabled = false;
            }

            DegistirIptalButton.Visibility = Visibility.Hidden;
            AdminButton.Visibility = Visibility.Hidden;
        }

        private void YeniHareketButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(AciklamaTextbox.Text) && !string.IsNullOrWhiteSpace(TutarTextbox.Text) && (AlacakRadioButton.IsChecked == true || BorcRadioButton.IsChecked == true) && TarihDatePicker.SelectedDate.HasValue && VadeDatePicker.SelectedDate.HasValue)
                {
                    Business? selectedBusiness = Degiskenler.selectedBusiness;
                    int ID = 0;
                    if (selectedBusiness != null)
                    {
                        ID = selectedBusiness.ID;
                    }

                    DateTime selectedDate = TarihDatePicker.SelectedDate.HasValue ? TarihDatePicker.SelectedDate.Value : DateTime.Now;
                    string tarih = selectedDate.ToString("dd.MM.yyyy");

                    DateTime selectedVadeDate = VadeDatePicker.SelectedDate.HasValue ? VadeDatePicker.SelectedDate.Value : DateTime.Now;
                    string vadeTarih = selectedVadeDate.ToString("dd.MM.yyyy");

                    string tip = AlacakRadioButton.IsChecked == true ? "A" : "B";

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
                        EvrakNo = EvrakNoTextbox.Text,
                        Aciklama = AciklamaTextbox.Text,
                        VadeTarihi = vadeTarih,
                        Borc = B,
                        Alacak = A,
                        Dosya = DosyaPath
                    };

                    using (SQLiteConnection connection = new SQLiteConnection(Config.ConnectionString))
                    {
                        connection.Open();

                        string checkQuery = $"SELECT COUNT(*) FROM Cari_{ID} WHERE lower(EvrakNo) = lower(@EvrakNo)";
                        using (SQLiteCommand checkCommand = new SQLiteCommand(checkQuery, connection))
                        {
                            checkCommand.Parameters.AddWithValue("@EvrakNo", EvrakNoTextbox.Text);
                            int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                            if (count > 0)
                            {
                                MessageBox.Show("Bu evrak numarası daha önce kaydedilmiş.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                            else
                            {
                                if (MessageBox.Show("Veriyi kaydetmek istediğinize emin misiniz?", "Kaydet", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                {
                                    string query = $"INSERT INTO Cari_{ID} (Tarih, Tip, EvrakNo, Aciklama, VadeTarihi, Borc, Alacak, Dosya) " +
                                           "VALUES (@Tarih, @Tip, @EvrakNo, @Aciklama, @VadeTarihi, @Borc, @Alacak, @Dosya)";

                                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                                    {
                                        command.Parameters.AddWithValue("@Tarih", yeniOdeme.Tarih);
                                        command.Parameters.AddWithValue("@Tip", yeniOdeme.Tip);
                                        command.Parameters.AddWithValue("@EvrakNo", yeniOdeme.EvrakNo);
                                        command.Parameters.AddWithValue("@Aciklama", yeniOdeme.Aciklama);
                                        command.Parameters.AddWithValue("@VadeTarihi", yeniOdeme.VadeTarihi);
                                        command.Parameters.AddWithValue("@Borc", yeniOdeme.Borc);
                                        command.Parameters.AddWithValue("@Alacak", yeniOdeme.Alacak);
                                        command.Parameters.AddWithValue("@Dosya", yeniOdeme.Dosya);

                                        command.ExecuteNonQuery();
                                    }

                                    if (selectedFilePath != "" && DosyaPath != "")
                                    {
                                        File.Copy(selectedFilePath, DosyaPath, true);
                                    }
                                    TarihDatePicker.SelectedDate = DateTime.Now;
                                    EvrakNoTextbox.Clear();
                                    TutarTextbox.Clear();
                                    AciklamaTextbox.Clear();
                                    VadeDatePicker.SelectedDate = DateTime.Now;
                                    dataGrid.SelectedItems.Clear();
                                    DosyaPath = "";
                                    DosyaIslem = "Saved";
                                    ChangeButtonContent("");
                                    MessageBox.Show("İşletme hareket bilgileri veritabanına kaydedildi.", "Kaydedildi", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
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
                LogError(ex);
            }
        }

        private void SilHareketButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Business? selectedBusiness = Degiskenler.selectedBusiness;
                int ID = 0;
                if (selectedBusiness != null)
                {
                    ID = selectedBusiness.ID;
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
                        using (SQLiteConnection connection = new SQLiteConnection(Config.ConnectionString))
                        {
                            connection.Open();

                            string deleteQuery = $"DELETE FROM Cari_{ID} WHERE ID = @OdemeId";
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
                LogError(ex);
            }
        }

        private int DegistirSayac = 0;
        private void DegistirHareketButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Business? selectedBusiness = Degiskenler.selectedBusiness;
                int ID = 0;
                if (selectedBusiness != null)
                {
                    ID = selectedBusiness.ID;
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

                if (DegistirSayac == 0)
                {
                    TarihDatePicker.IsEnabled = true;
                    EvrakNoTextbox.IsEnabled = true;
                    AciklamaTextbox.IsEnabled = true;
                    VadeDatePicker.IsEnabled = true;
                    BAGroupBox.IsEnabled = true;
                    YukleGroupBox.IsEnabled = true;
                    TutarTextbox.IsEnabled = true;
                    YeniHareketButton.IsEnabled = false;
                    SilHareketButton.IsEnabled = false;
                    YazdırButton.IsEnabled = false;
                    DegistirIptalButton.Visibility = Visibility.Visible;
                    DegistirSayac++;

                    DegistirTextbox.Text = "Onayla";
                    DegistirIcon.Kind = PackIconKind.Check;
                }
                else if (DegistirSayac == 1)
                {
                    Degistir(ID, selectedOdemeId, selectedOdemeDosya);
                }

            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void DegistirIptalButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Business? selectedBusiness = Degiskenler.selectedBusiness;
                int ID = 0;
                if (selectedBusiness != null)
                {
                    ID = selectedBusiness.ID;
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
                DegistirSayac = 3;
                Degistir(ID, selectedOdemeId, selectedOdemeDosya);

                YeniHareketButton.IsEnabled = true;
                SilHareketButton.IsEnabled = true;
                YazdırButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        public void Degistir(int ID = 0, int selectedOdemeId = 0, string selectedOdemeDosya = "")
        {
            try
            {
                if (DegistirSayac == 3)
                {
                    DegistirIptalButton.Visibility = Visibility.Hidden;
                    LoadDataGrid();
                    TarihDatePicker.SelectedDate = DateTime.Now;
                    EvrakNoTextbox.Clear();
                    TutarTextbox.Clear();
                    AciklamaTextbox.Clear();
                    VadeDatePicker.SelectedDate = DateTime.Now;
                    dataGrid.SelectedItems.Clear();
                    DosyaPath = "";
                    DegistirSayac = 0;
                    DegistirTextbox.Text = "Değiştir";
                    DegistirIcon.Kind = PackIconKind.Pencil;
                    BorcRadioButton.IsChecked = true;
                    DosyaIslem = "Saved";
                    ChangeButtonContent("");
                }
                if (!string.IsNullOrWhiteSpace(AciklamaTextbox.Text) && !string.IsNullOrWhiteSpace(TutarTextbox.Text) && (AlacakRadioButton.IsChecked == true || BorcRadioButton.IsChecked == true) && TarihDatePicker.SelectedDate.HasValue && VadeDatePicker.SelectedDate.HasValue)
                {
                    DateTime selectedDate = TarihDatePicker.SelectedDate.HasValue ? TarihDatePicker.SelectedDate.Value : DateTime.Now;
                    string tarih = selectedDate.ToString("dd.MM.yyyy");

                    DateTime selectedVadeDate = VadeDatePicker.SelectedDate.HasValue ? VadeDatePicker.SelectedDate.Value : DateTime.Now;
                    string vadeTarih = selectedVadeDate.ToString("dd.MM.yyyy");

                    string tip = AlacakRadioButton.IsChecked == true ? "A" : "B";

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
                        EvrakNo = EvrakNoTextbox.Text,
                        Aciklama = AciklamaTextbox.Text,
                        VadeTarihi = vadeTarih,
                        Borc = B,
                        Alacak = A,
                        Dosya = DosyaPath
                    };

                    if (selectedOdemeId > 0)
                    {

                        using (SQLiteConnection connection = new SQLiteConnection(Config.ConnectionString))
                        {
                            connection.Open();

                            string checkQuery = $"SELECT COUNT(*) FROM Cari_{ID} WHERE EvrakNo = @EvrakNo AND ID != @ID";
                            using (SQLiteCommand checkCommand = new SQLiteCommand(checkQuery, connection))
                            {
                                checkCommand.Parameters.AddWithValue("@EvrakNo", EvrakNoTextbox.Text);
                                checkCommand.Parameters.AddWithValue("@ID", selectedOdemeId);
                                int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                                if (count > 0)
                                {
                                    MessageBox.Show("Bu evrak numarası daha önce kaydedilmiş.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                                else
                                {
                                    if (MessageBox.Show("Veriyi değiştirmek istediğinize emin misiniz?", "Kaydet", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                    {
                                        string query = $"UPDATE Cari_{ID} " +
                                               "SET Tarih = @Tarih, " +
                                               "    Tip = @Tip, " +
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

                                        DegistirIptalButton.Visibility = Visibility.Hidden;
                                        DegistirSayac = 0;
                                        DegistirTextbox.Text = "Değiştir";
                                        DegistirIcon.Kind = PackIconKind.Pencil;

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

                                YeniHareketButton.IsEnabled = true;
                                SilHareketButton.IsEnabled = true;
                                YazdırButton.IsEnabled = true;
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

        private void YazdırButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Business? selectedBusiness = Degiskenler.selectedBusiness;
                string? isletmeAdi = "", telefonNo1 = "", telefonNo2 = "", adres = "", vergiNo = "", vergiDairesi = "";
                if (selectedBusiness != null)
                {
                    isletmeAdi = selectedBusiness.Isletme_Adi != "" ? selectedBusiness.Isletme_Adi : "-";
                    telefonNo1 = selectedBusiness.Telefon1 != "" ? selectedBusiness.Telefon1 : "-";
                    telefonNo2 = selectedBusiness.Telefon2 != "" ? selectedBusiness.Telefon2 : "-";
                    adres = selectedBusiness.Adres != "" ? selectedBusiness.Adres : "-";
                    vergiNo = selectedBusiness.VergiNo != "" ? selectedBusiness.VergiNo : "-";
                    vergiDairesi = selectedBusiness.VergiDairesi != "" ? selectedBusiness.VergiDairesi : "-";
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Excel Dosyası|*.xlsx";
                saveFileDialog.FileName = $"{isletmeAdi} Hareketleri.xlsx";
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


                        worksheet.Cell(1, 1).Value = "İşletme Bilgileri";
                        worksheet.Cell(1, 2).Value = isletmeAdi;
                        worksheet.Cell(2, 2).Value = $"Telefon 1: {telefonNo1} | Telefon 2: {telefonNo2}";
                        worksheet.Cell(3, 2).Value = $"Adres: {adres}";
                        worksheet.Cell(4, 2).Value = $"V.No: {vergiNo}";
                        worksheet.Cell(4, 3).Value = $"V.Dairesi: {vergiDairesi}";

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
                                            if (value.ToString() != null && value.ToString() != "")
                                            {
                                                var cell = worksheet.Cell(row, colIndex);
                                                string? link = value.ToString();
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
                LogError(ex);
            }
        }

        private void YukleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Business? selectedBusiness = Degiskenler.selectedBusiness;
                string? isletmeAdi = "";
                if (selectedBusiness != null)
                {
                    isletmeAdi = selectedBusiness.Isletme_Adi;
                }

                var selectedOdeme = dataGrid.SelectedItem as Odeme;
                if (selectedOdeme != null)
                {
                    if (!string.IsNullOrEmpty(selectedOdeme.Dosya))
                    {
                        DosyaIslem = "Uploaded";
                    }
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

                        DosyaPath = Path.Combine(Config.IsletmePath, isletmeAdi, fileName);

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
                LogError(ex);
            }
        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedOdeme = dataGrid.SelectedItem as Odeme;
                if (selectedOdeme != null && TarihDatePicker.SelectedDate.HasValue && VadeDatePicker.SelectedDate.HasValue)
                {
                    TarihDatePicker.SelectedDate = DateTime.Parse(selectedOdeme.Tarih);
                    EvrakNoTextbox.Text = selectedOdeme.EvrakNo;
                    AciklamaTextbox.Text = selectedOdeme.Aciklama;
                    DosyaPath = selectedOdeme.Dosya;
                    DosyaIslem = "View";
                    ChangeButtonContent();

                    VadeDatePicker.SelectedDate = DateTime.Parse(selectedOdeme.VadeTarihi);

                    if (selectedOdeme.Tip == "A")
                    {
                        AlacakRadioButton.IsChecked = true;
                    }
                    else if (selectedOdeme.Tip == "B")
                    {
                        BorcRadioButton.IsChecked = true;
                    }

                    if (selectedOdeme.Tip == "A")
                    {
                        TutarTextbox.Text = selectedOdeme.Alacak.ToString();
                    }
                    else if (selectedOdeme.Tip == "B")
                    {
                        TutarTextbox.Text = selectedOdeme.Borc.ToString();
                    }

                    TarihDatePicker.IsEnabled = false;
                    EvrakNoTextbox.IsEnabled = false;
                    AciklamaTextbox.IsEnabled = false;
                    VadeDatePicker.IsEnabled = false;
                    BAGroupBox.IsEnabled = false;
                    YukleGroupBox.IsEnabled = false;
                    TutarTextbox.IsEnabled = false;

                    if (DegistirSayac > 0)
                    {
                        DegistirIptalButton.Visibility = Visibility.Hidden;
                        DegistirSayac = 0;
                        DegistirTextbox.Text = "Değiştir";
                        DegistirIcon.Kind = PackIconKind.Pencil;
                        BorcRadioButton.IsChecked = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
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
                    if (selectedOdeme != null && selectedOdeme.Dosya != null && selectedOdeme.Dosya != "")
                    {
                        MessageBoxResult result = MessageBox.Show("Dosyayı incelemek ister misiniz?", "Dosya İnceleme", MessageBoxButton.YesNo, MessageBoxImage.Asterisk);
                        if (result == MessageBoxResult.Yes)
                        {
                            Process.Start(new ProcessStartInfo(selectedOdeme.Dosya) { UseShellExecute = true });
                        }
                    }
                    else if (selectedOdeme.Dosya == null || selectedOdeme.Dosya == "")
                    {
                        MessageBox.Show("Ekli dosya yok", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
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
                LogError(ex);
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
                    CariKodLabel.Content = selectedBusiness.ID;
                    IsletmeAdiLabel.Content = selectedBusiness.Isletme_Adi;
                    BorcTopTextbox.Clear();
                    AlacakTopTextbox.Clear();
                    BakiyeTopTextbox.Clear();
                    LoadDataGrid();

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
                    DegistirHareketButton.IsEnabled = true;
                    YazdırButton.IsEnabled = true;
                    txtSearch.IsEnabled = true;

                    TarihDatePicker.SelectedDate = DateTime.Now;
                    EvrakNoTextbox.Clear();
                    TutarTextbox.Clear();
                    AciklamaTextbox.Clear();
                    VadeDatePicker.SelectedDate = DateTime.Now;
                    dataGrid.SelectedItems.Clear();
                    txtSearch.Clear();
                    DosyaPath = "";
                    DegistirSayac = 0;
                    DegistirIptalButton.Visibility = Visibility.Hidden;
                    DegistirTextbox.Text = "Değiştir";
                    DegistirIcon.Kind = PackIconKind.Pencil;
                    DosyaIslem = "Saved";
                    ChangeButtonContent("");

                }
                else
                {
                    IsletmeAdiLabel.Content = "-";
                    CariKodLabel.Content = "-";
                    BorcRadioButton.IsChecked = false;
                    TarihDatePicker.IsEnabled = false;
                    EvrakNoTextbox.IsEnabled = false;
                    AciklamaTextbox.IsEnabled = false;
                    VadeDatePicker.IsEnabled = false;
                    BAGroupBox.IsEnabled = false;
                    YukleGroupBox.IsEnabled = false;
                    TutarTextbox.IsEnabled = false;
                    YeniHareketButton.IsEnabled = false;
                    SilHareketButton.IsEnabled = false;
                    DegistirHareketButton.IsEnabled = false;
                    YazdırButton.IsEnabled = false;
                    txtSearch.IsEnabled = false;

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
                    DegistirSayac = 0;
                    DegistirIptalButton.Visibility = Visibility.Hidden;
                    DegistirTextbox.Text = "Değiştir";
                    DegistirIcon.Kind = PackIconKind.Pencil;
                    DosyaIslem = "Saved";
                    ChangeButtonContent("");

                    MainViewModel? viewModel = (MainViewModel)this.DataContext;
                    viewModel.odemeler.Clear();
                    BorcTopTextbox.Text = "";
                    AlacakTopTextbox.Text = "";
                    BakiyeTopTextbox.Text = "";
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
                Business? selectedBusiness = Degiskenler.selectedBusiness;
                if (selectedBusiness != null)
                {
                    string searchTerm = txtSearch.Text;
                    SearchGetOdemeler(selectedBusiness.ID, searchTerm);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
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
                LogError(ex);
            }
        }

        private void LoadDataGrid()
        {
            try
            {
                Business? selectedBusiness = Degiskenler.selectedBusiness;
                if (selectedBusiness != null)
                {
                    GetOdemeler(selectedBusiness.ID);

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
                LogError(ex);
            }
        }

        public void SearchGetOdemeler(int businessId, string searchTerm)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                viewModel.odemeler.Clear();

                using (SQLiteConnection connection = new SQLiteConnection(Config.ConnectionString))
                {
                    connection.Open();

                    string query = $@"SELECT * FROM Cari_{businessId} 
                             WHERE LOWER(tarih) LIKE '%' || LOWER(@searchTerm) || '%' 
                             OR ID LIKE '%' || LOWER(@searchTerm) || '%' 
                             OR LOWER(tip) LIKE '%' || LOWER(@searchTerm) || '%' 
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
                                    EvrakNo = reader.GetString(3),
                                    Aciklama = reader.GetString(4),
                                    VadeTarihi = reader.GetString(5),
                                    Borc = reader.GetDouble(6),
                                    Alacak = reader.GetDouble(7),
                                    Dosya = reader.IsDBNull(8) ? null : reader.GetString(8),
                                };
                                viewModel.odemeler.Add(odeme);
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

        public void GetOdemeler(int businessId)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                viewModel.odemeler.Clear();
                using (SQLiteConnection connection = new SQLiteConnection(Config.ConnectionString))
                {
                    connection.Open();

                    string query = $"SELECT * FROM Cari_{businessId}";

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
                                    EvrakNo = reader.GetString(3),
                                    Aciklama = reader.GetString(4),
                                    VadeTarihi = reader.GetString(5),
                                    Borc = reader.GetDouble(6),
                                    Alacak = reader.GetDouble(7),
                                    Dosya = reader.IsDBNull(8) ? null : reader.GetString(8),
                                };
                                viewModel.odemeler.Add(odeme);
                            }
                        }
                    }
                }

                // Bakiye hesaplama
                double bakiye = 0, borcTop = 0, alacakTop = 0;
                viewModel.Odemeler = new ObservableCollection<Odeme>(viewModel.Odemeler.OrderBy(o => DateTime.Parse(o.Tarih)));
                foreach (var odeme in viewModel.odemeler)
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
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        public class Odeme
        {
            public int ID { get; set; }
            public string? Tarih { get; set; }
            public string? Tip { get; set; }
            public string? EvrakNo { get; set; }
            public string? Aciklama { get; set; }
            public string? VadeTarihi { get; set; }
            public double Borc { get; set; }
            public double Alacak { get; set; }
            public double Bakiye { get; set; }
            public string? Dosya { get; set; }
        }

        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            // Method intentionally left empty.
        }


        private void Page_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Eğer tıklanan öğe DataGrid değilse veya DataGrid'in bir çocuğu değilse seçimi temizle
                if (!IsMouseOverDataGrid(e))
                {
                    Business? selectedBusiness = Degiskenler.selectedBusiness;
                    if (selectedBusiness != null)
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
                        DegistirSayac = 0;
                        DegistirIptalButton.Visibility = Visibility.Hidden;
                        DegistirTextbox.Text = "Değiştir";
                        DegistirIcon.Kind = PackIconKind.Pencil;
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

                        Keyboard.ClearFocus();
                    }
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
                DependencyObject? originalSource = e.OriginalSource as DependencyObject;

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

        // Yardımcı metot: Bir ebeveyni belirli bir türde bulmak için
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            T parent = parentObject as T;
            return parent ?? FindParent<T>(parentObject);
        }

        private void TutarTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void TutarTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
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

        private static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("[^0-9,]+"); // Sadece sayısal karakterlere izin ver
            return !regex.IsMatch(text);
        }

        private void AddButtonToDataGrid()
        {
            try
            {
                // DataGrid'e yeni bir sütun ekle
                DataGridTemplateColumn buttonColumn = new DataGridTemplateColumn();
                buttonColumn.Header = "İşlem";

                // Düğme için hücre şablonu oluştur
                FrameworkElementFactory buttonFactory = new FrameworkElementFactory(typeof(Button));
                buttonFactory.SetValue(Button.ContentProperty, "İşlem Yap");
                buttonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(Button_Click));

                DataTemplate buttonTemplate = new DataTemplate();
                buttonTemplate.VisualTree = buttonFactory;

                buttonColumn.CellTemplate = buttonTemplate;

                // DataGrid'e sütunu ekle
                dataGrid.Columns.Add(buttonColumn);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }
    }
}