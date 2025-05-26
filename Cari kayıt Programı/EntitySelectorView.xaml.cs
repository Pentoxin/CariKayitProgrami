using Cari_kayıt_Programı.Models;
using System;
using System.Windows;

namespace Cari_kayıt_Programı
{
    public partial class EntitySelectorView : Window
    {
        public object SelectedItem => (DataContext as EntitySelectorViewModel)?.SelectedItem;

        public EntitySelectorView(System.Type entityType)
        {
            try
            {
                InitializeComponent();
                DataContext = new EntitySelectorViewModel(entityType);
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "EntitySelectorView", methodName: "EntitySelectorView", ex.StackTrace);
                throw;
            }            
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedItem != null)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Lütfen bir öğe seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "EntitySelectorView", methodName: "Select_Click", ex.StackTrace);
            }            
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = false;
                Close();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, className: "EntitySelectorView", methodName: "Cancel_Click", ex.StackTrace);
            }            
        }
    }
}
