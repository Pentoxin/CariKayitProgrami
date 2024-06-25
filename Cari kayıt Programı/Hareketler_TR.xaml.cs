using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Cari_kayıt_Programı.Main_TR;

namespace Cari_kayıt_Programı
{
    public partial class Hareketler_TR : Page, INotifyPropertyChanged
    {
        public Hareketler_TR()
        {
            InitializeComponent();

            DataContext = this; // DataContext'i sayfa olarak ayarla
            SelectedDate = DateTime.Now;
        }

        private DateTime _selectedDate;

        public DateTime SelectedDate
        {
            get { return _selectedDate; }
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
                    OnPropertyChanged(nameof(SelectedDate));
                }
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Business? selectedBusiness = Degiskenler.selectedBusiness;
            int ID = 0;
            string? isletmeAdı = "Bir Hata Oluştu";
            if (selectedBusiness != null)
            {
                ID = selectedBusiness.ID;
                isletmeAdı = selectedBusiness.İşletme_Adı;
            }

            if (isletmeAdı != null)
            {
                IsletmeAdiLabel.Content = isletmeAdı.ToString();
            }

            dataGrid.ItemsSource = GetOdemeler(Config.ConnectionString, ID);
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
                    string tarih = selectedDate.ToString("dd-MM-yyyy");

                    DateTime selectedVadeDate = VadeDatePicker.SelectedDate.HasValue ? VadeDatePicker.SelectedDate.Value : DateTime.Now;
                    string vadeTarih = selectedVadeDate.ToString("dd-MM-yyyy");

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
                        Alacak = A
                    };

                    using (SQLiteConnection connection = new SQLiteConnection(Config.ConnectionString))
                    {
                        connection.Open();

                        string checkQuery = $"SELECT COUNT(*) FROM Cari_{ID} WHERE EvrakNo = @EvrakNo";
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
                                    string query = $"INSERT INTO Cari_{ID} (Tarih, Tip, EvrakNo, Aciklama, VadeTarihi, Borc, Alacak) " +
                                           "VALUES (@Tarih, @Tip, @EvrakNo, @Aciklama, @VadeTarihi, @Borc, @Alacak)";

                                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                                    {
                                        command.Parameters.AddWithValue("@Tarih", yeniOdeme.Tarih);
                                        command.Parameters.AddWithValue("@Tip", yeniOdeme.Tip);
                                        command.Parameters.AddWithValue("@EvrakNo", yeniOdeme.EvrakNo);
                                        command.Parameters.AddWithValue("@Aciklama", yeniOdeme.Aciklama);
                                        command.Parameters.AddWithValue("@VadeTarihi", yeniOdeme.VadeTarihi);
                                        command.Parameters.AddWithValue("@Borc", yeniOdeme.Borc);
                                        command.Parameters.AddWithValue("@Alacak", yeniOdeme.Alacak);

                                        command.ExecuteNonQuery();
                                    }

                                    TarihDatePicker.SelectedDate = DateTime.Now;
                                    EvrakNoTextbox.Clear();
                                    TutarTextbox.Clear();
                                    AciklamaTextbox.Clear();
                                    VadeDatePicker.SelectedDate = DateTime.Now;
                                    dataGrid.SelectedItems.Clear();
                                }
                            }
                        }
                    }
                    dataGrid.ItemsSource = GetOdemeler(Config.ConnectionString, ID);
                    MessageBox.Show("İşletme hareket bilgileri veritabanına kaydedildi.", "Kaydedildi", MessageBoxButton.OK, MessageBoxImage.Information);
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
                if (dataGrid.SelectedItem != null)
                {
                    // Seçilen öğenin ID'sini alma
                    if (dataGrid.SelectedItem is Odeme selectedOdeme)
                    {
                        selectedOdemeId = selectedOdeme.ID;
                    }
                }

                // Seçilen öğenin ID'si 0'dan büyükse silme işlemini gerçekleştir
                if (selectedOdemeId > 0)
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
                            }
                            else
                            {
                                MessageBox.Show("Hareket silinemedi.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    dataGrid.ItemsSource = GetOdemeler(Config.ConnectionString, ID);
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

        private void DegistirHareketButton_Click(object sender, RoutedEventArgs e)
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

                    int selectedOdemeId = 0;
                    if (dataGrid.SelectedItem != null)
                    {
                        if (dataGrid.SelectedItem is Odeme selectedOdeme)
                        {
                            selectedOdemeId = selectedOdeme.ID;
                        }
                    }

                    DateTime selectedDate = TarihDatePicker.SelectedDate.HasValue ? TarihDatePicker.SelectedDate.Value : DateTime.Now;
                    string tarih = selectedDate.ToString("dd-MM-yyyy");

                    DateTime selectedVadeDate = VadeDatePicker.SelectedDate.HasValue ? VadeDatePicker.SelectedDate.Value : DateTime.Now;
                    string vadeTarih = selectedVadeDate.ToString("dd-MM-yyyy");

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
                        Alacak = A
                    };

                    if (selectedOdemeId > 0)
                    {

                        using (SQLiteConnection connection = new SQLiteConnection(Config.ConnectionString))
                        {
                            connection.Open();

                            string checkQuery = $"SELECT COUNT(*) FROM Cari_{ID} WHERE EvrakNo = @EvrakNo";
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
                                    if (MessageBox.Show("Veriyi değiştirmek istediğinize emin misiniz?", "Kaydet", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                    {
                                        string query = $"UPDATE Cari_{ID} " +
                                               "SET Tarih = @Tarih, " +
                                               "    Tip = @Tip, " +
                                               "    EvrakNo = @EvrakNo, " +
                                               "    Aciklama = @Aciklama, " +
                                               "    VadeTarihi = @VadeTarihi, " +
                                               "    Borc = @Borc, " +
                                               "    Alacak = @Alacak " +
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
                                            command.Parameters.AddWithValue("@ID", selectedOdemeId);

                                            command.ExecuteNonQuery();
                                        }
                                    }
                                    else
                                    {
                                        MessageBox.Show("Lütfen değiştirmek istediğiniz hareketi seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    }
                                }
                                dataGrid.ItemsSource = GetOdemeler(Config.ConnectionString, ID);
                                TarihDatePicker.SelectedDate = DateTime.Now;
                                EvrakNoTextbox.Clear();
                                TutarTextbox.Clear();
                                AciklamaTextbox.Clear();
                                VadeDatePicker.SelectedDate = DateTime.Now;
                                dataGrid.SelectedItems.Clear();
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
                string? isletmeAdi = "";
                if (selectedBusiness != null)
                {
                    isletmeAdi = selectedBusiness.İşletme_Adı;
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

                        // Başlık satırını ekle
                        var properties = typeof(Odeme).GetProperties();
                        for (int col = 0; col < properties.Length; col++)
                        {
                            worksheet.Cell(1, col + 1).Value = properties[col].Name;
                        }

                        // Veri satırlarını ekle
                        int row = 2;
                        if (odemeler != null)
                        {
                            foreach (var odeme in odemeler)
                            {
                                for (int col = 0; col < properties.Length; col++)
                                {
                                    var value = properties[col].GetValue(odeme);

                                    // Excel hücre değerini ayarla
                                    worksheet.Cell(row, col + 1).Value = value != null ? value.ToString() : "";
                                }
                                row++;
                            }
                        }

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
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
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

        public ObservableCollection<Odeme> GetOdemeler(string connectionString, int businessId)
        {
            List<Odeme> odemeler = new List<Odeme>();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
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
                                Alacak = reader.GetDouble(7)
                            };
                            odemeler.Add(odeme);
                        }
                    }
                }
            }

            // Bakiye hesaplama
            double bakiye = 0;
            var sortedOdemeler = odemeler.OrderBy(o => o.Tarih).ToList();
            foreach (var odeme in sortedOdemeler)
            {
                bakiye += odeme.Borc - odeme.Alacak;
                odeme.Bakiye = bakiye;
            }

            // ObservableCollection'a dönüştür
            ObservableCollection<Odeme> sortedObservableOdemeler = new ObservableCollection<Odeme>(sortedOdemeler);

            // DataGrid'in ItemsSource özelliğine atama yap
            dataGrid.ItemsSource = sortedObservableOdemeler;

            foreach (DataGridColumn column in dataGrid.Columns)
            {
                string? columnName = column.Header != null ? column.Header.ToString() : string.Empty;

                if (columnName == "ID")
                {
                    column.Visibility = Visibility.Hidden;
                }
            }
            return sortedObservableOdemeler;
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
        }
    }
}