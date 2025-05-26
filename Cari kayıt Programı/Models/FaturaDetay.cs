using System;
using System.ComponentModel;

namespace Cari_kayıt_Programı.Models
{
    public class FaturaDetay : INotifyPropertyChanged
    {
        private int detayID;
        private int faturaID;
        private string stokKod;
        private string stokAd;
        private int miktar;
        private decimal birimFiyat;
        private decimal tutar;
        private decimal iskonto;
        private decimal iadeMaliyet;
        private DateTime? fiiliTarih;
        private int olcuBirimiId;

        public int DetayID
        {
            get => detayID;
            set { detayID = value; OnPropertyChanged(nameof(DetayID)); }
        }

        public int FaturaID
        {
            get => faturaID;
            set { faturaID = value; OnPropertyChanged(nameof(FaturaID)); }
        }

        public string StokKod
        {
            get => stokKod;
            set { stokKod = value; OnPropertyChanged(nameof(StokKod)); }
        }

        public string StokAd
        {
            get => stokAd;
            set { stokAd = value; OnPropertyChanged(nameof(StokAd)); }
        }

        public int Miktar
        {
            get => miktar;
            set { miktar = value; OnPropertyChanged(nameof(Miktar)); }
        }

        public decimal BirimFiyat
        {
            get => birimFiyat;
            set { birimFiyat = value; OnPropertyChanged(nameof(BirimFiyat)); }
        }

        public decimal Tutar
        {
            get => tutar;
            set { tutar = value; OnPropertyChanged(nameof(Tutar)); }
        }

        public decimal Iskonto
        {
            get => iskonto;
            set { iskonto = value; OnPropertyChanged(nameof(Iskonto)); }
        }

        public decimal IadeMaliyet
        {
            get => iadeMaliyet;
            set { iadeMaliyet = value; OnPropertyChanged(nameof(IadeMaliyet)); }
        }

        public DateTime? FiiliTarih
        {
            get => fiiliTarih;
            set { fiiliTarih = value; OnPropertyChanged(nameof(FiiliTarih)); }
        }

        public int OlcuBirimiId
        {
            get => olcuBirimiId;
            set { olcuBirimiId = value; OnPropertyChanged(nameof(OlcuBirimiId)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
