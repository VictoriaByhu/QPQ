using System.Data;
using NSubstitute;
using UserSkills.Bll.Dtos;
using UserSkills.Bll.Services;
using UserSkills.Dal.Interfaces;
using UserSkills.Domain.Exceptions;
using UserSkills.Domain.Models;

namespace UserSkills.Tests;

public class UserSkillServiceTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ISkillRepository _skills = Substitute.For<ISkillRepository>();
    private readonly ILanguageRepository _languages = Substitute.For<ILanguageRepository>();
    private readonly IUserSkillRepository _userSkills = Substitute.For<IUserSkillRepository>();
    private readonly IUserSkillStatusHistoryRepository _history =
        Substitute.For<IUserSkillStatusHistoryRepository>();
    private readonly UserSkillService _service;

    public UserSkillServiceTests()
    {
        _uow.Skills.Returns(_skills);
        _uow.Languages.Returns(_languages);
        _uow.UserSkills.Returns(_userSkills);
        _uow.StatusHistory.Returns(_history);
        _service = new UserSkillService(_uow, MappingTests.CreateMapper());
    }

    private static CreateUserSkillDto ValidInput() => new()
    {
        SkillId = 5,
        Type = "Offer",
        Level = 3,
        Languages = [new LanguageInputDto { LanguageCode = "uk", Proficiency = 5 }],
        Slots = [new AvailabilitySlotInputDto { DayOfWeek = 1, StartTime = "10:00", EndTime = "12:00" }]
    };

    private void ArrangeCreatedRecord(int id)
    {
        _userSkills.GetFullAsync(id, Arg.Any<CancellationToken>()).Returns(new UserSkill
        {
            Id = id, UserId = UserId, SkillId = 5, SkillName = "C#", Type = "Offer", Level = 3,
            Status = "Active", RowVersion = new byte[8]
        });
    }

    [Fact]
    public async Task Create_HappyPath_CommitsAndReturnsRecord()
    {
        _skills.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(new Skill { Id = 5, Name = "C#" });
        _languages.GetUnknownCodesAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());
        _userSkills.AddFullAsync(Arg.Any<UserSkill>(), Arg.Any<CancellationToken>()).Returns(42);
        ArrangeCreatedRecord(42);

        var result = await _service.CreateAsync(UserId, ValidInput());

        Assert.Equal(42, result.Id);
        await _uow.Received(1).BeginTransactionAsync(IsolationLevel.ReadCommitted, Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_UnknownSkill_ThrowsNotFound_AndRollsBack()
    {
        _skills.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Skill?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(UserId, ValidInput()));

        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _userSkills.DidNotReceive().AddFullAsync(Arg.Any<UserSkill>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_UnknownLanguage_ThrowsValidation_AndRollsBack()
    {
        _skills.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(new Skill { Id = 5, Name = "C#" });
        _languages.GetUnknownCodesAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "uk" });

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(UserId, ValidInput()));

        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _userSkills.DidNotReceive().AddFullAsync(Arg.Any<UserSkill>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_RepositoryConflict_RollsBackAndRethrows()
    {
        _skills.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(new Skill { Id = 5, Name = "C#" });
        _languages.GetUnknownCodesAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());
        _userSkills.AddFullAsync(Arg.Any<UserSkill>(), Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new BusinessConflictException("This skill is already in the user list."));

        await Assert.ThrowsAsync<BusinessConflictException>(() => _service.CreateAsync(UserId, ValidInput()));

        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("25:00", "26:00")]
    [InlineData("10:00", "abc")]
    [InlineData("12:00", "10:00")]
    [InlineData("10:00", "10:00")]
    public async Task Create_InvalidSlot_ThrowsValidation_BeforeTouchingDatabase(string start, string end)
    {
        var input = ValidInput();
        input.Slots = [new AvailabilitySlotInputDto { DayOfWeek = 1, StartTime = start, EndTime = end }];

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(UserId, input));

        await _uow.DidNotReceive().BeginTransactionAsync(Arg.Any<IsolationLevel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_DuplicateLanguages_ThrowsValidation()
    {
        var input = ValidInput();
        input.Languages =
        [
            new LanguageInputDto { LanguageCode = "uk", Proficiency = 5 },
            new LanguageInputDto { LanguageCode = "UK", Proficiency = 3 }
        ];

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(UserId, input));
    }

    [Fact]
    public async Task GetById_Missing_ThrowsNotFound()
    {
        _userSkills.GetFullAsync(999, Arg.Any<CancellationToken>()).Returns((UserSkill?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(999));
    }

    [Fact]
    public async Task ChangeStatus_Success_ReturnsNewRowVersion()
    {
        var newVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 9 };
        _userSkills.ChangeStatusAsync(7, "Paused", UserId, Arg.Any<byte[]>(), null, Arg.Any<CancellationToken>())
            .Returns(newVersion);

        var result = await _service.ChangeStatusAsync(UserId, 7,
            new ChangeStatusDto { NewStatus = "Paused", RowVersion = new byte[8] });

        Assert.Equal(newVersion, result.RowVersion);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatus_ForbiddenTransition_RollsBackAndRethrows()
    {
        _userSkills.ChangeStatusAsync(7, "Active", UserId, Arg.Any<byte[]>(), null, Arg.Any<CancellationToken>())
            .Returns<byte[]>(_ => throw new BusinessConflictException("This status transition is not allowed."));

        await Assert.ThrowsAsync<BusinessConflictException>(() => _service.ChangeStatusAsync(UserId, 7,
            new ChangeStatusDto { NewStatus = "Active", RowVersion = new byte[8] }));

        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatus_BadRowVersion_ThrowsValidation()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.ChangeStatusAsync(UserId, 7,
            new ChangeStatusDto { NewStatus = "Paused", RowVersion = [1, 2, 3] }));
    }

    [Fact]
    public async Task GetHistory_ForeignRecord_ThrowsForbidden()
    {
        _userSkills.GetByIdAsync(3, Arg.Any<CancellationToken>())
            .Returns(new UserSkill { Id = 3, UserId = Guid.NewGuid() });

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.GetHistoryAsync(UserId, 3));
    }

    [Fact]
    public async Task List_InvalidPaging_ThrowsValidation()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.ListAsync(UserId, null, null, pageNumber: 0, pageSize: 20));
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.ListAsync(UserId, null, null, pageNumber: 1, pageSize: 500));
    }

    [Fact]
    public async Task List_InvalidType_ThrowsValidation()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.ListAsync(UserId, "Sell", null, 1, 20));
    }
}
