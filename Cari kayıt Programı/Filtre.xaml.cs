using System;
using System.Windows;

namespace Cari_kayıt_Programı
{
    public partial class Filtre : Window
    {
        public Filtre()
        {
            InitializeComponent();

            FiltreFrame.Source = new Uri("Filtre_TR.xaml", UriKind.RelativeOrAbsolute);
        }
    }
}
