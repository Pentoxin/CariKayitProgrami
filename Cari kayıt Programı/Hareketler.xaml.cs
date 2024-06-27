using System;
using System.Windows;

namespace Cari_kayıt_Programı
{
    /// <summary>
    /// Hareketler.xaml etkileşim mantığı
    /// </summary>
    public partial class Hareketler : Window
    {
        public Hareketler()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HareketlerFrame.Source = new Uri("Hareketler_TR.xaml", UriKind.RelativeOrAbsolute);
        }
    }
}
