namespace TSoftApiClient.Models
{
    /// <summary>
    /// T-Soft API'den dönen genel yanıt yapısı
    /// </summary>
    public class TSoftApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public List<MessageItem>? Message { get; set; }
    }

    public class MessageItem
    {
        public List<string>? Text { get; set; }
    }

    /// <summary>
    /// Ürün modeli - Geliştirilmiş versiyon
    /// </summary>
    public class Product
    {
        // Temel bilgiler
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public string? DefaultCategoryCode { get; set; }
        public string? Stock { get; set; }
        public string? SellingPrice { get; set; }
        public string? IsActive { get; set; }
        public string? StockUnit { get; set; }
        public string? Brand { get; set; }
        public string? Vat { get; set; }
        public string? Currency { get; set; }
        public string? BuyingPrice { get; set; }
        public string? ShortDescription { get; set; }
        public string? Price { get; set; }

        // Görsel bilgileri
        public string? ImageUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Image { get; set; }
        public List<ProductImage>? Images { get; set; }

        // Kategori bilgileri
        public string? CategoryName { get; set; }
        public List<string>? CategoryPath { get; set; }
        public List<string>? Categories { get; set; }

        // Tarih bilgileri
        public string? UpdateDate { get; set; }
        public string? UpdateDateTimeStamp { get; set; }
        public string? CreatedDate { get; set; }
        public string? DateCreated { get; set; }
        public string? LastModified { get; set; }

        // Ek bilgiler
        public string? ProductId { get; set; }
        public string? Description { get; set; }
        public string? Barcode { get; set; }
        public string? StockCode { get; set; }
    }

    /// <summary>
    /// Ürün görseli modeli
    /// </summary>
    public class ProductImage
    {
        public string? ImageId { get; set; }
        public string? ProductImageId { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImagePath { get; set; }
        public string? Image { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Thumbnail { get; set; }
        public string? IsPrimary { get; set; }
        public string? IsMain { get; set; }
        public string? IsActive { get; set; }
        public int? Order { get; set; }
        public string? OrderNo { get; set; }
    }

    /// <summary>
    /// Kategori modeli - Geliştirilmiş versiyon
    /// </summary>
    public class Category
    {
        public string? CategoryCode { get; set; }
        public string? CategoryName { get; set; }
        public string? ParentCategoryCode { get; set; }
        public string? IsActive { get; set; }

        // Kategori ağacı için
        public string? CategoryId { get; set; }
        public string? ParentCategoryId { get; set; }
        public int? Level { get; set; }
        public int? Order { get; set; }
        public List<Category>? Children { get; set; }
        public string? Path { get; set; } // "Ana > Alt > Ürün" formatında
    }

    /// <summary>
    /// Müşteri modeli
    /// </summary>
    public class Customer
    {
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? IsActive { get; set; }
    }

    /// <summary>
    /// Sipariş modeli
    /// </summary>
    public class Order
    {
        // Basic Info - ALL STRINGS (API returns them as strings)
        public string? Id { get; set; }
        public string? OrderId { get; set; }
        public string? OrderCode { get; set; }
        public string? Status { get; set; }
        public string? OrderStatus { get; set; }
        public string? OrderStatusId { get; set; }
        public string? SupplyStatus { get; set; }

        // Customer Info - ALL STRINGS
        public string? CustomerId { get; set; }
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerUsername { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerGroupId { get; set; }

        // Date - STRINGS
        public string? OrderDate { get; set; }
        public string? OrderDateTimeStamp { get; set; }
        public string? CreatedDate { get; set; }
        public string? DateCreated { get; set; }
        public string? UpdateDate { get; set; }
        public string? UpdateDateTimeStamp { get; set; }
        public string? ApprovalTime { get; set; }

        // Location
        public string? City { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingAddress { get; set; }
        public string? BillingCity { get; set; }

        // Financial - ALL STRINGS
        public string? Total { get; set; }
        public string? TotalAmount { get; set; }
        public string? OrderTotalPrice { get; set; }
        public string? OrderSubtotal { get; set; }
        public string? GeneralTotal { get; set; }
        public string? SubTotal { get; set; }
        public string? DiscountTotal { get; set; }
        public string? TaxTotal { get; set; }
        public string? ShippingTotal { get; set; }
        public string? Currency { get; set; }
        public string? SiteDefaultCurrency { get; set; }

        // Payment & Shipping - STRINGS
        public string? PaymentTypeId { get; set; }
        public string? PaymentType { get; set; }
        public string? PaymentTypeName { get; set; }
        public string? SubPaymentTypeId { get; set; }
        public string? PaymentSubMethod { get; set; }
        public string? PaymentBankName { get; set; }
        public string? Bank { get; set; }
        public string? PaymentInfo { get; set; }

        public string? CargoId { get; set; }
        public string? CargoCode { get; set; }
        public string? Cargo { get; set; }
        public string? CargoCompanyId { get; set; }
        public string? CargoCompanyName { get; set; }
        public string? ShippingCompanyName { get; set; }
        public string? CargoTrackingCode { get; set; }
        public string? CargoPaymentMethod { get; set; }
        public string? CargoChargeWithVat { get; set; }
        public string? CargoChargeWithoutVat { get; set; }

        // Additional fields
        public string? Application { get; set; }
        public string? Language { get; set; }
        public string? ExchangeRate { get; set; }
        public string? Installment { get; set; }
        public string? IsTransferred { get; set; }
        public string? NonMemberShopping { get; set; }
        public string? WaybillNumber { get; set; }
        public string? InvoiceNumber { get; set; }

        // Items
        public int ItemCount { get; set; }
        public List<OrderDetail>? OrderDetails { get; set; }
        public List<OrderDetail>? Items { get; set; }
    }

    /// <summary>
    /// Sipariş detay modeli
    /// </summary>
    public class OrderDetail
    {
        public string? Id { get; set; }
        public string? OrderId { get; set; }
        public string? ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public string? Quantity { get; set; }
        public string? Price { get; set; }
        public string? Total { get; set; }

        // Additional fields
        public string? City { get; set; }
        public string? ShippingCity { get; set; }
        public string? SupplyStatus { get; set; }
    }

    /// <summary>
    /// Sipariş durum modeli
    /// </summary>
    public class OrderStatusInfo
    {
        public string? Id { get; set; }
        public string? OrderStatusId { get; set; }
        public string? Name { get; set; }
        public string? OrderStatusName { get; set; }
        public string? Code { get; set; }
    }

    /// <summary>
    /// Ödeme tipi modeli
    /// </summary>
    public class PaymentType
    {
        public string? Id { get; set; }
        public string? PaymentTypeId { get; set; }
        public string? Name { get; set; }
        public string? PaymentTypeName { get; set; }
        public string? Code { get; set; }
    }

    /// <summary>
    /// Kargo firması modeli
    /// </summary>
    public class CargoCompany
    {
        public string? Id { get; set; }
        public string? CargoCompanyId { get; set; }
        public string? Name { get; set; }
        public string? CargoCompanyName { get; set; }
        public string? Code { get; set; }
    }
}