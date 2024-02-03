using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
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
