# Cari Kayıt Programı

[![GitHub Release](https://img.shields.io/github/v/release/Pentoxin/CariKayitProgrami?display_name=release&logo=Github&label=Release)](https://github.com/Pentoxin/CariKayitProgrami/releases/latest)

**Cari Kayıt Programı**, şirketlerin müşteri ve tedarikçileriyle olan finansal ilişkilerini takip etmelerini sağlayan, kullanıcı dostu bir **C# WPF** uygulamasıdır. Program sayesinde borç-alacak işlemleri, cari hesaplar ve gelecek ödemeler etkili bir şekilde yönetilebilir.

---

## 🚀 Özellikler

- ✅ **Cari Kayıt Yönetimi**  
  Müşteri ve tedarikçilerin tüm bilgilerini detaylı şekilde saklama ve güncelleme.

- 💰 **Borç / Alacak Takibi**  
  Her bir cari için gelir-gider hareketlerini kaydetme ve izleme.

- 📊 **Raporlama Desteği**  
  Cari hareketleri ve hesap durumları Excel formatında dışa aktarılabilir.

- 🔒 **MySQL Bağlantı Ayarları**  
  Harici bir MySQL sunucusuna bağlanma desteği. Ayarlar kullanıcı tarafından arayüz üzerinden kolayca değiştirilebilir.

- ⚙️ **Kurulum Yardımcısı ve Otomatik Yapılandırma**  
  MySQL Server yoksa kurulum sırasında indirilebilir ve yapılandırılır. Gerekli bağlantı ayarları otomatik tanımlanır.

---

## 🛠️ Kullanılan Teknolojiler

| Teknoloji     | Açıklama                                   |
|---------------|---------------------------------------------|
| **C# WPF**    | Masaüstü arayüz geliştirme                  |
| **MySQL**     | Veritabanı yönetimi                         |
| **Material Design** | Modern kullanıcı arayüz tasarımı     |
| **AutoUpdater.NET** | Otomatik güncelleme kontrolü         |
| **Inno Setup** | Kolay ve özelleştirilebilir kurulum sihirbazı |

---

## 💾 Kurulum

1. [En son sürümü](https://github.com/Pentoxin/CariKayitProgrami/releases/latest) indirerek `cari_kayit_programi_setup.exe` dosyasını çalıştırın.
2. Kurulum sırasında gerekli bileşenler (ör. .NET Runtime, MySQL Server) otomatik olarak indirilebilir.
3. Eğer farklı bir MySQL sunucusuna bağlanmak istiyorsanız, kurulum sırasında "MySQL Server kurulumu yapma" seçeneğini kaldırabilirsiniz.
4. Kurulum tamamlandıktan sonra bağlantı ayarlarını değiştirmek için program menüsünden **MySQL Ayarları** penceresini kullanabilirsiniz.

---

## 🧩 Geliştirici Notları

- Kurulumda `--skipmysql` parametresi ile MySQL bileşeninin kurulumu atlanabilir:
  
  ```bash
  cari_kayit_programi_setup.exe /skipmysql
