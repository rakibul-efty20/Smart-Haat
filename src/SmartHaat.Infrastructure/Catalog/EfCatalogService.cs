using Microsoft.EntityFrameworkCore;
using SmartHaat.Domain.Catalog;
using SmartHaat.Infrastructure.Persistence;
using SmartHaat.Application.Catalog;

namespace SmartHaat.Infrastructure.Catalog;

public sealed class EfCatalogService(SmartHaatDbContext db, IProductImageStorage imageStorage) : ICatalogService
{
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(bool includeInactive, CancellationToken ct) =>
        await db.Categories.AsNoTracking().Where(c => includeInactive || c.IsActive).OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.ParentCategoryId, c.IsActive, c.SortOrder)).ToListAsync(ct);

    public async Task<CategoryDto?> GetCategoryAsync(Guid id, CancellationToken ct) =>
        await db.Categories.AsNoTracking().Where(c => c.Id == id).Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.ParentCategoryId, c.IsActive, c.SortOrder)).SingleOrDefaultAsync(ct);

    public async Task<CategoryDto> CreateCategoryAsync(CategoryWrite r, CancellationToken ct)
    {
        if (r.ParentCategoryId is Guid parent && !await db.Categories.AnyAsync(x => x.Id == parent && x.IsActive, ct)) throw new CatalogValidationException("Parent category does not exist or is inactive.");
        var item = new Category(r.Name, r.Slug, r.Description, r.ParentCategoryId); item.Update(r.Name, r.Slug, r.Description, r.ParentCategoryId, r.SortOrder);
        db.Categories.Add(item); await SaveAsync(ct); return ToDto(item);
    }
    public async Task<CategoryDto?> UpdateCategoryAsync(Guid id, CategoryWrite r, CancellationToken ct)
    {
        var item = await db.Categories.FindAsync([id], ct); if (item is null) return null;
        if (r.ParentCategoryId is Guid parent && (parent == id || !await db.Categories.AnyAsync(x => x.Id == parent && x.IsActive, ct))) throw new CatalogValidationException("Parent category is invalid.");
        var ancestor = r.ParentCategoryId;
        while (ancestor.HasValue)
        {
            if (ancestor.Value == id) throw new CatalogValidationException("Category hierarchy cannot contain a cycle.");
            ancestor = await db.Categories.AsNoTracking().Where(x => x.Id == ancestor.Value).Select(x => x.ParentCategoryId).SingleOrDefaultAsync(ct);
        }
        item.Update(r.Name, r.Slug, r.Description, r.ParentCategoryId, r.SortOrder); await SaveAsync(ct); return ToDto(item);
    }
    public async Task<bool> SetCategoryActiveAsync(Guid id, bool active, CancellationToken ct)
    {
        var item = await db.Categories.FindAsync([id], ct); if (item is null) return false;
        if (!active && await db.Categories.AnyAsync(c => c.ParentCategoryId == id && c.IsActive, ct)) throw new CatalogValidationException("Deactivate child categories first.");
        item.SetActive(active); await db.SaveChangesAsync(ct); return true;
    }

    public async Task<Page<ProductDto>> GetProductsAsync(string? search, Guid? categoryId, decimal? minPrice, decimal? maxPrice, string? sort, int page, int pageSize, bool includeInactive, CancellationToken ct)
    {
        var q = db.Products.AsNoTracking().Include(p => p.Category).AsQueryable();
        if (!includeInactive) q = q.Where(p => p.IsActive && p.Category!.IsActive);
        if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); q = q.Where(p => p.Name.Contains(term) || p.Sku.Contains(term) || (p.Brand != null && p.Brand.Contains(term))); }
        if (minPrice.HasValue) q = q.Where(p => p.Price >= minPrice);
        if (maxPrice.HasValue) q = q.Where(p => p.Price <= maxPrice);
        q = sort?.ToLowerInvariant() switch { "price_asc" => q.OrderBy(p => p.Price), "price_desc" => q.OrderByDescending(p => p.Price), "name_desc" => q.OrderByDescending(p => p.Name), _ => q.OrderBy(p => p.Name) };
        var count = await q.CountAsync(ct); var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new ProductDto(p.Id, p.Name, p.Sku, p.Slug, p.Description, p.Brand, p.CategoryId, p.Category!.Name, p.Unit, p.Price, p.ReorderLevel, p.IsActive, p.ImageKey == null ? null : "/media/products/" + p.ImageKey)).ToListAsync(ct);
        return new(items, page, pageSize, count);
    }
    public async Task<ProductDto?> GetProductAsync(Guid id, bool includeInactive, CancellationToken ct) =>
        await db.Products.AsNoTracking().Include(p => p.Category).Where(p => p.Id == id && (includeInactive || (p.IsActive && p.Category!.IsActive)))
            .Select(p => new ProductDto(p.Id, p.Name, p.Sku, p.Slug, p.Description, p.Brand, p.CategoryId, p.Category!.Name, p.Unit, p.Price, p.ReorderLevel, p.IsActive, p.ImageKey == null ? null : "/media/products/" + p.ImageKey)).SingleOrDefaultAsync(ct);

    public async Task<ProductDto> CreateProductAsync(ProductWrite r, CancellationToken ct)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == r.CategoryId && c.IsActive, ct)) throw new CatalogValidationException("Category does not exist or is inactive.");
        var p = new Product(r.Name, r.Sku, r.CategoryId, r.Unit, r.Price, r.ReorderLevel, r.Description, r.Brand); db.Products.Add(p); await SaveAsync(ct);
        await db.Entry(p).Reference(x => x.Category).LoadAsync(ct); return ToDto(p);
    }
    public async Task<ProductDto?> UpdateProductAsync(Guid id, ProductWrite r, CancellationToken ct)
    {
        var p = await db.Products.FindAsync([id], ct); if (p is null) return null;
        if (!await db.Categories.AnyAsync(c => c.Id == r.CategoryId && c.IsActive, ct)) throw new CatalogValidationException("Category does not exist or is inactive.");
        p.Update(r.Name, r.Sku, r.CategoryId, r.Unit, r.Price, r.ReorderLevel, r.Description, r.Brand); await SaveAsync(ct);
        await db.Entry(p).Reference(x => x.Category).LoadAsync(ct); return ToDto(p);
    }
    public async Task<bool> SetProductActiveAsync(Guid id, bool active, CancellationToken ct)
    { var p = await db.Products.FindAsync([id], ct); if (p is null) return false; p.SetActive(active); await db.SaveChangesAsync(ct); return true; }

    public async Task<ProductDto?> SetProductImageAsync(Guid id, Stream image, CancellationToken ct)
    {
        var p = await db.Products.Include(x => x.Category).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;
        var previous = p.ImageKey;
        var key = await imageStorage.SaveAsync(image, ct);
        p.SetImageKey(key);
        try { await db.SaveChangesAsync(ct); }
        catch { await imageStorage.DeleteAsync(key, CancellationToken.None); throw; }
        if (previous is not null) await imageStorage.DeleteAsync(previous, ct);
        return ToDto(p);
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true || e.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true)
        { throw new CatalogConflictException("A category slug or product SKU is already in use."); }
    }
    private static CategoryDto ToDto(Category c) => new(c.Id, c.Name, c.Slug, c.Description, c.ParentCategoryId, c.IsActive, c.SortOrder);
    private static ProductDto ToDto(Product p) => new(p.Id, p.Name, p.Sku, p.Slug, p.Description, p.Brand, p.CategoryId, p.Category!.Name, p.Unit, p.Price, p.ReorderLevel, p.IsActive,
        p.ImageKey is null ? null : $"/media/products/{p.ImageKey}");
}
