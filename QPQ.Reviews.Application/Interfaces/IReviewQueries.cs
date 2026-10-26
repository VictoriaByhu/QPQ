using QPQ.Reviews.Application.Reviews.Queries.GetReviewById;
using System.Threading.Tasks;

namespace QPQ.Reviews.Application.Interfaces;

public interface IReviewQueries
{
    Task<ReviewDto?> GetByIdAsync(string id);
    Task<IEnumerable<ReviewDto>> GetListAsync(int page, int pageSize);
}