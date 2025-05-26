namespace Cari_kayıt_Programı.Models
{
    public class Stoklar
    {
        public string StokKod { get; set; }
        public string StokAd { get; set; }

        public int OlcuBirimi1Id { get; set; }
        public int? OlcuBirimi2Id { get; set; }

        public int OlcuOranPay { get; set; } = 1;
        public int OlcuOranPayda { get; set; } = 1;

        public decimal KdvAlis { get; set; }
        public decimal KdvSatis { get; set; }

        // UI'da göstermek ve rahat erişim için isteğe bağlı:
        public OlcuBirimleri OlcuBirimi1 { get; set; }
        public OlcuBirimleri? OlcuBirimi2 { get; set; }
    }
}
