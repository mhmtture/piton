using QuoteManagement.Domain.Enums;

namespace QuoteManagement.Domain.Entities;

public class Request
{
    public Guid Id { get; set; }
    public string RequestNo { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public DateTime RequestDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? Notes { get; set; }
    public string? ExcelFilePath { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
    public ICollection<RequestItem> Items { get; set; } = new List<RequestItem>();
}
