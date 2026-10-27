using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHaat.Application.Catalog;

namespace SmartHaat.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(ICatalogService catalog) : ControllerBase
{
    [HttpGet("/api/admin/products")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminList([FromQuery] string? search, [FromQuery] Guid? categoryId, [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice, [FromQuery] string? sort = "name_asc", [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || page > int.MaxValue / pageSize || minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["query"] = ["Use page >= 1, pageSize 1–100 and a valid non-negative price range."] }) { Status = StatusCodes.Status400BadRequest });
        return Ok(await catalog.GetProductsAsync(search, categoryId, minPrice, maxPrice, sort, page, pageSize, true, ct));
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] Guid? categoryId, [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice, [FromQuery] string? sort = "name_asc", [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || page > int.MaxValue / pageSize || minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["query"] = ["Use page >= 1, pageSize 1–100 and a valid non-negative price range."] }) { Status = StatusCodes.Status400BadRequest });
        return Ok(await catalog.GetProductsAsync(search, categoryId, minPrice, maxPrice, sort, page, pageSize, false, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    { var product = await catalog.GetProductAsync(id, false, ct); return product is null ? NotFound() : Ok(product); }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(ProductWrite request, CancellationToken ct)
    { var product = await catalog.CreateProductAsync(request, ct); return CreatedAtAction(nameof(Get), new { id = product.Id }, product); }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, ProductWrite request, CancellationToken ct)
    { var product = await catalog.UpdateProductAsync(id, request, ct); return product is null ? NotFound() : Ok(product); }

    [HttpPost("{id:guid}/image")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(Guid id, IFormFile? image, CancellationToken ct)
    {
        if (image is null || image.Length == 0 || image.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "Provide one product image no larger than 5 MB." });
        await using var stream = image.OpenReadStream();
        var product = await catalog.SetProductImageAsync(id, stream, ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPatch("{id:guid}/active")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] bool isActive, CancellationToken ct) =>
        await catalog.SetProductActiveAsync(id, isActive, ct) ? NoContent() : NotFound();
}
