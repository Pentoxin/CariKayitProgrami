﻿using System;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using static Cari_kayıt_Programı.Main_TR;


namespace Cari_kayıt_Programı
{
    public partial class Degistir_TR : Page
    {
        public Degistir_TR()
        {
            InitializeComponent();


            // TextBox'leri dizi içinde tutuyoruz
            textBoxes = new TextBox[]
            {
            isletmeadiTextBox, vergidairesiTextBox, verginoTextBox, bankaTextBox,
            hesapnoTextBox, adresTextBox, mail1TextBox, mail2TextBox, telefon1TextBox,
            telefon2TextBox};

            // Tüm TextBox'lerin GotFocus olayına aynı olay işleyicisini bağlıyoruz
            foreach (TextBox textBox in textBoxes)
            {
                textBox.GotFocus += TextBox_GotFocus;
            }

            Business? selectedBusiness = Degiskenler.selectedBusiness;

            if (selectedBusiness != null)
            {
                isletmeadiTextBox.Text = selectedBusiness.Isletme_Adi;
                vergidairesiTextBox.Text = selectedBusiness.VergiDairesi;
                verginoTextBox.Text = selectedBusiness.VergiNo;
                bankaTextBox.Text = selectedBusiness.Banka;
                hesapnoTextBox.Text = selectedBusiness.HesapNo;
                adresTextBox.Text = selectedBusiness.Adres;
                mail1TextBox.Text = selectedBusiness.EPosta1;
                mail2TextBox.Text = selectedBusiness.EPosta2;
                telefon1TextBox.Text = selectedBusiness.Telefon1;
                telefon2TextBox.Text = selectedBusiness.Telefon2;
            }
        }

        private TextBox[] textBoxes;
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            textBox.SelectAll();
        }

        private void DegistirButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Business? selectedBusiness = Degiskenler.selectedBusiness;

                if (selectedBusiness == null)
                    return;

                string firstBusiness = selectedBusiness.Isletme_Adi;


                using (SQLiteConnection connection = new SQLiteConnection(Config.ConnectionString))
                {
                    connection.Open();

                    string checkSql = "SELECT COUNT(*) FROM CariKayit WHERE lower(isletmeadi) = lower(@isletmeadi) AND ID != @ID";

                    using (SQLiteCommand checkCommand = new SQLiteCommand(checkSql, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@isletmeadi", isletmeadiTextBox.Text);
                        checkCommand.Parameters.AddWithValue("@ID", selectedBusiness.ID);

                        int result = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (result > 0)
                        {
                            MessageBox.Show("Bu işletme adı daha önce girilmiş!", "Veri Kaydetme Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    if (MessageBox.Show("Seçili veriyi değiştirmek istediğinize emin misiniz?", "Veri Güncelleme", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        string updateSql = "UPDATE CariKayit SET isletmeadi=@isletmeadi, vergidairesi=@vergidairesi, vergino=@vergino, banka=@banka, hesapno=@hesapno, adres=@adres, mail1=@mail1, mail2=@mail2, telefon1=@telefon1, telefon2=@telefon2 WHERE ID=@ID";

                        using (SQLiteCommand updateCommand = new SQLiteCommand(updateSql, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@isletmeadi", isletmeadiTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@vergidairesi", vergidairesiTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@vergino", verginoTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@banka", bankaTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@hesapno", hesapnoTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@adres", adresTextBox.Text);
                            updateCommand.Parameters.AddWithValue("@mail1", mail1TextBox.Text);
                            updateCommand.Parameters.AddWithValue("@mail2", mail2TextBox.Text);
                            updateCommand.Parameters.AddWithValue("@telefon1", telefon1TextBox.Text);
                            updateCommand.Parameters.AddWithValue("@telefon2", telefon2TextBox.Text);
                            updateCommand.Parameters.AddWithValue("@ID", selectedBusiness.ID);

                            int rowsAffected = updateCommand.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Veri güncellendi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBox.Show("Veri güncelleme hatası!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        if (!Directory.Exists(Path.Combine(Config.IsletmePath, isletmeadiTextBox.Text)))
                        {
                            Directory.Move(Path.Combine(Config.IsletmePath, firstBusiness), Path.Combine(Config.IsletmePath, isletmeadiTextBox.Text));
                        }
                    }
                }

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
                LogError(ex);
            }
        }
    }
}
