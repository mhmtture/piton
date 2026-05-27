namespace QuoteManagement.Domain.Entities;

public class RequestItem
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal? UnitPrice { get; set; }
    public decimal DiscountRate { get; set; } = 0;
    public decimal? LineTotal { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Request Request { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
