using QuoteManagement.Domain.Entities;
using QuoteManagement.Application.DTOs;

namespace QuoteManagement.Application.Interfaces;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync(string? category = null, string? search = null, string? sort = null, CancellationToken ct = default);
    Task<IEnumerable<Product>> GetAllForAdminAsync(CancellationToken ct = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Product>> GetPopularAsync(int count = 6, CancellationToken ct = default);
    Task UpdateLastPriceAsync(Guid id, decimal price, DateTime date, CancellationToken ct = default);
    Task<Product> AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IProductPriceHistoryRepository
{
    Task<IEnumerable<ProductPriceHistory>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task AddAsync(ProductPriceHistory history, CancellationToken ct = default);
}

public interface IRequestRepository
{
    Task<IEnumerable<Request>> GetAllAsync(CancellationToken ct = default);
    Task<Request?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Request> AddAsync(Request request, CancellationToken ct = default);
    Task UpdateAsync(Request request, CancellationToken ct = default);
    Task<string> GenerateRequestNoAsync(CancellationToken ct = default);
}

public interface ICustomerRepository
{
    Task<IEnumerable<Customer>> GetAllAsync(CancellationToken ct = default);
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Customer> AddAsync(Customer customer, CancellationToken ct = default);
}

public interface IExcelService
{
    byte[] GenerateRequestExcel(ExcelRequestDto requestDto);
    IEnumerable<ExcelImportRowDto> ParseImportExcel(Stream excelStream);
    ExcelImportResult ParseImportExcelFull(Stream excelStream);
}

public interface INotificationService
{
    Task SendQuoteEmailAsync(string toEmail, string customerName, string requestNo, byte[] excelAttachment, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
