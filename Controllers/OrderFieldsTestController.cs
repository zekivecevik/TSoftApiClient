using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.Services;
using TSoftApiClient.Models;
using System.Text.Json;

namespace TSoftApiClient.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderFieldsTestController : ControllerBase
    {
        private readonly TSoftApiService _tsoftService;
        private readonly ILogger<OrderFieldsTestController> _logger;

        public OrderFieldsTestController(
            TSoftApiService tsoftService,
            ILogger<OrderFieldsTestController> logger)
        {
            _tsoftService = tsoftService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                _logger.LogInformation("🔍 OrderFieldsTest: Starting analysis...");

                // Get first order to analyze
                var ordersResult = await _tsoftService.GetOrdersAsync(limit: 1);

                _logger.LogInformation("📊 OrderFieldsTest: Orders result - Success: {Success}", ordersResult.Success);

                if (!ordersResult.Success || ordersResult.Data == null || ordersResult.Data.Count == 0)
                {
                    _logger.LogWarning("⚠️ OrderFieldsTest: No orders found");
                    return Ok(new
                    {
                        success = false,
                        message = "Failed to get orders",
                        order = (object?)null
                    });
                }

                var firstOrder = ordersResult.Data[0];
                _logger.LogInformation("✅ OrderFieldsTest: Got order {OrderCode}", firstOrder.OrderCode);

                // Analyze critical fields
                var analysis = new
                {
                    success = true,
                    message = "Order retrieved successfully",

                    // Critical fields analysis
                    fields = new
                    {
                        itemCount = new { value = firstOrder.ItemCount, exists = firstOrder.ItemCount > 0 },
                        city = new { value = firstOrder.City, exists = !string.IsNullOrEmpty(firstOrder.City) },
                        shippingCity = new { value = firstOrder.ShippingCity, exists = !string.IsNullOrEmpty(firstOrder.ShippingCity) },
                        supplyStatus = new { value = firstOrder.SupplyStatus, exists = !string.IsNullOrEmpty(firstOrder.SupplyStatus) },

                        // Additional fields
                        orderId = firstOrder.OrderId,
                        orderCode = firstOrder.OrderCode,
                        customerName = firstOrder.CustomerName,
                        orderDate = firstOrder.OrderDate,
                        total = firstOrder.OrderTotalPrice ?? firstOrder.Total,
                        orderStatusId = firstOrder.OrderStatusId,
                        orderStatus = firstOrder.OrderStatus,
                        paymentType = firstOrder.PaymentType,
                        cargo = firstOrder.Cargo
                    },

                    // Full order object
                    fullOrder = firstOrder,

                    // Recommendation
                    recommendation = GetRecommendation(firstOrder)
                };

                _logger.LogInformation("✅ OrderFieldsTest: Analysis complete");
                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 OrderFieldsTest: Exception occurred");
                return Ok(new
                {
                    success = false,
                    message = $"Exception: {ex.Message}",
                    stack = ex.StackTrace
                });
            }
        }

        private object GetRecommendation(Order order)
        {
            var hasItemCount = order.ItemCount > 0;
            var hasCity = !string.IsNullOrEmpty(order.City) || !string.IsNullOrEmpty(order.ShippingCity);
            var hasSupplyStatus = !string.IsNullOrEmpty(order.SupplyStatus);

            if (hasItemCount && hasCity && hasSupplyStatus)
            {
                return new
                {
                    status = "ALL_GOOD",
                    message = "All required fields exist in main order! No need for order details API.",
                    action = "None - just use the existing fields",
                    hasItemCount = true,
                    hasCity = true,
                    hasSupplyStatus = true
                };
            }
            else
            {
                var missing = new List<string>();
                if (!hasItemCount) missing.Add("ItemCount");
                if (!hasCity) missing.Add("City/ShippingCity");
                if (!hasSupplyStatus) missing.Add("SupplyStatus");

                return new
                {
                    status = "MISSING_FIELDS",
                    message = $"Missing: {string.Join(", ", missing)}",
                    action = "Copy the fullOrder JSON and send to Claude to find alternative field names",
                    missingFields = missing,
                    hasItemCount = hasItemCount,
                    hasCity = hasCity,
                    hasSupplyStatus = hasSupplyStatus
                };
            }
        }
    }
}