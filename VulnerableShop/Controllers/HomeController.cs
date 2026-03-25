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
                // ZAFİYET: SQL Injection (Ham string birleştirme ve çoklu komut desteği)
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
            return View(products);
        }

        // Stored XSS İçin Ürün Detay & Yorum Sayfası
        public IActionResult Details(int id)
        {
            var product = new Product();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // 1. Ürün Bilgilerini Getir
                string sql = "SELECT * FROM Products WHERE ProductId = " + id;
                using (var cmd = new SqlCommand(sql, conn))
                {
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
                }

                // 2. Yorumları Getir (Stored XSS Tetikleme)
                // Connection string üzerinde MultipleActiveResultSets=True eklendiği için sorun çıkmaz.
                string commentSql = "SELECT * FROM Comments WHERE ProductId = " + id;
                using (var commentCmd = new SqlCommand(commentSql, conn))
                {
                    using (var reader = commentCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            product.Comments.Add(new Comment
                            {
                                UserNickname = reader["UserNickname"].ToString(),
                                CommentText = reader["CommentText"].ToString(),
                                CreatedAt = (DateTime)reader["CreatedAt"]
                            });
                        }
                    }
                }
            }
            return View(product);
        }

        [HttpPost]
        public IActionResult AddComment(int productId, string userNickname, string commentText)
        {
            // ZAFİYET: Stored XSS (Girdi sanitize edilmeden kaydediliyor)
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
            }
            return RedirectToAction("UploadProductImage");
        }

        public IActionResult TriggerFile(string fileName)
        {
            var filePath = Path.Combine(_env.WebRootPath, "uploads", fileName);
            if (System.IO.File.Exists(filePath))
            {
                try {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                    ViewBag.Message = fileName + " tetiklendi.";
                } catch (Exception ex) {
                    ViewBag.Error = "Hata: " + ex.Message;
                }
            }
            return View("UploadProductImage");
        }
    }
}
