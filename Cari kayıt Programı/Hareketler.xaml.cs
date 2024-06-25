using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
