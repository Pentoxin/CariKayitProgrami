using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using static Cari_kayıt_Programı.CariHareketKayitlari;
using static Cari_kayıt_Programı.CariHesapKayitlari;
using static Cari_kayıt_Programı.Stok;

namespace Cari_kayıt_Programı
{

    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Odeme> odemeler;

        [ObservableProperty]
        private ObservableCollection<Business> businesses;

        [ObservableProperty]
        private ObservableCollection<Stoklar> stoklar;

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
            Odemeler = new ObservableCollection<Odeme>();
            Businesses = new ObservableCollection<Business>();
            Stoklar = new ObservableCollection<Stoklar>();
            TarihDate = DateTime.Now;
            VadeDate = TarihDate;
        }
    }
}