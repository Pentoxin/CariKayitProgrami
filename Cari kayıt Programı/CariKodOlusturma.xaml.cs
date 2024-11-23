using System;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using static Cari_kayıt_Programı.Anasayfa;
using static Cari_kayıt_Programı.CariHesapKayitlari;

namespace Cari_kayıt_Programı
{
    public partial class CariKodOlusturma : Window
    {
        public MainViewModel ViewModel { get; set; }
        public Business? Business { get; set; }
        public string? NewCariKod { get; private set; }
        public bool OtomatikGir { get; private set; }

        public CariKodOlusturma(Anasayfa anasayfa)
        {
            try
            {
                InitializeComponent();
                ViewModel = new MainViewModel();
                DataContext = ViewModel;
                _anasayfa = anasayfa;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariKodOlusturma", methodName: "Main()", stackTrace: ex.StackTrace);
                MessageBox.Show("Beklenmeyen bir hata oluştu. Lütfen destek ekibiyle iletişime geçin.", "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void KaydetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NewCariKod = CariKodTextbox.Text;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariKodOlusturma", methodName: "KaydetButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                CariIDLabel.Content = Business.ID;
                CariIsimLabel.Content = Business.CariIsim;

                CariKodTextbox.Focus();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariKodOlusturma", methodName: "Window_Loaded()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (DialogResult != true)
                {
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariKodOlusturma", methodName: "Window_Closing()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void TumunuGirButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GenerateAndAssignCariKodForAll();
                OtomatikGir = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariKodOlusturma", methodName: "TumunuGirButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private readonly Anasayfa _anasayfa;

        private void GenerateAndAssignCariKodForAll()
        {
            try
            {
                Anasayfa anasayfa = new Anasayfa();
                Check check = new Check(anasayfa);
                MainViewModel viewModel = (MainViewModel)_anasayfa.DataContext;
                var missingCariKodBusinesses = viewModel.Businesses.Where(b => string.IsNullOrEmpty(b.CariKod)).ToList();

                if (missingCariKodBusinesses.Count > 0)
                {
                    using (SQLiteConnection connection = new SQLiteConnection(ConfigManager.ConnectionString))
                    {
                        connection.Open();

                        string query = "SELECT CariKod FROM CariKayit WHERE CariKod LIKE 'D%'";
                        using (SQLiteCommand command = new SQLiteCommand(query, connection))
                        {
                            using (SQLiteDataReader reader = command.ExecuteReader())
                            {
                                int maxNumber = 0;
                                while (reader.Read())
                                {
                                    string cariKod = reader.GetString(0);
                                    if (cariKod.StartsWith("D") && int.TryParse(cariKod.Substring(1), out int number) && number > maxNumber)
                                    {
                                        maxNumber = number;
                                    }
                                }

                                foreach (var business in missingCariKodBusinesses)
                                {
                                    maxNumber++;
                                    string newCariKod = $"D{maxNumber.ToString("D3")}";
                                    Check.UpdateCariKodInDatabase(business.ID, newCariKod);
                                    business.CariKod = newCariKod;  // Güncellenmiş veriyi ViewModel'de de güncelle
                                    business.Tip = "D";
                                }
                            }
                        }
                    }

                    MessageBox.Show("Eksik CariKod değerleri otomatik olarak atandı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariKodOlusturma", methodName: "GenerateAndAssignCariKodForAll()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CariKodTextbox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(CariKodTextbox.Text))
                {
                    KaydetButton.IsEnabled = false;
                }
                else
                {
                    KaydetButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariKodOlusturma", methodName: "CariKodTextbox_TextChanged()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CıkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MessageBox.Show("Cari kodlar girilmeden devam edilemez. Programdan çıkmak istiyor musunuz?", "Uyarı", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes && DialogResult != true)
                {
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "CariKodOlusturma", methodName: "CıkButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
