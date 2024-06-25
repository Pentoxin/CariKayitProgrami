using System;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;

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
                    MessageBox.Show("Lütfen işletme adını giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    bool kayit = false;

                    using (SQLiteConnection connection = new SQLiteConnection(Config.ConnectionString))
                    {
                        connection.Open();
                        string selectSql = "SELECT COUNT(*) FROM CariKayit WHERE lower(isletmeadi) = lower(@isletmeadi)";
                        using (SQLiteCommand command = new SQLiteCommand(selectSql, connection))
                        {
                            command.Parameters.AddWithValue("@isletmeadi", isletmeadiTextBox.Text);
                            int count = Convert.ToInt32(command.ExecuteScalar());
                            if (count > 0)
                            {
                                MessageBox.Show("Bu işletme adı daha önce girilmiş.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            else
                            {
                                if (MessageBox.Show("Seçilen veriyi kaydetmek istediğinize emin misiniz?", "Kaydet", MessageBoxButton.YesNo, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
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

                                        // Yeni kaydın ID'sini al
                                        long newId = connection.LastInsertRowId;

                                        // Yeni tabloyu oluştur
                                        string createTableSql = $"CREATE TABLE Cari_{newId} (" +
                                                                "ID INTEGER PRIMARY KEY AUTOINCREMENT, " +
                                                                "tarih TEXT, " +
                                                                "tip TEXT, " +
                                                                "evrakno TEXT, " +
                                                                "aciklama TEXT, " +
                                                                "vadetarihi TEXT, " +
                                                                "borc INTEGER, " +
                                                                "alacak INTEGER)";
                                        using (SQLiteCommand createTableCommand = new SQLiteCommand(createTableSql, connection))
                                        {
                                            createTableCommand.ExecuteNonQuery();
                                        }

                                        MessageBox.Show("İşletme bilgileri veritabanına kaydedildi.", "Kaydedildi", MessageBoxButton.OK, MessageBoxImage.Information);
                                    }
                                }
                            }
                        }
                    }
                    if (kayit)
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
