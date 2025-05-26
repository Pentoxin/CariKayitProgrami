using Cari_kayıt_Programı.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Cari_kayıt_Programı
{
    public static class OlcuBirimleriService
    {
        private static ObservableCollection<OlcuBirimleri> _olcuBirimleri = new();

        public static bool Yüklendi => _olcuBirimleri.Count > 0;

        /// <summary>
        /// Tüm ölçü birimlerini getirir. Daha önce yüklendiyse tekrar veritabanına bağlanmaz.
        /// </summary>
        public static ObservableCollection<OlcuBirimleri> GetAll()
        {
            if (Yüklendi)
                return _olcuBirimleri;

            try
            {
                using var conn = DatabaseManager.GetConnection();
                conn.Open();

                string query = "SELECT * FROM OlcuBirimleri";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var birim = new OlcuBirimleri
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        BirimAdi = reader.GetString("BirimAdi"),
                        Aciklama = reader["Aciklama"]?.ToString()
                    };

                    _olcuBirimleri.Add(birim);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, "OlcuBirimleriService", "GetAll", ex.StackTrace);
            }

            return _olcuBirimleri;
        }

        /// <summary>
        /// Ölçü birimlerini tekrar veritabanından yükler (cache'i temizleyerek).
        /// </summary>
        public static void Reload()
        {
            _olcuBirimleri.Clear();
            GetAll();
        }

        /// <summary>
        /// ID'ye göre ölçü birimi getirir. Cache'e bakar, yoksa null döner.
        /// </summary>
        public static OlcuBirimleri? GetById(int? id)
        {
            if (!Yüklendi)
                GetAll();

            return _olcuBirimleri.FirstOrDefault(x => x.Id == id);
        }

        /// <summary>
        /// UI'da kullanılacak koleksiyonu verir (örneğin ComboBox için).
        /// </summary>
        public static ObservableCollection<OlcuBirimleri> GetForUI()
        {
            return GetAll(); // aynı koleksiyonu döner
        }
    }
}
