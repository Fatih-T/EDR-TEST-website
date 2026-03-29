# Detaylı Kurulum ve Konfigürasyon Rehberi

Bu rehber, VulnerableShop uygulamasını iki ayrı sunucuda (IIS ve MSSQL) nasıl kuracağınızı adım adım anlatır.

---

## 1. SQL Server Sunucusu (Veritabanı Sunucusu) Yapılandırması

### A. SQL Server Authentication (Karma Mod)
1. **SSMS** üzerinden **Mixed Mode** aktif edin.
2. Sunucuyu yeniden başlatın.

### B. TCP/IP ve Portlar
1. **SQL Configuration Manager** üzerinden **TCP/IP** protokolünü etkinleştirin.
2. Portun **1433** olduğundan emin olun.
3. **Firewall** üzerinden 1433 portuna gelen bağlantılara izin verin.

### C. Veritabanı ve xp_cmdshell
1. `init_db.sql` scriptini çalıştırın. Bu script:
   - `ECommerceDB` veritabanını oluşturur.
   - `xp_cmdshell` özelliğini aktif eder.
   - `Users`, `Products` ve `Comments` tablolarını oluşturur.

---

## 2. IIS Web Sunucusu Yapılandırması

### A. Gerekli Bileşenler
1. **.NET 8.0 Hosting Bundle**'ı kurun.
2. IIS altındaki **Application Development** özelliklerini aktif edin.

### B. Yayınlama (Publish)
1. `dotnet publish -c Release -o C:\inetpub\vulnerableshop` komutuyla dosyaları oluşturun.

### C. IIS Site Ayarları
1. **Application Pool:** **.NET CLR Version: No Managed Code** seçin.
2. **Handler Mappings:** Web shell (.aspx) çalıştırabilmek için `uploads` klasöründe "Execute" izninin açık olduğundan emin olun.

### D. Klasör İzinleri
1. `wwwroot\uploads` klasörüne `IIS AppPool\<SiteAdi>` kullanıcısı için **Write** ve **Execute** yetkisi verin.

---

## 3. Test Edilecek Zafiyetler & EDR Analizi

1. **Broken Auth & SQLi Login:** `admin' --` ile giriş yaparak SQLi üzerinden authentication bypass'ı test edin.
2. **Stored XSS:** Ürün detaylarına `<script>alert(1)</script>` içeren yorumlar ekleyerek kalıcı XSS saldırısını gözlemleyin.
3. **RCE (via SQLi):** Arama kutusunda `xp_cmdshell` tetikleyerek `sqlservr.exe` altındaki süreçleri izleyin.
4. **RCE (via File Upload):** Yüklenen dosyaları "Tetikle" butonu veya direkt web erişimi ile çalıştırarak `w3wp.exe` altındaki süreçleri izleyin.

---

## 5. Önemli: Dosya Yükleme (HTTP 500) Hatası Çözümü

Eğer dosya yüklerken "HTTP 500" hatası alıyorsanız, bunun en yaygın sebebi IIS kullanıcısının klasöre yazma yetkisi olmamasıdır.

**Çözüm Adımları:**
1. Dosya gezgininde `C:\inetpub\vulnerableshop\wwwroot` klasörüne gidin.
2. `uploads` klasörü yoksa manuel oluşturun.
3. Klasöre sağ tıklayın -> **Properties** -> **Security** sekmesi.
4. **Edit** -> **Add** butonuna basın.
5. Kutuya `IIS AppPool\<SitenizinAdi>` (Örn: `IIS AppPool\VulnerableShop`) yazın ve **Check Names** diyerek ekleyin.
6. Bu kullanıcıya **"Modify"** (Değiştirme) ve **"Write"** (Yazma) yetkisi verin.
7. Uygulamanın `appsettings.json` dosyasındaki bağlantı dizesinin (Connection String) de MSSQL tarafında yetkili bir kullanıcıya (`sa` gibi) sahip olduğundan emin olun.
