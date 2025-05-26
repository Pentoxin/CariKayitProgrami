using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cari_kayıt_Programı.Helpers
{
    public static class SmartDateBehavior
    {
        public static readonly DependencyProperty EnableSmartParsingProperty =
            DependencyProperty.RegisterAttached(
                "EnableSmartParsing",
                typeof(bool),
                typeof(SmartDateBehavior),
                new PropertyMetadata(false, OnEnableSmartParsingChanged));

        public static void SetEnableSmartParsing(DependencyObject element, bool value)
        {
            element.SetValue(EnableSmartParsingProperty, value);
        }

        public static bool GetEnableSmartParsing(DependencyObject element)
        {
            return (bool)element.GetValue(EnableSmartParsingProperty);
        }

        private static void OnEnableSmartParsingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DatePicker datePicker && e.NewValue is true)
            {
                datePicker.Loaded += (s, args) =>
                {
                    var textBox = FindChild<TextBox>(datePicker);
                    if (textBox != null)
                    {
                        textBox.PreviewLostKeyboardFocus -= HandleSmartDateParsing;
                        textBox.PreviewLostKeyboardFocus += HandleSmartDateParsing;
                    }
                };
            }
        }

        private static void HandleSmartDateParsing(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            string input = textBox.Text.Trim();
            if (string.IsNullOrEmpty(input)) return;

            DateTime today = DateTime.Today;

            try
            {
                DateTime parsedDate;

                if (input.Length == 4)
                {
                    parsedDate = new DateTime(today.Year,
                                              int.Parse(input.Substring(2, 2)),
                                              int.Parse(input.Substring(0, 2)));
                }
                else if (input.Length == 8)
                {
                    parsedDate = new DateTime(int.Parse(input.Substring(4, 4)),
                                              int.Parse(input.Substring(2, 2)),
                                              int.Parse(input.Substring(0, 2)));
                }
                else
                {
                    parsedDate = DateTime.Parse(input, new CultureInfo("tr-TR"));
                }

                textBox.Text = parsedDate.ToString("dd.MM.yyyy");
            }
            catch
            {
                // Hatalı giriş varsa sessizce geç
            }
        }

        private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var result = FindChild<T>(child);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}