using MediatR;
using QPQ.Reviews.Application.Interfaces;
using QPQ.Reviews.Application.Reviews.Queries.GetReviewById;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QPQ.Reviews.Application.Reviews.Queries.GetReviewsList;

public class GetReviewsListQueryHandler : IRequestHandler<GetReviewsListQuery, IEnumerable<ReviewDto>>
{
    private readonly IReviewQueries _queries;

    public GetReviewsListQueryHandler(IReviewQueries queries)
    {
        _queries = queries;
    }

    public async Task<IEnumerable<ReviewDto>> Handle(GetReviewsListQuery request, CancellationToken cancellationToken)
    {
        return await _queries.GetListAsync(request.Page, request.PageSize);
    }
}