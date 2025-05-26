using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using static Cari_kayıt_Programı.CariHesapKayitlari;

namespace Cari_kayıt_Programı.UserControls
{
    public partial class Modules : UserControl
    {
        public Modules()
        {
            try
            {
                InitializeComponent();

                this.DataContext = this;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "UserControls.Modules", methodName: "Main", stackTrace: ex.StackTrace);
                MessageBox.Show("Beklenmeyen bir hata oluştu. Lütfen destek ekibiyle iletişime geçin.", "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string Title { get; set; }
        public string Icon { get; set; }
        public string WindowName { get; set; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenWindow(WindowName);
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "UserControls.Modules", methodName: "Button_Click()", stackTrace: ex.StackTrace);
                MessageBox.Show($"Hata Oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Tüm pencereleri tanımlayın
        private readonly Dictionary<string, Func<Window>> _windowFactories = new Dictionary<string, Func<Window>>
        {
            { "AlisFaturasi", () => new Fatura("Alış") },
            { "SatisFaturasi", () => new Fatura("Satış") },
            { "CariHesapKayitlari", () => new CariHesapKayitlari() },
            { "CariHareketKayitlari", () => new CariHareketKayitlari() },
            { "Stok", () => new Stok() }
        };

        private void OpenWindow(string windowName)
        {
            try
            {
                if (_windowFactories.TryGetValue(windowName, out Func<Window> windowFactory))
                {
                    Window window = windowFactory(); // Her seferinde yeni bir pencere oluştur

                    window.Owner = App.Current.MainWindow;
                    window.Show();
                }
                else
                {
                    MessageBox.Show($"Belirtilen pencere bulunamadı: {windowName}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "UserControls.Modules", methodName: "OpenWindow()", stackTrace: ex.StackTrace);
                throw;
            }
        }
    }
}
