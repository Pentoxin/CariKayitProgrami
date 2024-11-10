using System;
using System.Windows;

namespace CariKayitProgrami
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Main.Source = new Uri("Main_TR.xaml", UriKind.RelativeOrAbsolute);
        }
    }
}