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
