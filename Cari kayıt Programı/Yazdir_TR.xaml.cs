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
using static Cari_kayıt_Programı.Yazdir;

namespace Cari_kayıt_Programı
{
    /// <summary>
    /// Yazdir_TR.xaml etkileşim mantığı
    /// </summary>
    public partial class Yazdir_TR : Page
    {
        public Yazdir_TR()
        {
            InitializeComponent();

            degiskenler.olustur = false;

            degiskenler.secilenOzellikler.Clear();
        }

        public class degiskenler
        {
            public static List<string> secilenOzellikler = new List<string>();
            public static bool olustur = false;

        }

        private void HepsiniSecButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (CheckBox checkBox in YazdirListbox.Items)
                {
                    checkBox.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                Main_TR.LogError(ex);
            }
        }

        private void HicbiriniSecmeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                for (int i = 2; i < YazdirListbox.Items.Count; i++)
                {
                    if (YazdirListbox.Items[i] is CheckBox checkBox)
                    {
                        checkBox.IsChecked = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Main_TR.LogError(ex);
            }
        }

        private void OlusturButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (CheckBox checkBox in YazdirListbox.Items)
                {
                    if (checkBox.IsChecked == true)
                    {
                        degiskenler.secilenOzellikler.Add(checkBox.Content.ToString());
                    }
                }
                degiskenler.olustur = true;

                Window mainWindow = Window.GetWindow(this);

                if (mainWindow != null)
                {
                    // Ana pencereyi kapat
                    mainWindow.Close();
                }
            }
            catch (Exception ex)
            {
                Main_TR.LogError(ex);
            }
        }

        private void VazgecButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window mainWindow = Window.GetWindow(this);

                if (mainWindow != null)
                {
                    // Ana pencereyi kapat
                    mainWindow.Close();
                }
            }
            catch (Exception ex)
            {
                Main_TR.LogError(ex);
            }
        }
    }
}
