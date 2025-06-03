using System;

namespace Cari_kayıt_Programı.Models
{
    public class CariHareketGorunum
    {
        public int FaturaID { get; set; }          // Primary Key
        public string Numara { get; set; }         // Evrak No
        public string CariKod { get; set; }
        public DateTime Tarih { get; set; }
        public DateTime VadeTarih { get; set; }
        public string FaturaTip { get; set; }            // "Alış" veya "Satış"
        public string? Aciklama { get; set; }
        public string Tip { get; set; }          // Açık, Kapalı, Muhtelif, İade, Zayi İade
        public decimal Tutar { get; set; }
        public decimal Bakiye { get; set; }        // Arayüz'daki bakiye için kullanılabilir

        public bool FaturaDetay { get; set; }      // Fatura detayı var mı kontrolü için
    }
}
