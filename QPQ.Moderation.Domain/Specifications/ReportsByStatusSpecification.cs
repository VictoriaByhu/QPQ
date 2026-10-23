using QPQ.Moderation.Domain;

namespace QPQ.Moderation.Domain.Specifications;

// Головна зміна: явно вказано Entities.Report
public class ReportsByStatusSpecification : BaseSpecification<Report>
{
    public ReportsByStatusSpecification(string? status)
        : base(x => string.IsNullOrEmpty(status) || x.Status == status)
    {
    }
}