using Cari_kayıt_Programı.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using static Cari_kayıt_Programı.CariHareketKayitlari;

namespace Cari_kayıt_Programı
{

    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Odeme> odemeler; // Silinecek

        [ObservableProperty]
        private ObservableCollection<Cariler> cariler;

        [ObservableProperty]
        private ObservableCollection<Stoklar> stoklar;

        [ObservableProperty]
        private ObservableCollection<Faturalar> faturalar;

        [ObservableProperty]
        private ObservableCollection<OlcuBirimleri> olcuBirimleri;

        [ObservableProperty]
        private Faturalar yeniFatura;

        private DateTime _tarihDate;

        public DateTime TarihDate
        {
            get { return _tarihDate; }
            set
            { SetProperty(ref _tarihDate, value); VadeDate = value; }
        }

        private DateTime _vadeDate;

        public DateTime VadeDate
        {
            get { return _vadeDate; }
            set { SetProperty(ref _vadeDate, value); }
        }

        public MainViewModel()
        {
            Odemeler = new ObservableCollection<Odeme>(); // Silinecek
            Cariler = new ObservableCollection<Cariler>();
            Stoklar = new ObservableCollection<Stoklar>();
            Faturalar = new ObservableCollection<Faturalar>();
            OlcuBirimleri = OlcuBirimleriService.GetForUI();

            YeniFatura = new Faturalar
            {
                Tarih = DateTime.Now,
                VadeTarih = DateTime.Now,
                Detaylar = new ObservableCollection<FaturaDetay>()
            };

            TarihDate = DateTime.Now;
            VadeDate = TarihDate;
        }

        public async Task LoadFaturalarAsync()
        {
            var list = new List<Faturalar>();

            try
            {
                using var conn = DatabaseManager.GetConnection();
                await conn.OpenAsync();

                var cmd = new MySqlCommand("SELECT * FROM Faturalar", conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var fatura = new Faturalar
                    {
                        FaturaID = reader.GetInt32("FaturaID"),
                        Numara = reader.GetString("Numara"),
                        CariKod = reader.GetString("CariKod"),
                        Tarih = reader.GetDateTime("Tarih"),
                        VadeTarih = reader.GetDateTime("VadeTarih"),
                        FaturaTipi = reader.GetString("FaturaTipi"),
                        Tip = reader.GetString("Tip"),
                        Aciklama = reader["Aciklama"]?.ToString(),
                        ToplamTutar = reader.GetDecimal("ToplamTutar"),
                        KDV1Oran = reader.GetDecimal("KDV1Oran"),
                        KDV2Oran = reader.GetDecimal("KDV2Oran"),
                        KDV3Oran = reader.GetDecimal("KDV3Oran"),
                        KDV4Oran = reader.GetDecimal("KDV4Oran"),
                        KDV5Oran = reader.GetDecimal("KDV5Oran"),
                        Iskonto = reader.GetDecimal("Iskonto")
                    };

                    // Detayları ayrıca yüklüyoruz
                    fatura.Detaylar = await LoadFaturaDetaylarAsync(fatura.FaturaID);
                    list.Add(fatura);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, "MainViewModel", "LoadFaturalarAsync", ex.StackTrace);
            }

            Faturalar = new ObservableCollection<Faturalar>(list);
        }

        public static async Task<ObservableCollection<FaturaDetay>> LoadFaturaDetaylarAsync(int faturaId)
        {
            var detaylar = new ObservableCollection<FaturaDetay>();

            try
            {
                using var conn = DatabaseManager.GetConnection();
                await conn.OpenAsync();

                var cmd = new MySqlCommand("SELECT * FROM FaturaDetay WHERE FaturaID = @faturaId", conn);
                cmd.Parameters.AddWithValue("@faturaId", faturaId);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    detaylar.Add(new FaturaDetay
                    {
                        DetayID = reader.GetInt32("DetayID"),
                        FaturaID = reader.GetInt32("FaturaID"),
                        StokKod = reader.GetString("StokKod"),
                        StokAd = reader.GetString("StokAd"),
                        Miktar = reader.GetInt32("Miktar"),
                        BirimFiyat = reader.GetDecimal("BirimFiyat"),
                        Tutar = reader.GetDecimal("Tutar"),
                        Iskonto = reader.GetDecimal("Iskonto"),
                        IadeMaliyet = reader.GetDecimal("IadeMaliyet"),
                        FiiliTarih = reader["FiiliTarih"] != DBNull.Value ? Convert.ToDateTime(reader["FiiliTarih"]) : null,
                        OlcuBirimiId = reader.GetInt32("OlcuBirimiId")
                    });
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, "MainViewModel", "LoadFaturaDetaylarAsync", ex.StackTrace);
            }

            return detaylar;
        }
    }
}