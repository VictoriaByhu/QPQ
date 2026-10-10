-- Перевірочні запити для QPQ_UserSkills (Проєкт №1).
-- Запускати після schema.sql, procedures.sql і seed.sql:
--   sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i db\p1\userskills.checks.sql
-- Сценарії, що змінюють дані, виконуються в транзакції з ROLLBACK, тож тестові дані не псуються.
-- Користувачі з seed.sql: U1 = 3f2a9c10..., U2 = 7c4d1e22..., U3 = b19e6a33..., U4 = e8d05b44...
-- UserSkills: 1 = U1 Offer C# (Active), 8 = U3 Want C# (Paused), 10 = U4 Want C# (Archived).
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
USE QPQ_UserSkills;
GO

PRINT N'=== 1. Зовнішні ключі: жодних посилань за межі бази (усі таблиці з QPQ_UserSkills) ===';
SELECT fk.name AS ForeignKey,
       OBJECT_NAME(fk.parent_object_id)     AS ChildTable,
       OBJECT_NAME(fk.referenced_object_id) AS ParentTable,
       fk.delete_referential_action_desc    AS OnDelete
FROM sys.foreign_keys fk
ORDER BY ChildTable, ForeignKey;
GO

PRINT N'=== 2. Індекси та CHECK-обмеження ===';
SELECT OBJECT_NAME(i.object_id) AS TableName, i.name AS IndexName, i.is_unique AS IsUnique,
       i.filter_definition AS FilterDefinition
FROM sys.indexes i
WHERE i.object_id IN (SELECT object_id FROM sys.tables) AND i.name IS NOT NULL AND i.is_primary_key = 0
ORDER BY TableName, IndexName;

SELECT OBJECT_NAME(parent_object_id) AS TableName, name AS ConstraintName, [definition]
FROM sys.check_constraints
ORDER BY TableName, name;
GO

PRINT N'=== 3. Недопустимий перехід: Archived -> Active (очікується помилка 50012) ===';
DECLARE @RV BINARY(8) = (SELECT RowVersion FROM dbo.UserSkills WHERE Id = 10);
BEGIN TRY
    EXEC dbo.usp_ChangeUserSkillStatus @UserSkillId = 10, @NewStatus = N'Active',
         @ChangedByUserId = 'e8d05b44-9f7a-4c21-a6b3-1d2e4f7a8b04', @RowVersion = @RV;
    PRINT N'ПОМИЛКА: перехід було виконано.';
END TRY
BEGIN CATCH
    PRINT CONCAT(N'OK, керована помилка ', ERROR_NUMBER(), N': ', ERROR_MESSAGE());
END CATCH
GO

PRINT N'=== 4. Статус змінює не власник (очікується 50014) ===';
DECLARE @RV BINARY(8) = (SELECT RowVersion FROM dbo.UserSkills WHERE Id = 1);
BEGIN TRY
    EXEC dbo.usp_ChangeUserSkillStatus @UserSkillId = 1, @NewStatus = N'Paused',
         @ChangedByUserId = '7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02', @RowVersion = @RV;
    PRINT N'ПОМИЛКА: перехід було виконано.';
END TRY
BEGIN CATCH
    PRINT CONCAT(N'OK, керована помилка ', ERROR_NUMBER(), N': ', ERROR_MESSAGE());
END CATCH
GO

PRINT N'=== 5. Застаріла RowVersion (очікується 50011) ===';
BEGIN TRY
    EXEC dbo.usp_ChangeUserSkillStatus @UserSkillId = 1, @NewStatus = N'Paused',
         @ChangedByUserId = '3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01', @RowVersion = 0x0000000000000000;
    PRINT N'ПОМИЛКА: зміну було виконано.';
END TRY
BEGIN CATCH
    PRINT CONCAT(N'OK, керована помилка ', ERROR_NUMBER(), N': ', ERROR_MESSAGE());
END CATCH
GO

PRINT N'=== 6. Допустимі переходи Active -> Paused -> Active (у транзакції з відкотом) ===';
BEGIN TRANSACTION;
DECLARE @RV BINARY(8) = (SELECT RowVersion FROM dbo.UserSkills WHERE Id = 1);
DECLARE @Tmp TABLE (RowVersion BINARY(8));
INSERT @Tmp EXEC dbo.usp_ChangeUserSkillStatus @UserSkillId = 1, @NewStatus = N'Paused',
     @ChangedByUserId = '3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01', @RowVersion = @RV, @Comment = N'check';
SET @RV = (SELECT TOP (1) RowVersion FROM @Tmp);
EXEC dbo.usp_ChangeUserSkillStatus @UserSkillId = 1, @NewStatus = N'Active',
     @ChangedByUserId = '3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01', @RowVersion = @RV, @Comment = N'check back';
SELECT UserSkillId, OldStatus, NewStatus, Comment FROM dbo.UserSkillStatusHistory WHERE UserSkillId = 1 ORDER BY ChangedAt, Id;
ROLLBACK TRANSACTION;
SELECT Id, Status FROM dbo.UserSkills WHERE Id = 1; -- знову Active, історія без перевірочних записів
GO

PRINT N'=== 7. CRUD: додавання, читання, оновлення, м''яке видалення (у транзакції з відкотом) ===';
BEGIN TRANSACTION;
DECLARE @NewId INT;
DECLARE @Langs dbo.UserSkillLanguageList;
DECLARE @Slots dbo.AvailabilitySlotList;
INSERT @Langs VALUES (N'uk', 5), (N'de', 2);
INSERT @Slots VALUES (2, '18:00', '19:30'), (4, '18:00', '19:30');

EXEC dbo.usp_AddUserSkill @UserId = '3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01', @SkillId = 5, @Type = N'Want',
     @Level = 1, @Description = N'Хочу навчитися фотографувати', @Languages = @Langs, @Slots = @Slots,
     @NewUserSkillId = @NewId OUTPUT;

EXEC dbo.usp_GetUserSkillById @UserSkillId = @NewId;

DECLARE @RV BINARY(8) = (SELECT RowVersion FROM dbo.UserSkills WHERE Id = @NewId);
DELETE FROM @Langs WHERE 1 = 1; DELETE FROM @Slots WHERE 1 = 1;
INSERT @Langs VALUES (N'uk', 5);
INSERT @Slots VALUES (6, '10:00', '12:00');
EXEC dbo.usp_UpdateUserSkill @UserSkillId = @NewId, @UpdatedByUserId = '3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01',
     @RowVersion = @RV, @Level = 2, @Description = N'Оновлений опис', @Languages = @Langs, @Slots = @Slots;
EXEC dbo.usp_GetUserSkillById @UserSkillId = @NewId;

SET @RV = (SELECT RowVersion FROM dbo.UserSkills WHERE Id = @NewId);
EXEC dbo.usp_SoftDeleteUserSkill @UserSkillId = @NewId, @DeletedByUserId = '3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01', @RowVersion = @RV;
SELECT Id, IsDeleted, DeletedAt FROM dbo.UserSkills WHERE Id = @NewId;
ROLLBACK TRANSACTION;
GO

PRINT N'=== 8. Дубль живого запису (U1 вже пропонує C#, очікується 50021) ===';
DECLARE @NewId INT;
DECLARE @Langs dbo.UserSkillLanguageList;
DECLARE @Slots dbo.AvailabilitySlotList;
BEGIN TRY
    EXEC dbo.usp_AddUserSkill @UserId = '3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01', @SkillId = 1, @Type = N'Offer',
         @Level = 3, @Languages = @Langs, @Slots = @Slots, @NewUserSkillId = @NewId OUTPUT;
    PRINT N'ПОМИЛКА: запис додано.';
END TRY
BEGIN CATCH
    PRINT CONCAT(N'OK, керована помилка ', ERROR_NUMBER(), N': ', ERROR_MESSAGE());
END CATCH
GO

PRINT N'=== 9. Після архівування запис можна додати знову (U4 Want C# уже Archived, у транзакції з відкотом) ===';
BEGIN TRANSACTION;
DECLARE @NewId INT;
DECLARE @Langs dbo.UserSkillLanguageList;
DECLARE @Slots dbo.AvailabilitySlotList;
EXEC dbo.usp_AddUserSkill @UserId = 'e8d05b44-9f7a-4c21-a6b3-1d2e4f7a8b04', @SkillId = 1, @Type = N'Want',
     @Level = 2, @Languages = @Langs, @Slots = @Slots, @NewUserSkillId = @NewId OUTPUT;
SELECT @NewId AS NewUserSkillId;
ROLLBACK TRANSACTION;
GO

PRINT N'=== 10. Архівний запис не можна редагувати (UserSkills 10, очікується 50013) ===';
DECLARE @RV BINARY(8) = (SELECT RowVersion FROM dbo.UserSkills WHERE Id = 10);
DECLARE @Langs dbo.UserSkillLanguageList;
DECLARE @Slots dbo.AvailabilitySlotList;
BEGIN TRY
    EXEC dbo.usp_UpdateUserSkill @UserSkillId = 10, @UpdatedByUserId = 'e8d05b44-9f7a-4c21-a6b3-1d2e4f7a8b04',
         @RowVersion = @RV, @Level = 3, @Languages = @Langs, @Slots = @Slots;
    PRINT N'ПОМИЛКА: оновлення було виконано.';
END TRY
BEGIN CATCH
    PRINT CONCAT(N'OK, керована помилка ', ERROR_NUMBER(), N': ', ERROR_MESSAGE());
END CATCH
GO

PRINT N'=== 11. Пошук і пагінація: навички U2, сторінки по 2 записи; лише Want ===';
EXEC dbo.usp_GetUserSkills @UserId = '7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02', @PageNumber = 1, @PageSize = 2;
EXEC dbo.usp_GetUserSkills @UserId = '7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02', @PageNumber = 2, @PageSize = 2;
EXEC dbo.usp_GetUserSkills @UserId = '7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02', @Type = N'Want';
GO

PRINT N'=== 12. Хто активно пропонує навичку C# (запит під IX_UserSkills_Skill_Type) ===';
SELECT us.UserId, us.Level
FROM dbo.UserSkills us
WHERE us.SkillId = 1 AND us.Type = N'Offer' AND us.IsDeleted = 0 AND us.Status = N'Active';
GO

PRINT N'=== 13. CHECK-обмеження: рівень 9 (очікується помилка 547) ===';
BEGIN TRY
    INSERT dbo.UserSkills (UserId, SkillId, Type, Level, CreatedBy)
    VALUES ('3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01', 6, N'Want', 9, '3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01');
    PRINT N'ПОМИЛКА: рядок вставлено.';
END TRY
BEGIN CATCH
    PRINT CONCAT(N'OK, помилка ', ERROR_NUMBER(), N': ', ERROR_MESSAGE());
END CATCH
GO

PRINT N'=== 14. Ідемпотентність seed: кількість рядків після повторного запуску seed.sql не змінюється ===';
SELECT (SELECT COUNT(*) FROM dbo.Skills) AS Skills, (SELECT COUNT(*) FROM dbo.Languages) AS Languages,
       (SELECT COUNT(*) FROM dbo.UserSkills) AS UserSkills, (SELECT COUNT(*) FROM dbo.UserSkillDetails) AS UserSkillDetails,
       (SELECT COUNT(*) FROM dbo.UserSkillLanguages) AS UserSkillLanguages,
       (SELECT COUNT(*) FROM dbo.AvailabilitySlots) AS AvailabilitySlots,
       (SELECT COUNT(*) FROM dbo.UserSkillStatusHistory) AS UserSkillStatusHistory;
-- очікується: 6, 4, 10, 10, 15, 11, 12
GO
