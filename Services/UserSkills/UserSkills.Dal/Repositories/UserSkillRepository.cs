using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using UserSkills.Dal.Interfaces;
using UserSkills.Domain.Models;

namespace UserSkills.Dal.Repositories;

/// <summary>
/// UserSkills на Dapper. Спільний CRUD — у базовому класі; багатотаблична логіка виконується
/// збереженими процедурами з Лаб. 1 (TVP передаються через DataTable.AsTableValuedParameter).
/// </summary>
public sealed class UserSkillRepository : BaseDapperRepository<UserSkill>, IUserSkillRepository
{
    public UserSkillRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction,
            table: "dbo.UserSkills",
            columns: "Id, UserId, SkillId, Type, Level, Status, CreatedAt, CreatedBy, UpdatedAt, RowVersion",
            softDelete: true)
    {
    }

    protected override IReadOnlyList<string> InsertColumns { get; } =
        ["UserId", "SkillId", "Type", "Level", "Status", "CreatedBy"];

    // Звичайний INSERT оминув би деталі й журнал статусів, тому створення йде через процедуру.
    public override Task<int> AddAsync(UserSkill entity, CancellationToken ct = default)
        => AddFullAsync(entity, ct);

    public async Task<UserSkill?> GetFullAsync(int id, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        try
        {
            using var grid = await Connection.QueryMultipleAsync(new CommandDefinition(
                "dbo.usp_GetUserSkillById", new { UserSkillId = id }, Transaction,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

            var userSkill = await grid.ReadSingleOrDefaultAsync<UserSkill>();
            if (userSkill is null) return null;

            userSkill.Details = await grid.ReadSingleOrDefaultAsync<UserSkillDetails>();
            userSkill.Languages = (await grid.ReadAsync<UserSkillLanguage>()).ToList();
            userSkill.Slots = (await grid.ReadAsync<AvailabilitySlot>()).ToList();
            return userSkill;
        }
        catch (SqlException ex) when (SqlExceptionTranslator.IsKnown(ex))
        {
            throw SqlExceptionTranslator.Translate(ex);
        }
    }

    public async Task<IReadOnlyList<UserSkill>> ListByUserAsync(
        Guid userId, string? type, string? status, int pageNumber, int pageSize,
        CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        try
        {
            var rows = await Connection.QueryAsync<UserSkill>(new CommandDefinition(
                "dbo.usp_GetUserSkills",
                new { UserId = userId, Type = type, Status = status, PageNumber = pageNumber, PageSize = pageSize },
                Transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct));
            return rows.ToList();
        }
        catch (SqlException ex) when (SqlExceptionTranslator.IsKnown(ex))
        {
            throw SqlExceptionTranslator.Translate(ex);
        }
    }

    /// <summary>
    /// Dapper multi-mapping: один JOIN повертає UserSkill і Skill, а <c>splitOn: "Id"</c>
    /// каже, з якого стовпця починається другий об'єкт. Назва навички переноситься в UserSkill.SkillName.
    /// Усі значення йдуть параметрами; запит використовує індекс IX_UserSkills_Skill_Type.
    /// </summary>
    public async Task<IReadOnlyList<UserSkill>> ListBySkillAsync(
        int skillId, string? type, int limit, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP (@Limit)
                   us.Id, us.UserId, us.SkillId, us.Type, us.Level, us.Status,
                   us.CreatedAt, us.CreatedBy, us.UpdatedAt, us.RowVersion,
                   s.Id, s.Name
            FROM dbo.UserSkills AS us
            INNER JOIN dbo.Skills AS s ON s.Id = us.SkillId
            WHERE us.SkillId = @SkillId
              AND us.IsDeleted = 0
              AND us.Status = N'Active'
              AND (@Type IS NULL OR us.Type = @Type)
            ORDER BY us.Level DESC, us.Id
            """;

        await EnsureOpenAsync(ct);
        try
        {
            var rows = await Connection.QueryAsync<UserSkill, Skill, UserSkill>(
                new CommandDefinition(sql, new { SkillId = skillId, Type = type, Limit = limit },
                    Transaction, cancellationToken: ct),
                (userSkill, skill) =>
                {
                    userSkill.SkillName = skill.Name;
                    return userSkill;
                },
                splitOn: "Id");
            return rows.ToList();
        }
        catch (SqlException ex) when (SqlExceptionTranslator.IsKnown(ex))
        {
            throw SqlExceptionTranslator.Translate(ex);
        }
    }

    public async Task<int> AddFullAsync(UserSkill entity, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);

        var parameters = new DynamicParameters();
        parameters.Add("@UserId", entity.UserId, DbType.Guid);
        parameters.Add("@SkillId", entity.SkillId, DbType.Int32);
        parameters.Add("@Type", entity.Type, DbType.String, size: 10);
        parameters.Add("@Level", entity.Level, DbType.Byte);
        parameters.Add("@Description", entity.Details?.Description, DbType.String, size: 1000);
        parameters.Add("@ExperienceYears", entity.Details?.ExperienceYears, DbType.Byte);
        parameters.Add("@PortfolioUrl", entity.Details?.PortfolioUrl, DbType.String, size: 500);
        parameters.Add("@Languages",
            ToLanguageTable(entity.Languages).AsTableValuedParameter("dbo.UserSkillLanguageList"));
        parameters.Add("@Slots",
            ToSlotTable(entity.Slots).AsTableValuedParameter("dbo.AvailabilitySlotList"));
        parameters.Add("@NewUserSkillId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        try
        {
            await Connection.ExecuteAsync(new CommandDefinition(
                "dbo.usp_AddUserSkill", parameters, Transaction,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
        }
        catch (SqlException ex) when (SqlExceptionTranslator.IsKnown(ex))
        {
            throw SqlExceptionTranslator.Translate(ex);
        }

        return parameters.Get<int>("@NewUserSkillId");
    }

    public async Task<byte[]> UpdateFullAsync(
        int id, Guid updatedBy, byte[] rowVersion, UserSkill changes, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);

        var parameters = new DynamicParameters();
        parameters.Add("@UserSkillId", id, DbType.Int32);
        parameters.Add("@UpdatedByUserId", updatedBy, DbType.Guid);
        parameters.Add("@RowVersion", rowVersion, DbType.Binary, size: 8);
        parameters.Add("@Level", changes.Level, DbType.Byte);
        parameters.Add("@Description", changes.Details?.Description, DbType.String, size: 1000);
        parameters.Add("@ExperienceYears", changes.Details?.ExperienceYears, DbType.Byte);
        parameters.Add("@PortfolioUrl", changes.Details?.PortfolioUrl, DbType.String, size: 500);
        parameters.Add("@Languages",
            ToLanguageTable(changes.Languages).AsTableValuedParameter("dbo.UserSkillLanguageList"));
        parameters.Add("@Slots",
            ToSlotTable(changes.Slots).AsTableValuedParameter("dbo.AvailabilitySlotList"));

        try
        {
            return await Connection.QuerySingleAsync<byte[]>(new CommandDefinition(
                "dbo.usp_UpdateUserSkill", parameters, Transaction,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
        }
        catch (SqlException ex) when (SqlExceptionTranslator.IsKnown(ex))
        {
            throw SqlExceptionTranslator.Translate(ex);
        }
    }

    public async Task<byte[]> ChangeStatusAsync(
        int id, string newStatus, Guid changedBy, byte[] rowVersion, string? comment,
        CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);

        var parameters = new DynamicParameters();
        parameters.Add("@UserSkillId", id, DbType.Int32);
        parameters.Add("@NewStatus", newStatus, DbType.String, size: 20);
        parameters.Add("@ChangedByUserId", changedBy, DbType.Guid);
        parameters.Add("@RowVersion", rowVersion, DbType.Binary, size: 8);
        parameters.Add("@Comment", comment, DbType.String, size: 500);

        try
        {
            return await Connection.QuerySingleAsync<byte[]>(new CommandDefinition(
                "dbo.usp_ChangeUserSkillStatus", parameters, Transaction,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
        }
        catch (SqlException ex) when (SqlExceptionTranslator.IsKnown(ex))
        {
            throw SqlExceptionTranslator.Translate(ex);
        }
    }

    public async Task SoftDeleteAsync(
        int id, Guid deletedBy, byte[] rowVersion, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);

        var parameters = new DynamicParameters();
        parameters.Add("@UserSkillId", id, DbType.Int32);
        parameters.Add("@DeletedByUserId", deletedBy, DbType.Guid);
        parameters.Add("@RowVersion", rowVersion, DbType.Binary, size: 8);

        try
        {
            await Connection.ExecuteAsync(new CommandDefinition(
                "dbo.usp_SoftDeleteUserSkill", parameters, Transaction,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
        }
        catch (SqlException ex) when (SqlExceptionTranslator.IsKnown(ex))
        {
            throw SqlExceptionTranslator.Translate(ex);
        }
    }

    private static DataTable ToLanguageTable(IEnumerable<UserSkillLanguage> languages)
    {
        var table = new DataTable();
        table.Columns.Add("LanguageCode", typeof(string));
        table.Columns.Add("Proficiency", typeof(byte));
        foreach (var language in languages)
            table.Rows.Add(language.LanguageCode, language.Proficiency);
        return table;
    }

    private static DataTable ToSlotTable(IEnumerable<AvailabilitySlot> slots)
    {
        var table = new DataTable();
        table.Columns.Add("DayOfWeek", typeof(byte));
        table.Columns.Add("StartTime", typeof(TimeSpan));
        table.Columns.Add("EndTime", typeof(TimeSpan));
        foreach (var slot in slots)
            table.Rows.Add(slot.DayOfWeek, slot.StartTime, slot.EndTime);
        return table;
    }
}
