using AutoUpdaterDotNET;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Cari_kayıt_Programı
{
    public partial class Anasayfa : Window
    {
        public MainViewModel ViewModel { get; set; }

        public Anasayfa()
        {
            try
            {
                InitializeComponent();

                var startup = new StartupService();

                ViewModel = new MainViewModel();
                DataContext = ViewModel;

                LogManager.LogInformation(message: "Program Başlatıldı", className: "Program", methodName: "Main");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Program", methodName: "Main", stackTrace: ex.StackTrace);
                MessageBox.Show("Beklenmeyen bir hata oluştu. Lütfen destek ekibiyle iletişime geçin.", "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UygulamayıGuncelle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StartupService.AppUpdateCheck();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Anasayfa", methodName: "UygulamayıGuncelle_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SurumNotlariButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string owner = "Pentoxin";
                string repo = "CariKayitProgrami";
                Assembly? assembly = Assembly.GetExecutingAssembly();
                Version? version = assembly.GetName().Version;
                string? versionString = $"{version.Major}.{version.Minor}.{version.Build}";

                string url = $"https://github.com/{owner}/{repo}/releases/tag/v{versionString}";

                // Örnek changelog sayfası
                string changelogUrl = "https://github.com/Pentoxin/CariKayitProgrami/releases";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Anasayfa", methodName: "SurumNotlariButton_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void VeritabaniAyarlari_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ayarPenceresi = new MySqlSettingsWindow();
                ayarPenceresi.ShowDialog();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Anasayfa", methodName: "VeritabaniAyarlari_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}