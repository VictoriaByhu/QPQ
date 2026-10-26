using Catalog.Domain.Constants;

namespace Catalog.Domain.Models;

public class Tag
{
    public int Id { get; private set; }
    public string Name { get; private set; } = "";

    public List<SkillTag> SkillTags { get; } = new();

    private Tag() { }

    public Tag(string name)
    {
        Name = Guard.Name(name, "Tag name", CatalogConstants.TagNameMaxLength);
    }
}
