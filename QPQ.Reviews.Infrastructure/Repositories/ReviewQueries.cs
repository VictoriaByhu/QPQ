using AutoMapper;
using MongoDB.Bson;
using MongoDB.Driver;
using QPQ.Reviews.Application.Interfaces;
using QPQ.Reviews.Application.Reviews.Queries.GetReviewById;
using QPQ.Reviews.Domain.Entities;
using QPQ.Reviews.Infrastructure.Persistence;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QPQ.Reviews.Infrastructure.Repositories;

public class ReviewQueries : IReviewQueries
{
    private readonly MongoDbContext _context;
    private readonly IMapper _mapper;

    public ReviewQueries(MongoDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ReviewDto?> GetByIdAsync(string id)
    {
        var objectId = ObjectId.Parse(id);

        // Робимо проєкцію напряму в DTO, оминаючи завантаження всієї сутності
        return await _context.Reviews.Find(x => x.Id == objectId)
            .Project(x => new ReviewDto(
                x.Id.ToString(),
                x.ProductId,
                x.Author.UserId,
                x.Author.Username,
                x.Rating,
                x.Comment,
                x.CreatedAt))
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ReviewDto>> GetListAsync(int page, int pageSize)
    {
        var reviews = await _context.Reviews.Find(_ => true)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ReviewDto>>(reviews);
    }
}