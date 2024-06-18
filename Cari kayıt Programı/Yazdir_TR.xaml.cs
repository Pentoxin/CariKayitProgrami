using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

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

            YazdirDegiskenler.olustur = false;

            YazdirDegiskenler.ClearSecilenOzellikler();
        }

        public static class YazdirDegiskenler
        {
            private static readonly List<string> SecilenOzellikler = new List<string>();
            public static bool olustur { get; set; } = false;

            public static IReadOnlyList<string> GetSecilenOzellikler()
            {
                return YazdirDegiskenler.SecilenOzellikler.AsReadOnly();
            }

            public static void AddSecilenOzellik(string ozellik)
            {
                // Null kontrolü
                if (ozellik == null)
                {
                    throw new ArgumentNullException(nameof(ozellik), "Ozellik parametresi null olamaz.");
                }
                else
                {
                    // Daha önce eklenmemişse ekle
                    if (!SecilenOzellikler.Contains(ozellik))
                    {
                        SecilenOzellikler.Add(ozellik);
                    }
                }
            }

            public static void ClearSecilenOzellikler()
            {
                SecilenOzellikler.Clear();
            }
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
                        YazdirDegiskenler.AddSecilenOzellik(checkBox.Content.ToString());
                    }
                }
                YazdirDegiskenler.olustur = true;

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
