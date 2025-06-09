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
                if (!startup.InitializeApplication())
                {
                    MessageBox.Show("Program başlatılamadı. Bağlantı yapılamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(1);
                    return;
                }

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckAtStartup();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Anasayfa", methodName: "Window_Loaded()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        public static void CheckAtStartup()
        {
            try
            {
                CheckAndCreateAppDataFolders();

                LogManager.LogInformation(message: "Tüm başlangıç kontrolleri tamamlandı.", className: "Anasayfa", methodName: "CheckAtStartup()");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Anasayfa", methodName: "CheckAtStartup()", stackTrace: ex.StackTrace);
                throw;
            }
        }

        public static void CheckAndCreateAppDataFolders()
        {
            try
            {
                // Önce AppData klasörü varsa kontrol et yoksa oluştur
                if (!Directory.Exists(ConfigManager.AppDataPath))
                {
                    Directory.CreateDirectory(ConfigManager.AppDataPath);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Anasayfa / Check", methodName: "CheckAndCreateAppDataAndIsletmeFolders()", stackTrace: ex.StackTrace);
                throw;
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