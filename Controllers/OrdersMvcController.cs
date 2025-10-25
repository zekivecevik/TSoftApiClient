using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.Services;

namespace TSoftApiClient.Controllers
{
    /// <summary>
    /// Sipariş sayfaları için MVC Controller
    /// </summary>
    public class OrdersMvcController : Controller
    {
        private readonly TSoftApiService _tsoftService;
        private readonly ILogger<OrdersMvcController> _logger;
        private static bool _detailsApiWorking = true; // Static flag to prevent repeated API calls

        public OrdersMvcController(
            TSoftApiService tsoftService,
            ILogger<OrdersMvcController> logger)
        {
            _tsoftService = tsoftService;
            _logger = logger;
        }

        /// <summary>
        /// Sipariş listesi sayfası
        /// </summary>
        [Route("/Orders")]
        [Route("/OrdersMvc")]
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("📦 Fetching orders...");

                var result = await _tsoftService.GetOrdersAsync(limit: 1000);

                _logger.LogInformation("📊 Orders API result: Success={Success}, DataCount={Count}",
                    result.Success,
                    result.Data?.Count ?? 0);

                if (!result.Success)
                {
                    var errorMsg = result.Message?.FirstOrDefault()?.Text?.FirstOrDefault() ?? "Bilinmeyen hata";
                    _logger.LogError("❌ Orders API failed: {Error}", errorMsg);
                    ViewBag.Error = $"Siparişler yüklenemedi: {errorMsg}";
                    return View("~/Views/Orders/Index.cshtml", new List<Models.Order>());
                }

                var orders = result.Data ?? new List<Models.Order>();
                _logger.LogInformation("✅ Orders loaded successfully: {Count} orders", orders.Count);

                // ⚡ SMART ORDER DETAILS FETCHING
                // Only try if we haven't confirmed API doesn't work
                if (_detailsApiWorking && orders.Count > 0)
                {
                    _logger.LogInformation("🔍 Attempting to fetch order details...");

                    // Test with first order to see if API works
                    var testOrder = orders.First();
                    if (int.TryParse(testOrder.OrderId, out var testOrderId))
                    {
                        var testResult = await _tsoftService.GetOrderDetailsByOrderIdAsync(testOrderId);

                        if (!testResult.Success)
                        {
                            // API doesn't work - disable future attempts
                            _detailsApiWorking = false;
                            _logger.LogWarning("⚠️ Order details API not available. Disabling future attempts.");
                            _logger.LogWarning("   Reason: {Error}",
                                testResult.Message?.FirstOrDefault()?.Text?.FirstOrDefault() ?? "Unknown");

                            ViewBag.Warning = "Sipariş detayları API'sine erişim yok. Ürün sayısı ve paketleme durumu görüntülenemiyor.";
                        }
                        else if (testResult.Data != null && testResult.Data.Count > 0)
                        {
                            // API works! Fetch details for all orders
                            _logger.LogInformation("✅ Order details API works! Fetching for all orders...");

                            var successCount = 0;
                            var failCount = 0;

                            // Paralel çek ama max 5 aynı anda (T-Soft API'si yavaş)
                            var semaphore = new System.Threading.SemaphoreSlim(5);

                            var detailTasks = orders.Select(async order =>
                            {
                                await semaphore.WaitAsync();
                                try
                                {
                                    if (int.TryParse(order.OrderId, out var orderId))
                                    {
                                        var detailsResult = await _tsoftService.GetOrderDetailsByOrderIdAsync(orderId);

                                        if (detailsResult.Success && detailsResult.Data != null)
                                        {
                                            order.OrderDetails = detailsResult.Data;
                                            order.ItemCount = detailsResult.Data.Count;

                                            // Şehir bilgisi yoksa detaydan al
                                            if (string.IsNullOrEmpty(order.City) && string.IsNullOrEmpty(order.ShippingCity))
                                            {
                                                var firstDetail = detailsResult.Data.FirstOrDefault();
                                                order.City = firstDetail?.City;
                                                order.ShippingCity = firstDetail?.ShippingCity;
                                            }

                                            // Paketleme durumu yoksa detaydan al
                                            if (string.IsNullOrEmpty(order.SupplyStatus))
                                            {
                                                var firstDetail = detailsResult.Data.FirstOrDefault();
                                                order.SupplyStatus = firstDetail?.SupplyStatus;
                                            }

                                            successCount++;
                                        }
                                        else
                                        {
                                            failCount++;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug(ex, "Detail fetch failed for order {OrderId}", order.OrderId);
                                    failCount++;
                                }
                                finally
                                {
                                    semaphore.Release();
                                }
                            });

                            await Task.WhenAll(detailTasks);

                            _logger.LogInformation("✅ Details fetched: {Success} success, {Fail} failed",
                                successCount, failCount);

                            if (successCount == 0 && failCount > 0)
                            {
                                ViewBag.Warning = "Sipariş detayları yüklenemedi. API yetki sorunu olabilir.";
                            }
                        }
                        else
                        {
                            _logger.LogInformation("ℹ️ First order has no details (might be empty order)");
                        }
                    }
                }
                else if (!_detailsApiWorking)
                {
                    _logger.LogInformation("ℹ️ Order details API is disabled (previous check failed)");
                    ViewBag.Warning = "Sipariş detayları gösterilemiyor (API yetki sorunu).";
                }

                return View("~/Views/Orders/Index.cshtml", orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exception while loading orders: {Message}", ex.Message);
                ViewBag.Error = $"Bir hata oluştu: {ex.Message}";
                return View("~/Views/Orders/Index.cshtml", new List<Models.Order>());
            }
        }

        /// <summary>
        /// Reset the details API flag (for testing)
        /// </summary>
        [Route("/Orders/ResetApiFlag")]
        public IActionResult ResetApiFlag()
        {
            _detailsApiWorking = true;
            TempData["Success"] = "API flag reset. Details fetching will be attempted again.";
            return RedirectToAction("Index");
        }
    }
}