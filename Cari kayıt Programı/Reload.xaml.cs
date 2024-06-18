using System.Windows;

namespace Cari_kayıt_Programı
{
    /// <summary>
    /// Reload.xaml etkileşim mantığı
    /// </summary>
    public partial class Reload : Window
    {
        public Reload()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Method intentionally left empty.
        }

        public void WindowReloadSD(Window window)
        {
            window.ShowDialog();
            this.Close();
        }
        public void WindowReload(Window window)
        {
            window.Show();
            this.Close();
        }
    }
}
