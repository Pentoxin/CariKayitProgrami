using System;
using System.Windows;
using static Cari_kayıt_Programı.Hareketler_TR;
using static Cari_kayıt_Programı.Main_TR;

namespace Cari_kayıt_Programı
{
    /// <summary>
    /// HareketlerFiltre.xaml etkileşim mantığı
    /// </summary>
    public partial class HareketlerFiltre : Window
    {
        public HareketlerFiltre()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BaslangicTarihDatePicker.SelectedDate = FiltreDegiskenler.enKucukTarih;
            BitisTarihDatePicker.SelectedDate = FiltreDegiskenler.enBuyukTarih;
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
                LogError(ex);
            }
        }

        private void VazgecButton_Click(object sender, RoutedEventArgs e)
        {
            Window mainWindow = Window.GetWindow(this);

            if (mainWindow != null)
            {
                // Ana pencereyi kapat
                mainWindow.Close();
            }
        }
    }
}
