using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TSoftApiClient.Models;

namespace TSoftApiClient.Services
{
    /// <summary>
    /// T-Soft API Service - Product Extensions
    /// Ürün görsel ve kategori ağacı için ek metodlar
    /// </summary>
    public partial class TSoftApiService
    {
        /// <summary>
        /// Get product images by product code
        /// API: Product.getProductImages (REST1)
        /// </summary>
        public async Task<TSoftApiResponse<List<ProductImage>>> GetProductImagesAsync(
            string productCode,
            CancellationToken ct = default)
        {
            var form = new Dictionary<string, string>
            {
                ["ProductCode"] = productCode
            };

            // REST1 endpoint
            var (success, body, _) = await Rest1PostAsync("/product/getProductImages", form, ct);

            if (success)
            {
                _logger.LogInformation("✅ Product images endpoint succeeded");
                return ParseResponse<List<ProductImage>>(body);
            }

            _logger.LogDebug("ℹ️ Product images not available for {Code}", productCode);

            // Return empty list instead of error
            return new TSoftApiResponse<List<ProductImage>>
            {
                Success = true,
                Data = new List<ProductImage>()
            };
        }

        /// <summary>
        /// Get all product images in bulk
        /// Bu metod tüm ürünler için görselleri paralel çeker
        /// </summary>
        public async Task<Dictionary<string, List<ProductImage>>> GetBulkProductImagesAsync(
            List<string> productCodes,
            int maxParallel = 5,
            CancellationToken ct = default)
        {
            var result = new Dictionary<string, List<ProductImage>>();
            var semaphore = new SemaphoreSlim(maxParallel);

            var tasks = productCodes.Select(async code =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    var images = await GetProductImagesAsync(code, ct);
                    if (images.Success && images.Data != null)
                    {
                        lock (result)
                        {
                            result[code] = images.Data;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to get images for product {Code}", code);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            return result;
        }

        /// <summary>
        /// Get category tree
        /// API: Category.getCategoryTree (REST1)
        /// </summary>
        public async Task<TSoftApiResponse<List<Category>>> GetCategoryTreeAsync(
            CancellationToken ct = default)
        {
            // Try REST1 getCategoryTree
            var (success, body, _) = await Rest1PostAsync("/category/getCategoryTree", new Dictionary<string, string>(), ct);

            if (success)
            {
                _logger.LogInformation("✅ Category tree endpoint succeeded");
                var parsed = ParseResponse<List<Category>>(body);

                if (parsed.Success && parsed.Data != null)
                {
                    BuildCategoryPaths(parsed.Data);
                }

                return parsed;
            }

            // Fallback: Use flat category list and build tree manually
            _logger.LogInformation("ℹ️ Category tree not available, building from flat list");
            var flatCategories = await GetCategoriesAsync(ct);

            if (flatCategories.Success && flatCategories.Data != null)
            {
                var tree = BuildTreeFromFlatList(flatCategories.Data);
                BuildCategoryPaths(tree);

                return new TSoftApiResponse<List<Category>>
                {
                    Success = true,
                    Data = tree
                };
            }

            return new TSoftApiResponse<List<Category>>
            {
                Success = false,
                Message = new() { new() { Text = new() { "Category tree failed" } } }
            };
        }

        /// <summary>
        /// Build category tree from flat list
        /// </summary>
        private List<Category> BuildTreeFromFlatList(List<Category> flatList)
        {
            var categoryDict = flatList.ToDictionary(c => c.CategoryCode ?? "", c => c);
            var rootCategories = new List<Category>();

            foreach (var category in flatList)
            {
                category.Children = new List<Category>();

                if (string.IsNullOrEmpty(category.ParentCategoryCode))
                {
                    rootCategories.Add(category);
                }
                else if (categoryDict.TryGetValue(category.ParentCategoryCode, out var parent))
                {
                    if (parent.Children == null)
                        parent.Children = new List<Category>();

                    parent.Children.Add(category);
                }
                else
                {
                    rootCategories.Add(category);
                }
            }

            return rootCategories;
        }

        /// <summary>
        /// Build category paths recursively (e.g., "Electronics > Phones > Samsung")
        /// </summary>
        private void BuildCategoryPaths(List<Category> categories, string parentPath = "")
        {
            foreach (var category in categories)
            {
                category.Path = string.IsNullOrEmpty(parentPath)
                    ? category.CategoryName ?? category.CategoryCode ?? "Unknown"
                    : $"{parentPath} > {category.CategoryName ?? category.CategoryCode}";

                if (category.Children != null && category.Children.Count > 0)
                {
                    BuildCategoryPaths(category.Children, category.Path);
                }
            }
        }

        /// <summary>
        /// Get enhanced products with images and category info
        /// </summary>
        public async Task<TSoftApiResponse<List<Product>>> GetEnhancedProductsAsync(
            int limit = 50,
            int page = 1,
            bool includeImages = true,
            CancellationToken ct = default)
        {
            // Get products
            var productsResult = await GetProductsAsync(limit, page, null, null, ct);

            if (!productsResult.Success || productsResult.Data == null)
            {
                return productsResult;
            }

            var products = productsResult.Data;

            // Get category tree for category names
            var categoryTreeResult = await GetCategoryTreeAsync(ct);
            var categoryDict = new Dictionary<string, Category>();

            if (categoryTreeResult.Success && categoryTreeResult.Data != null)
            {
                FlattenCategoryTree(categoryTreeResult.Data, categoryDict);
            }

            // Enrich products with category info
            foreach (var product in products)
            {
                if (!string.IsNullOrEmpty(product.DefaultCategoryCode) &&
                    categoryDict.TryGetValue(product.DefaultCategoryCode, out var category))
                {
                    product.CategoryName = category.CategoryName;
                    product.CategoryPath = category.Path?.Split(" > ").ToList();
                }
            }

            // Get images if requested (only for first page to avoid performance issues)
            if (includeImages && page == 1 && products.Count > 0)
            {
                _logger.LogInformation("🖼️ Fetching images for {Count} products...", Math.Min(20, products.Count));

                // Limit to first 20 products for images to avoid rate limiting
                var productCodes = products
                    .Take(20)
                    .Select(p => p.ProductCode ?? "")
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();

                var imagesDict = await GetBulkProductImagesAsync(productCodes, maxParallel: 3, ct);

                foreach (var product in products.Take(20))
                {
                    if (!string.IsNullOrEmpty(product.ProductCode) &&
                        imagesDict.TryGetValue(product.ProductCode, out var images) &&
                        images.Count > 0)
                    {
                        product.Images = images;

                        // Set primary image
                        var primaryImage = images.FirstOrDefault(i =>
                            i.IsPrimary == "1" ||
                            i.IsMain == "1" ||
                            i.IsMain?.ToLower() == "true" ||
                            i.IsPrimary?.ToLower() == "true");

                        if (primaryImage != null)
                        {
                            product.ThumbnailUrl = primaryImage.ThumbnailUrl ?? primaryImage.Thumbnail ?? primaryImage.ImageUrl;
                            product.ImageUrl = primaryImage.ImageUrl ?? primaryImage.Image;
                        }
                        else if (images.Count > 0)
                        {
                            product.ThumbnailUrl = images[0].ThumbnailUrl ?? images[0].Thumbnail ?? images[0].ImageUrl;
                            product.ImageUrl = images[0].ImageUrl ?? images[0].Image;
                        }
                    }
                }
            }

            return new TSoftApiResponse<List<Product>>
            {
                Success = true,
                Data = products
            };
        }

        /// <summary>
        /// Flatten category tree into dictionary for quick lookup
        /// </summary>
        private void FlattenCategoryTree(List<Category> categories, Dictionary<string, Category> dict)
        {
            foreach (var category in categories)
            {
                if (!string.IsNullOrEmpty(category.CategoryCode))
                {
                    dict[category.CategoryCode] = category;
                }

                if (category.Children != null && category.Children.Count > 0)
                {
                    FlattenCategoryTree(category.Children, dict);
                }
            }
        }
    }
}