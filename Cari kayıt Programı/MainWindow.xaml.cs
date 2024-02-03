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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SqlClient;
using System.Xml;
using System.Data;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using Cari_kayıt_Programı;

namespace CariKayitProgrami
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Yazdir_TR yazdir_tr = new Yazdir_TR();

            Yazdir_TR.degiskenler.secilenOzellikler.Clear();

            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Main_TR.selectedValue == 0)
            {
                Main.Source = new Uri("Main_TR.xaml", UriKind.RelativeOrAbsolute);
            }
            else if (Main_TR.selectedValue == 1)
            {
                Main.Source = new Uri("Main_EN.xaml", UriKind.RelativeOrAbsolute);
            }
        }
    }
}