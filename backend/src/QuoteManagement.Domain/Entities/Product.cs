namespace QuoteManagement.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;   // HMI / LED_PANEL / LCD
    public string? ModelNumber { get; set; }
    public string? Specifications { get; set; }            // JSONB stored as string
    public decimal BasePrice { get; set; }
    public string Currency { get; set; } = "TRY";
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal? LastRequestPrice { get; set; }
    public DateTime? LastRequestDate { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Rating { get; set; } = 4.0m;
    public int SalesCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<RequestItem> RequestItems { get; set; } = new List<RequestItem>();
    public ICollection<ProductPriceHistory> PriceHistories { get; set; } = new List<ProductPriceHistory>();
}
