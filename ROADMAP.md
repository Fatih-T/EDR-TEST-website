# .NET Siber Güvenlik Laboratuvarı - Yerel Laboratuvar Kurulum Rehberi

Bu proje, Carbon Black ve diğer EDR çözümlerinin Windows IIS üzerindeki `w3wp.exe` (Worker Process) davranışlarını analiz etmek için tasarlanmıştır.

## 1. Mimari Yapı (Senaryo)
- **Sunucu A (Web Sunucusu):** IIS kurulu, Windows Server (veya Win 10/11), Carbon Black Agent yüklü.
- **Sunucu B (Veritabanı Sunucusu):** MSSQL Server yüklü.

## 2. Veritabanı Sunucusu (Sunucu B) Hazırlığı
1. MSSQL Server'da "SQL Server Authentication" (SQL Kullanıcı Kimlik Doğrulaması) özelliğinin açık olduğunu teyit edin.
2. `init_db.sql` scriptini çalıştırarak `ECommerceDB` veritabanını ve tablolarını oluşturun.
3. SQL Server Configuration Manager üzerinden **TCP/IP** protokolünü etkinleştirin ve varsayılan 1433 portunun dışarıdan erişilebilir olduğundan emin olun.
4. Windows Firewall üzerinden 1433 portuna izin verin.

## 3. Web Sunucusu (Sunucu A) Hazırlığı & IIS Kurulumu
1. **Kodun Derlenmesi:**
   ```powershell
   dotnet publish -c Release -o C:\inetpub\vulnerableshop
   ```
2. **IIS Yapılandırması:**
   - Yeni bir Web Sitesi oluşturun. (Port: 80 veya 8080)
   - Uygulama Havuzu (AppPool) ayarlarında **.NET CLR Version: No Managed Code** seçin.
   - **Identity:** Application Pool Identity olarak bırakabilirsiniz ancak veritabanına erişim yetkisi olduğundan emin olun.
3. **Yazma Yetkisi:** `wwwroot\uploads` klasörüne sağ tıklayıp Güvenlik sekmesinden `IIS AppPool\<SiteAdi>` kullanıcısına **Yazma** yetkisi verin.
4. **Connection String:** `appsettings.json` içindeki sunucu adresini Sunucu B'nin IP adresiyle güncelleyin.

## 4. Carbon Black Analiz Senaryoları

### Senaryo 1: Command Injection (RCE)
- **Modül:** `/Home/Ping`
- **Girdi:** `127.0.0.1 & whoami`
- **Analiz:** Carbon Black panelinde `w3wp.exe` sürecinin altında bir child process olarak `cmd.exe` ve `whoami.exe`'nin oluştuğunu görmelisiniz. Bu, "Unusual Child Process" veya "Suspicious Command Execution" olarak alarm üretmelidir.

### Senaryo 2: Insecure File Upload
- **Modül:** `/Home/UploadProductImage`
- **Girdi:** Bir .exe veya .aspx dosyası yükleyin.
- **Analiz:** Dosya sisteme yazıldığında EDR'ın "File Write" event'ini nasıl yakaladığını görün. Eğer yüklediğiniz dosyayı IIS üzerinden tetiklerseniz, w3wp.exe'nin bu dosyayı çalıştırma girişimini izleyin.

### Senaryo 3: SQL Injection
- **Modül:** `/Home/Index` (Arama Kutusu)
- **Girdi:** `' OR 1=1--`
- **Analiz:** Uygulamanın MSSQL sunucusuyla kurduğu bağlantıda geçen ham SQL sorgularını ve sızan verileri izleyin.

## 5. İpucu: w3wp.exe Davranış Kuralları
Normalde bir web servisi sadece gelen HTTP isteklerini cevaplar. Eğer bu servis:
- Bir shell (`cmd`, `powershell`) başlatıyorsa,
- Sistem araçlarını (`net`, `tasklist`, `whoami`) çalıştırıyorsa,
- Yerel diskte yürütülebilir dosyalar oluşturuyorsa,
Bu durum doğrudan bir güvenlik ihlali (Exploitation) belirtisidir ve Carbon Black üzerinde bu davranışlar en yüksek öncelikle takip edilmelidir.
