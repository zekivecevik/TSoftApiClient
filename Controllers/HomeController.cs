using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.Services;
using System.Globalization;

namespace TSoftApiClient.Controllers
{
    /// <summary>
    /// Enhanced Dashboard Controller - Comprehensive Stats & Analytics
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

        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("🚀 Loading Enhanced Dashboard...");

                // ========== PARALEL DATA FETCHING ==========
                var productsTask = _tsoftService.GetProductsAsync(limit: 1000);
                var categoriesTask = _tsoftService.GetCategoriesAsync();
                var ordersTask = _tsoftService.GetOrdersAsync(limit: 1000);
                var customersTask = _tsoftService.GetCustomersAsync(limit: 1000);

                await Task.WhenAll(productsTask, categoriesTask, ordersTask, customersTask);

                var products = await productsTask;
                var categories = await categoriesTask;
                var orders = await ordersTask;
                var customers = await customersTask;

                var productList = products.Success && products.Data != null ? products.Data : new List<Models.Product>();
                var orderList = orders.Success && orders.Data != null ? orders.Data : new List<Models.Order>();
                var customerList = customers.Success && customers.Data != null ? customers.Data : new List<Models.Customer>();
                var categoryList = categories.Success && categories.Data != null ? categories.Data : new List<Models.Category>();

                _logger.LogInformation($"✅ Data loaded: {productList.Count} products, {orderList.Count} orders, {customerList.Count} customers");

                // ========== 1. ÖZET KUTUCUKLAR (STAT CARDS) ==========

                var today = DateTime.Now.Date;
                var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
                var thisMonthStart = new DateTime(today.Year, today.Month, 1);

                // Sipariş İstatistikleri
                var ordersToday = orderList.Count(o => ParseDate(o.OrderDate) >= today);
                var ordersThisWeek = orderList.Count(o => ParseDate(o.OrderDate) >= thisWeekStart);
                var ordersThisMonth = orderList.Count(o => ParseDate(o.OrderDate) >= thisMonthStart);

                ViewBag.TotalOrders = orderList.Count;
                ViewBag.OrdersToday = ordersToday;
                ViewBag.OrdersThisWeek = ordersThisWeek;
                ViewBag.OrdersThisMonth = ordersThisMonth;

                // Ciro İstatistikleri
                var revenueToday = CalculateRevenue(orderList.Where(o => ParseDate(o.OrderDate) >= today));
                var revenueThisWeek = CalculateRevenue(orderList.Where(o => ParseDate(o.OrderDate) >= thisWeekStart));
                var revenueThisMonth = CalculateRevenue(orderList.Where(o => ParseDate(o.OrderDate) >= thisMonthStart));
                var totalRevenue = CalculateRevenue(orderList);

                ViewBag.RevenueToday = revenueToday;
                ViewBag.RevenueThisWeek = revenueThisWeek;
                ViewBag.RevenueThisMonth = revenueThisMonth;
                ViewBag.TotalRevenue = totalRevenue;

                // Müşteri İstatistikleri
                var newCustomersToday = customerList.Count(c => ParseDate(c.CreatedDate ?? c.DateCreated) >= today);
                var newCustomersThisWeek = customerList.Count(c => ParseDate(c.CreatedDate ?? c.DateCreated) >= thisWeekStart);
                var newCustomersThisMonth = customerList.Count(c => ParseDate(c.CreatedDate ?? c.DateCreated) >= thisMonthStart);

                ViewBag.TotalCustomers = customerList.Count;
                ViewBag.NewCustomersToday = newCustomersToday;
                ViewBag.NewCustomersThisWeek = newCustomersThisWeek;
                ViewBag.NewCustomersThisMonth = newCustomersThisMonth;

                // Ürün İstatistikleri
                var activeProducts = productList.Count(p => p.IsActive == "1" || string.IsNullOrEmpty(p.IsActive));
                var passiveProducts = productList.Count(p => p.IsActive == "0" || p.IsActive?.ToLower() == "false");
                var totalStockValue = CalculateTotalStockValue(productList);
                var totalStock = productList.Sum(p => int.TryParse(p.Stock, out var s) ? s : 0);

                ViewBag.TotalProducts = productList.Count;
                ViewBag.ActiveProducts = activeProducts;
                ViewBag.PassiveProducts = passiveProducts;
                ViewBag.TotalStock = totalStock;
                ViewBag.TotalStockValue = totalStockValue;

                // Kritik Stok (10'dan az)
                var lowStockProducts = productList.Where(p =>
                {
                    if (int.TryParse(p.Stock, out var stock))
                        return stock > 0 && stock <= 10;
                    return false;
                }).ToList();

                var outOfStockProducts = productList.Count(p => int.TryParse(p.Stock, out var s) && s == 0);

                ViewBag.LowStockCount = lowStockProducts.Count;
                ViewBag.OutOfStockCount = outOfStockProducts;
                ViewBag.LowStockProducts = lowStockProducts.Take(5).ToList();

                // Sipariş Durum İstatistikleri
                var pendingOrders = orderList.Count(o => o.OrderStatusId == "1");
                var processingOrders = orderList.Count(o => o.OrderStatusId == "2");
                var completedOrders = orderList.Count(o => o.OrderStatusId == "3");
                var cancelledOrders = orderList.Count(o => o.OrderStatusId == "4");

                ViewBag.PendingOrders = pendingOrders;
                ViewBag.ProcessingOrders = processingOrders;
                ViewBag.CompletedOrders = completedOrders;
                ViewBag.CancelledOrders = cancelledOrders;

                // ========== 2. GRAFİKLER / TRENDLER ==========

                // Son 30 Gün Ciro Grafiği
                var last30Days = new List<(DateTime Date, decimal Revenue, int OrderCount)>();
                for (int i = 29; i >= 0; i--)
                {
                    var date = today.AddDays(-i);
                    var dayOrders = orderList.Where(o => ParseDate(o.OrderDate) == date).ToList();
                    var dayRevenue = CalculateRevenue(dayOrders);
                    last30Days.Add((date, dayRevenue, dayOrders.Count));
                }
                ViewBag.Last30DaysChart = last30Days;

                // Son 7 Gün Detaylı
                var last7Days = last30Days.TakeLast(7).ToList();
                ViewBag.Last7DaysChart = last7Days;

                // Kategori Bazlı Satış Dağılımı (Top 5)
                var categoryStats = productList
                    .Where(p => !string.IsNullOrEmpty(p.DefaultCategoryCode))
                    .GroupBy(p => p.DefaultCategoryCode)
                    .Select(g => new
                    {
                        CategoryCode = g.Key,
                        CategoryName = categoryList.FirstOrDefault(c => c.CategoryCode == g.Key)?.CategoryName ?? g.Key,
                        ProductCount = g.Count(),
                        TotalValue = g.Sum(p =>
                        {
                            if (int.TryParse(p.Stock, out var stock) &&
                                decimal.TryParse(p.SellingPrice ?? p.Price, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                                return stock * price;
                            return 0;
                        })
                    })
                    .OrderByDescending(x => x.TotalValue)
                    .Take(5)
                    .ToList();
                ViewBag.CategoryStats = categoryStats;

                // Ödeme Tipi Dağılımı
                var paymentTypeStats = orderList
                    .Where(o => !string.IsNullOrEmpty(o.PaymentType))
                    .GroupBy(o => o.PaymentType)
                    .Select(g => new
                    {
                        PaymentType = g.Key,
                        Count = g.Count(),
                        TotalAmount = CalculateRevenue(g)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();
                ViewBag.PaymentTypeStats = paymentTypeStats;

                // ========== 3. SON İŞLEMLER / HIZLI TAKİP ==========

                // Son 5 Sipariş
                var recentOrders = orderList
                    .OrderByDescending(o => ParseDate(o.OrderDate))
                    .Take(5)
                    .ToList();
                ViewBag.RecentOrders = recentOrders;

                // Yeni Üyeler (Son 5)
                var recentCustomers = customerList
                    .OrderByDescending(c => ParseDate(c.CreatedDate ?? c.DateCreated))
                    .Take(5)
                    .ToList();
                ViewBag.RecentCustomers = recentCustomers;

                // Yeni Eklenen Ürünler (Son 5)
                var recentProducts = productList
                    .OrderByDescending(p => ParseDate(p.CreatedDate ?? p.DateCreated))
                    .Take(5)
                    .ToList();
                ViewBag.RecentProducts = recentProducts;

                // ========== 4. BİLDİRİM & UYARILAR ==========

                var alerts = new List<DashboardAlert>();

                // Kritik Stok Uyarısı
                if (lowStockProducts.Count > 0)
                {
                    alerts.Add(new DashboardAlert
                    {
                        Type = "warning",
                        Icon = "⚠️",
                        Title = "Kritik Stok",
                        Message = $"{lowStockProducts.Count} ürünün stoğu 10'un altında!",
                        Count = lowStockProducts.Count,
                        Link = "/Products?filter=lowstock"
                    });
                }

                // Stok Bitmiş Ürünler
                if (outOfStockProducts > 0)
                {
                    alerts.Add(new DashboardAlert
                    {
                        Type = "danger",
                        Icon = "🚫",
                        Title = "Tükenen Ürünler",
                        Message = $"{outOfStockProducts} ürünün stoğu tükenmiş!",
                        Count = outOfStockProducts,
                        Link = "/Products?filter=outofstock"
                    });
                }

                // Bekleyen Siparişler
                if (pendingOrders > 0)
                {
                    alerts.Add(new DashboardAlert
                    {
                        Type = "info",
                        Icon = "⏳",
                        Title = "Bekleyen Siparişler",
                        Message = $"{pendingOrders} sipariş onay bekliyor!",
                        Count = pendingOrders,
                        Link = "/Orders?status=1"
                    });
                }

                // İşlemde Olan Siparişler
                if (processingOrders > 0)
                {
                    alerts.Add(new DashboardAlert
                    {
                        Type = "primary",
                        Icon = "📦",
                        Title = "İşlemde Olan Siparişler",
                        Message = $"{processingOrders} sipariş işleme alındı",
                        Count = processingOrders,
                        Link = "/Orders?status=2"
                    });
                }

                // Pasif Ürünler
                if (passiveProducts > 0)
                {
                    alerts.Add(new DashboardAlert
                    {
                        Type = "secondary",
                        Icon = "💤",
                        Title = "Pasif Ürünler",
                        Message = $"{passiveProducts} ürün pasif durumda",
                        Count = passiveProducts,
                        Link = "/Products?status=passive"
                    });
                }

                ViewBag.Alerts = alerts;

                // ========== 5. PERFORMANS METRİKLERİ ==========

                // Ortalama Sipariş Değeri
                ViewBag.AverageOrderValue = orderList.Count > 0 ? totalRevenue / orderList.Count : 0;

                // Conversion Rate (Varsayılan %3 - gerçek veriyle değiştirilebilir)
                ViewBag.ConversionRate = 3.2m;

                // Tamamlanma Oranı
                ViewBag.CompletionRate = orderList.Count > 0 ? (decimal)completedOrders / orderList.Count * 100 : 0;

                // Ortalama Sipariş Tamamlanma Süresi
                var completedOrdersWithDates = orderList
                    .Where(o => o.OrderStatusId == "3" && !string.IsNullOrEmpty(o.OrderDate))
                    .ToList();

                if (completedOrdersWithDates.Count > 0)
                {
                    var totalDays = 0.0;
                    var validCount = 0;

                    foreach (var order in completedOrdersWithDates)
                    {
                        var orderDate = ParseDate(order.OrderDate);
                        if (orderDate != DateTime.MinValue)
                        {
                            var daysSinceOrder = (DateTime.Now - orderDate).Days;
                            if (daysSinceOrder >= 0 && daysSinceOrder <= 365)
                            {
                                totalDays += daysSinceOrder;
                                validCount++;
                            }
                        }
                    }

                    ViewBag.AverageCycleDays = validCount > 0 ? (int)(totalDays / validCount) : 0;
                }
                else
                {
                    ViewBag.AverageCycleDays = 0;
                }

                // ========== 6. ŞEHIR BAZLI ANALİZ ==========

                var cityStats = orderList
                    .Where(o => !string.IsNullOrEmpty(o.City ?? o.ShippingCity))
                    .GroupBy(o => o.City ?? o.ShippingCity)
                    .Select(g => new
                    {
                        City = g.Key,
                        OrderCount = g.Count(),
                        Revenue = CalculateRevenue(g)
                    })
                    .OrderByDescending(x => x.OrderCount)
                    .Take(10)
                    .ToList();
                ViewBag.CityStats = cityStats;

                _logger.LogInformation("✅ Enhanced Dashboard loaded successfully!");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Dashboard yüklenirken hata");
                ViewBag.Error = "Veriler yüklenirken bir hata oluştu.";

                // Hata durumunda boş değerler
                InitializeEmptyViewBag();
                return View();
            }
        }

        // ========== HELPER METHODS ==========

        private DateTime ParseDate(string? dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return DateTime.MinValue;

            if (DateTime.TryParse(dateStr, out var date))
                return date.Date;

            return DateTime.MinValue;
        }

        private decimal CalculateRevenue(IEnumerable<Models.Order> orders)
        {
            decimal total = 0;
            foreach (var order in orders)
            {
                var priceStr = order.OrderTotalPrice ?? order.Total ?? order.TotalAmount ?? "0";
                if (decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                {
                    total += price;
                }
            }
            return total;
        }

        private decimal CalculateTotalStockValue(List<Models.Product> products)
        {
            decimal total = 0;
            foreach (var product in products)
            {
                if (int.TryParse(product.Stock, out var stock) &&
                    decimal.TryParse(product.SellingPrice ?? product.Price, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                {
                    total += stock * price;
                }
            }
            return total;
        }

        private void InitializeEmptyViewBag()
        {
            ViewBag.TotalOrders = 0;
            ViewBag.OrdersToday = 0;
            ViewBag.OrdersThisWeek = 0;
            ViewBag.OrdersThisMonth = 0;
            ViewBag.TotalRevenue = 0;
            ViewBag.RevenueToday = 0;
            ViewBag.RevenueThisWeek = 0;
            ViewBag.RevenueThisMonth = 0;
            ViewBag.TotalCustomers = 0;
            ViewBag.NewCustomersToday = 0;
            ViewBag.NewCustomersThisWeek = 0;
            ViewBag.TotalProducts = 0;
            ViewBag.ActiveProducts = 0;
            ViewBag.LowStockCount = 0;
            ViewBag.OutOfStockCount = 0;
            ViewBag.Last7DaysChart = new List<(DateTime, decimal, int)>();
            ViewBag.Last30DaysChart = new List<(DateTime, decimal, int)>();
            ViewBag.CategoryStats = new List<object>();
            ViewBag.PaymentTypeStats = new List<object>();
            ViewBag.RecentOrders = new List<Models.Order>();
            ViewBag.RecentCustomers = new List<Models.Customer>();
            ViewBag.RecentProducts = new List<Models.Product>();
            ViewBag.LowStockProducts = new List<Models.Product>();
            ViewBag.Alerts = new List<DashboardAlert>();
            ViewBag.CityStats = new List<object>();
            ViewBag.AverageOrderValue = 0;
            ViewBag.ConversionRate = 0;
            ViewBag.CompletionRate = 0;
            ViewBag.AverageCycleDays = 0;
        }
    }

    // ========== DASHBOARD ALERT MODEL ==========
    public class DashboardAlert
    {
        public string Type { get; set; } = "info"; // primary, secondary, success, danger, warning, info
        public string Icon { get; set; } = "ℹ️";
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public int Count { get; set; }
        public string Link { get; set; } = "#";
    }
}