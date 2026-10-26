using QPQ.Reviews.Application.Common.Interfaces;
using QPQ.Reviews.Application.Reviews.Queries.GetReviewById;
using System.Collections.Generic;
// Äמהאי using הכÿ עגמדמ ReviewDto, ÿךשמ ג³ם ג ³םרמלף םוילסןויס³

namespace QPQ.Reviews.Application.Reviews.Queries.GetReviewsList;

public record GetReviewsListQuery(int Page = 1, int PageSize = 10) : IQuery<IEnumerable<ReviewDto>>;