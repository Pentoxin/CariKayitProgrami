using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Cari_kayıt_Programı.Models
{
    public class FilterField : INotifyPropertyChanged
    {
        public string PropertyName { get; set; }
        public string DisplayName { get; set; }

        private string _value;
        public string Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

}
