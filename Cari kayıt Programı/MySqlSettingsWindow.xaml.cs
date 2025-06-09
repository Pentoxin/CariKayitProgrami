using System;
using System.Windows;
using System.Windows.Controls;

namespace Cari_kayıt_Programı
{
    /// <summary>
    /// MySqlSettingsWindow.xaml etkileşim mantığı
    /// </summary>
    public partial class MySqlSettingsWindow : Window
    {
        public MySqlSettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                var ini = new IniFile();
                ServerBox.Text = ini.Read("Server", "Connection");
                PortBox.Text = ini.Read("Port", "Connection");
                UserBox.Text = ini.Read("User", "Connection");
                PasswordBox.Password = ini.ReadDecrypted("Password", "Connection");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ini = new IniFile();
                ini.Write("Server", ServerBox.Text, "Connection");
                ini.Write("Port", PortBox.Text, "Connection");
                ini.Write("User", UserBox.Text, "Connection");
                ini.WriteEncrypted("Password", PasswordBox.Password, "Connection");

                // Bağlantı testini burada yap
                if (!TestConnection())
                {
                    MessageBox.Show("Bağlantı kurulamadı. Lütfen bilgileri kontrol ediniz.", "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return; // pencere kapanmasın
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }            
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = false;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }            
        }

        private bool TestConnection()
        {
            try
            {
                string connStr = $"server={ServerBox.Text};user={UserBox.Text};password={PasswordBox.Password};port={PortBox.Text};";
                using var conn = new MySql.Data.MySqlClient.MySqlConnection(connStr);
                conn.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
