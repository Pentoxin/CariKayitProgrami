using System;
using System.Windows;
using static Cari_kayıt_Programı.CariHesapKayitlari;

namespace Cari_kayıt_Programı
{
    public partial class YuklemeEkrani : Window
    {

        public YuklemeEkrani()
        {
            InitializeComponent();

            if (Degiskenler.guncellemeOnay)
            {
                this.Width = 330;
                this.Height = 150;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
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
    }
}
