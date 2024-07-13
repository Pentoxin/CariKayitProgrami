using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Cari_kayıt_Programı.Main_TR;

namespace Cari_kayıt_Programı
{
    /// <summary>
    /// CariKart.xaml etkileşim mantığı
    /// </summary>
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
            Main_TR main_TR = new Main_TR();
            dataGrid.ItemsSource = main_TR.GetBusinesses();
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
                Main_TR main_TR = new Main_TR();
                string searchTerm = txtSearch.Text;
                main_TR.Businesses(searchTerm);
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
                    CariAdLabel.Content = selectedBusiness.Isletme_Adi;
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
