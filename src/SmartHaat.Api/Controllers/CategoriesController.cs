using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHaat.Application.Catalog;

namespace SmartHaat.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ICatalogService catalog) : ControllerBase
{
    [HttpGet("/api/admin/categories")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminList(CancellationToken ct) => Ok(await catalog.GetCategoriesAsync(true, ct));

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await catalog.GetCategoriesAsync(false, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var category = await catalog.GetCategoryAsync(id, ct);
        return category is null || !category.IsActive ? NotFound() : Ok(category);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CategoryWrite request, CancellationToken ct)
    {
        var category = await catalog.CreateCategoryAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = category.Id }, category);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, CategoryWrite request, CancellationToken ct)
    {
        var category = await catalog.UpdateCategoryAsync(id, request, ct);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPatch("{id:guid}/active")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] bool isActive, CancellationToken ct) =>
        await catalog.SetCategoryActiveAsync(id, isActive, ct) ? NoContent() : NotFound();
}
