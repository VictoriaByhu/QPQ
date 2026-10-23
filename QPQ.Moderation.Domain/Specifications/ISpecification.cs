using System.Linq.Expressions;

namespace QPQ.Moderation.Domain.Specifications;

public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
}