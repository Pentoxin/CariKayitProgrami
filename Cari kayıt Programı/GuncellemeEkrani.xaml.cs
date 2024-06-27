using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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
            var releaseInfo = await Main_TR.Check.GetLatestReleaseInfoAsync();
            TitleLabel.Content = releaseInfo.title;
            ContentTextbox.Text = releaseInfo.notes;
        }

        private static void SetGuncellemeOnay()
        {
            Main_TR.Degiskenler.guncellemeOnay = true;
        }

        private void GuncelleButton_Click(object sender, RoutedEventArgs e)
        {
            SetGuncellemeOnay();

            Window mainWindow = Window.GetWindow(this);

            if (mainWindow != null)
            {
                // Ana pencereyi kapat
                mainWindow.Close();
            }
        }

        private void IptalButton_Click(object sender, RoutedEventArgs e)
        {
            Window mainWindow = Window.GetWindow(this);

            if (mainWindow != null)
            {
                // Ana pencereyi kapat
                mainWindow.Close();
            }
        }
    }
}
