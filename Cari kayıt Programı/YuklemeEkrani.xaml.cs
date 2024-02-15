using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;

namespace Cari_kayıt_Programı
{
    /// <summary>
    /// YuklemeEkrani.xaml etkileşim mantığı
    /// </summary>
    public partial class YuklemeEkrani : Window
    {
        public YuklemeEkrani()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StartDownload();
        }

        private async void StartDownload()
        {
            try
            {
                string urlP = "https://github.com/Pentoxin/CariKayitProgrami/releases/download/latest/cari_kayit_programi_setup.exe";

                Main_TR main_TR = new Main_TR();
                if (main_TR.guncel == false)
                {
                    using (var client = new WebClient())
                    {
                        client.DownloadProgressChanged += (sender, e) =>
                        {
                            progressBar.Value = e.ProgressPercentage;
                            statusLabel.Content = $"İndiriliyor: {e.ProgressPercentage}%";
                        };

                        client.DownloadFileCompleted += async (sender, e) =>
                        {
                            if (!e.Cancelled && e.Error == null)
                            {
                                statusLabel.Content = "İndirme tamamlandı, dosya açılıyor...";
                                await Task.Delay(1000); // İşlemi tamamlamadan önce bekleme
                                Process.Start(Main_TR.dosyaAdi);
                            }
                            else
                            {
                                statusLabel.Content = "İndirme iptal edildi veya hata oluştu.";
                            }
                        };
                        await client.DownloadFileTaskAsync(new Uri(urlP), Main_TR.dosyaAdi);
                    }
                }
                this.Close();
            }
            catch (Exception ex)
            {
                Main_TR.LogError(ex);
            }
        }
    }
}
