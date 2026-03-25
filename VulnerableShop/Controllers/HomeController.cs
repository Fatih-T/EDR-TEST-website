using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VulnerableShop.Models;
using System.Diagnostics;
using System.IO;
using System.Data;

namespace VulnerableShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _env;

        public HomeController(IConfiguration configuration, IWebHostEnvironment env)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _env = env;
        }

        // 1. Gelişmiş SQL Injection (Stacked Queries Desteği ile)
        // Hedef: '; EXEC xp_cmdshell 'whoami'-- gibi komutların çalışabilmesi
        public IActionResult Index(string query)
        {
            var products = new List<Product>();
            if (!string.IsNullOrEmpty(query))
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    // ZAFİYET: SQL Injection (Ham string birleştirme ve çoklu komut desteği)
                    // NOT: MSSQL'de stacked query için CommandType.Text yeterlidir.
                    string sql = "SELECT * FROM Products WHERE ProductName LIKE '%" + query + "%'";
                    var cmd = new SqlCommand(sql, conn);

                    try {
                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                products.Add(new Product
                                {
                                    ProductId = (int)reader["ProductId"],
                                    ProductName = reader["ProductName"].ToString(),
                                    Price = (decimal)reader["Price"],
                                    Description = reader["Description"].ToString()
                                });
                            }
                        }
                    } catch (Exception ex) {
                        ViewBag.Error = "SQL Hatası: " + ex.Message;
                    }
                }
                ViewBag.SearchQuery = query;
            }
            return View(products);
        }

        // 2. Command Injection (RCE - w3wp.exe Child Process Test)
        public IActionResult Ping(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return View();

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "cmd.exe";
            psi.Arguments = "/c ping " + ip;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;

            var process = Process.Start(psi);
            string result = process.StandardOutput.ReadToEnd();
            ViewBag.PingResult = result;

            return View();
        }

        // 3. Insecure File Upload & Trigger (Web Shell / RCE Test)
        public IActionResult UploadProductImage()
        {
            var uploads = Path.Combine(_env.WebRootPath, "uploads");
            if (Directory.Exists(uploads))
            {
                var files = Directory.GetFiles(uploads).Select(Path.GetFileName).ToList();
                ViewBag.UploadedFiles = files;
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadProductImage(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                var uploads = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                var filePath = Path.Combine(uploads, file.FileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                ViewBag.Message = "Dosya başarıyla yüklendi: " + file.FileName;
            }
            return RedirectToAction("UploadProductImage");
        }

        // Zararlı dosyayı tetikleme simülasyonu (IIS üzerinde .aspx vb. dosyaları çalıştırmak için)
        public IActionResult TriggerFile(string fileName)
        {
            // ZAFİYET: Path Traversal ve Dosya Çalıştırma (RCE)
            // NOT: IIS üzerinde .aspx veya .exe'nin w3wp.exe altında tetiklenmesi için direkt URL erişimi de kullanılabilir.
            // Bu metod sunucu tarafında bu dosyanın Process.Start ile başlatılmasını sağlar.
            var filePath = Path.Combine(_env.WebRootPath, "uploads", fileName);

            if (System.IO.File.Exists(filePath))
            {
                try {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                    ViewBag.Message = fileName + " dosyası sunucu tarafında tetiklendi (RCE).";
                } catch (Exception ex) {
                    ViewBag.Error = "Dosya tetikleme hatası: " + ex.Message;
                }
            }
            return View("UploadProductImage");
        }
    }
}
