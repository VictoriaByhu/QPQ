using QPQ.Reviews.Domain.Entities;
using System.Threading.Tasks;

namespace QPQ.Reviews.Application.Interfaces;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(string id);
    Task AddAsync(Review review);
    Task UpdateAsync(Review review);
    Task DeleteAsync(string id);
}