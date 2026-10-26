using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Models;

/// <summary>Категорія навичок. 1:N Category -> Skill.</summary>
public class Category
{
    public int Id { get; private set; }
    public string Name { get; private set; } = "";
    public string Slug { get; private set; } = "";
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public List<Skill> Skills { get; } = new();

    private Category() { }                                   // для EF Core

    public Category(string name, string slug, int sortOrder = 0)
    {
        Apply(name, slug, sortOrder);
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string slug, int sortOrder)
    {
        EnsureNotDeleted();
        Apply(name, slug, sortOrder);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        EnsureNotDeleted();
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    private void Apply(string name, string slug, int sortOrder)
    {
        if (sortOrder < 0) throw new DomainException("SortOrder must not be negative.");
        Name = Guard.Name(name, "Category name");
        Slug = Guard.Slug(slug);
        SortOrder = sortOrder;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted) throw new DomainException("Category is deleted.");
    }
}
