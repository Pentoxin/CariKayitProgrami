using MySql.Data.MySqlClient;
using System.Windows;

namespace Cari_kayıt_Programı
{
    class DatabaseManager
    {
        private readonly string _serverConnection;
        private readonly string _dbConnection;

        public void InitializeDatabase()
        {
            CreateDatabaseIfNotExists();
            CreateTablesIfNotExists();
        }

        public DatabaseManager()
        {
            try
            {
                _serverConnection = ConfigManager.ServerConnection;
                _dbConnection = ConfigManager.DbConnection;
            }
            catch (MySqlException ex)
            {
                LogManager.LogError(ex, className: "DatabaseManager", methodName: "DatabaseManager", stackTrace: ex.StackTrace);
                MessageBox.Show($"Veritabanı bağlantı hatası: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateDatabaseIfNotExists()
        {
            try
            {
                using var connection = new MySqlConnection(_serverConnection);
                connection.Open();
                var cmd = new MySqlCommand("CREATE DATABASE IF NOT EXISTS CariTakipDB CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;", connection);
                cmd.ExecuteNonQuery();
                LogManager.LogInformation("Veritabanı oluşturuldu.", className: "DatabaseManager", methodName: "CreateDatabaseIfNotExists");
            }
            catch (MySqlException ex)
            {
                LogManager.LogError(ex, className: "DatabaseManager", methodName: "CreateDatabaseIfNotExists", stackTrace: ex.StackTrace);
                throw;
            }
        }

        private void CreateTablesIfNotExists()
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnection);
                connection.Open();

                var script = @"
                CREATE TABLE IF NOT EXISTS Cariler (
                    CariKod VARCHAR(50) PRIMARY KEY NOT NULL UNIQUE,
                    Unvan VARCHAR(255) NOT NULL UNIQUE,
                    Adres VARCHAR(255),
                    Il VARCHAR(50),
                    Ilce VARCHAR(50),
                    Telefon1 VARCHAR(20),
                    Telefon2 VARCHAR(20),
                    PostaKodu VARCHAR(20),
                    UlkeKodu VARCHAR(20),
                    VergiDairesi VARCHAR(100),
                    VergiNo VARCHAR(50),
                    TCNo VARCHAR(20),
                    Tip VARCHAR(20),
                    Email VARCHAR(100),
                    Banka VARCHAR(100),
                    HesapNo VARCHAR(100)
                );

                CREATE TABLE IF NOT EXISTS CariHareketler (
                    HareketID INT AUTO_INCREMENT PRIMARY KEY,
                    CariKod VARCHAR(50) NOT NULL,
                    Tarih DATE NOT NULL,
                    Tip VARCHAR(20) NOT NULL,
                    Durum VARCHAR(20) NOT NULL,
                    EvrakNo VARCHAR(50) NOT NULL UNIQUE,
                    Aciklama VARCHAR(50) NOT NULL,
                    VadeTarihi VARCHAR(50) NOT NULL,
                    Borc DECIMAL(12,2) DEFAULT 0,
                    Alacak DECIMAL(12,2) DEFAULT 0,
                    FOREIGN KEY (CariKod) REFERENCES Cariler(CariKod)
                );

                CREATE TABLE IF NOT EXISTS OlcuBirimleri (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    BirimAdi VARCHAR(20) NOT NULL UNIQUE,
                    Aciklama TEXT
                );

                -- Varsayılan ölçü birimlerini ekle
                INSERT IGNORE INTO OlcuBirimleri (BirimAdi, Aciklama) VALUES
                ('Adet', 'Birim olarak sayılan ürünler'),
                ('Kg', 'Kilogram cinsinden ölçülen ürünler'),
                ('Koli', 'Birden fazla ürün içeren kutu'),
                ('Litre', 'Sıvı ürünler için'),
                ('Metre', 'Uzunluk ölçümü için'),
                ('Metrekare', 'Alan ölçümü için'),
                ('Paket', 'Birden çok ürün içeren ambalaj'),
                ('Çuval', 'Büyük hacimli ürün ambalajı'),
                ('Kutu', 'Küçük ambalaj birimi'),
                ('Düzine', '12’li gruplar hâlinde satılan ürünler');

                CREATE TABLE IF NOT EXISTS Stoklar (
                    StokKod VARCHAR(50) PRIMARY KEY NOT NULL UNIQUE,
                    StokAd VARCHAR(255) NOT NULL UNIQUE,
                    OlcuBirimi1Id INT NOT NULL,
                    OlcuBirimi2Id INT,
                    OlcuOranPay INT DEFAULT 1,
                    OlcuOranPayda INT DEFAULT 1,

                    KDVAlis DECIMAL(5,2) DEFAULT 0.00,
                    KDVSatis DECIMAL(5,2) DEFAULT 0.00,

                    FOREIGN KEY (OlcuBirimi1Id) REFERENCES OlcuBirimleri(Id),
                    FOREIGN KEY (OlcuBirimi2Id) REFERENCES OlcuBirimleri(Id)
                );

                CREATE TABLE IF NOT EXISTS Faturalar (
                    FaturaID INT AUTO_INCREMENT PRIMARY KEY,
                    Numara VARCHAR(50) NOT NULL UNIQUE,
                    CariKod VARCHAR(50) NOT NULL,
                    Tarih DATE NOT NULL,
                    VadeTarih DATE NOT NULL,
                    FaturaTipi ENUM('Alış', 'Satış') NOT NULL,
                    Tip VARCHAR(20) NOT NULL,
                    Aciklama VARCHAR(255),
                    ToplamTutar DECIMAL(12,2) NOT NULL,

                    KDV1Oran DECIMAL(5,2) DEFAULT 0.00,
                    KDV2Oran DECIMAL(5,2) DEFAULT 0.00,
                    KDV3Oran DECIMAL(5,2) DEFAULT 0.00,
                    KDV4Oran DECIMAL(5,2) DEFAULT 0.00,
                    KDV5Oran DECIMAL(5,2) DEFAULT 0.00,

                    Iskonto DECIMAL(5,2) DEFAULT 0.00,
                    FOREIGN KEY (CariKod) REFERENCES Cariler(CariKod)
                );

                CREATE TABLE IF NOT EXISTS FaturaDetay (
                    DetayID INT AUTO_INCREMENT PRIMARY KEY,
                    FaturaID INT NOT NULL,
                    StokKod VARCHAR(50) NOT NULL,
                    StokAd VARCHAR(255) NOT NULL,
                    Miktar INT NOT NULL,
                    BirimFiyat DECIMAL(10,2) NOT NULL,
                    Tutar DECIMAL(12,2) NOT NULL,
                    Iskonto DECIMAL(5,2) DEFAULT 0.00,
                    IadeMaliyet DECIMAL(10,2) DEFAULT 0.00,
                    FiiliTarih DATE,
                    OlcuBirimiId INT NOT NULL,
                    FOREIGN KEY (FaturaID) REFERENCES Faturalar(FaturaID),
                    FOREIGN KEY (StokKod) REFERENCES Stoklar(StokKod),
                    FOREIGN KEY (OlcuBirimiId) REFERENCES OlcuBirimleri(Id)
                );";

                var cmd = new MySqlCommand(script, connection);
                cmd.ExecuteNonQuery();
                LogManager.LogInformation("Veritabanı tabloları oluşturuldu.", className: "DatabaseManager", methodName: "CreateTablesIfNotExists");
            }
            catch (MySqlException ex)
            {
                LogManager.LogError(ex, className: "DatabaseManager", methodName: "CreateTablesIfNotExists", stackTrace: ex.StackTrace);
                throw;
            }

        }

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConfigManager.DbConnection);
        }
    }
}
