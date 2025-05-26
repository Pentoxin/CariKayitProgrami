using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Cari_kayıt_Programı.Models
{
    public class EntitySelectorViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<object> AllItems { get; set; } = new();
        public ObservableCollection<object> FilteredItems { get; set; } = new();
        public ObservableCollection<FilterField> Filters { get; set; } = new();

        private object _selectedItem;
        public object SelectedItem
        {
            get => _selectedItem;
            set { _selectedItem = value; OnPropertyChanged(); }
        }

        private Type _entityType;

        public EntitySelectorViewModel(Type entityType)
        {
            _entityType = entityType;

            LoadData();
            CreateFilterFields();
        }

        private void LoadData()
        {
            // Gerçek kullanımda servisten çekeceksin
            if (_entityType == typeof(Stoklar))
            {
                var items = new Stok().GetStoklar().Cast<object>();
                foreach (var item in items)
                    AllItems.Add(item);
            }
            else if (_entityType == typeof(Cariler))
            {
                var items = new CariHesapKayitlari().GetBusinesses().Cast<object>();
                foreach (var item in items)
                    AllItems.Add(item);
            }

            FilteredItems = new ObservableCollection<object>(AllItems);
        }

        private void CreateFilterFields()
        {
            var props = _entityType.GetProperties()
                .Where(p =>
                p.PropertyType == typeof(string) ||
                p.PropertyType.IsValueType); // int, decimal, DateTime, bool, vs.

            foreach (var prop in props)
            {
                var filter = new FilterField
                {
                    PropertyName = prop.Name,
                    DisplayName = SplitPascalCase(prop.Name),
                    Value = ""
                };
                filter.PropertyChanged += (_, __) => ApplyFilters();
                Filters.Add(filter);
            }
        }

        private void ApplyFilters()
        {
            var query = AllItems.AsEnumerable();

            foreach (var filter in Filters)
            {
                if (!string.IsNullOrWhiteSpace(filter.Value))
                {
                    query = query.Where(item =>
                    {
                        var prop = item.GetType().GetProperty(filter.PropertyName);
                        var val = prop?.GetValue(item)?.ToString()?.ToLower();
                        return val != null && val.Contains(filter.Value.ToLower());
                    });
                }
            }

            FilteredItems.Clear();
            foreach (var item in query)
                FilteredItems.Add(item);
        }

        private string SplitPascalCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            // 1. Küçük harf + büyük harf veya sayı
            input = Regex.Replace(input, @"([a-z])([A-Z0-9])", "$1 $2");

            // 2. Büyük harf + büyük harf + küçük harf (örneğin "IDKodu")
            input = Regex.Replace(input, @"([A-Z])([A-Z][a-z])", "$1 $2");

            // 3. Harf + sayı (örneğin "Birim1" → "Birim 1")
            input = Regex.Replace(input, @"([A-Za-z])([0-9])", "$1 $2");

            // 4. Sayı + harf (örneğin "1Id" → "1 Id")
            input = Regex.Replace(input, @"([0-9])([A-Za-z])", "$1 $2");

            return input.Trim();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
