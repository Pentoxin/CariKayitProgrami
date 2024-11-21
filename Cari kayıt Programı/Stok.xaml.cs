using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Cari_kayıt_Programı.CariHesapKayitlari;

namespace Cari_kayıt_Programı
{
    public partial class Stok : Window
    {
        public Stok()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dataGrid.ItemsSource = GetStoklar();

            List<string> comboBoxItems = new List<string> { "Adet", "Kasa", "Kilogram", "Koli", "Paket" };

            foreach (string item in comboBoxItems)
            {
                OlcuBirimi1ComboBox.Items.Add(item);
                OlcuBirimi2ComboBox.Items.Add(item);
            }
        }

        private void KaydetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(StokAdTextBox.Text) || string.IsNullOrEmpty(StokKodTextBox.Text))
                {
                    MessageBox.Show("Lütfen zorunlu yerleri doldurunuz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                    {
                        connection.Open();

                        List<Stoklar> stokList = new List<Stoklar>();

                        string query = "SELECT * FROM Stok";

                        using (SQLiteCommand command = new SQLiteCommand(query, connection))
                        {
                            using (SQLiteDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    Stoklar b = new Stoklar
                                    {
                                        StokKod = reader.GetString(2),
                                        StokAdi = reader.GetString(3),
                                    };
                                    stokList.Add(b);
                                }
                            }
                        }

                        foreach (var item in stokList)
                        {
                            if (item is Stoklar stok)
                            {
                                string? StokAdi = stok.StokAdi;
                                string? StokKod = stok.StokKod;

                                string StokAdiLower = StokAdi.ToLower(new CultureInfo("tr-TR"));
                                string normalizedStokAdi = StokAdTextBox.Text.ToLower(new CultureInfo("tr-TR"));
                                string StokKodLower = StokKod.ToLower(new CultureInfo("tr-TR"));
                                string normalizedStokKod = StokKodTextBox.Text.ToLower(new CultureInfo("tr-TR"));
                                if (StokAdiLower == normalizedStokAdi || StokKodLower == normalizedStokKod)
                                {
                                    MessageBox.Show("Bu stok adı veya stok kodu daha önce girilmiş.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                            }
                        }

                        if (MessageBox.Show("Seçilen veriyi kaydetmek istediğinize emin misiniz?", "Kaydet", MessageBoxButton.YesNo, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
                        {
                            string? olcubirimi2oran;
                            if (string.IsNullOrEmpty(OlcuBirimiOran1.Text))
                            {
                                olcubirimi2oran = $"1/{OlcuBirimiOran2.Text}";
                            }
                            else if (string.IsNullOrEmpty(OlcuBirimiOran2.Text))
                            {
                                olcubirimi2oran = $"{OlcuBirimiOran1.Text}/1";
                            }
                            else
                            {
                                olcubirimi2oran = $"{OlcuBirimiOran1.Text}/{OlcuBirimiOran2.Text}";
                            }

                            string insertSql = "INSERT INTO Stok (stokkodu, stokadi, kdvsatis, kdvalis, olcubirimi1, olcubirimi2, olcubirimi2oran) VALUES (@stokkodu, @stokadi, @kdvsatis, @kdvalis, @olcubirimi1, @olcubirimi2, @olcubirimi2oran)";
                            using (SQLiteCommand insertCommand = new SQLiteCommand(insertSql, connection))
                            {
                                insertCommand.Parameters.AddWithValue("@stokkodu", StokKodTextBox.Text);
                                insertCommand.Parameters.AddWithValue("@stokadi", StokAdTextBox.Text);
                                insertCommand.Parameters.AddWithValue("@kdvsatis", SatisKDVOranTextBox.Text);
                                insertCommand.Parameters.AddWithValue("@kdvalis", AlisKDVOranTextBox.Text);
                                insertCommand.Parameters.AddWithValue("@olcubirimi1", OlcuBirimi1ComboBox.Text);
                                insertCommand.Parameters.AddWithValue("@olcubirimi2", OlcuBirimi2ComboBox.Text);
                                insertCommand.Parameters.AddWithValue("@olcubirimi2oran", olcubirimi2oran);
                                insertCommand.ExecuteNonQuery();

                                MessageBox.Show("Stok bilgileri veritabanına kaydedildi.", "Kaydedildi", MessageBoxButton.OK, MessageBoxImage.Information);
                            }

                            StokKodTextBox.Clear();
                            StokAdTextBox.Clear();
                            SatisKDVOranTextBox.Clear();
                            AlisKDVOranTextBox.Clear();
                            OlcuBirimi1ComboBox.Text = null;
                            OlcuBirimi2ComboBox.Text = null;
                            OlcuBirimiOran1.Text = "1";
                            OlcuBirimiOran2.Text = "1";
                            olcubirimi2oran = null;
                            dataGrid.ItemsSource = GetStoklar();

                            Keyboard.ClearFocus();
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
                Stoklar selectedStokItem = (Stoklar)dataGrid.SelectedItem;

                if (selectedStokItem == null)
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
                            string query = "DELETE FROM Stok WHERE ID=@Id";

                            using (SQLiteCommand command = new SQLiteCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@Id", selectedStokItem.ID);
                                command.ExecuteNonQuery();
                            }
                        }
                        dataGrid.ItemsSource = GetStoklar();
                    }
                }
                StokKodTextBox.Clear();
                StokAdTextBox.Clear();
                SatisKDVOranTextBox.Clear();
                AlisKDVOranTextBox.Clear();
                OlcuBirimi1ComboBox.Text = null;
                OlcuBirimi2ComboBox.Text = null;
                OlcuBirimiOran1.Text = "1";
                OlcuBirimiOran2.Text = "1";
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
                Stoklar selectedStokItem = (Stoklar)dataGrid.SelectedItem;

                if (selectedStokItem == null)
                {
                    MessageBox.Show("Önce değiştirmek istediğiniz veriyi seçiniz", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                {
                    connection.Open();

                    List<Stoklar> stokList = new List<Stoklar>();

                    string query = "SELECT * FROM Stok";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Stoklar b = new Stoklar
                                {
                                    ID = reader.GetInt32(0),
                                    StokKod = reader.GetString(1),
                                    StokAdi = reader.GetString(2),
                                };
                                stokList.Add(b);
                            }
                        }
                    }

                    foreach (var item in stokList)
                    {
                        if (item is Stoklar stok && selectedStokItem.ID != stok.ID)
                        {
                            string? StokAdi = stok.StokAdi;
                            string? StokKod = stok.StokKod;

                            string StokAdiLower = StokAdi.ToLower(new CultureInfo("tr-TR"));
                            string normalizedStokAdi = StokAdTextBox.Text.ToLower(new CultureInfo("tr-TR"));
                            string StokKodLower = StokKod.ToLower(new CultureInfo("tr-TR"));
                            string normalizedStokKod = StokKodTextBox.Text.ToLower(new CultureInfo("tr-TR"));
                            if (StokAdiLower == normalizedStokAdi || StokKodLower == normalizedStokKod)
                            {
                                MessageBox.Show("Bu stok adı veya stok kodu daha önce girilmiş.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                    }

                    if (MessageBox.Show("Seçili veriyi değiştirmek istediğinize emin misiniz?", "Veri Güncelleme", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        string? olcubirimi2oran;
                        if (string.IsNullOrEmpty(OlcuBirimiOran1.Text))
                        {
                            olcubirimi2oran = $"1/{OlcuBirimiOran2.Text}";
                        }
                        else if (string.IsNullOrEmpty(OlcuBirimiOran2.Text))
                        {
                            olcubirimi2oran = $"{OlcuBirimiOran1.Text}/1";
                        }
                        else
                        {
                            olcubirimi2oran = $"{OlcuBirimiOran1.Text}/{OlcuBirimiOran2.Text}";
                        }

                        string updateSql = "UPDATE Stok SET stokadi=@stokadi, kdvsatis=@kdvsatis, kdvalis=@kdvalis, olcubirimi1=@olcubirimi1, olcubirimi2=@olcubirimi2, olcubirimi2oran=@olcubirimi2oran WHERE ID=@ID";
                        using (SQLiteCommand updateCommand = new SQLiteCommand(updateSql, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@ID", selectedStokItem.ID);
                            updateCommand.Parameters.AddWithValue("@stokadi", StokAdTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@kdvsatis", SatisKDVOranTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@kdvalis", AlisKDVOranTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@olcubirimi1", OlcuBirimi1ComboBox.Text);
                            updateCommand.Parameters.AddWithValue("@olcubirimi2", OlcuBirimi2ComboBox.Text);
                            updateCommand.Parameters.AddWithValue("@olcubirimi2oran", olcubirimi2oran);
                            updateCommand.ExecuteNonQuery();

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

                        StokKodTextBox.Clear();
                        StokAdTextBox.Clear();
                        SatisKDVOranTextBox.Clear();
                        AlisKDVOranTextBox.Clear();
                        OlcuBirimi1ComboBox.Text = null;
                        OlcuBirimi2ComboBox.Text = null;
                        OlcuBirimiOran1.Text = "1";
                        OlcuBirimiOran2.Text = "1";
                        olcubirimi2oran = null;
                        dataGrid.ItemsSource = GetStoklar();

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
                LogError(ex);
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
                    StokAdTextBox.Text = selectedStok.StokAdi;
                    SatisKDVOranTextBox.Text = selectedStok.KdvSatis.ToString();
                    AlisKDVOranTextBox.Text = selectedStok.KdvAlis.ToString();
                    OlcuBirimi1ComboBox.Text = selectedStok.OlcuBirimi1;
                    OlcuBirimi2ComboBox.Text = selectedStok.OlcuBirimi2;

                    string? OlcuBirimiOran = selectedStok.OlcuBirimi2Oran;

                    // "/" karakterine göre metni böl
                    string[] parts = OlcuBirimiOran.Split('/');

                    if (parts.Length == 2)
                    {
                        string leftPart = parts[0];
                        string rightPart = parts[1];

                        OlcuBirimiOran1.Text = leftPart;
                        OlcuBirimiOran2.Text = rightPart;
                    }
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
                // Datagrid yüklendiğinde çalışır.
            }
            catch (Exception ex)
            {
                LogError(ex);
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
                            StokAdTextBox.Text = stoklar.StokAdi;
                            SatisKDVOranTextBox.Text = stoklar.KdvSatis.ToString();
                            AlisKDVOranTextBox.Text = stoklar.KdvAlis.ToString();
                            OlcuBirimi1ComboBox.Text = stoklar.OlcuBirimi1;
                            OlcuBirimi2ComboBox.Text = stoklar.OlcuBirimi2;

                            string? OlcuBirimiOran = stoklar.OlcuBirimi2Oran;

                            // "/" karakterine göre metni böl
                            string[] parts = OlcuBirimiOran.Split('/');

                            if (parts.Length == 2)
                            {
                                string leftPart = parts[0];
                                string rightPart = parts[1];

                                OlcuBirimiOran1.Text = leftPart;
                                OlcuBirimiOran2.Text = rightPart;
                            }

                            dataGrid.SelectedItem = stoklar;
                            var = true;
                            break;
                        }
                        else
                        {
                            StokAdTextBox.Clear();
                            SatisKDVOranTextBox.Clear();
                            AlisKDVOranTextBox.Clear();
                            OlcuBirimi1ComboBox.Text = null;
                            OlcuBirimi2ComboBox.Text = null;
                            OlcuBirimiOran1.Text = "1";
                            OlcuBirimiOran2.Text = "1";
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

        public ObservableCollection<Stoklar> GetStoklar()
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                viewModel.Stoklar.Clear();

                using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM Stok";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id = reader.GetInt32(0);
                                string stokkod = reader.GetString(1);
                                string stokAdi = reader.GetString(2);
                                int kdvsatis = reader.GetInt32(3);
                                int kdvalis = reader.GetInt32(4);
                                string olcubirimi1 = reader.GetString(5);
                                string olcubirimi2 = reader.GetString(6);
                                string olcubirimi2oran = reader.GetString(7);

                                Stoklar s = new Stoklar()
                                {
                                    ID = id,
                                    StokKod = stokkod,
                                    StokAdi = stokAdi,
                                    KdvSatis = kdvsatis,
                                    KdvAlis = kdvalis,
                                    OlcuBirimi1 = olcubirimi1,
                                    OlcuBirimi2 = olcubirimi2,
                                    OlcuBirimi2Oran = olcubirimi2oran
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
                LogError(ex);

                return null;
            }
        }

        public class Stoklar
        {
            public int ID { get; set; }
            public string? StokKod { get; set; }
            public string? StokAdi { get; set; }
            public int KdvSatis { get; set; }
            public int KdvAlis { get; set; }
            public string? OlcuBirimi1 { get; set; }
            public string? OlcuBirimi2 { get; set; }
            public string? OlcuBirimi2Oran { get; set; }
            public int StokMiktar { get; set; }
            public int GelenMiktar { get; set; }
            public int GelenTutar { get; set; }
            public int GidenMiktar { get; set; }
            public int GidenTutar { get; set; }
        }
    }
}
