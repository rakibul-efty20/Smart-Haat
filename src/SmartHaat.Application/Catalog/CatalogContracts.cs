using SmartHaat.Domain.Catalog;
using System.ComponentModel.DataAnnotations;

namespace SmartHaat.Application.Catalog;

public sealed record CategoryDto(Guid Id, string Name, string Slug, string? Description, Guid? ParentCategoryId, bool IsActive, int SortOrder);
public sealed record CategoryWrite([Required, StringLength(120, MinimumLength = 2)] string Name,
    [Required, RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")] string Slug, [StringLength(500)] string? Description,
    Guid? ParentCategoryId, int SortOrder = 0);
public sealed record ProductDto(Guid Id, string Name, string Sku, string Slug, string? Description, string? Brand, Guid CategoryId, string CategoryName, string Unit, decimal Price, decimal ReorderLevel, bool IsActive, string? ImageUrl);
public sealed record ProductWrite([Required, StringLength(200, MinimumLength = 2)] string Name,
    [Required, StringLength(64, MinimumLength = 1)] string Sku, [Required] Guid CategoryId,
    [Required, StringLength(32)] string Unit, [Range(typeof(decimal), "0", "9999999999999999.99")] decimal Price,
    [Range(typeof(decimal), "0", "999999999999999.999")] decimal ReorderLevel = 0,
    [StringLength(2000)] string? Description = null, [StringLength(100)] string? Brand = null);
public sealed record Page<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount);

public interface ICatalogService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(bool includeInactive, CancellationToken ct);
    Task<CategoryDto?> GetCategoryAsync(Guid id, CancellationToken ct);
    Task<CategoryDto> CreateCategoryAsync(CategoryWrite request, CancellationToken ct);
    Task<CategoryDto?> UpdateCategoryAsync(Guid id, CategoryWrite request, CancellationToken ct);
    Task<bool> SetCategoryActiveAsync(Guid id, bool active, CancellationToken ct);
    Task<Page<ProductDto>> GetProductsAsync(string? search, Guid? categoryId, decimal? minPrice, decimal? maxPrice, string? sort, int page, int pageSize, bool includeInactive, CancellationToken ct);
    Task<ProductDto?> GetProductAsync(Guid id, bool includeInactive, CancellationToken ct);
    Task<ProductDto> CreateProductAsync(ProductWrite request, CancellationToken ct);
    Task<ProductDto?> UpdateProductAsync(Guid id, ProductWrite request, CancellationToken ct);
    Task<bool> SetProductActiveAsync(Guid id, bool active, CancellationToken ct);
    Task<ProductDto?> SetProductImageAsync(Guid id, Stream image, CancellationToken ct);
}

public interface IProductImageStorage
{
    Task<string> SaveAsync(Stream content, CancellationToken ct);
    Task DeleteAsync(string key, CancellationToken ct);
}

public sealed class CatalogConflictException(string message) : Exception(message);
public sealed class CatalogValidationException(string message) : Exception(message);
