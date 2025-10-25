using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.DTOs;
using TSoftApiClient.Services;

namespace TSoftApiClient.Controllers
{
    /// <summary>
    /// Ürün sayfaları için MVC Controller
    /// </summary>
    public class ProductsMvcController : Controller
    {
        private readonly TSoftApiService _tsoftService;
        private readonly ILogger<ProductsMvcController> _logger;

        public ProductsMvcController(
            TSoftApiService tsoftService,
            ILogger<ProductsMvcController> logger)
        {
            _tsoftService = tsoftService;
            _logger = logger;
        }

        /// <summary>
        /// Ürün listesi sayfası
        /// </summary>
        [Route("/Products")]
        [Route("/ProductsMvc")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var result = await _tsoftService.GetProductsAsync(limit: 1000);
                
                if (!result.Success)
                {
                    ViewBag.Error = "Ürünler yüklenemedi";
                    return View("~/Views/Products/Index.cshtml", new List<Models.Product>());
                }

                return View("~/Views/Products/Index.cshtml", result.Data ?? new List<Models.Product>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürünler yüklenirken hata");
                ViewBag.Error = "Bir hata oluştu";
                return View("~/Views/Products/Index.cshtml", new List<Models.Product>());
            }
        }

        /// <summary>
        /// Ürün ekleme sayfası
        /// </summary>
        [Route("/Products/Create")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Kategorileri yükle
            var categories = await _tsoftService.GetCategoriesAsync();
            ViewBag.Categories = categories.Data ?? new List<Models.Category>();
            
            return View("~/Views/Products/Create.cshtml");
        }

        /// <summary>
        /// Ürün ekleme işlemi
        /// </summary>
        [Route("/Products/Create")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateProductDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var categories = await _tsoftService.GetCategoriesAsync();
                    ViewBag.Categories = categories.Data ?? new List<Models.Category>();
                    return View("~/Views/Products/Create.cshtml", dto);
                }

                var extraFields = new Dictionary<string, string>();
                
                if (!string.IsNullOrEmpty(dto.Brand))
                    extraFields["Brand"] = dto.Brand;
                if (!string.IsNullOrEmpty(dto.Vat))
                    extraFields["Vat"] = dto.Vat;
                if (!string.IsNullOrEmpty(dto.Currency))
                    extraFields["Currency"] = dto.Currency;
                if (!string.IsNullOrEmpty(dto.BuyingPrice))
                    extraFields["BuyingPrice"] = dto.BuyingPrice;
                if (!string.IsNullOrEmpty(dto.ShortDescription))
                    extraFields["ShortDescription"] = dto.ShortDescription;

                var result = await _tsoftService.AddProductAsync(
                    dto.Code,
                    dto.Name,
                    dto.CategoryCode,
                    dto.Price,
                    dto.Stock,
                    extraFields
                );

                if (result.Success)
                {
                    TempData["Success"] = "Ürün başarıyla eklendi!";
                    return RedirectToAction("Index");
                }
                else
                {
                    var message = result.Message?.FirstOrDefault()?.Text?.FirstOrDefault() ?? "Bilinmeyen hata";
                    ViewBag.Error = message;
                    
                    var categories = await _tsoftService.GetCategoriesAsync();
                    ViewBag.Categories = categories.Data ?? new List<Models.Category>();
                    return View("~/Views/Products/Create.cshtml", dto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün eklenirken hata");
                ViewBag.Error = "Bir hata oluştu: " + ex.Message;
                
                var categories = await _tsoftService.GetCategoriesAsync();
                ViewBag.Categories = categories.Data ?? new List<Models.Category>();
                return View("~/Views/Products/Create.cshtml", dto);
            }
        }
    }
}
