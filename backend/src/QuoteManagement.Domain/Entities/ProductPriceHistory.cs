namespace QuoteManagement.Domain.Entities;

public class ProductPriceHistory
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime RequestDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Product Product { get; set; } = null!;
}
