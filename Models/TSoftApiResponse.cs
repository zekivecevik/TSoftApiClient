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
    /// Ürün modeli
    /// </summary>
    public class Product
    {
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
    }

    /// <summary>
    /// Kategori modeli
    /// </summary>
    public class Category
    {
        public string? CategoryCode { get; set; }
        public string? CategoryName { get; set; }
        public string? ParentCategoryCode { get; set; }
        public string? IsActive { get; set; }
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
        public string? OrderStatus { get; set; }           // Sipariş süreci name
        public string? OrderStatusId { get; set; }
        public string? SupplyStatus { get; set; }          // Paketleme durumu
        
        // Customer Info - ALL STRINGS
        public string? CustomerId { get; set; }
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerUsername { get; set; }  // API uses this for email
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
        
        // Location - Not in this API response, keep for compatibility
        public string? City { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingAddress { get; set; }
        public string? BillingCity { get; set; }
        
        // Financial - ALL STRINGS (API returns as decimal strings)
        public string? Total { get; set; }
        public string? TotalAmount { get; set; }
        public string? OrderTotalPrice { get; set; }  // API uses this
        public string? OrderSubtotal { get; set; }     // API uses this
        public string? GeneralTotal { get; set; }
        public string? SubTotal { get; set; }
        public string? DiscountTotal { get; set; }
        public string? TaxTotal { get; set; }
        public string? ShippingTotal { get; set; }
        public string? Currency { get; set; }
        public string? SiteDefaultCurrency { get; set; }
        
        // Payment & Shipping - STRINGS
        public string? PaymentTypeId { get; set; }
        public string? PaymentType { get; set; }        // API uses this for name
        public string? PaymentTypeName { get; set; }
        public string? SubPaymentTypeId { get; set; }
        public string? PaymentSubMethod { get; set; }
        public string? PaymentBankName { get; set; }
        public string? Bank { get; set; }
        public string? PaymentInfo { get; set; }
        
        public string? CargoId { get; set; }
        public string? CargoCode { get; set; }
        public string? Cargo { get; set; }              // API uses this for name
        public string? CargoCompanyId { get; set; }
        public string? CargoCompanyName { get; set; }
        public string? ShippingCompanyName { get; set; }
        public string? CargoTrackingCode { get; set; }
        public string? CargoPaymentMethod { get; set; }
        public string? CargoChargeWithVat { get; set; }
        public string? CargoChargeWithoutVat { get; set; }
        
        // Additional fields from API
        public string? Application { get; set; }
        public string? Language { get; set; }
        public string? ExchangeRate { get; set; }
        public string? Installment { get; set; }
        public string? IsTransferred { get; set; }
        public string? NonMemberShopping { get; set; }
        public string? WaybillNumber { get; set; }
        public string? InvoiceNumber { get; set; }
        
        // Items - Keep for future use
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
        
        // Additional fields from detail API
        public string? City { get; set; }
        public string? ShippingCity { get; set; }
        public string? SupplyStatus { get; set; }
    }

    /// <summary>
    /// Sipariş durum modeli (OrderStatusList için)
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
