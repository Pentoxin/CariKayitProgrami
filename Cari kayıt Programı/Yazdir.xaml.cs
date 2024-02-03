using CariKayitProgrami;
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
using Cari_kayıt_Programı;

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
