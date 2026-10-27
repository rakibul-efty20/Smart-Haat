namespace SmartHaat.Domain.Catalog;

public sealed class Category
{
    private Category() { }

    public Category(string name, string slug, string? description = null, Guid? parentCategoryId = null)
    {
        Rename(name, slug);
        Description = Clean(description);
        ParentCategoryId = parentCategoryId;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public Category? ParentCategory { get; private set; }
    public ICollection<Category> Children { get; private set; } = new List<Category>();
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    public void Update(string name, string slug, string? description, Guid? parentCategoryId, int sortOrder)
    {
        if (parentCategoryId == Id) throw new ArgumentException("A category cannot be its own parent.");
        Rename(name, slug);
        Description = Clean(description);
        ParentCategoryId = parentCategoryId;
        SortOrder = sortOrder;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetActive(bool active) { IsActive = active; UpdatedAtUtc = DateTime.UtcNow; }
    private void Rename(string name, string slug)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name is required.") : name.Trim();
        Slug = string.IsNullOrWhiteSpace(slug) ? throw new ArgumentException("Slug is required.") : slug.Trim().ToLowerInvariant();
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
