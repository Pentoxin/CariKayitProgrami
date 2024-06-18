using System;
using System.Windows;

namespace Cari_kayıt_Programı
{
    /// <summary>
    /// YeniKayit.xaml etkileşim mantığı
    /// </summary>
    public partial class YeniKayit : Window
    {
        public YeniKayit()
        {
            InitializeComponent();

            YeniKayitFrame.Source = new Uri("YeniKayit_TR.xaml", UriKind.RelativeOrAbsolute);
        }
    }
}
