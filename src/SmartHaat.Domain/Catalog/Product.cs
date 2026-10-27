namespace SmartHaat.Domain.Catalog;

public sealed class Product
{
    private Product() { }

    public Product(string name, string sku, Guid categoryId, string unit, decimal price, decimal reorderLevel = 0, string? description = null, string? brand = null)
    {
        Update(name, sku, categoryId, unit, price, reorderLevel, description, brand);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Brand { get; private set; }
    public string? ImageKey { get; private set; }
    public Guid CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public string Unit { get; private set; } = "piece";
    public decimal Price { get; private set; }
    public decimal ReorderLevel { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    public void Update(string name, string sku, Guid categoryId, string unit, decimal price, decimal reorderLevel, string? description, string? brand)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) throw new ArgumentException("Name is required and must be 200 characters or fewer.");
        if (string.IsNullOrWhiteSpace(sku) || sku.Trim().Length > 64) throw new ArgumentException("SKU is required and must be 64 characters or fewer.");
        if (categoryId == Guid.Empty) throw new ArgumentException("Category is required.");
        if (string.IsNullOrWhiteSpace(unit) || unit.Trim().Length > 32) throw new ArgumentException("Unit is required and must be 32 characters or fewer.");
        if (price < 0 || reorderLevel < 0) throw new ArgumentOutOfRangeException(nameof(price), "Price and reorder level cannot be negative.");
        Name = name.Trim(); Sku = sku.Trim().ToUpperInvariant(); Slug = $"{ToSlug(Name)}-{Sku.ToLowerInvariant()}";
        CategoryId = categoryId; Unit = unit.Trim(); Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero);
        ReorderLevel = reorderLevel; Description = Clean(description); Brand = Clean(brand); UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetActive(bool active) { IsActive = active; UpdatedAtUtc = DateTime.UtcNow; }
    public void SetImageKey(string key) { ImageKey = string.IsNullOrWhiteSpace(key) ? null : key; UpdatedAtUtc = DateTime.UtcNow; }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string ToSlug(string text) => string.Join('-', text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
