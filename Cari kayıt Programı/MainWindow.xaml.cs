using Cari_kayıt_Programı;
using System;
using System.Windows;

namespace CariKayitProgrami
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Yazdir_TR.YazdirDegiskenler.ClearSecilenOzellikler();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Main.Source = new Uri("Main_TR.xaml", UriKind.RelativeOrAbsolute);
        }
    }
}