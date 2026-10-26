using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Exceptions;
using Catalog.Domain.Models;

namespace Catalog.Bll.Services;

public class TagService(IUnitOfWork uow, IMapper mapper) : ITagService
{
    public async Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken ct = default)
    {
        var tags = await uow.Tags.GetAllAsync(ct);
        return mapper.Map<List<TagDto>>(tags.OrderBy(t => t.Name));
    }

    public async Task<TagDto> CreateAsync(CreateTagDto input, CancellationToken ct = default)
    {
        var name = input.Name.Trim();
        if (await uow.Tags.ExistsByNameAsync(name, ct))
            throw new ConflictException($"Tag '{name}' already exists.");

        var tag = new Tag(name);
        await uow.Tags.AddAsync(tag, ct);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<TagDto>(tag);
    }
}
