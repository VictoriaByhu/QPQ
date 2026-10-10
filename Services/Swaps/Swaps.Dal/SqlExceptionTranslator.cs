using Microsoft.Data.SqlClient;
using Swaps.Domain.Exceptions;

namespace Swaps.Dal;

public static class SqlExceptionTranslator
{
    private const int SkillNotFound = 50002;
    private const int SameUser = 50003;
    private const int SameSkill = 50004;
    private const int SwapNotFound = 50010;
    private const int ConcurrencyConflict = 50011;
    private const int TransitionNotAllowed = 50012;
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public static bool IsKnown(SqlException ex) => ex.Number is
        SkillNotFound or SameUser or SameSkill or SwapNotFound or ConcurrencyConflict
        or TransitionNotAllowed or UniqueIndexViolation or UniqueConstraintViolation;

    public static Exception Translate(SqlException ex) => ex.Number switch
    {
        SkillNotFound or SwapNotFound => new NotFoundException(ex.Message),
        SameUser or SameSkill => new ValidationException(ex.Message),
        ConcurrencyConflict or TransitionNotAllowed => new BusinessConflictException(ex.Message),
        UniqueIndexViolation or UniqueConstraintViolation =>
            new BusinessConflictException("Record with the same key already exists"),
        _ => ex
    };
}
