using System;
using System.Collections.ObjectModel;

namespace Cari_kayıt_Programı.Models
{
    public class Faturalar
    {
        public int FaturaID { get; set; }
        public string Numara { get; set; }
        public string CariKod { get; set; }
        public DateTime Tarih { get; set; }
        public DateTime VadeTarih { get; set; }
        public string FaturaTipi { get; set; } // "Alış" veya "Satış"
        public string Tip { get; set; }
        public string? Aciklama { get; set; }
        public decimal ToplamTutar { get; set; }

        public decimal KDV1Oran { get; set; }
        public decimal KDV2Oran { get; set; }
        public decimal KDV3Oran { get; set; }
        public decimal KDV4Oran { get; set; }
        public decimal KDV5Oran { get; set; }

        public decimal Iskonto { get; set; }

        public ObservableCollection<FaturaDetay> Detaylar { get; set; } = new();
    }
}
