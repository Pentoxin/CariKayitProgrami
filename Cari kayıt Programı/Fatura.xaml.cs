using Cari_kayıt_Programı.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cari_kayıt_Programı
{
    public partial class Fatura : Window
    {
        private MainViewModel ViewModel { get; set; }
        private static Stoklar? SelectedStoklar { get; set; }
        private static Cariler? SelectedCariler { get; set; }

        public Fatura(string _islem)
        {
            try
            {
                InitializeComponent();

                ViewModel = new MainViewModel();
                DataContext = ViewModel;

                Islem = _islem;

                if (Islem == "Alış")
                {
                    this.Title = "Alış Faturası";
                }
                else if (Islem == "Satış")
                {
                    this.Title = "Satış Faturası";
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Faturalar", methodName: "Faturalar()", stackTrace: ex.StackTrace);
                MessageBox.Show("Beklenmeyen bir hata oluştu. Lütfen destek ekibiyle iletişime geçin.", "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public string Islem { get; set; }

        private bool arayuzHazir = false;

        private string lastChanged = string.Empty;

        private bool isUpdatingIskonto = false;

        private async Task DatabaseLoad()
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                await viewModel.LoadFaturalarAsync();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Faturalar", methodName: "DatabaseLoad", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            arayuzHazir = true;

            _ = DatabaseLoad();

            GirdiKontrolu(); // Arayüz tamamen hazır, şimdi güvenle kontrol edebilirsin
        }

        private void CariKartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selector = new EntitySelectorView(typeof(Cariler));

                if (selector.ShowDialog() == true)
                {
                    var selectedBusiness = selector.SelectedItem as Cariler;
                    CariKodTextBox.Text = selectedBusiness.CariKod;
                    UnvanTextBox.Text = selectedBusiness.Unvan;
                    AdresTextBox.Text = selectedBusiness.Adres;
                    IlTextBox.Text = selectedBusiness.Il;
                    IlceTextBox.Text = selectedBusiness.Ilce;
                    VergiDairesiTextBox.Text = selectedBusiness.VergiDairesi;
                    VergiNoTextBox.Text = selectedBusiness.VergiNo;
                    TcKimlikNoTextBox.Text = selectedBusiness.TcNo;
                }
                else
                {
                    CariKodTextBox.Text = string.Empty;
                    UnvanTextBox.Text = string.Empty;
                    AdresTextBox.Text = string.Empty;
                    IlTextBox.Text = string.Empty;
                    IlceTextBox.Text = string.Empty;
                    VergiDairesiTextBox.Text = string.Empty;
                    VergiNoTextBox.Text = string.Empty;
                    TcKimlikNoTextBox.Text = string.Empty;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Faturalar", methodName: "CariKartButton_Click", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void CariKodTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                CariKodTextBox.TextChanged += Ortak_TextChanged;

                MainViewModel viewModel = (MainViewModel)this.DataContext;

                CariHesapKayitlari cariHesapKayitlari = new();
                var businesses = cariHesapKayitlari.GetBusinesses();

                foreach (var item in businesses)
                {
                    if (item is Cariler business)
                    {
                        string? carikod = business.CariKod;

                        if (CariKodTextBox.Text == carikod)
                        {
                            SelectedCariler = business;
                            UnvanTextBox.Text = business.Unvan;
                            AdresTextBox.Text = business.Adres;
                            IlTextBox.Text = business.Il;
                            IlceTextBox.Text = business.Ilce;
                            VergiDairesiTextBox.Text = business.VergiDairesi;
                            VergiNoTextBox.Text = business.VergiNo;
                            TcKimlikNoTextBox.Text = business.TcNo;
                            break;
                        }
                        else
                        {
                            UnvanTextBox.Text = string.Empty;
                            AdresTextBox.Text = string.Empty;
                            IlTextBox.Text = string.Empty;
                            IlceTextBox.Text = string.Empty;
                            VergiDairesiTextBox.Text = string.Empty;
                            VergiNoTextBox.Text = string.Empty;
                            TcKimlikNoTextBox.Text = string.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Faturalar", methodName: "CariKodTextBox_TextChanged", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void StokEkleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;

                // Giriş doğrulama (boş değer kontrolü vs.)
                if (string.IsNullOrWhiteSpace(StokKodTextBox.Text) ||
                    string.IsNullOrWhiteSpace(StokAdTextBox.Text) ||
                    string.IsNullOrWhiteSpace(MiktarTextBox.Text) ||
                    string.IsNullOrWhiteSpace(BirimFiyatTextBox.Text) ||
                    OlcuBirimiComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Lütfen zorunlu alanları doldurun.", "Eksik Bilgi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal kdvOrani = Islem == "Alış" ? SelectedStoklar.KdvAlis : SelectedStoklar.KdvSatis;

                // Stok detay nesnesini oluştur
                FaturaDetay detay = new()
                {
                    StokKod = StokKodTextBox.Text,
                    StokAd = StokAdTextBox.Text,
                    Miktar = int.TryParse(MiktarTextBox.Text, out var miktar) ? miktar : 0,
                    BirimFiyat = decimal.TryParse(BirimFiyatTextBox.Text, out var birimFiyat) ? birimFiyat : 0,
                    IadeMaliyet = decimal.TryParse(IadeMaliyetTextBox.Text, out var iade) ? iade : 0,
                    Iskonto = int.TryParse(IskontoTextBox.Text, out var iskonto) ? iskonto : 0,
                    FiiliTarih = FiiliTarihPicker.SelectedDate ?? DateTime.Now,
                    OlcuBirimiId = (int)OlcuBirimiComboBox.SelectedValue,
                    Tutar = ((miktar * birimFiyat) * (1 - iskonto / 100m))
                };

                // Listeye ekle
                viewModel.YeniFatura.Detaylar.Add(detay);

                Hesapla();

                SelectedStoklar = null;
                StokKodTextBox.Clear();
                StokAdTextBox.Clear();
                MiktarTextBox.Clear();
                BirimFiyatTextBox.Clear();
                IadeMaliyetTextBox.Clear();
                IskontoTextBox.Clear();
                FiiliTarihPicker.SelectedDate = DateTime.Now;
                OlcuBirimiComboBox.Items.Clear();

                StokEkleButton.IsEnabled = false;
                StokDegistirButton.IsEnabled = false;
                StokSilButton.IsEnabled = false;

                StokAdTextBox.IsEnabled = false;
                MiktarTextBox.IsEnabled = false;
                BirimFiyatTextBox.IsEnabled = false;
                IadeMaliyetTextBox.IsEnabled = false;
                IskontoTextBox.IsEnabled = false;
                FiiliTarihPicker.IsEnabled = false;
                OlcuBirimiComboBox.IsEnabled = false;

                GirdiKontrolu();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Faturalar", methodName: "StokEkleButton_Click", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void StokDegistirButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Giriş doğrulama (boş değer kontrolü vs.)
                if (string.IsNullOrWhiteSpace(StokKodTextBox.Text) ||
                    string.IsNullOrWhiteSpace(StokAdTextBox.Text) ||
                    string.IsNullOrWhiteSpace(MiktarTextBox.Text) ||
                    string.IsNullOrWhiteSpace(BirimFiyatTextBox.Text) ||
                    OlcuBirimiComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Lütfen zorunlu alanları doldurun.", "Eksik Bilgi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var secilenDetay = DataGrid.SelectedItem as FaturaDetay;
                if (secilenDetay == null)
                {
                    MessageBox.Show("Güncellemek için bir kalem seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Yeni değerleri al
                secilenDetay.StokAd = StokAdTextBox.Text;
                secilenDetay.Miktar = int.TryParse(MiktarTextBox.Text, out var miktar) ? miktar : 0;
                secilenDetay.BirimFiyat = decimal.TryParse(BirimFiyatTextBox.Text, out var birimFiyat) ? birimFiyat : 0;
                secilenDetay.IadeMaliyet = decimal.TryParse(IadeMaliyetTextBox.Text, out var iade) ? iade : 0;
                secilenDetay.Iskonto = int.TryParse(IskontoTextBox.Text, out var iskonto) ? iskonto : 0;
                secilenDetay.FiiliTarih = FiiliTarihPicker.SelectedDate ?? DateTime.Now;
                secilenDetay.OlcuBirimiId = (int)OlcuBirimiComboBox.SelectedValue;
                secilenDetay.Tutar = ((secilenDetay.Miktar * secilenDetay.BirimFiyat) * (1 - secilenDetay.Iskonto / 100m));

                Hesapla();

                SelectedStoklar = null;
                StokKodTextBox.Clear();
                StokAdTextBox.Clear();
                MiktarTextBox.Clear();
                BirimFiyatTextBox.Clear();
                IadeMaliyetTextBox.Clear();
                IskontoTextBox.Clear();
                FiiliTarihPicker.SelectedDate = DateTime.Now;
                OlcuBirimiComboBox.Items.Clear();

                StokEkleButton.IsEnabled = false;
                StokDegistirButton.IsEnabled = false;
                StokSilButton.IsEnabled = false;

                StokAdTextBox.IsEnabled = false;
                MiktarTextBox.IsEnabled = false;
                BirimFiyatTextBox.IsEnabled = false;
                IadeMaliyetTextBox.IsEnabled = false;
                IskontoTextBox.IsEnabled = false;
                FiiliTarihPicker.IsEnabled = false;
                OlcuBirimiComboBox.IsEnabled = false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Faturalar", methodName: "StokDegistirButton_Click", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void StokSilButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;

                var secilenDetay = DataGrid.SelectedItem as FaturaDetay;
                if (secilenDetay == null)
                {
                    MessageBox.Show("Güncellemek için bir kalem seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Hesapla();

                // Listeden kaldır
                viewModel.YeniFatura.Detaylar.Remove(secilenDetay);

                GirdiKontrolu();

                SelectedStoklar = null;
                StokKodTextBox.Clear();
                StokAdTextBox.Clear();
                MiktarTextBox.Clear();
                BirimFiyatTextBox.Clear();
                IadeMaliyetTextBox.Clear();
                IskontoTextBox.Clear();
                FiiliTarihPicker.SelectedDate = DateTime.Now;
                OlcuBirimiComboBox.Items.Clear();

                StokEkleButton.IsEnabled = false;
                StokDegistirButton.IsEnabled = false;
                StokSilButton.IsEnabled = false;

                StokAdTextBox.IsEnabled = false;
                MiktarTextBox.IsEnabled = false;
                BirimFiyatTextBox.IsEnabled = false;
                IadeMaliyetTextBox.IsEnabled = false;
                IskontoTextBox.IsEnabled = false;
                FiiliTarihPicker.IsEnabled = false;
                OlcuBirimiComboBox.IsEnabled = false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Faturalar", methodName: "StokSilButton_Click", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void StokKartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selector = new EntitySelectorView(typeof(Stoklar));
                if (selector.ShowDialog() == true)
                {
                    var selectedStok = selector.SelectedItem as Stoklar;
                    if (selectedStok != null)
                    {
                        StokKodTextBox.Text = selectedStok.StokKod;
                        StokAdTextBox.Text = selectedStok.StokAd;
                        OlcuBirimiComboBox.Items.Clear();
                        OlcuBirimiComboBox.Items.Add(selectedStok.OlcuBirimi1);
                        if (selectedStok.OlcuBirimi2 != null)
                        {
                            OlcuBirimiComboBox.Items.Add(selectedStok.OlcuBirimi2);
                        }
                        OlcuBirimiComboBox.SelectedValue = selectedStok.OlcuBirimi1Id;
                        SelectedStoklar = selectedStok;
                        SetInputState(true);
                        SetButtonState(true, false, false); // Ekle açık, diğerleri kapalı
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Faturalar", methodName: "StokKartButton_Click", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void StokKodTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var viewModel = (MainViewModel)this.DataContext;
                var stokKod = StokKodTextBox.Text.Trim();

                // Stok bilgilerini al
                var stokList = new Stok().GetStoklar();
                SelectedStoklar = stokList.FirstOrDefault(s => s.StokKod == stokKod);

                if (SelectedStoklar == null)
                {
                    // Stok yoksa her şeyi temizle ve kapat
                    ClearInputs();
                    SetInputState(false);
                    SetButtonState(false, false, false);
                    return;
                }

                // Stok bulundu, alanları aktif hale getir
                SetInputState(true);
                StokAdTextBox.Text = SelectedStoklar.StokAd;

                // Önce varsa eski item'ları temizle
                OlcuBirimiComboBox.Items.Clear();

                // OlcuBirimi1 her zaman vardır, doğrudan ekle
                if (SelectedStoklar?.OlcuBirimi1 != null)
                    OlcuBirimiComboBox.Items.Add(SelectedStoklar.OlcuBirimi1);

                // OlcuBirimi2 varsa onu da ekle
                if (SelectedStoklar?.OlcuBirimi2 != null)
                    OlcuBirimiComboBox.Items.Add(SelectedStoklar.OlcuBirimi2);

                // Seçili olanı ID ile işaretle (ItemsSource olmadığı için SelectedItem değil, SelectedValue kullanımı uygundur)
                OlcuBirimiComboBox.SelectedValue = SelectedStoklar.OlcuBirimi1?.Id;

                // Fatura detayda var mı?
                var detay = viewModel.YeniFatura.Detaylar.FirstOrDefault(fd => fd.StokKod == stokKod);
                bool detayVar = detay != null;

                if (detayVar)
                {
                    // DataGrid'deki satırı seç
                    DataGrid.SelectedItem = detay;
                    DataGrid.ScrollIntoView(detay);

                    // Detaydan gelen bilgileri göster
                    MiktarTextBox.Text = detay.Miktar.ToString();
                    BirimFiyatTextBox.Text = detay.BirimFiyat.ToString();
                    IadeMaliyetTextBox.Text = detay.IadeMaliyet.ToString();
                    IskontoTextBox.Text = detay.Iskonto.ToString();
                    FiiliTarihPicker.SelectedDate = detay.FiiliTarih;
                    OlcuBirimiComboBox.SelectedValue = detay.OlcuBirimiId;

                    SetButtonState(false, true, true); // Ekle kapalı, diğerleri açık
                }
                else
                {
                    // Yeni giriş hazırlığı
                    DataGrid.SelectedItem = null;
                    MiktarTextBox.Clear();
                    BirimFiyatTextBox.Clear();
                    IadeMaliyetTextBox.Clear();
                    IskontoTextBox.Clear();
                    FiiliTarihPicker.SelectedDate = DateTime.Now;
                    OlcuBirimiComboBox.SelectedIndex = -1;

                    SetButtonState(true, false, false); // Sadece ekle açık
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Faturalar", methodName: "StokKodTextBox_TextChanged", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void ClearInputs()
        {
            DataGrid.SelectedItem = null;
            StokAdTextBox.Clear();
            MiktarTextBox.Clear();
            BirimFiyatTextBox.Clear();
            IadeMaliyetTextBox.Clear();
            IskontoTextBox.Clear();
            FiiliTarihPicker.SelectedDate = null;
            OlcuBirimiComboBox.Items.Clear();
            OlcuBirimiComboBox.SelectedIndex = -1;
        }

        private void SetInputState(bool enabled)
        {
            StokAdTextBox.IsEnabled = enabled;
            MiktarTextBox.IsEnabled = enabled;
            BirimFiyatTextBox.IsEnabled = enabled;
            IadeMaliyetTextBox.IsEnabled = enabled;
            IskontoTextBox.IsEnabled = enabled;
            FiiliTarihPicker.IsEnabled = enabled;
            OlcuBirimiComboBox.IsEnabled = enabled;
        }

        private void SetButtonState(bool ekle, bool degistir, bool sil)
        {
            StokEkleButton.IsEnabled = ekle;
            StokDegistirButton.IsEnabled = degistir;
            StokSilButton.IsEnabled = sil;
        }

        private void KaydetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;

                if (viewModel.YeniFatura.Detaylar.Count == 0)
                {
                    MessageBox.Show("En az bir kalem eklemelisiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Fatura numarası kontrolü
                var faturaNumarasi = viewModel.Faturalar.FirstOrDefault(f => f.Numara == NumaraTextBox.Text);
                if (faturaNumarasi != null)
                {
                    MessageBox.Show("Bu fatura numarası zaten mevcut.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (MySqlConnection connection = DatabaseManager.GetConnection())
                {
                    connection.Open();
                    using (MySqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. Faturalar Ekle
                            string insertFaturaQuery = @"INSERT INTO Faturalar (Numara, CariKod, Tarih, VadeTarih, FaturaTipi, Tip, Aciklama, ToplamTutar, 
                                KDV1Oran, KDV2Oran, KDV3Oran, KDV4Oran, KDV5Oran, Iskonto) VALUES
                                (@Numara, @CariKod, @Tarih, @VadeTarih, @FaturaTipi, @Tip, @Aciklama, @ToplamTutar,
                                @KDV1, @KDV2, @KDV3, @KDV4, @KDV5, @Iskonto); SELECT LAST_INSERT_ID();";

                            MySqlCommand faturaCmd = new(insertFaturaQuery, connection, transaction);
                            faturaCmd.Parameters.AddWithValue("@Numara", NumaraTextBox.Text);
                            faturaCmd.Parameters.AddWithValue("@CariKod", CariKodTextBox.Text);
                            faturaCmd.Parameters.AddWithValue("@Tarih", TarihDatePicker.SelectedDate ?? DateTime.Now);
                            faturaCmd.Parameters.AddWithValue("@VadeTarih", VadeDatePicker.SelectedDate ?? DateTime.Now);
                            faturaCmd.Parameters.AddWithValue("@FaturaTipi", Islem);
                            faturaCmd.Parameters.AddWithValue("@Tip", TipComboBox.Text);
                            faturaCmd.Parameters.AddWithValue("@Aciklama", AciklamaTextbox.Text);
                            faturaCmd.Parameters.AddWithValue("@ToplamTutar", viewModel.YeniFatura.ToplamTutar);

                            faturaCmd.Parameters.AddWithValue("@KDV1", viewModel.YeniFatura.KDV1Oran);
                            faturaCmd.Parameters.AddWithValue("@KDV2", viewModel.YeniFatura.KDV2Oran);
                            faturaCmd.Parameters.AddWithValue("@KDV3", viewModel.YeniFatura.KDV3Oran);
                            faturaCmd.Parameters.AddWithValue("@KDV4", viewModel.YeniFatura.KDV4Oran);
                            faturaCmd.Parameters.AddWithValue("@KDV5", viewModel.YeniFatura.KDV5Oran);
                            faturaCmd.Parameters.AddWithValue("@Iskonto", viewModel.YeniFatura.Iskonto);

                            int insertedFaturaId = Convert.ToInt32(faturaCmd.ExecuteScalar());

                            // 2. Faturalar Detaylarını Ekle
                            string insertDetayQuery = @"INSERT INTO FaturaDetay (FaturaID, StokKod, StokAd, Miktar, BirimFiyat, Tutar, Iskonto, IadeMaliyet, FiiliTarih, OlcuBirimiId)
                                VALUES (@FaturaID, @StokKod, @StokAd, @Miktar, @BirimFiyat, @Tutar, @Iskonto, @IadeMaliyet, @FiiliTarih, @OlcuBirimiId);";

                            foreach (var detay in viewModel.YeniFatura.Detaylar)
                            {
                                MySqlCommand detayCmd = new(insertDetayQuery, connection, transaction);
                                detayCmd.Parameters.AddWithValue("@FaturaID", insertedFaturaId);
                                detayCmd.Parameters.AddWithValue("@StokKod", detay.StokKod);
                                detayCmd.Parameters.AddWithValue("@StokAd", detay.StokAd);
                                detayCmd.Parameters.AddWithValue("@Miktar", detay.Miktar);
                                detayCmd.Parameters.AddWithValue("@BirimFiyat", detay.BirimFiyat);
                                detayCmd.Parameters.AddWithValue("@Tutar", detay.Tutar);
                                detayCmd.Parameters.AddWithValue("@Iskonto", detay.Iskonto);
                                detayCmd.Parameters.AddWithValue("@IadeMaliyet", detay.IadeMaliyet);
                                detayCmd.Parameters.AddWithValue("@FiiliTarih", detay.FiiliTarih);
                                detayCmd.Parameters.AddWithValue("@OlcuBirimiId", detay.OlcuBirimiId);

                                detayCmd.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            MessageBox.Show("Faturalar başarıyla kaydedildi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);

                            viewModel.Faturalar.Add(new Faturalar
                            {
                                FaturaID = insertedFaturaId,
                                Numara = NumaraTextBox.Text,
                                CariKod = CariKodTextBox.Text,
                                Tarih = TarihDatePicker.SelectedDate ?? DateTime.Now,
                                VadeTarih = VadeDatePicker.SelectedDate ?? DateTime.Now,
                                FaturaTipi = Islem,
                                Tip = TipComboBox.Text,
                                Aciklama = AciklamaTextbox.Text,
                                ToplamTutar = viewModel.YeniFatura.ToplamTutar,
                                KDV1Oran = viewModel.YeniFatura.KDV1Oran,
                                KDV2Oran = viewModel.YeniFatura.KDV2Oran,
                                KDV3Oran = viewModel.YeniFatura.KDV3Oran,
                                KDV4Oran = viewModel.YeniFatura.KDV4Oran,
                                KDV5Oran = viewModel.YeniFatura.KDV5Oran,
                                Iskonto = viewModel.YeniFatura.Iskonto,
                                Detaylar = viewModel.YeniFatura.Detaylar
                            });

                            // Temizle
                            viewModel.YeniFatura.Detaylar.Clear();
                            viewModel.YeniFatura = new();

                            UstBilgilerTabItem.IsSelected = true;

                            NumaraTextBox.Clear();
                            CariKodTextBox.Clear();
                            SelectedCariler = null;
                            TarihDatePicker.SelectedDate = DateTime.Now;
                            VadeDatePicker.SelectedDate = DateTime.Now;
                            TipComboBox.SelectedIndex = -1;
                            AciklamaTextbox.Clear();
                            KDVDahilMiCheckBox.IsChecked = false;

                            StokKodTextBox.Clear();
                            SelectedStoklar = null;
                            StokAdTextBox.Clear();
                            MiktarTextBox.Clear();
                            BirimFiyatTextBox.Clear();
                            IadeMaliyetTextBox.Clear();
                            IskontoTextBox.Clear();
                            FiiliTarihPicker.SelectedDate = DateTime.Now;
                            OlcuBirimiComboBox.Items.Clear();
                            ToplamMiktarTextBox.Clear();
                            BakiyeTextBox.Clear();
                            IskontoOranTextBox.Clear();
                            IskontoTutarTextBox.Clear();
                            TevkifatTextBox.Clear();

                            StokEkleButton.IsEnabled = false;
                            StokDegistirButton.IsEnabled = false;
                            StokSilButton.IsEnabled = false;

                            StokAdTextBox.IsEnabled = false;
                            MiktarTextBox.IsEnabled = false;
                            BirimFiyatTextBox.IsEnabled = false;
                            IadeMaliyetTextBox.IsEnabled = false;
                            IskontoTextBox.IsEnabled = false;
                            FiiliTarihPicker.IsEnabled = false;
                            OlcuBirimiComboBox.IsEnabled = false;

                            GirdiKontrolu();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            LogManager.LogError(ex, className: "Faturalar", methodName: "KaydetButton_Click", stackTrace: ex.StackTrace);
                            MessageBox.Show("Faturalar kaydedilirken bir hata oluştu.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Faturalar", methodName: "KaydetButton_Click", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void Hesapla()
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;

                Stok stokPage = new();
                ObservableCollection<Stoklar> stoklar = null;
                if (viewModel.Stoklar == null || viewModel.Stoklar.Count == 0)
                {
                    stoklar = stokPage.GetStoklar(); // Stokları yükle
                }

                Dictionary<decimal, decimal> kdvOranTutarMap = new Dictionary<decimal, decimal>();
                decimal araToplam = 0;

                decimal miktarTop = viewModel.YeniFatura.Detaylar.Sum(f => f.Miktar);
                decimal bakiye = viewModel.YeniFatura.Detaylar.Sum(f => f.Tutar);

                ToplamMiktarTextBox.Text = miktarTop.ToString();
                BakiyeTextBox.Text = bakiye.ToString();

                foreach (var detay in viewModel.YeniFatura.Detaylar)
                {
                    var stok = stoklar.FirstOrDefault(s => s.StokKod == detay.StokKod);
                    if (stok == null) continue;

                    decimal tutar = detay.Miktar * detay.BirimFiyat;
                    decimal iskontoOrani = detay.Iskonto > 0 ? detay.Iskonto : 0;
                    decimal iskontoluTutar = tutar * (1 - iskontoOrani / 100);

                    decimal kdvOrani = Islem == "Alış" ? stok.KdvAlis : stok.KdvSatis;
                    decimal kdvTutar = 0;

                    // KDV dahil mi?
                    bool kdvDahilMi = KDVDahilMiCheckBox.IsChecked == true;

                    if (kdvDahilMi)
                    {
                        decimal kdvsizTutar = iskontoluTutar / (1 + kdvOrani / 100);
                        kdvTutar = iskontoluTutar - kdvsizTutar;
                        iskontoluTutar = kdvsizTutar; // Net tutar
                    }
                    else
                    {
                        kdvTutar = iskontoluTutar * kdvOrani / 100;
                    }

                    if (!kdvOranTutarMap.ContainsKey(kdvOrani))
                        kdvOranTutarMap[kdvOrani] = 0;

                    kdvOranTutarMap[kdvOrani] += kdvTutar;
                    araToplam += iskontoluTutar;
                }

                // KDV TextBox listeleri
                List<TextBox> kdvOranKutular = new List<TextBox> { KDV1TextBox, KDV2TextBox, KDV3TextBox, KDV4TextBox, KDV5TextBox };
                List<TextBox> kdvTutarKutular = new List<TextBox> { KDV1TutarTextBox, KDV2TutarTextBox, KDV3TutarTextBox, KDV4TutarTextBox, KDV5TutarTextBox };

                // Kutuları temizle
                foreach (var tb in kdvOranKutular) tb.Text = "";
                foreach (var tb in kdvTutarKutular) tb.Text = "";

                int index = 0;
                foreach (var kvp in kdvOranTutarMap)
                {
                    if (index < kdvOranKutular.Count)
                    {
                        kdvOranKutular[index].Text = $"{kvp.Key}";
                        kdvTutarKutular[index].Text = kvp.Value.ToString("N2");
                        index++;
                    }
                }

                decimal toplamKdv = kdvOranTutarMap.Values.Sum();
                KDVTutarToplamTextBox.Text = toplamKdv.ToString("N2");
                AraToplamTextBox.Text = araToplam.ToString("N2");

                // Kullanıcı iskonto girişleri
                decimal iskontoTutar = 0;
                decimal iskontoOran = 0;

                isUpdatingIskonto = true;

                if (lastChanged == "oran")
                {
                    decimal.TryParse(IskontoOranTextBox.Text, out iskontoOran);
                    iskontoTutar = araToplam * (iskontoOran / 100);
                    IskontoTutarTextBox.Text = iskontoTutar.ToString("N2");
                }
                else if (lastChanged == "tutar")
                {
                    decimal.TryParse(IskontoTutarTextBox.Text, out iskontoTutar);
                    iskontoOran = (araToplam > 0) ? (iskontoTutar / araToplam * 100) : 0;
                    IskontoOranTextBox.Text = iskontoOran.ToString("N2");
                }

                isUpdatingIskonto = false;

                // Hesaplanmış değerlerle genel toplam
                decimal genelToplam = araToplam - iskontoTutar + toplamKdv;

                // Tevkifat oranı girildiyse uygula (KDV üzerinden)
                decimal.TryParse(TevkifatTextBox.Text, out decimal tevkifatOran);
                decimal tevkifatTutar = 0;
                if (tevkifatOran > 0)
                {
                    tevkifatTutar = toplamKdv * (tevkifatOran / 100);
                    genelToplam -= tevkifatTutar;
                }

                GenelToplamTextBox.Text = genelToplam.ToString("N2");

                viewModel.YeniFatura.ToplamTutar = genelToplam;
                viewModel.YeniFatura.Iskonto = iskontoOran;

                // KDV oranlarını fatura modeline sırayla yaz
                var uniqueKdvOranlari = kdvOranTutarMap.Keys.OrderBy(x => x).ToList();
                if (uniqueKdvOranlari.Count > 0) viewModel.YeniFatura.KDV1Oran = uniqueKdvOranlari.ElementAtOrDefault(0);
                if (uniqueKdvOranlari.Count > 1) viewModel.YeniFatura.KDV2Oran = uniqueKdvOranlari.ElementAtOrDefault(1);
                if (uniqueKdvOranlari.Count > 2) viewModel.YeniFatura.KDV3Oran = uniqueKdvOranlari.ElementAtOrDefault(2);
                if (uniqueKdvOranlari.Count > 3) viewModel.YeniFatura.KDV4Oran = uniqueKdvOranlari.ElementAtOrDefault(3);
                if (uniqueKdvOranlari.Count > 4) viewModel.YeniFatura.KDV5Oran = uniqueKdvOranlari.ElementAtOrDefault(4);
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Faturalar", methodName: "HesaplaKDV", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void Ortak_TextChanged(object sender, TextChangedEventArgs e)
        {
            GirdiKontrolu();
        }

        private void Ortak_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            GirdiKontrolu();
        }

        private void TipComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (TipComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    string selectedTip = selectedItem.Content?.ToString();
                    if (selectedTip == "İade" || selectedTip == "Zayi İade")
                    {
                        IskontoOranTextBox.IsEnabled = false;
                        IskontoTutarTextBox.IsEnabled = false;
                        IadeMaliyetTextBox.IsEnabled = false;
                        IadeMaliyetTextBox.Clear();
                        IskontoOranTextBox.Clear();
                        IskontoTutarTextBox.Clear();
                    }
                    else
                    {
                        IskontoOranTextBox.IsEnabled = true;
                        IskontoTutarTextBox.IsEnabled = true;
                        IadeMaliyetTextBox.IsEnabled = true;
                        IadeMaliyetTextBox.Clear();
                        IskontoOranTextBox.Clear();
                        IskontoTutarTextBox.Clear();
                    }
                }

                GirdiKontrolu();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Faturalar", methodName: "TipComboBox_SelectionChanged", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void GirdiKontrolu()
        {
            if (!arayuzHazir) return;

            bool numaraDolu = !string.IsNullOrWhiteSpace(NumaraTextBox.Text);
            bool cariKodDolu = SelectedCariler != null;
            bool tipSecili = TipComboBox.SelectedItem is ComboBoxItem selectedItem && !string.IsNullOrWhiteSpace(selectedItem.Content?.ToString());

            bool tarihSecili = TarihDatePicker.SelectedDate != null;
            bool vadeSecili = VadeDatePicker.SelectedDate != null;

            bool kalemBilgileriHazir = numaraDolu && cariKodDolu && tipSecili && tarihSecili && vadeSecili;
            KalemBilgileriTabItem.Visibility = kalemBilgileriHazir ? Visibility.Visible : Visibility.Hidden;

            bool enAzBirKalemVar = DataGrid.Items.Count > 0;
            ToplamlarTabItem.Visibility = enAzBirKalemVar ? Visibility.Visible : Visibility.Hidden;
        }

        private void OnlyNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void TevkifatTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                Hesapla();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Faturalar", methodName: "TevkifatTextBox_TextChanged", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void IskontoOranTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (isUpdatingIskonto) return;
                lastChanged = "oran";
                Hesapla();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Faturalar", methodName: "IskontoTutarTextBox_TextChanged", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void IskontoTutarTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (isUpdatingIskonto) return;
                lastChanged = "tutar";
                Hesapla();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Faturalar", methodName: "IskontoTutarTextBox_TextChanged", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void KDVDahilMiCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataGrid.Items.Count > 0)
                {
                    Hesapla();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "Faturalar", methodName: "KDVDahilMiCheckBox_Changed", stackTrace: ex.StackTrace);
                throw;
            }
        }
    }
}
