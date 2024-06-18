using System;
using System.Windows;

namespace Cari_kayıt_Programı
{
    public partial class YuklemeEkrani : Window
    {

        public YuklemeEkrani()
        {
            InitializeComponent();

            if (Main_TR.Degiskenler.guncellemeOnay)
            {
                this.Width = 330;
                this.Height = 150;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Main_TR.Degiskenler.guncellemeOnay)
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
