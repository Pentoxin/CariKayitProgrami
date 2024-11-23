using System;
using System.Windows;
using static Cari_kayıt_Programı.CariHesapKayitlari;

namespace Cari_kayıt_Programı
{
    public partial class YuklemeEkrani : Window
    {

        public YuklemeEkrani()
        {
            try
            {
                InitializeComponent();

                if (Degiskenler.guncellemeOnay)
                {
                    this.Width = 330;
                    this.Height = 150;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "YuklemeEkrani", methodName: "Main()", stackTrace: ex.StackTrace);
                MessageBox.Show("Beklenmeyen bir hata oluştu. Lütfen destek ekibiyle iletişime geçin.", "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Degiskenler.guncellemeOnay)
                {
                    YuklemeEkraniFrame.Source = new Uri("GuncellemeEkrani.xaml", UriKind.RelativeOrAbsolute);
                }
                else
                {
                    YuklemeEkraniFrame.Source = new Uri("YuklemeEkrani_TR.xaml", UriKind.RelativeOrAbsolute);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "YuklemeEkrani", methodName: "Window_Loaded()", stackTrace: ex.StackTrace);
                throw;
            }
        }
    }
}
