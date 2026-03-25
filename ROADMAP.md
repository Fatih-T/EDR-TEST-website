# .NET Siber Güvenlik Laboratuvarı - Gelişmiş RCE & EDR Analiz Rehberi

Bu proje, SQL Injection ve Dosya Yükleme üzerinden Remote Code Execution (RCE) senaryolarını test etmek için geliştirilmiştir.

## 1. SQL Injection Üzerinden Komut Çalıştırma (xp_cmdshell)
MSSQL üzerinden komut çalıştırmak için şu adımları izleyin:
1. **Hazırlık:** `init_db.sql` scriptini çalıştırarak `xp_cmdshell` özelliğini aktif edin.
2. **Saldırı:** Arama kutusuna şu komutu girin:
   `'; EXEC xp_cmdshell 'whoami'--`
3. **Analiz:** Carbon Black üzerinde `sqlservr.exe` (MSSQL) süreci altından bir `cmd.exe` oluştuğunu görmelisiniz.

## 2. Insecure File Upload & Trigger (Web Shell / RCE)
Dosya yükleyip sunucu tarafında tetiklemek için:
1. **Yükleme:** Bir `.aspx` web shell veya bir `.exe` dosyası yükleyin.
2. **Tetikleme:**
   - **Tetikle (Process.Start):** Sunucu tarafında `Process.Start()` metoduyla dosyayı çalıştırır. (w3wp.exe -> child process)
   - **Web Erişimi:** Dosyaya tarayıcı üzerinden direkt erişir. (`.aspx` web shell'ler için idealdir)
3. **IIS İzni:** IIS Manager üzerinden `uploads` klasörüne sağ tıklayıp "Handler Mappings" kısmından `Execute` izni vermeniz gerekebilir.

## 3. IIS & Yazma Yetkileri
- **Handler Mappings:** Yüklediğiniz `.aspx` dosyalarının çalışması için IIS üzerinde "ASP.NET" modülünün kurulu ve aktif olduğundan emin olun.
- **Dizin İzinleri:** `wwwroot/uploads` dizinine `IIS AppPool\<SiteAdi>` kullanıcısı için "Full Control" veya en azından "Write/Execute" yetkisi verin.

## 4. Carbon Black Önemli Takip Noktaları
- **Process Lineage:** `w3wp.exe` -> `cmd.exe` veya `sqlservr.exe` -> `cmd.exe` ağaçlarını takip edin.
- **File Mod Events:** `w3wp.exe` tarafından `uploads` klasörüne yazılan dosyaları ve bu dosyaların sonraki süreçte çalıştırılma girişimlerini analiz edin.
