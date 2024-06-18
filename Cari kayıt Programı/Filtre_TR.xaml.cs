using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Cari_kayıt_Programı
{
    /// <summary>
    /// Filtre_TR.xaml etkileşim mantığı
    /// </summary>
    public partial class Filtre_TR : Page
    {
        public Filtre_TR()
        {
            InitializeComponent();
        }

        private static readonly List<string> SecilenSutunlar = new List<string>();
        private static readonly List<string> SecilmeyenSutunlar = new List<string>();

        // Listedeki elemanları okumak için bir metot
        public static IReadOnlyList<string> GetSecilenSutunlar()
        {
            return SecilenSutunlar.AsReadOnly();
        }

        // Listedeki elemanları okumak için bir metot
        public static IReadOnlyList<string> GetSecilmeyenSutunlar()
        {
            return SecilmeyenSutunlar.AsReadOnly();
        }

        private void HepsiniSecButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (CheckBox checkBox in FiltreListbox.Items)
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
                for (int i = 2; i < FiltreListbox.Items.Count; i++)
                {
                    if (FiltreListbox.Items[i] is CheckBox checkBox)
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

        private void FiltreleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SecilenSutunlar.Clear();
                SecilmeyenSutunlar.Clear();
                foreach (CheckBox checkBox in FiltreListbox.Items)
                {
                    if (checkBox.Content.ToString() == "Vergi Dairesi")
                    {
                        if (checkBox.IsChecked == true)
                        {
                            SecilenSutunlar.Add("VergiDairesi");
                        }
                        if (checkBox.IsChecked == false)
                        {
                            SecilmeyenSutunlar.Add("VergiDairesi");
                        }
                    }
                    else if (checkBox.Content.ToString() == "Vergi No")
                    {
                        if (checkBox.IsChecked == true)
                        {
                            SecilenSutunlar.Add("VergiNo");
                        }
                        if (checkBox.IsChecked == false)
                        {
                            SecilmeyenSutunlar.Add("VergiNo");
                        }
                    }
                    else if (checkBox.Content.ToString() == "Banka")
                    {
                        if (checkBox.IsChecked == true)
                        {
                            SecilenSutunlar.Add("Banka");
                        }
                        if (checkBox.IsChecked == false)
                        {
                            SecilmeyenSutunlar.Add("Banka");
                        }
                    }
                    else if (checkBox.Content.ToString() == "Hesap No / IBAN")
                    {
                        if (checkBox.IsChecked == true)
                        {
                            SecilenSutunlar.Add("HesapNo");
                        }
                        if (checkBox.IsChecked == false)
                        {
                            SecilmeyenSutunlar.Add("HesapNo");
                        }
                    }
                    else if (checkBox.Content.ToString() == "Adres")
                    {
                        if (checkBox.IsChecked == true)
                        {
                            SecilenSutunlar.Add("Adres");
                        }
                        if (checkBox.IsChecked == false)
                        {
                            SecilmeyenSutunlar.Add("Adres");
                        }
                    }
                    else if (checkBox.Content.ToString() == "E-Posta 1")
                    {
                        if (checkBox.IsChecked == true)
                        {
                            SecilenSutunlar.Add("EPosta1");
                        }
                        if (checkBox.IsChecked == false)
                        {
                            SecilmeyenSutunlar.Add("EPosta1");
                        }
                    }
                    else if (checkBox.Content.ToString() == "E-Posta 2")
                    {
                        if (checkBox.IsChecked == true)
                        {
                            SecilenSutunlar.Add("EPosta2");
                        }
                        if (checkBox.IsChecked == false)
                        {
                            SecilmeyenSutunlar.Add("EPosta2");
                        }
                    }
                    else if (checkBox.Content.ToString() == "Telefon 1")
                    {
                        if (checkBox.IsChecked == true)
                        {
                            SecilenSutunlar.Add("Telefon1");
                        }
                        if (checkBox.IsChecked == false)
                        {
                            SecilmeyenSutunlar.Add("Telefon1");
                        }
                    }
                    else if (checkBox.Content.ToString() == "Telefon 2")
                    {
                        if (checkBox.IsChecked == true)
                        {
                            SecilenSutunlar.Add("Telefon2");
                        }
                        if (checkBox.IsChecked == false)
                        {
                            SecilmeyenSutunlar.Add("Telefon2");
                        }
                    }
                }

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
