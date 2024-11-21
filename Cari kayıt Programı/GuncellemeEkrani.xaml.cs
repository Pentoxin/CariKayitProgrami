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
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _ = Veriler();
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
                LogError(ex);
            }
        }

        private static void SetGuncellemeOnay()
        {
            Degiskenler.guncellemeOnay = true;
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
                LogError(ex);
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
                LogError(ex);
            }
        }
    }
}
