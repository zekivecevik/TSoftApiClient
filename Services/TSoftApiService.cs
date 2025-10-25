using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TSoftApiClient.Models;

namespace TSoftApiClient.Services
{
    /// <summary>
    /// T-Soft REST API Client - Supports both V3 and REST1 APIs
    /// </summary>
    public class TSoftApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _token;
        private readonly string _baseUrl;
        private readonly ILogger<TSoftApiService> _logger;
        private readonly bool _debug;

        public TSoftApiService(HttpClient httpClient, IConfiguration config, ILogger<TSoftApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _token = config["TSoftApi:Token"]
                ?? throw new InvalidOperationException("T-Soft API Token is not configured");

            _baseUrl = (config["TSoftApi:BaseUrl"] ?? "https://wawtesettur.tsoft.biz/rest1").TrimEnd('/');
            _debug = config["TSoftApi:Debug"] == "true";
        }

        // ========== REST1 API (Form-URLEncoded POST) ==========

        /// <summary>
        /// REST1 API POST request with form-urlencoded data
        /// </summary>
        private async Task<(bool success, string body, int status)> Rest1PostAsync(
            string path,
            Dictionary<string, string> formData,
            CancellationToken ct = default)
        {
            try
            {
                var url = _baseUrl + (path.StartsWith('/') ? "" : "/") + path;

                // Add token to form data
                var allData = new Dictionary<string, string>(formData)
                {
                    ["token"] = _token
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, url);

                // REST1 uses multiple auth methods
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                req.Headers.Add("X-Auth-Token", _token);
                req.Headers.Accept.ParseAdd("application/json, text/plain, */*");

                // Form-urlencoded content
                req.Content = new FormUrlEncodedContent(allData);
                req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                    "application/x-www-form-urlencoded")
                { CharSet = "UTF-8" };

                if (_debug)
                {
                    var formStr = string.Join("&", allData.Select(kv => $"{kv.Key}={kv.Value}"));
                    _logger.LogDebug("🟢 POST {Url} Form: {Form}", url, formStr);
                }

                using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                if (_debug)
                {
                    _logger.LogDebug("📊 Response: {Status} {Body}",
                        (int)resp.StatusCode,
                        body.Length > 500 ? body.Substring(0, 500) + "..." : body);
                }

                return (resp.IsSuccessStatusCode, body, (int)resp.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "REST1 POST failed: {Path}", path);
                return (false, "", 0);
            }
        }

        // ========== V3 API (JSON GET/POST) ==========

        /// <summary>
        /// V3 API GET request with query params
        /// </summary>
        private async Task<(bool success, string body, int status)> V3GetAsync(
            string path,
            Dictionary<string, string>? queryParams = null,
            CancellationToken ct = default)
        {
            try
            {
                var url = _baseUrl + (path.StartsWith('/') ? "" : "/") + path;

                if (queryParams is { Count: > 0 })
                {
                    var qs = string.Join("&", queryParams.Select(kvp =>
                        $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                    url += "?" + qs;
                }

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                req.Headers.Accept.ParseAdd("application/json");

                if (_debug) _logger.LogDebug("🔵 GET {Url}", url);

                using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                if (_debug) _logger.LogDebug("📊 Response: {Status}", (int)resp.StatusCode);

                return (resp.IsSuccessStatusCode, body, (int)resp.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "V3 GET failed: {Path}", path);
                return (false, "", 0);
            }
        }

        /// <summary>
        /// V3 API POST request with JSON body
        /// </summary>
        private async Task<(bool success, string body, int status)> V3PostAsync(
            string path,
            object jsonBody,
            CancellationToken ct = default)
        {
            try
            {
                var url = _baseUrl + (path.StartsWith('/') ? "" : "/") + path;

                var json = JsonSerializer.Serialize(jsonBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                using var req = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                req.Headers.Accept.ParseAdd("application/json");

                if (_debug) _logger.LogDebug("🟢 POST {Url} JSON: {Json}", url, json);

                using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                if (_debug) _logger.LogDebug("📊 Response: {Status}", (int)resp.StatusCode);

                return (resp.IsSuccessStatusCode, body, (int)resp.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "V3 POST failed: {Path}", path);
                return (false, "", 0);
            }
        }

        // ========== PARSING ==========

        private TSoftApiResponse<T> ParseResponse<T>(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return new TSoftApiResponse<T>
                {
                    Success = false,
                    Message = new() { new() { Text = new() { "Empty response" } } }
                };
            }

            // Try wrapped format
            try
            {
                var wrapped = JsonSerializer.Deserialize<TSoftApiResponse<T>>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (wrapped != null) return wrapped;
            }
            catch { /* ignore */ }

            // Try direct deserialization
            try
            {
                var direct = JsonSerializer.Deserialize<T>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return new TSoftApiResponse<T> { Success = true, Data = direct };
            }
            catch
            {
                return new TSoftApiResponse<T>
                {
                    Success = false,
                    Message = new() { new() { Text = new() { "Failed to parse response" } } }
                };
            }
        }

        // ========== PRODUCT OPERATIONS ==========

        /// <summary>
        /// Get products - tries REST1 first, then V3
        /// </summary>
        public async Task<TSoftApiResponse<List<Product>>> GetProductsAsync(
            int limit = 50,
            int page = 1,
            string? search = null,
            Dictionary<string, string>? filters = null,
            CancellationToken ct = default)
        {
            // REST1 API format
            var form = new Dictionary<string, string>
            {
                ["limit"] = limit.ToString()
            };

            if (filters != null)
            {
                foreach (var kv in filters)
                {
                    form[kv.Key] = kv.Value;
                }
            }

            // Try REST1 endpoints first
            var rest1Endpoints = new[]
            {
                "/product/getProducts",
                "/product/get",
                "/products/get"
            };

            foreach (var endpoint in rest1Endpoints)
            {
                var (success, body, _) = await Rest1PostAsync(endpoint, form, ct);

                if (success)
                {
                    _logger.LogInformation("✅ REST1 endpoint succeeded: {Endpoint}", endpoint);
                    return ParseResponse<List<Product>>(body);
                }

                _logger.LogDebug("⚠️ REST1 endpoint failed: {Endpoint}", endpoint);
            }

            // Try V3 API as fallback
            var queryParams = new Dictionary<string, string>
            {
                ["page"] = page.ToString(),
                ["limit"] = limit.ToString()
            };

            if (!string.IsNullOrWhiteSpace(search)) queryParams["search"] = search;
            if (filters != null) foreach (var kv in filters) queryParams[kv.Key] = kv.Value;

            var v3Endpoints = new[]
            {
                "/catalog/products",
                "/api/v3/catalog/products"
            };

            foreach (var endpoint in v3Endpoints)
            {
                var (success, body, _) = await V3GetAsync(endpoint, queryParams, ct);

                if (success)
                {
                    _logger.LogInformation("✅ V3 endpoint succeeded: {Endpoint}", endpoint);
                    return ParseResponse<List<Product>>(body);
                }

                _logger.LogDebug("⚠️ V3 endpoint failed: {Endpoint}", endpoint);
            }

            _logger.LogError("❌ All product endpoints failed");
            return new TSoftApiResponse<List<Product>>
            {
                Success = false,
                Message = new() { new() { Text = new() { "All product endpoints failed" } } }
            };
        }

        /// <summary>
        /// Add product - tries V3 first (for creation), then REST1
        /// </summary>
        public async Task<TSoftApiResponse<Product>> AddProductAsync(
            string code,
            string name,
            string categoryCode,
            decimal price,
            int stock = 0,
            Dictionary<string, string>? extraFields = null,
            CancellationToken ct = default)
        {
            var categoryId = int.TryParse(categoryCode.TrimStart('T', 't'), out var id) ? id : 1;

            // Try V3 format first
            var productV3 = new
            {
                name,
                wsProductCode = code,
                priceSale = price,
                stock,
                vat = extraFields?.TryGetValue("Vat", out var vatStr) == true ? int.Parse(vatStr) : 18,
                visibility = true,
                relation_hierarchy = new[] { new { id = categoryId, type = "category" } }
            };

            var v3Endpoints = new[]
            {
                "/catalog/products",
                "/api/v3/catalog/products"
            };

            foreach (var endpoint in v3Endpoints)
            {
                var (success, body, _) = await V3PostAsync(endpoint, productV3, ct);

                if (success)
                {
                    _logger.LogInformation("✅ V3 product creation succeeded: {Endpoint}", endpoint);
                    return ParseResponse<Product>(body);
                }
            }

            // Try REST1 format as fallback
            var productData = new Dictionary<string, string>
            {
                ["ProductCode"] = code,
                ["ProductName"] = name,
                ["DefaultCategoryCode"] = categoryCode,
                ["SellingPrice"] = price.ToString("F2"),
                ["Stock"] = stock.ToString(),
                ["IsActive"] = "1"
            };

            if (extraFields != null)
            {
                foreach (var kv in extraFields)
                {
                    productData[kv.Key] = kv.Value;
                }
            }

            var rest1Endpoints = new[]
            {
                "/product/createProducts",
                "/product/create",
                "/product/add"
            };

            foreach (var endpoint in rest1Endpoints)
            {
                // REST1 expects array wrapped in data parameter
                var formData = new Dictionary<string, string>
                {
                    ["data"] = JsonSerializer.Serialize(new[] { productData })
                };

                var (success, body, _) = await Rest1PostAsync(endpoint, formData, ct);

                if (success)
                {
                    _logger.LogInformation("✅ REST1 product creation succeeded: {Endpoint}", endpoint);
                    return ParseResponse<Product>(body);
                }
            }

            _logger.LogError("❌ All product creation endpoints failed");
            return new TSoftApiResponse<Product>
            {
                Success = false,
                Message = new() { new() { Text = new() { "All product creation endpoints failed" } } }
            };
        }

        public async Task<TSoftApiResponse<object>> CreateProductsAsync(
            List<Product> products,
            CancellationToken ct = default)
        {
            var ok = new List<object>();
            var fail = new List<object>();

            foreach (var p in products)
            {
                var r = await AddProductAsync(
                    p.ProductCode ?? "",
                    p.ProductName ?? "",
                    p.DefaultCategoryCode ?? "T1",
                    decimal.TryParse(p.SellingPrice ?? p.Price, out var price) ? price : 0,
                    int.TryParse(p.Stock, out var stock) ? stock : 0,
                    null,
                    ct
                );

                if (r.Success) ok.Add(r.Data!);
                else fail.Add(new { p.ProductCode, r.Message });
            }

            return new TSoftApiResponse<object>
            {
                Success = fail.Count == 0,
                Data = new { success = ok.Count, failed = fail.Count, ok, fail }
            };
        }

        // ========== CATEGORY OPERATIONS ==========

        public async Task<TSoftApiResponse<List<Category>>> GetCategoriesAsync(CancellationToken ct = default)
        {
            // Try REST1 first
            var rest1Endpoints = new[]
            {
                "/category/getCategories",
                "/category/get",
                "/categories/get"
            };

            foreach (var endpoint in rest1Endpoints)
            {
                var (success, body, _) = await Rest1PostAsync(endpoint, new Dictionary<string, string>(), ct);

                if (success)
                {
                    _logger.LogInformation("✅ REST1 categories succeeded: {Endpoint}", endpoint);
                    return ParseResponse<List<Category>>(body);
                }
            }

            // Try V3 as fallback
            var v3Endpoints = new[]
            {
                "/catalog/categories",
                "/api/v3/catalog/categories"
            };

            foreach (var endpoint in v3Endpoints)
            {
                var (success, body, _) = await V3GetAsync(endpoint, null, ct);

                if (success)
                {
                    _logger.LogInformation("✅ V3 categories succeeded: {Endpoint}", endpoint);
                    return ParseResponse<List<Category>>(body);
                }
            }

            return new TSoftApiResponse<List<Category>>
            {
                Success = false,
                Message = new() { new() { Text = new() { "All category endpoints failed" } } }
            };
        }

        // ========== CUSTOMER OPERATIONS ==========

        public async Task<TSoftApiResponse<List<Customer>>> GetCustomersAsync(
            int limit = 50,
            Dictionary<string, string>? filters = null,
            CancellationToken ct = default)
        {
            var form = new Dictionary<string, string> { ["limit"] = limit.ToString() };
            if (filters != null) foreach (var kv in filters) form[kv.Key] = kv.Value;

            // Try REST1
            var rest1Endpoints = new[] { "/customer/getCustomers", "/customer/get", "/customers/get" };

            foreach (var endpoint in rest1Endpoints)
            {
                var (success, body, _) = await Rest1PostAsync(endpoint, form, ct);
                if (success) return ParseResponse<List<Customer>>(body);
            }

            // Try V3
            var v3Endpoints = new[] { "/customers", "/api/v3/customers" };

            foreach (var endpoint in v3Endpoints)
            {
                var (success, body, _) = await V3GetAsync(endpoint, form, ct);
                if (success) return ParseResponse<List<Customer>>(body);
            }

            return new TSoftApiResponse<List<Customer>>
            {
                Success = false,
                Message = new() { new() { Text = new() { "All customer endpoints failed" } } }
            };
        }

        // ========== ORDER OPERATIONS ==========

        public async Task<TSoftApiResponse<List<Order>>> GetOrdersAsync(
            int limit = 50,
            Dictionary<string, string>? filters = null,
            CancellationToken ct = default)
        {
            var form = new Dictionary<string, string> { ["limit"] = limit.ToString() };
            if (filters != null) foreach (var kv in filters) form[kv.Key] = kv.Value;

            // Try REST1
            var rest1Endpoints = new[] { "/order/getOrders", "/order/get", "/orders/get" };

            foreach (var endpoint in rest1Endpoints)
            {
                var (success, body, _) = await Rest1PostAsync(endpoint, form, ct);
                if (success)
                {
                    // Only log raw response in debug mode to prevent spam
                    if (_debug)
                    {
                        _logger.LogDebug("📄 RAW RESPONSE from {Endpoint}:", endpoint);
                        _logger.LogDebug("{Body}", body.Length > 2000 ? body.Substring(0, 2000) + "..." : body);
                    }

                    var parsed = ParseResponse<List<Order>>(body);

                    if (!parsed.Success && _debug)
                    {
                        _logger.LogDebug("⚠️ Parse failed for {Endpoint}, trying alternative parsing...", endpoint);

                        // Try parsing as wrapped array
                        try
                        {
                            var doc = System.Text.Json.JsonDocument.Parse(body);
                            if (doc.RootElement.TryGetProperty("data", out var dataElement))
                            {
                                _logger.LogDebug("📦 Found 'data' property, attempting direct parse...");
                                var orders = System.Text.Json.JsonSerializer.Deserialize<List<Order>>(
                                    dataElement.GetRawText(),
                                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                                );

                                if (orders != null && orders.Count > 0)
                                {
                                    _logger.LogInformation("✅ Alternative parsing succeeded! Orders: {Count}", orders.Count);
                                    return new TSoftApiResponse<List<Order>> { Success = true, Data = orders };
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Alternative parsing also failed");
                        }
                    }

                    return parsed;
                }
            }

            // Try V3
            var v3Endpoints = new[] { "/orders", "/api/v3/orders" };

            foreach (var endpoint in v3Endpoints)
            {
                var (success, body, _) = await V3GetAsync(endpoint, form, ct);
                if (success) return ParseResponse<List<Order>>(body);
            }

            return new TSoftApiResponse<List<Order>>
            {
                Success = false,
                Message = new() { new() { Text = new() { "All order endpoints failed" } } }
            };
        }

        /// <summary>
        /// Get order details by order ID
        /// ⚠️ WARNING: This endpoint requires special token permission!
        /// If you get "Bu modüle erişim yetkiniz bulunmamaktadır", ask T-Soft admin
        /// to grant "order/details" module access to your API token.
        /// </summary>
        public async Task<TSoftApiResponse<List<OrderDetail>>> GetOrderDetailsByOrderIdAsync(
            int orderId,
            CancellationToken ct = default)
        {
            var form = new Dictionary<string, string>
            {
                ["OrderId"] = orderId.ToString(),
                ["orderId"] = orderId.ToString()
            };

            // Try REST1 - but only log errors once to prevent spam
            var rest1Endpoints = new[]
            {
                "/order/getOrderDetailsByOrderId",
                "/order/getOrderDetails",
                "/order/details",
                "/orders/details"
            };

            foreach (var endpoint in rest1Endpoints)
            {
                var (success, body, _) = await Rest1PostAsync(endpoint, form, ct);
                if (success)
                {
                    if (_debug)
                    {
                        _logger.LogDebug("📦 ORDER DETAILS for OrderId {OrderId} succeeded", orderId);
                    }

                    var parsed = ParseResponse<List<OrderDetail>>(body);

                    if (parsed.Success && parsed.Data != null)
                    {
                        return parsed;
                    }
                }
            }

            // Try V3
            var v3Endpoints = new[]
            {
                $"/orders/{orderId}/details",
                $"/api/v3/orders/{orderId}/details"
            };

            foreach (var endpoint in v3Endpoints)
            {
                var (success, body, _) = await V3GetAsync(endpoint, null, ct);
                if (success)
                {
                    return ParseResponse<List<OrderDetail>>(body);
                }
            }

            // Only log this once every 10 failures to prevent spam
            if (orderId % 10 == 0)
            {
                _logger.LogWarning("❌ Order detail endpoints failed for OrderId {OrderId}. Token may lack 'order/details' permission.", orderId);
            }

            return new TSoftApiResponse<List<OrderDetail>>
            {
                Success = false,
                Message = new() { new() { Text = new() { "Order details endpoint failed - check token permissions" } } }
            };
        }

        /// <summary>
        /// Get payment types list
        /// </summary>
        public async Task<TSoftApiResponse<List<PaymentType>>> GetPaymentTypesAsync(
            CancellationToken ct = default)
        {
            // Try REST1
            var rest1Endpoints = new[]
            {
                "/order/getPaymentTypeList",
                "/payment/getTypes",
                "/paymenttype/get"
            };

            foreach (var endpoint in rest1Endpoints)
            {
                var (success, body, _) = await Rest1PostAsync(endpoint, new Dictionary<string, string>(), ct);
                if (success)
                {
                    _logger.LogInformation("✅ Payment types succeeded: {Endpoint}", endpoint);
                    return ParseResponse<List<PaymentType>>(body);
                }
            }

            // Try V3
            var v3Endpoints = new[]
            {
                "/payment-types",
                "/api/v3/payment-types"
            };

            foreach (var endpoint in v3Endpoints)
            {
                var (success, body, _) = await V3GetAsync(endpoint, null, ct);
                if (success)
                {
                    _logger.LogInformation("✅ Payment types succeeded: {Endpoint}", endpoint);
                    return ParseResponse<List<PaymentType>>(body);
                }
            }

            return new TSoftApiResponse<List<PaymentType>>
            {
                Success = false,
                Message = new() { new() { Text = new() { "All payment type endpoints failed" } } }
            };
        }

        /// <summary>
        /// Get cargo companies list
        /// </summary>
        public async Task<TSoftApiResponse<List<CargoCompany>>> GetCargoCompaniesAsync(
            CancellationToken ct = default)
        {
            // Try REST1
            var rest1Endpoints = new[]
            {
                "/order/getCargoCompanyList",
                "/cargo/getCompanies",
                "/cargocompany/get"
            };

            foreach (var endpoint in rest1Endpoints)
            {
                var (success, body, _) = await Rest1PostAsync(endpoint, new Dictionary<string, string>(), ct);
                if (success)
                {
                    _logger.LogInformation("✅ Cargo companies succeeded: {Endpoint}", endpoint);
                    return ParseResponse<List<CargoCompany>>(body);
                }
            }

            // Try V3
            var v3Endpoints = new[]
            {
                "/cargo-companies",
                "/api/v3/cargo-companies"
            };

            foreach (var endpoint in v3Endpoints)
            {
                var (success, body, _) = await V3GetAsync(endpoint, null, ct);
                if (success)
                {
                    _logger.LogInformation("✅ Cargo companies succeeded: {Endpoint}", endpoint);
                    return ParseResponse<List<CargoCompany>>(body);
                }
            }

            return new TSoftApiResponse<List<CargoCompany>>
            {
                Success = false,
                Message = new() { new() { Text = new() { "All cargo company endpoints failed" } } }
            };
        }

        /// <summary>
        /// Get order status list
        /// </summary>
        public async Task<TSoftApiResponse<List<OrderStatusInfo>>> GetOrderStatusListAsync(
            CancellationToken ct = default)
        {
            // Try REST1
            var rest1Endpoints = new[]
            {
                "/order/getOrderStatusList",
                "/orderstatus/get",
                "/order/statuses"
            };

            foreach (var endpoint in rest1Endpoints)
            {
                var (success, body, _) = await Rest1PostAsync(endpoint, new Dictionary<string, string>(), ct);
                if (success)
                {
                    _logger.LogInformation("✅ Order statuses succeeded: {Endpoint}", endpoint);
                    return ParseResponse<List<OrderStatusInfo>>(body);
                }
            }

            // Try V3
            var v3Endpoints = new[]
            {
                "/order-statuses",
                "/api/v3/order-statuses"
            };

            foreach (var endpoint in v3Endpoints)
            {
                var (success, body, _) = await V3GetAsync(endpoint, null, ct);
                if (success)
                {
                    _logger.LogInformation("✅ Order statuses succeeded: {Endpoint}", endpoint);
                    return ParseResponse<List<OrderStatusInfo>>(body);
                }
            }

            return new TSoftApiResponse<List<OrderStatusInfo>>
            {
                Success = false,
                Message = new() { new() { Text = new() { "All order status endpoints failed" } } }
            };
        }

        /// <summary>
        /// Get customer by ID
        /// </summary>
        public async Task<TSoftApiResponse<Customer>> GetCustomerByIdAsync(
            int customerId,
            CancellationToken ct = default)
        {
            var form = new Dictionary<string, string>
            {
                ["CustomerId"] = customerId.ToString(),
                ["customerId"] = customerId.ToString(),
                ["Id"] = customerId.ToString()
            };

            // Try REST1
            var rest1Endpoints = new[]
            {
                "/customer/getCustomerById",
                "/customer/get",
                "/customers/get"
            };

            foreach (var endpoint in rest1Endpoints)
            {
                var (success, body, _) = await Rest1PostAsync(endpoint, form, ct);
                if (success) return ParseResponse<Customer>(body);
            }

            // Try V3
            var v3Endpoints = new[]
            {
                $"/customers/{customerId}",
                $"/api/v3/customers/{customerId}"
            };

            foreach (var endpoint in v3Endpoints)
            {
                var (success, body, _) = await V3GetAsync(endpoint, null, ct);
                if (success) return ParseResponse<Customer>(body);
            }

            return new TSoftApiResponse<Customer>
            {
                Success = false,
                Message = new() { new() { Text = new() { "All customer endpoints failed" } } }
            };
        }
    }
}