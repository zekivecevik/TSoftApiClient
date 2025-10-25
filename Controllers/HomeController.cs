using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.Services;

namespace TSoftApiClient.Controllers
{
    /// <summary>
    /// Ana sayfa controller'ı
    /// </summary>
    public class HomeController : Controller
    {
        private readonly TSoftApiService _tsoftService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            TSoftApiService tsoftService,
            ILogger<HomeController> logger)
        {
            _tsoftService = tsoftService;
            _logger = logger;
        }

        /// <summary>
        /// Dashboard ana sayfası
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                // İstatistikleri çek
                var productsTask = _tsoftService.GetProductsAsync(limit: 1000);
                var categoriesTask = _tsoftService.GetCategoriesAsync();
                var ordersTask = _tsoftService.GetOrdersAsync(limit: 1000);

                await Task.WhenAll(productsTask, categoriesTask, ordersTask);

                var products = await productsTask;
                var categories = await categoriesTask;
                var orders = await ordersTask;

                ViewBag.TotalProducts = products.Success ? products.Data?.Count ?? 0 : 0;
                ViewBag.TotalCategories = categories.Success ? categories.Data?.Count ?? 0 : 0;
                ViewBag.TotalOrders = orders.Success ? orders.Data?.Count ?? 0 : 0;

                // Son 5 ürünü göster
                ViewBag.RecentProducts = products.Success ? products.Data?.Take(5).ToList() : new();

                // ========== GERÇEK İSTATİSTİKLER ==========
                var orderList = orders.Success && orders.Data != null ? orders.Data : new List<Models.Order>();
                var productList = products.Success && products.Data != null ? products.Data : new List<Models.Product>();

                // Toplam Sipariş Değeri
                decimal totalRevenue = 0;
                foreach (var order in orderList)
                {
                    var priceStr = order.OrderTotalPrice ?? order.Total ?? order.TotalAmount ?? "0";
                    if (decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var price))
                    {
                        totalRevenue += price;
                    }
                }
                ViewBag.TotalRevenue = totalRevenue;

                // Ortalama Sipariş Değeri
                ViewBag.AverageOrderValue = orderList.Count > 0 ? totalRevenue / orderList.Count : 0;

                // Tamamlanan Sipariş Oranı (OrderStatusId == "3" tamamlandı demek)
                var completedOrders = orderList.Count(o => o.OrderStatusId == "3");
                ViewBag.CompletionRate = orderList.Count > 0 ? (decimal)completedOrders / orderList.Count * 100 : 0;

                // Son 7 günlük sipariş aktivitesi
                var last7Days = new List<(string Day, int Count)>();
                var today = DateTime.Now.Date;

                for (int i = 6; i >= 0; i--)
                {
                    var date = today.AddDays(-i);
                    var dayName = date.ToString("ddd", new System.Globalization.CultureInfo("tr-TR"));

                    var count = orderList.Count(o =>
                    {
                        if (DateTime.TryParse(o.OrderDate, out var orderDate))
                        {
                            return orderDate.Date == date;
                        }
                        return false;
                    });

                    last7Days.Add((dayName, count));
                }
                ViewBag.Last7Days = last7Days;

                // En çok satan 5 ürün (stok değerine göre sıralama)
                var topProducts = productList
                    .Where(p => !string.IsNullOrEmpty(p.ProductName))
                    .OrderByDescending(p =>
                    {
                        if (decimal.TryParse(p.SellingPrice ?? p.Price,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var price) &&
                            int.TryParse(p.Stock, out var stock))
                        {
                            return price * stock; // Stok değeri
                        }
                        return 0;
                    })
                    .Take(5)
                    .Select(p => new
                    {
                        Name = p.ProductName?.Length > 30 ? p.ProductName.Substring(0, 30) + "..." : p.ProductName,
                        Value = decimal.TryParse(p.SellingPrice ?? p.Price,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var price) &&
                            int.TryParse(p.Stock, out var stock) ? price * stock : 0
                    })
                    .ToList();
                ViewBag.TopProducts = topProducts;

                // Sipariş durum dağılımı (Pipeline)
                var totalOrderCount = orderList.Count;
                var statusCounts = new Dictionary<string, (int Count, decimal Percent)>
                {
                    { "Beklemede", (orderList.Count(o => o.OrderStatusId == "1"), 0) },
                    { "İşlemde", (orderList.Count(o => o.OrderStatusId == "2"), 0) },
                    { "Tamamlandı", (orderList.Count(o => o.OrderStatusId == "3"), 0) },
                    { "İptal", (orderList.Count(o => o.OrderStatusId == "4"), 0) },
                    { "Diğer", (orderList.Count(o => !new[] { "1", "2", "3", "4" }.Contains(o.OrderStatusId ?? "")), 0) }
                };

                // Yüzdeleri hesapla
                if (totalOrderCount > 0)
                {
                    statusCounts = statusCounts.ToDictionary(
                        kvp => kvp.Key,
                        kvp => (kvp.Value.Count, (decimal)kvp.Value.Count / totalOrderCount * 100)
                    );
                }

                ViewBag.StatusCounts = statusCounts;

                // Ortalama sipariş tamamlanma süresi
                var completedOrdersWithDates = orderList
                    .Where(o => o.OrderStatusId == "3" && !string.IsNullOrEmpty(o.OrderDate))
                    .ToList();

                if (completedOrdersWithDates.Count > 0)
                {
                    var averageDays = 0.0;
                    var validCount = 0;

                    foreach (var order in completedOrdersWithDates)
                    {
                        if (DateTime.TryParse(order.OrderDate, out var orderDate))
                        {
                            var daysSinceOrder = (DateTime.Now - orderDate).Days;
                            if (daysSinceOrder >= 0 && daysSinceOrder <= 365)
                            {
                                averageDays += daysSinceOrder;
                                validCount++;
                            }
                        }
                    }

                    ViewBag.AverageCycleDays = validCount > 0 ? (int)(averageDays / validCount) : 0;
                }
                else
                {
                    ViewBag.AverageCycleDays = 0;
                }

                // Toplam stok değeri
                decimal totalStockValue = 0;
                foreach (var product in productList)
                {
                    if (int.TryParse(product.Stock, out var stock) &&
                        decimal.TryParse(product.SellingPrice ?? product.Price,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var price))
                    {
                        totalStockValue += stock * price;
                    }
                }
                ViewBag.TotalStockValue = totalStockValue;

                // Aktif ürün oranı
                var activeProducts = productList.Count(p =>
                    p.IsActive == "1" ||
                    p.IsActive?.ToLower() == "true" ||
                    string.IsNullOrEmpty(p.IsActive)); // Varsayılan olarak aktif

                var activePercent = productList.Count > 0 ? (decimal)activeProducts / productList.Count * 100 : 0;

                _logger.LogInformation("🔍 Aktif Ürün Debug: Total={Total}, Active={Active}, Percent={Percent}",
                    productList.Count, activeProducts, activePercent);

                // İlk 3 ürünün IsActive değerini de logla
                foreach (var p in productList.Take(3))
                {
                    _logger.LogInformation("   Ürün: {Name}, IsActive={IsActive}", p.ProductName, p.IsActive ?? "NULL");
                }

                ViewBag.ActiveProductsPercent = activePercent;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard yüklenirken hata");
                ViewBag.Error = "Veriler yüklenirken bir hata oluştu.";

                // Hata durumunda boş değerler
                ViewBag.TotalRevenue = 0;
                ViewBag.AverageOrderValue = 0;
                ViewBag.CompletionRate = 0;
                ViewBag.Last7Days = new List<(string, int)>();
                ViewBag.TopProducts = new List<object>();
                ViewBag.StatusCounts = new Dictionary<string, (int, decimal)>();
                ViewBag.AverageCycleDays = 0;
                ViewBag.TotalStockValue = 0;
                ViewBag.ActiveProductsPercent = 0;

                return View();
            }
        }
    }
}