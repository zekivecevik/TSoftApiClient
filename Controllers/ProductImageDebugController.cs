using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.Services;
using System.Text.Json;

namespace TSoftApiClient.Controllers
{
    [Route("ProductImageDebug")]
    public class ProductImageDebugController : Controller
    {
        private readonly TSoftApiService _tsoftService;
        private readonly ILogger<ProductImageDebugController> _logger;

        public ProductImageDebugController(
            TSoftApiService tsoftService,
            ILogger<ProductImageDebugController> logger)
        {
            _tsoftService = tsoftService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var logs = new List<string>();

            try
            {
                logs.Add("🔍 PRODUCT IMAGE DEBUG - BAŞLANGIČ");
                logs.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                logs.Add("");

                // 1. İlk ürünü al
                logs.Add("📦 STEP 1: İlk ürünü getiriyoruz...");
                var productsResult = await _tsoftService.GetProductsAsync(limit: 1);

                if (!productsResult.Success || productsResult.Data == null || productsResult.Data.Count == 0)
                {
                    logs.Add("❌ HATA: Ürün bulunamadı!");
                    ViewBag.Logs = logs;
                    return View();
                }

                var firstProduct = productsResult.Data[0];
                logs.Add($"✅ Ürün bulundu: {firstProduct.ProductCode} - {firstProduct.ProductName}");
                logs.Add("");

                // 2. Ürün nesnesindeki görsel field'larını kontrol et
                logs.Add("📸 STEP 2: Ürün nesnesindeki görsel field'ları:");
                logs.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                logs.Add($"  ImageUrl: {firstProduct.ImageUrl ?? "NULL"}");
                logs.Add($"  ThumbnailUrl: {firstProduct.ThumbnailUrl ?? "NULL"}");
                logs.Add($"  Image: {firstProduct.Image ?? "NULL"}");
                logs.Add($"  HasImages: {firstProduct.HasImages ?? "NULL"}");
                logs.Add($"  Images Collection: {(firstProduct.Images?.Count > 0 ? $"{firstProduct.Images.Count} adet" : "NULL veya BOŞ")}");
                logs.Add("");

                // 3. GetProductImages API'sini test et
                logs.Add("🌐 STEP 3: GetProductImages API'sini test ediyoruz...");
                logs.Add($"  ProductCode: {firstProduct.ProductCode}");
                logs.Add("");

                var imagesResult = await _tsoftService.GetProductImagesAsync(firstProduct.ProductCode ?? "");

                logs.Add($"  API Success: {imagesResult.Success}");
                logs.Add($"  Data Count: {imagesResult.Data?.Count ?? 0}");
                logs.Add("");

                if (imagesResult.Success && imagesResult.Data != null && imagesResult.Data.Count > 0)
                {
                    logs.Add("✅ BAŞARILI! Görseller geldi!");
                    logs.Add("");
                    logs.Add($"📊 TOPLAM {imagesResult.Data.Count} GÖRSEL:");
                    logs.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                    foreach (var img in imagesResult.Data.Take(3))
                    {
                        logs.Add("");
                        logs.Add($"🖼️ Görsel #{imagesResult.Data.IndexOf(img) + 1}:");
                        logs.Add($"  ImageId: {img.ImageId ?? "NULL"}");
                        logs.Add($"  ImageUrl: {img.ImageUrl ?? "NULL"}");
                        logs.Add($"  ThumbnailUrl: {img.ThumbnailUrl ?? "NULL"}");
                        logs.Add($"  Image: {img.Image ?? "NULL"}");
                        logs.Add($"  Thumbnail: {img.Thumbnail ?? "NULL"}");
                        logs.Add($"  IsPrimary: {img.IsPrimary ?? "NULL"}");
                        logs.Add($"  IsMain: {img.IsMain ?? "NULL"}");
                        logs.Add($"  IsActive: {img.IsActive ?? "NULL"}");
                    }

                    logs.Add("");
                    logs.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    logs.Add("");
                    logs.Add("💡 ÇÖZÜM:");
                    logs.Add("  API görselleri döndürüyor!");
                    logs.Add("  Sorun: ProductsMvcController'da GetEnhancedProductsAsync kullanılmıyor olabilir");
                    logs.Add("  veya görseller yüklenirken hata oluyor.");
                    logs.Add("");
                    logs.Add("✅ YAPILACAKLAR:");
                    logs.Add("  1. ProductsMvcController'ın GetEnhancedProductsAsync kullandığından emin ol");
                    logs.Add("  2. Görsel URL'lerinin doğru formatta olduğunu kontrol et");
                    logs.Add("  3. CORS sorunları olabilir - tarayıcı konsoluna bak");

                    ViewBag.FirstImage = imagesResult.Data[0];
                }
                else if (imagesResult.Message != null && imagesResult.Message.Count > 0)
                {
                    logs.Add("❌ API HATASI:");
                    foreach (var msg in imagesResult.Message)
                    {
                        if (msg.Text != null)
                        {
                            foreach (var text in msg.Text)
                            {
                                logs.Add($"  {text}");
                            }
                        }
                    }
                    logs.Add("");
                    logs.Add("💡 NEDEN OLABILIR:");
                    logs.Add("  1. API yetki sorunu - görsel endpoint'i için yetki yok");
                    logs.Add("  2. Ürünün görseli yok");
                    logs.Add("  3. API endpoint yanlış");
                    logs.Add("");
                    logs.Add("✅ ÇÖZÜM:");
                    logs.Add("  T-Soft API dokümantasyonunu kontrol et");
                    logs.Add("  veya API sağlayıcısıyla iletişime geç");
                }
                else
                {
                    logs.Add("⚠️ API çalıştı ama veri yok");
                    logs.Add("  Bu ürünün görseli olmayabilir.");
                    logs.Add("");
                    logs.Add("💡 TEST İÇİN:");
                    logs.Add("  Başka bir ürün kodunu dene:");
                    logs.Add("  /ProductImageDebug/TestProduct?code=ÜRÜN_KODU");
                }

                // 4. Full product JSON
                logs.Add("");
                logs.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                logs.Add("📄 TAM ÜRÜN JSON (ilk 1000 karakter):");
                logs.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                var json = JsonSerializer.Serialize(firstProduct, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                ViewBag.ProductJson = json.Length > 1000 ? json.Substring(0, 1000) + "..." : json;

                ViewBag.Logs = logs;
            }
            catch (Exception ex)
            {
                logs.Add($"💥 EXCEPTION: {ex.Message}");
                logs.Add($"Stack: {ex.StackTrace}");
                ViewBag.Logs = logs;
            }

            return View();
        }

        [Route("ProductImageDebug/TestProduct")]
        public async Task<IActionResult> TestProduct(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return Content("❌ Ürün kodu gerekli! Kullanım: /ProductImageDebug/TestProduct?code=PROD001");
            }

            var imagesResult = await _tsoftService.GetProductImagesAsync(code);

            var result = new
            {
                productCode = code,
                success = imagesResult.Success,
                imageCount = imagesResult.Data?.Count ?? 0,
                images = imagesResult.Data,
                message = imagesResult.Message
            };

            return Json(result);
        }
    }
}