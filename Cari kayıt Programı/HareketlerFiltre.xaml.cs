using System;
using System.Windows;
using static Cari_kayıt_Programı.CariHareketKayitlari;
using static Cari_kayıt_Programı.CariHesapKayitlari;

namespace Cari_kayıt_Programı
{
    public partial class HareketlerFiltre : Window
    {
        public HareketlerFiltre()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "HareketlerFiltre", methodName: "Main()", stackTrace: ex.StackTrace);
                MessageBox.Show("Beklenmeyen bir hata oluştu. Lütfen destek ekibiyle iletişime geçin.", "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Bulunduğumuz yılın ilk ve son gününü hesapla
                DateTime yilBasi = new DateTime(DateTime.Now.Year, 1, 1); // Yılın ilk günü
                DateTime yilSonu = new DateTime(DateTime.Now.Year, 12, 31); // Yılın son günü

                // DatePicker'lara atama yap
                BaslangicTarihDatePicker.SelectedDate = yilBasi;
                BitisTarihDatePicker.SelectedDate = yilSonu;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "HareketlerFiltre", methodName: "Window_Loaded()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void FiltreleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FiltreDegiskenler.baslangicTarihi = BaslangicTarihDatePicker.SelectedDate.HasValue ? BaslangicTarihDatePicker.SelectedDate.Value : DateTime.Now;
                FiltreDegiskenler.bitisTarihi = BitisTarihDatePicker.SelectedDate.HasValue ? BitisTarihDatePicker.SelectedDate.Value : DateTime.Now;

                string tip = "";
                if (TumuRadioButton.IsChecked == true)
                {
                    tip = "T";
                }
                else if (BorcRadioButton.IsChecked == true)
                {
                    tip = "B";
                }
                else if (AlacakRadioButton.IsChecked == true)
                {
                    tip = "A";
                }
                FiltreDegiskenler.tip = tip;

                FiltreDegiskenler.filtrele = true;

                Window mainWindow = Window.GetWindow(this);

                if (mainWindow != null)
                {
                    // Ana pencereyi kapat
                    mainWindow.Close();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "HareketlerFiltre", methodName: "FiltreleButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void VazgecButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window mainWindow = Window.GetWindow(this);

                if (mainWindow != null)
                {
                    // Ana pencereyi kapat
                    mainWindow.Close();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "HareketlerFiltre", methodName: "VazgecButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }
    }
}
