using Cari_kayıt_Programı.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Cari_kayıt_Programı
{

    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<CariHareketGorunum> hareketler;
        
        [ObservableProperty]
        private ObservableCollection<CariHareketGorunum> filteredHareketler;

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
            Hareketler = new ObservableCollection<CariHareketGorunum>();
            FilteredHareketler = new ObservableCollection<CariHareketGorunum>();
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

        private decimal _cariBorcToplam;
        public decimal CariBorcToplam
        {
            get => _cariBorcToplam;
            set { _cariBorcToplam = value; OnPropertyChanged(nameof(CariBorcToplam)); }
        }

        private decimal _cariAlacakToplam;
        public decimal CariAlacakToplam
        {
            get => _cariAlacakToplam;
            set { _cariAlacakToplam = value; OnPropertyChanged(nameof(CariAlacakToplam)); }
        }

        private decimal _cariBakiye;
        public decimal CariBakiye
        {
            get => _cariBakiye;
            set { _cariBakiye = value; OnPropertyChanged(nameof(CariBakiye)); }
        }

        public async Task GetCariHareketlerAsync(string cariKod)
        {
            var list = new List<CariHareketGorunum>();

            try
            {
                using var conn = DatabaseManager.GetConnection();
                await conn.OpenAsync();

                var cmd = new MySqlCommand(@"SELECT F.FaturaID, F.Numara, F.CariKod, F.Tarih, F.VadeTarih, F.FaturaTipi, F.Tip, F.Aciklama, F.ToplamTutar,
                    EXISTS (SELECT 1 FROM FaturaDetay FD WHERE FD.FaturaID = F.FaturaID LIMIT 1) AS DetayVarMi
                    FROM Faturalar F WHERE F.CariKod = @CariKod AND (F.Tip = 'Açık' OR F.Tip = 'Kapalı')
                    ORDER BY F.Tarih DESC;", conn);
                cmd.Parameters.AddWithValue("@CariKod", cariKod);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new CariHareketGorunum
                    {
                        FaturaID = reader.GetInt32("FaturaID"),
                        Numara = reader.GetString("Numara"),
                        CariKod = reader.GetString("CariKod"),
                        Tarih = reader.GetDateTime("Tarih"),
                        VadeTarih = reader.GetDateTime("VadeTarih"),
                        FaturaTip = reader.GetString("FaturaTipi"),
                        Aciklama = reader["Aciklama"]?.ToString() ?? "",
                        Tutar = reader.GetDecimal("ToplamTutar"),
                        Tip = reader.GetString("Tip") ?? "Kapalı",
                        FaturaDetay = reader.GetBoolean("DetayVarMi")
                    });
                }

                decimal cariBakiye = 0;
                decimal cariBorcToplam = 0;
                decimal cariAlacakToplam = 0;

                foreach (var hareket in list)
                {
                    if (hareket.FaturaTip == "Alış") // Para giriyor → Borçlanıyor
                    {
                        cariBorcToplam += hareket.Tutar;
                        cariBakiye += hareket.Tutar;
                    }
                    else if (hareket.FaturaTip == "Satış") // Para çıkıyor → Alacaklı
                    {
                        cariAlacakToplam += hareket.Tutar;
                        cariBakiye -= hareket.Tutar;
                    }

                    // Her hareketin o andaki bakiyesi
                    hareket.Bakiye = cariBakiye;
                }

                // İlgili property'lere aktar (ViewModel'de tanımlı olmalı)
                CariBorcToplam = cariBorcToplam;
                CariAlacakToplam = cariAlacakToplam;
                CariBakiye = cariBakiye;

            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, "MainViewModel", "GetCariHareketlerAsync", ex.StackTrace);
            }

            Hareketler = new ObservableCollection<CariHareketGorunum>(list);
            FilteredHareketler = new ObservableCollection<CariHareketGorunum>(list);
        }

        public void FilterHareketler(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                FilteredHareketler = new ObservableCollection<CariHareketGorunum>(Hareketler);
            }
            else
            {
                searchTerm = searchTerm.ToLower();

                var filtered = Hareketler.Where(h =>
                    (h.Numara != null && h.Numara.ToLower().Contains(searchTerm)) ||
                    h.Tarih.ToString("d").ToLower().Contains(searchTerm) ||
                    h.VadeTarih.ToString("d").ToLower().Contains(searchTerm) ||
                    (h.FaturaTip != null && h.FaturaTip.ToLower().Contains(searchTerm)) ||
                    (h.Tip != null && h.Tip.ToLower().Contains(searchTerm)) ||
                    (h.Aciklama != null && h.Aciklama.ToLower().Contains(searchTerm)) ||
                    h.Tutar.ToString("C").ToLower().Contains(searchTerm) ||
                    h.Bakiye.ToString("C").ToLower().Contains(searchTerm)
                );

                decimal cariBakiye = 0;
                decimal cariBorcToplam = 0;
                decimal cariAlacakToplam = 0;

                foreach (var hareket in filtered)
                {
                    if (hareket.FaturaTip == "Alış") // Para giriyor → Borçlanıyor
                    {
                        cariBorcToplam += hareket.Tutar;
                        cariBakiye += hareket.Tutar;
                    }
                    else if (hareket.FaturaTip == "Satış") // Para çıkıyor → Alacaklı
                    {
                        cariAlacakToplam += hareket.Tutar;
                        cariBakiye -= hareket.Tutar;
                    }

                    // Her hareketin o andaki bakiyesi
                    hareket.Bakiye = cariBakiye;
                }

                // İlgili property'lere aktar (ViewModel'de tanımlı olmalı)
                CariBorcToplam = cariBorcToplam;
                CariAlacakToplam = cariAlacakToplam;
                CariBakiye = cariBakiye;

                FilteredHareketler = new ObservableCollection<CariHareketGorunum>(filtered);
            }
        }
    }
}