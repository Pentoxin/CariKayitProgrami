using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SQLite;

namespace Cari_kayıt_Programı
{
    /// <summary>
    /// YeniKayit_TR.xaml etkileşim mantığı
    /// </summary>
    public partial class YeniKayit_TR : Page
    {
        public YeniKayit_TR()
        {
            InitializeComponent();
        }
        private void KaydetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isletmeadiTextBox.Text == "")
                {
                    MessageBox.Show("Lütfen İşletme Adını Giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    bool kayit = false;

                    using (SQLiteConnection connection = new SQLiteConnection(Main_TR.ConnectionString))
                    {
                        connection.Open();
                        string selectSql = "SELECT COUNT(*) FROM CariKayit WHERE lower(isletmeadi) = lower(@isletmeadi)";
                        using (SQLiteCommand command = new SQLiteCommand(selectSql, connection))
                        {
                            command.Parameters.AddWithValue("@isletmeadi", isletmeadiTextBox.Text);
                            int count = Convert.ToInt32(command.ExecuteScalar());
                            if (count > 0)
                            {
                                MessageBox.Show("Bu İşletme Adı Daha Önce Girilmiş.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            else
                            {
                                if (MessageBox.Show("Seçilen Veriyi Kaydetmek İstediğinize Emin Misiniz?", "Kaydet", MessageBoxButton.YesNo, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
                                {
                                    kayit = true;

                                    string insertSql = "INSERT INTO CariKayit (isletmeadi, vergidairesi, vergino, banka, hesapno, adres, mail1, mail2, telefon1, telefon2) VALUES (@isletmeadi, @vergidairesi, @vergino, @banka, @hesapno, @adres, @mail1, @mail2, @telefon1, @telefon2)";
                                    using (SQLiteCommand insertCommand = new SQLiteCommand(insertSql, connection))
                                    {
                                        insertCommand.Parameters.AddWithValue("@isletmeadi", isletmeadiTextBox.Text);
                                        insertCommand.Parameters.AddWithValue("@vergidairesi", vergidairesiTextBox.Text);
                                        insertCommand.Parameters.AddWithValue("@vergino", verginoTextBox.Text);
                                        insertCommand.Parameters.AddWithValue("@banka", bankaTextBox.Text);
                                        insertCommand.Parameters.AddWithValue("@hesapno", hesapnoTextBox.Text);
                                        insertCommand.Parameters.AddWithValue("@adres", adresTextBox.Text);
                                        insertCommand.Parameters.AddWithValue("@mail1", mail1TextBox.Text);
                                        insertCommand.Parameters.AddWithValue("@mail2", mail2TextBox.Text);
                                        insertCommand.Parameters.AddWithValue("@telefon1", telefon1TextBox.Text);
                                        insertCommand.Parameters.AddWithValue("@telefon2", telefon2TextBox.Text);
                                        insertCommand.ExecuteNonQuery();
                                        MessageBox.Show("İşletme Bilgileri Veritabanina Kaydedildi.", "Kaydedildi", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                                    }
                                }
                            }
                        }
                    }
                    if (kayit == true)
                    {
                        isletmeadiTextBox.Clear();
                        vergidairesiTextBox.Clear();
                        verginoTextBox.Clear();
                        bankaTextBox.Clear();
                        hesapnoTextBox.Clear();
                        adresTextBox.Clear();
                        mail1TextBox.Clear();
                        mail2TextBox.Clear();
                        telefon1TextBox.Clear();
                        telefon2TextBox.Clear();

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
                Main_TR.LogError(ex);
            }
        }

        private void VazgecButton_Click(object sender, RoutedEventArgs e)
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
                Main_TR.LogError(ex);
            }
        }
    }
}
