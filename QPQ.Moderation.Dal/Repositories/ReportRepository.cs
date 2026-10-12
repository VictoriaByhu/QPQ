using Dapper;
using Microsoft.Data.SqlClient;
using QPQ.Moderation.Dal.Interfaces;
using QPQ.Moderation.Domain;
using System.Data;

namespace QPQ.Moderation.Dal.Repositories;

public sealed class ReportRepository : BaseDapperRepository<Report>, IReportRepository
{
    public ReportRepository(SqlConnection c, Func<SqlTransaction?> tx)
        : base(c, tx, "dbo.Reports")
    {
    }

    public async Task ChangeStatusAsync(long reportId, string newStatus, CancellationToken ct = default)
    {
        await Connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.usp_ChangeReportStatus",
                new { Id = reportId, NewStatus = newStatus },
                Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }
}