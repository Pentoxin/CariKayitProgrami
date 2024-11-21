using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Cari_kayıt_Programı.CariHesapKayitlari;

namespace Cari_kayıt_Programı
{
    public partial class CariKart : Window
    {
        public MainViewModel ViewModel { get; set; }

        public CariKart()
        {
            InitializeComponent();

            ViewModel = new MainViewModel();
            DataContext = ViewModel;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CariHesapKayitlari cariHesapKayitlari = new CariHesapKayitlari();
            dataGrid.ItemsSource = cariHesapKayitlari.GetBusinesses();
        }

        private void SecButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Degiskenler.selectedBusiness = (Business)dataGrid.SelectedItem;

                Window mainWindow = Window.GetWindow(this);

                if (mainWindow != null)
                {
                    // Ana pencereyi kapat
                    mainWindow.Close();
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                CariHesapKayitlari cariHesapKayitlari = new CariHesapKayitlari();
                string searchTerm = txtSearch.Text;
                dataGrid.ItemsSource = cariHesapKayitlari.Businesses(searchTerm);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void dataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is DataGrid grid)
                {
                    if (grid.SelectedItem != null)
                    {
                        Degiskenler.selectedBusiness = (Business)dataGrid.SelectedItem;

                        Window mainWindow = Window.GetWindow(this);

                        if (mainWindow != null)
                        {
                            // Ana pencereyi kapat
                            mainWindow.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void VazgecButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Degiskenler.selectedBusiness = null;

                Window mainWindow = Window.GetWindow(this);

                if (mainWindow != null)
                {
                    // Ana pencereyi kapat
                    mainWindow.Close();
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Business? selectedBusiness = (Business)dataGrid.SelectedItem;
                if (selectedBusiness != null)
                {
                    CariAdLabel.Content = selectedBusiness.CariIsim;
                }
                else
                {
                    CariAdLabel.Content = "-";
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }
    }
}
