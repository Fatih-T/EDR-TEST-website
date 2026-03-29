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

        public IActionResult Index(string query)
        {
            var products = new List<Product>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string sql = "SELECT * FROM Products";
                if (!string.IsNullOrEmpty(query))
                {
                    sql += " WHERE ProductName LIKE '%" + query + "%'";
                    ViewBag.SearchQuery = query;
                }
                var cmd = new SqlCommand(sql, conn);
                try {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Product {
                                ProductId = (int)reader["ProductId"],
                                ProductName = reader["ProductName"].ToString(),
                                Price = (decimal)reader["Price"],
                                Description = reader["Description"].ToString()
                            });
                        }
                    }
                } catch (Exception ex) { ViewBag.Error = "SQL Hatası: " + ex.Message; }
            }
            return View(products);
        }

        public IActionResult Details(int id)
        {
            var product = new Product();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM Products WHERE ProductId = " + id;
                using (var cmd = new SqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        product.ProductId = (int)reader["ProductId"];
                        product.ProductName = reader["ProductName"].ToString();
                        product.Description = reader["Description"].ToString();
                        product.Price = (decimal)reader["Price"];
                    }
                }
                string commentSql = "SELECT * FROM Comments WHERE ProductId = " + id;
                using (var commentCmd = new SqlCommand(commentSql, conn))
                using (var reader = commentCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        product.Comments.Add(new Comment {
                            UserNickname = reader["UserNickname"].ToString(),
                            CommentText = reader["CommentText"].ToString(),
                            CreatedAt = (DateTime)reader["CreatedAt"]
                        });
                    }
                }
            }
            return View(product);
        }

        [HttpPost]
        public IActionResult AddComment(int productId, string userNickname, string commentText)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string sql = "INSERT INTO Comments (ProductId, UserNickname, CommentText) VALUES (@pId, @nick, @text)";
                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@pId", productId);
                cmd.Parameters.AddWithValue("@nick", userNickname);
                cmd.Parameters.AddWithValue("@text", commentText);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Details", new { id = productId });
        }

        public IActionResult Ping(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return View();
            try {
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c ping " + ip) { RedirectStandardOutput = true, UseShellExecute = false };
                var process = Process.Start(psi);
                ViewBag.PingResult = process.StandardOutput.ReadToEnd();
            } catch (Exception ex) { ViewBag.PingResult = "Hata: " + ex.Message; }
            return View();
        }

        [HttpGet]
        public IActionResult UploadProductImage()
        {
            try {
                if (string.IsNullOrEmpty(_env.WebRootPath)) {
                     ViewBag.Error = "WebRootPath boş! IIS üzerinde Static Files özelliğinin açık olduğundan ve projenin doğru publish edildiğinden emin olun.";
                     return View();
                }
                var uploads = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
                var files = Directory.GetFiles(uploads).Select(Path.GetFileName).ToList();
                ViewBag.UploadedFiles = files;
            } catch (Exception ex) { ViewBag.Error = "Dizin listeleme hatası: " + ex.Message; }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadProductImage(IFormFile file)
        {
            if (file == null || file.Length == 0) {
                TempData["Error"] = "Lütfen bir dosya seçin.";
                return RedirectToAction("UploadProductImage");
            }

            try {
                if (string.IsNullOrEmpty(_env.WebRootPath)) {
                    throw new Exception("WebRootPath belirlenemedi. IIS yapılandırmasını kontrol edin.");
                }

                var uploads = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploads)) {
                    Directory.CreateDirectory(uploads);
                }

                var fileName = Path.GetFileName(file.FileName);
                var filePath = Path.Combine(uploads, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                TempData["Message"] = "Dosya başarıyla yüklendi: " + fileName;
            }
            catch (Exception ex) {
                TempData["Error"] = "Hata Detayı: " + ex.Message + " | Inner: " + ex.InnerException?.Message;
            }

            return RedirectToAction("UploadProductImage");
        }

        public IActionResult TriggerFile(string fileName)
        {
            try {
                var filePath = Path.Combine(_env.WebRootPath, "uploads", fileName);
                if (System.IO.File.Exists(filePath)) {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                    TempData["Message"] = fileName + " tetiklendi.";
                } else {
                    TempData["Error"] = "Dosya bulunamadı: " + filePath;
                }
            } catch (Exception ex) { TempData["Error"] = "Tetikleme Hatası: " + ex.Message; }
            return RedirectToAction("UploadProductImage");
        }
    }
}
