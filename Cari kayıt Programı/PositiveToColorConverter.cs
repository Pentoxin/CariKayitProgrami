using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Cari_kayıt_Programı
{
    public class PositiveToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal amount)
            {
                if (amount > 0)
                {
                    return Brushes.Green; // Pozitif değerler için yeşil renk
                }
                else if (amount < 0)
                {
                    return Brushes.Red; // Negatif değerler için kırmızı renk
                }
            }

            return Brushes.Black; // Sıfır veya null ise beyaz renk
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
