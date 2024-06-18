using System;
using System.Windows;

namespace Cari_kayıt_Programı
{
    /// <summary>
    /// Degistir.xaml etkileşim mantığı
    /// </summary>
    public partial class Degistir : Window
    {
        public Degistir()
        {
            InitializeComponent();

            DegistirFrame.Source = new Uri("Degistir_TR.xaml", UriKind.RelativeOrAbsolute);
        }
    }
}
