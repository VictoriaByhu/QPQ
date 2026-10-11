using Microsoft.Data.SqlClient;
using UserSkills.Domain.Exceptions;

namespace UserSkills.Dal;

/// <summary>
/// Перетворює помилки процедур QPQ_UserSkills (THROW 500xx) і порушення UNIQUE на доменні винятки,
/// щоб сирі SQL-повідомлення не потрапляли в API.
/// </summary>
public static class SqlExceptionTranslator
{
    private const int SkillNotFound = 50002;
    private const int UserSkillNotFound = 50010;
    private const int ConcurrencyConflict = 50011;
    private const int TransitionNotAllowed = 50012;
    private const int ArchivedReadOnly = 50013;
    private const int OwnerOnly = 50014;
    private const int InvalidType = 50020;
    private const int DuplicateUserSkill = 50021;
    private const int UnknownLanguage = 50023;
    private const int InvalidPaging = 50030;
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public static bool IsKnown(SqlException ex) => ex.Number is
        SkillNotFound or UserSkillNotFound or ConcurrencyConflict or TransitionNotAllowed
        or ArchivedReadOnly or OwnerOnly or InvalidType or DuplicateUserSkill or UnknownLanguage
        or InvalidPaging or UniqueIndexViolation or UniqueConstraintViolation;

    public static Exception Translate(SqlException ex) => ex.Number switch
    {
        SkillNotFound or UserSkillNotFound => new NotFoundException(ex.Message),
        InvalidType or UnknownLanguage or InvalidPaging => new ValidationException(ex.Message),
        OwnerOnly => new ForbiddenException(ex.Message),
        ConcurrencyConflict or TransitionNotAllowed or ArchivedReadOnly or DuplicateUserSkill =>
            new BusinessConflictException(ex.Message),
        UniqueIndexViolation or UniqueConstraintViolation =>
            new BusinessConflictException("Record with the same key already exists"),
        _ => ex
    };
}
