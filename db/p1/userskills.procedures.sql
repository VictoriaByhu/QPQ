SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
USE QPQ_UserSkills;
GO

-- Коди помилок цієї бази:
--   50002 навичку не знайдено або видалено      50010 запис не знайдено
--   50011 конфлікт RowVersion                   50012 недопустимий перехід статусу
--   50013 архівний запис не можна редагувати    50014 дію може виконати лише власник
--   50020 неправильний тип (Offer/Want)         50021 така навичка вже є в списку користувача
--   50023 невідомий код мови                    50030 неправильні параметри пагінації

-- Додавання навички користувача однією транзакцією: UserSkills, деталі, мови, доступність, журнал статусів.
CREATE OR ALTER PROCEDURE dbo.usp_AddUserSkill
    @UserId          UNIQUEIDENTIFIER,
    @SkillId         INT,
    @Type            NVARCHAR(10),
    @Level           TINYINT,
    @Description     NVARCHAR(1000) = NULL,
    @ExperienceYears TINYINT        = NULL,
    @PortfolioUrl    NVARCHAR(500)  = NULL,
    @Languages       dbo.UserSkillLanguageList READONLY,
    @Slots           dbo.AvailabilitySlotList  READONLY,
    @NewUserSkillId  INT            OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        IF @Type NOT IN (N'Offer', N'Want')
            THROW 50020, N'Type must be Offer or Want.', 1;

        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Skills WHERE Id = @SkillId AND IsDeleted = 0)
            THROW 50002, N'The skill was not found or has been deleted.', 1;

        IF EXISTS (SELECT 1 FROM dbo.UserSkills
                   WHERE UserId = @UserId AND SkillId = @SkillId AND Type = @Type
                     AND IsDeleted = 0 AND Status <> N'Archived')
            THROW 50021, N'This skill is already in the user list.', 1;

        IF EXISTS (SELECT 1 FROM @Languages l
                   WHERE NOT EXISTS (SELECT 1 FROM dbo.Languages x WHERE x.Code = l.LanguageCode))
            THROW 50023, N'Unknown language code.', 1;

        INSERT dbo.UserSkills (UserId, SkillId, Type, Level, Status, CreatedBy)
        VALUES (@UserId, @SkillId, @Type, @Level, N'Active', @UserId);
        SET @NewUserSkillId = SCOPE_IDENTITY();

        INSERT dbo.UserSkillDetails (UserSkillId, Description, ExperienceYears, PortfolioUrl)
        VALUES (@NewUserSkillId, @Description, @ExperienceYears, @PortfolioUrl);

        INSERT dbo.UserSkillLanguages (UserSkillId, LanguageCode, Proficiency)
        SELECT @NewUserSkillId, LanguageCode, Proficiency FROM @Languages;

        INSERT dbo.AvailabilitySlots (UserSkillId, DayOfWeek, StartTime, EndTime)
        SELECT @NewUserSkillId, DayOfWeek, StartTime, EndTime FROM @Slots;

        INSERT dbo.UserSkillStatusHistory (UserSkillId, OldStatus, NewStatus, ChangedByUserId, Comment)
        VALUES (@NewUserSkillId, NULL, N'Active', @UserId, N'User skill created');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Транзакційна зміна статусу. Допустимі переходи (лише власником):
--   Active -> Paused, Active -> Archived, Paused -> Active, Paused -> Archived. Archived є кінцевим.
CREATE OR ALTER PROCEDURE dbo.usp_ChangeUserSkillStatus
    @UserSkillId     INT,
    @NewStatus       NVARCHAR(20),
    @ChangedByUserId UNIQUEIDENTIFIER,
    @RowVersion      BINARY(8),
    @Comment         NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @OldStatus NVARCHAR(20), @OwnerId UNIQUEIDENTIFIER,
                @CurrentRowVersion BINARY(8), @IsDeleted BIT;

        SELECT @OldStatus = Status, @OwnerId = UserId,
               @CurrentRowVersion = RowVersion, @IsDeleted = IsDeleted
        FROM dbo.UserSkills WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = @UserSkillId;

        IF @OldStatus IS NULL OR @IsDeleted = 1
            THROW 50010, N'User skill not found.', 1;
        IF @CurrentRowVersion <> @RowVersion
            THROW 50011, N'Parallel editing conflict: user skill has already been modified.', 1;
        IF @ChangedByUserId <> @OwnerId
            THROW 50014, N'Only the owner can perform this action.', 1;

        DECLARE @Allowed BIT = 0;
        IF @OldStatus = N'Active' AND @NewStatus IN (N'Paused', N'Archived') SET @Allowed = 1;
        IF @OldStatus = N'Paused' AND @NewStatus IN (N'Active', N'Archived') SET @Allowed = 1;

        IF @Allowed = 0
            THROW 50012, N'This status transition is not allowed.', 1;

        UPDATE dbo.UserSkills
        SET Status = @NewStatus, UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @ChangedByUserId
        WHERE Id = @UserSkillId;

        INSERT dbo.UserSkillStatusHistory (UserSkillId, OldStatus, NewStatus, ChangedByUserId, Comment)
        VALUES (@UserSkillId, @OldStatus, @NewStatus, @ChangedByUserId, @Comment);

        COMMIT TRANSACTION;

        SELECT RowVersion FROM dbo.UserSkills WHERE Id = @UserSkillId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Запис з деталями, мовами та доступністю: чотири набори результатів.
CREATE OR ALTER PROCEDURE dbo.usp_GetUserSkillById
    @UserSkillId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT us.Id, us.UserId, us.SkillId, sk.Name AS SkillName, us.Type, us.Level, us.Status,
           us.CreatedAt, us.UpdatedAt, us.RowVersion
    FROM dbo.UserSkills us
    JOIN dbo.Skills sk ON sk.Id = us.SkillId
    WHERE us.Id = @UserSkillId AND us.IsDeleted = 0;

    SELECT d.UserSkillId, d.Description, d.ExperienceYears, d.PortfolioUrl
    FROM dbo.UserSkillDetails d
    JOIN dbo.UserSkills us ON us.Id = d.UserSkillId
    WHERE d.UserSkillId = @UserSkillId AND us.IsDeleted = 0;

    SELECT l.LanguageCode, lg.Name AS LanguageName, l.Proficiency
    FROM dbo.UserSkillLanguages l
    JOIN dbo.Languages lg ON lg.Code = l.LanguageCode
    JOIN dbo.UserSkills us ON us.Id = l.UserSkillId
    WHERE l.UserSkillId = @UserSkillId AND us.IsDeleted = 0
    ORDER BY l.Proficiency DESC, l.LanguageCode;

    SELECT a.DayOfWeek, a.StartTime, a.EndTime
    FROM dbo.AvailabilitySlots a
    JOIN dbo.UserSkills us ON us.Id = a.UserSkillId
    WHERE a.UserSkillId = @UserSkillId AND us.IsDeleted = 0
    ORDER BY a.DayOfWeek, a.StartTime;
END
GO

-- Список навичок користувача з фільтрами за типом і статусом та пагінацією.
CREATE OR ALTER PROCEDURE dbo.usp_GetUserSkills
    @UserId     UNIQUEIDENTIFIER,
    @Type       NVARCHAR(10) = NULL,
    @Status     NVARCHAR(20) = NULL,
    @PageNumber INT = 1,
    @PageSize   INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber < 1 OR @PageSize < 1 OR @PageSize > 100
        THROW 50030, N'Invalid paging parameters.', 1;

    SELECT us.Id, us.UserId, us.SkillId, sk.Name AS SkillName, us.Type, us.Level, us.Status,
           us.CreatedAt, us.RowVersion
    FROM dbo.UserSkills us
    JOIN dbo.Skills sk ON sk.Id = us.SkillId
    WHERE us.IsDeleted = 0
      AND us.UserId = @UserId
      AND (@Type IS NULL OR us.Type = @Type)
      AND (@Status IS NULL OR us.Status = @Status)
    ORDER BY us.CreatedAt DESC, us.Id DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- Оновлення навички (U у CRUD): рівень, деталі, мови та доступність замінюються цілком.
-- Лише власник і лише для незаархівованого запису; перевіряється RowVersion, повертається нова.
CREATE OR ALTER PROCEDURE dbo.usp_UpdateUserSkill
    @UserSkillId     INT,
    @UpdatedByUserId UNIQUEIDENTIFIER,
    @RowVersion      BINARY(8),
    @Level           TINYINT,
    @Description     NVARCHAR(1000) = NULL,
    @ExperienceYears TINYINT        = NULL,
    @PortfolioUrl    NVARCHAR(500)  = NULL,
    @Languages       dbo.UserSkillLanguageList READONLY,
    @Slots           dbo.AvailabilitySlotList  READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @Status NVARCHAR(20), @OwnerId UNIQUEIDENTIFIER,
                @CurrentRowVersion BINARY(8), @IsDeleted BIT;

        SELECT @Status = Status, @OwnerId = UserId,
               @CurrentRowVersion = RowVersion, @IsDeleted = IsDeleted
        FROM dbo.UserSkills WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = @UserSkillId;

        IF @Status IS NULL OR @IsDeleted = 1
            THROW 50010, N'User skill not found.', 1;
        IF @CurrentRowVersion <> @RowVersion
            THROW 50011, N'Parallel editing conflict: user skill has already been modified.', 1;
        IF @UpdatedByUserId <> @OwnerId
            THROW 50014, N'Only the owner can perform this action.', 1;
        IF @Status = N'Archived'
            THROW 50013, N'An archived user skill cannot be edited.', 1;
        IF EXISTS (SELECT 1 FROM @Languages l
                   WHERE NOT EXISTS (SELECT 1 FROM dbo.Languages x WHERE x.Code = l.LanguageCode))
            THROW 50023, N'Unknown language code.', 1;

        UPDATE dbo.UserSkills
        SET Level = @Level, UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @UpdatedByUserId
        WHERE Id = @UserSkillId;

        UPDATE dbo.UserSkillDetails
        SET Description = @Description, ExperienceYears = @ExperienceYears,
            PortfolioUrl = @PortfolioUrl, UpdatedAt = SYSUTCDATETIME()
        WHERE UserSkillId = @UserSkillId;

        -- дочірні колекції замінюються цілком у межах тієї ж транзакції
        DELETE FROM dbo.UserSkillLanguages WHERE UserSkillId = @UserSkillId;
        INSERT dbo.UserSkillLanguages (UserSkillId, LanguageCode, Proficiency)
        SELECT @UserSkillId, LanguageCode, Proficiency FROM @Languages;

        DELETE FROM dbo.AvailabilitySlots WHERE UserSkillId = @UserSkillId;
        INSERT dbo.AvailabilitySlots (UserSkillId, DayOfWeek, StartTime, EndTime)
        SELECT @UserSkillId, DayOfWeek, StartTime, EndTime FROM @Slots;

        COMMIT TRANSACTION;

        SELECT RowVersion FROM dbo.UserSkills WHERE Id = @UserSkillId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- М'яке видалення (лише власник) з перевіркою RowVersion.
CREATE OR ALTER PROCEDURE dbo.usp_SoftDeleteUserSkill
    @UserSkillId     INT,
    @DeletedByUserId UNIQUEIDENTIFIER,
    @RowVersion      BINARY(8)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @OwnerId UNIQUEIDENTIFIER;
    SELECT @OwnerId = UserId FROM dbo.UserSkills WHERE Id = @UserSkillId AND IsDeleted = 0;

    IF @OwnerId IS NULL
        THROW 50010, N'User skill not found.', 1;
    IF @OwnerId <> @DeletedByUserId
        THROW 50014, N'Only the owner can perform this action.', 1;

    UPDATE dbo.UserSkills
    SET IsDeleted = 1, DeletedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @DeletedByUserId
    WHERE Id = @UserSkillId AND IsDeleted = 0 AND RowVersion = @RowVersion;

    IF @@ROWCOUNT = 0
        THROW 50011, N'Parallel editing conflict: user skill has already been modified.', 1;
END
GO
