# Detaylı Kurulum ve Konfigürasyon Rehberi

Bu rehber, VulnerableShop uygulamasını iki ayrı sunucuda (IIS ve MSSQL) nasıl kuracağınızı adım adım anlatır.

---

## 1. SQL Server Sunucusu (Veritabanı Sunucusu) Yapılandırması

SQL Server'ın dışarıdan gelen bağlantıları kabul etmesi ve komut çalıştırması için şu adımları uygulayın:

### A. SQL Server Authentication (Karma Mod)
1. **SQL Server Management Studio (SSMS)** açın.
2. Sunucuya sağ tıklayın -> **Properties** -> **Security** sekmesine gelin.
3. **"SQL Server and Windows Authentication mode"** (Mixed Mode) seçeneğini işaretleyin.
4. Sunucuyu yeniden başlatın.

### B. TCP/IP Protokolünü Etkinleştirme
1. **SQL Server Configuration Manager**'ı açın.
2. **SQL Server Network Configuration** -> **Protocols for MSSQLSERVER** (veya SQLEXPRESS) yoluna gidin.
3. **TCP/IP** durumunu **Enabled** (Etkin) yapın.
4. TCP/IP üzerine sağ tıklayıp **Properties** -> **IP Addresses** sekmesinde **IPAll** kısmındaki **TCP Port**'un **1433** olduğundan emin olun.

### C. Firewall (Güvenlik Duvarı) İzni
1. **Windows Firewall with Advanced Security** açın.
2. **Inbound Rules** (Gelen Kurallar) -> **New Rule** seçin.
3. **Port** -> **TCP** -> **Specific local ports: 1433** yazın ve izin verin.

### D. Veritabanı ve xp_cmdshell Kurulumu
1. `init_db.sql` scriptini SSMS üzerinde çalıştırın. Bu script veritabanını oluşturur ve **xp_cmdshell**'i aktif eder.

---

## 2. IIS Web Sunucusu (Web Sunucusu) Yapılandırması

IIS'in .NET Core uygulamalarını çalıştırabilmesi için şu bileşenler gereklidir:

### A. Gerekli Bileşenlerin Kurulumu
1. **.NET 8.0 Hosting Bundle:** [Microsoft sitesinden](https://dotnet.microsoft.com/download/dotnet/8.0) "Hosting Bundle" indirin ve kurun. Bu paket IIS için **ASP.NET Core Module (ANCM)** ekler.
2. **IIS Özellikleri:** Server Manager -> Add Roles and Features üzerinden Web Server (IIS) altında **"Application Development"** kısmındaki tüm **.NET Extensibility** ve **ASP.NET** özelliklerini açın.

### B. Uygulamanın Derlenmesi ve Yayınlanması (Publish)
1. Proje klasöründe şu komutu çalıştırarak yayına hazır dosyaları oluşturun:
   ```powershell
   dotnet publish -c Release -o C:\inetpub\vulnerableshop
   ```

### C. IIS Site ve AppPool Ayarları
1. **IIS Manager**'ı açın.
2. **Application Pools** -> Uygulamanızın AppPool'una sağ tıklayın -> **Basic Settings**.
3. **.NET CLR Version:** **No Managed Code** seçin.
4. **Sites** -> **Add Website**:
   - **Physical Path:** `C:\inetpub\vulnerableshop`
   - **Binding:** Port 80 veya 8080.

### D. Klasör ve Dosya Yetkileri (KRİTİK)
1. `C:\inetpub\vulnerableshop\wwwroot\uploads` klasörüne sağ tıklayın -> **Properties** -> **Security**.
2. **Edit** -> **Add** -> **"IIS AppPool\<SitenizinAdi>"** yazıp ekleyin.
3. Bu kullanıcıya **"Full Control"** (veya en azından Read, Write, Execute) yetkisi verin. Bu, web shell tetiklemek için gereklidir.

---

## 3. Bağlantı Ayarları (appsettings.json)

`C:\inetpub\vulnerableshop\appsettings.json` dosyasını açın ve bağlantı dizesini güncelleyin:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=SQL_SUNUCU_IP_ADRESI;Database=ECommerceDB;User Id=sa;Password=SQL_SIFRENIZ;TrustServerCertificate=True;"
}
```

---

## 4. Carbon Black & EDR Testi İçin Son Notlar

1. **Handler Mappings:** IIS üzerinde `.aspx` dosyalarının çalışması için `uploads` klasöründe "Execute" izninin aktif olduğundan emin olun. (IIS Manager -> Site -> Request Filtering -> Edit Feature Settings -> **Allow unlisted file name extensions** ve **Allow unlisted verbs** işaretli olmalı).
2. **w3wp.exe İzleme:** Carbon Black üzerinden `w3wp.exe` sürecini "Live Response" veya "Event Search" ile takip edin. Uygulama üzerinden "Tetikle" butonuna bastığınızda oluşan alt süreçleri analiz edin.

Bu yapılandırma ile tam kapsamlı bir siber güvenlik laboratuvarına sahip olacaksınız.
