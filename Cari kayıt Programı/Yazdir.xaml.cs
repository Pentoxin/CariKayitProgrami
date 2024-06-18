using System;
using System.Windows;

namespace Cari_kayıt_Programı
{
    public partial class Yazdir : Window
    {
        public Yazdir()
        {
            InitializeComponent();

            YazdirFrame.Source = new Uri("Yazdir_TR.xaml", UriKind.RelativeOrAbsolute);
        }
    }
}
