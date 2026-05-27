using Microsoft.EntityFrameworkCore;
using QuoteManagement.Domain.Entities;
using QuoteManagement.Domain.Enums;

namespace QuoteManagement.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductPriceHistory> ProductPriceHistories => Set<ProductPriceHistory>();
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<RequestItem> RequestItems => Set<RequestItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        // ─── User ───────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuid_generate_v4()");
            e.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            e.Property(x => x.Email).HasColumnName("email").IsRequired().HasMaxLength(200);
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
            e.Property(x => x.Role)
             .HasColumnName("role")
             .HasConversion(
                 v => v.ToString().ToUpperInvariant(),
                 v => Enum.Parse<Role>(v, true));
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            e.HasIndex(x => x.Email).IsUnique();
        });

        // ─── Customer ───────────────────────────────────────────
        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("customers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuid_generate_v4()");
            e.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            e.Property(x => x.Email).HasColumnName("email").IsRequired().HasMaxLength(200);
            e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(50);
            e.Property(x => x.Company).HasColumnName("company").HasMaxLength(200);
            e.Property(x => x.Address).HasColumnName("address");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            e.HasIndex(x => x.Email).IsUnique();
        });

        // ─── Product ────────────────────────────────────────────
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuid_generate_v4()");
            e.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(300);
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Category).HasColumnName("category").IsRequired().HasMaxLength(100);
            e.Property(x => x.ModelNumber).HasColumnName("model_number").HasMaxLength(100);
            e.Property(x => x.Specifications).HasColumnName("specifications").HasColumnType("jsonb");
            e.Property(x => x.BasePrice).HasColumnName("base_price").HasPrecision(18, 2);
            e.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(10);
            e.Property(x => x.StockQuantity).HasColumnName("stock_quantity");
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.LastRequestPrice).HasColumnName("last_request_price").HasPrecision(18, 2);
            e.Property(x => x.LastRequestDate).HasColumnName("last_request_date");
            e.Property(x => x.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
            e.Property(x => x.Rating).HasColumnName("rating").HasPrecision(3, 2);
            e.Property(x => x.SalesCount).HasColumnName("sales_count");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        });

        // ─── ProductPriceHistory ────────────────────────────────
        modelBuilder.Entity<ProductPriceHistory>(e =>
        {
            e.ToTable("product_price_histories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuid_generate_v4()");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.Price).HasColumnName("price").HasPrecision(18, 2);
            e.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(10);
            e.Property(x => x.RequestDate).HasColumnName("request_date").HasDefaultValueSql("NOW()");
            e.Property(x => x.Notes).HasColumnName("notes");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            e.HasOne(x => x.Product)
             .WithMany(p => p.PriceHistories)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── Request ────────────────────────────────────────────
        modelBuilder.Entity<Request>(e =>
        {
            e.ToTable("requests");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuid_generate_v4()");
            e.Property(x => x.RequestNo).HasColumnName("request_no").IsRequired().HasMaxLength(50);
            e.Property(x => x.CustomerId).HasColumnName("customer_id");
            e.Property(x => x.RequestDate).HasColumnName("request_date").HasDefaultValueSql("NOW()");
            e.Property(x => x.TotalAmount).HasColumnName("total_amount").HasPrecision(18, 2);
            e.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(10);
            e.Property(x => x.Status)
             .HasColumnName("status")
             .HasConversion(
                 v => v.ToString().ToUpperInvariant(),
                 v => Enum.Parse<RequestStatus>(v, true));
            e.Property(x => x.Notes).HasColumnName("notes");
            e.Property(x => x.ExcelFilePath).HasColumnName("excel_file_path").HasMaxLength(500);
            e.Property(x => x.SentAt).HasColumnName("sent_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            e.HasIndex(x => x.RequestNo).IsUnique();
            e.HasOne(x => x.Customer)
             .WithMany(c => c.Requests)
             .HasForeignKey(x => x.CustomerId);
        });

        // ─── RequestItem ─────────────────────────────────────────
        modelBuilder.Entity<RequestItem>(e =>
        {
            e.ToTable("request_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuid_generate_v4()");
            e.Property(x => x.RequestId).HasColumnName("request_id");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
            e.Property(x => x.DiscountRate).HasColumnName("discount_rate").HasPrecision(5, 2);
            e.Property(x => x.LineTotal).HasColumnName("line_total").HasPrecision(18, 2);
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            e.HasOne(x => x.Request)
             .WithMany(r => r.Items)
             .HasForeignKey(x => x.RequestId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product)
             .WithMany(p => p.RequestItems)
             .HasForeignKey(x => x.ProductId);
        });
    }
}
