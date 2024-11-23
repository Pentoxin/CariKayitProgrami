using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Cari_kayıt_Programı.CariHesapKayitlari;

namespace Cari_kayıt_Programı
{
    public partial class GuncellemeEkrani : Page
    {
        public GuncellemeEkrani()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "GuncellemeEkrani", methodName: "Main()", stackTrace: ex.StackTrace);
                MessageBox.Show("Beklenmeyen bir hata oluştu. Lütfen destek ekibiyle iletişime geçin.", "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _ = Veriler();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "GuncellemeEkrani", methodName: "Page_Loaded()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private async Task Veriler()
        {
            try
            {
                var releaseInfo = await Anasayfa.Check.GetLatestReleaseInfoAsync();
                TitleLabel.Content = releaseInfo.title;
                ContentTextbox.Text = releaseInfo.notes;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "GuncellemeEkrani", methodName: "Veriler()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private static void SetGuncellemeOnay()
        {
            try
            {
                Degiskenler.guncellemeOnay = true;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "GuncellemeEkrani", methodName: "SetGuncellemeOnay()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void GuncelleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetGuncellemeOnay();

                Window mainWindow = Window.GetWindow(this);

                if (mainWindow != null)
                {
                    // Ana pencereyi kapat
                    mainWindow.Close();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "GuncellemeEkrani", methodName: "GuncelleButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void IptalButton_Click(object sender, RoutedEventArgs e)
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
                LogManager.LogError(ex, className: "GuncellemeEkrani", methodName: "IptalButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
