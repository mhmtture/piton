namespace QuoteManagement.Application.DTOs;

// ─── Product DTOs ───────────────────────────────────────────
public record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    string Category,
    string? ModelNumber,
    string? Specifications,
    decimal BasePrice,
    string Currency,
    int StockQuantity,
    bool IsActive,
    decimal? LastRequestPrice,
    DateTime? LastRequestDate,
    string? ImageUrl,
    decimal Rating,
    int SalesCount
);

public record ProductPriceHistoryDto(
    Guid Id,
    Guid ProductId,
    decimal Price,
    string Currency,
    DateTime RequestDate,
    string? Notes
);

// ─── Customer DTOs ──────────────────────────────────────────
public record CustomerDto(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    string? Company,
    string? Address
);

// ─── Request DTOs ───────────────────────────────────────────
public record RequestDto(
    Guid Id,
    string RequestNo,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    string? CustomerCompany,
    DateTime RequestDate,
    decimal TotalAmount,
    string Currency,
    string Status,
    string? Notes,
    DateTime? SentAt,
    List<RequestItemDto> Items
);

public record RequestItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductCategory,
    int Quantity,
    decimal? UnitPrice,
    decimal DiscountRate,
    decimal? LineTotal,
    decimal? LastRequestPrice,
    DateTime? LastRequestDate
);

public record CreateRequestDto(
    string CustomerEmail,
    string CustomerName,
    string? CustomerPhone,
    string? CustomerCompany,
    List<CreateRequestItemDto> Items,
    string? Notes
);

public record CreateRequestItemDto(
    Guid ProductId,
    int Quantity
);

public record SubmitRequestDto(
    List<PriceUpdateItemDto> Items
);

public record PriceUpdateItemDto(
    Guid RequestItemId,
    decimal UnitPrice,
    decimal DiscountRate
);

// ─── Excel DTOs ─────────────────────────────────────────────
public record ExcelRequestDto(
    string RequestNo,
    string CustomerName,
    string CustomerEmail,
    DateTime RequestDate,
    List<ExcelRequestItemDto> Items
);

public record ExcelRequestItemDto(
    Guid ProductId,
    string ProductName,
    string Category,
    int Quantity,
    decimal? LastPrice,
    DateTime? LastPriceDate
);

public record ExcelImportRowDto(
    Guid RequestItemId,
    Guid ProductId,
    decimal UnitPrice,
    decimal DiscountRate
);

// ─── Product Admin DTOs ──────────────────────────────────────
public record CreateProductDto(
    string Name,
    string Category,
    string? Description,
    string? ModelNumber,
    string? Specifications,
    decimal BasePrice,
    int StockQuantity,
    string? ImageUrl
);

public record UpdateProductDto(
    string Name,
    string Category,
    string? Description,
    string? ModelNumber,
    string? Specifications,
    decimal BasePrice,
    int StockQuantity,
    string? ImageUrl,
    bool IsActive
);

// ─── Excel Quote Submit ──────────────────────────────────────
public record ExcelImportResult(
    string? CustomerEmail,
    string? CustomerName,
    List<ExcelImportProductRow> Products
);

public record ExcelImportProductRow(
    Guid ProductId,
    string ProductName,
    string Category,
    int Quantity,
    decimal? LastPrice,
    DateTime? LastPriceDate,
    bool HasHistory,
    string Message
);

public record SubmitExcelQuoteDto(
    string CustomerEmail,
    string CustomerName,
    List<ExcelQuoteItemDto> Items
);

public record ExcelQuoteItemDto(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice
);

