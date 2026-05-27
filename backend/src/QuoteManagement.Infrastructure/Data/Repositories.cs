using Microsoft.EntityFrameworkCore;
using QuoteManagement.Application.Interfaces;
using QuoteManagement.Domain.Entities;
using QuoteManagement.Infrastructure.Data;

namespace QuoteManagement.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;
    public ProductRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Product>> GetAllAsync(string? category = null, string? search = null, string? sort = null, CancellationToken ct = default)
    {
        var q = _db.Products.Where(p => p.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(p => p.Category == category.ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => EF.Functions.ILike(p.Name, $"%{search}%") ||
                             (p.ModelNumber != null && EF.Functions.ILike(p.ModelNumber, $"%{search}%")));

        q = (sort?.ToLowerInvariant()) switch
        {
            "price_asc" => q.OrderBy(p => p.BasePrice),
            "price_desc" => q.OrderByDescending(p => p.BasePrice),
            "rating_desc" => q.OrderByDescending(p => p.Rating).ThenBy(p => p.Name),
            "sales_desc" => q.OrderByDescending(p => p.SalesCount).ThenBy(p => p.Name),
            "name_asc" => q.OrderBy(p => p.Name),
            _ => q.OrderBy(p => p.Category).ThenBy(p => p.Name)
        };

        return await q.ToListAsync(ct);
    }

    public async Task<IEnumerable<Product>> GetAllForAdminAsync(CancellationToken ct = default)
        => await _db.Products.OrderBy(p => p.Category).ThenBy(p => p.Name).ToListAsync(ct);

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IEnumerable<Product>> GetPopularAsync(int count = 6, CancellationToken ct = default)
        => await _db.Products
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.LastRequestDate)
            .Take(count)
            .ToListAsync(ct);

    public async Task UpdateLastPriceAsync(Guid id, decimal price, DateTime date, CancellationToken ct = default)
    {
        var product = await _db.Products.FindAsync(new object[] { id }, ct);
        if (product is not null)
        {
            product.LastRequestPrice = price;
            product.LastRequestDate = date;
            product.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<Product> AddAsync(Product product, CancellationToken ct = default)
    {
        product.Id = product.Id == Guid.Empty ? Guid.NewGuid() : product.Id;
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return product;
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        // Entity zaten DbContext tarafından takip ediliyor, sadece SaveChanges yeterli
        var entry = _db.Entry(product);
        if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            _db.Products.Attach(product);
        product.UpdatedAt = DateTime.UtcNow;
        entry.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _db.Products.FindAsync(new object[] { id }, ct);
        if (product is null) return false;
        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class ProductPriceHistoryRepository : IProductPriceHistoryRepository
{
    private readonly AppDbContext _db;
    public ProductPriceHistoryRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<ProductPriceHistory>> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        => await _db.ProductPriceHistories
            .Where(h => h.ProductId == productId)
            .OrderByDescending(h => h.RequestDate)
            .ToListAsync(ct);

    public async Task AddAsync(ProductPriceHistory history, CancellationToken ct = default)
    {
        await _db.ProductPriceHistories.AddAsync(history, ct);
        await _db.SaveChangesAsync(ct);
    }
}

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _db;
    public CustomerRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Customer>> GetAllAsync(CancellationToken ct = default)
        => await _db.Customers.OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Customers.FindAsync(new object[] { id }, ct);

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _db.Customers.FirstOrDefaultAsync(c => c.Email == email, ct);

    public async Task<Customer> AddAsync(Customer customer, CancellationToken ct = default)
    {
        customer.Id = Guid.NewGuid();
        customer.CreatedAt = DateTime.UtcNow;
        customer.UpdatedAt = DateTime.UtcNow;
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);
        return customer;
    }
}

public class RequestRepository : IRequestRepository
{
    private readonly AppDbContext _db;
    public RequestRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Request>> GetAllAsync(CancellationToken ct = default)
        => await _db.Requests
            .Include(r => r.Customer)
            .Include(r => r.Items).ThenInclude(i => i.Product)
            .OrderByDescending(r => r.RequestDate)
            .ToListAsync(ct);

    public async Task<Request?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Requests
            .Include(r => r.Customer)
            .Include(r => r.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Request> AddAsync(Request request, CancellationToken ct = default)
    {
        _db.Requests.Add(request);
        await _db.SaveChangesAsync(ct);
        return request;
    }

    public async Task UpdateAsync(Request request, CancellationToken ct = default)
    {
        request.UpdatedAt = DateTime.UtcNow;
        _db.Requests.Update(request);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<string> GenerateRequestNoAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _db.Requests.CountAsync(r => r.RequestDate.Year == year, ct);
        return $"TEK-{year}-{(count + 1):D3}";
    }
}
