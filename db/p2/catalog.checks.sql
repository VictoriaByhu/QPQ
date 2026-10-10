-- Перевірочні запити для QPQ_Catalog (Проєкт №2).
-- Запускати після schema.sql і seed.sql:
--   sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i db\p2\catalog.checks.sql
-- Сценарії, що змінюють дані, виконуються в транзакції з ROLLBACK.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
USE QPQ_Catalog;
GO

PRINT N'=== 1. Зовнішні ключі: лише між таблицями QPQ_Catalog ===';
SELECT fk.name AS ForeignKey,
       OBJECT_NAME(fk.parent_object_id)     AS ChildTable,
       OBJECT_NAME(fk.referenced_object_id) AS ParentTable,
       fk.delete_referential_action_desc    AS OnDelete
FROM sys.foreign_keys fk
ORDER BY ChildTable, ForeignKey;
GO

PRINT N'=== 2. Усі три типи зв''язків на прикладі навички C# ===';
-- 1:N категорія -> навички, 1:1 деталі, 1:N рівні, M:N теги з датою додавання
SELECT c.Name AS Category, s.Id AS SkillId, s.Name AS Skill, d.Description
FROM dbo.Skills s
JOIN dbo.Categories c ON c.Id = s.CategoryId
JOIN dbo.SkillDetails d ON d.SkillId = s.Id
WHERE s.Id = 1;

SELECT Level, Title FROM dbo.SkillLevels WHERE SkillId = 1 ORDER BY Level;

SELECT t.Name AS Tag, st.AddedAt
FROM dbo.SkillTags st
JOIN dbo.Tags t ON t.Id = st.TagId
WHERE st.SkillId = 1
ORDER BY t.Name;
GO

PRINT N'=== 3. Пошук: навички за тегом "Онлайн" (запит під IX_SkillTags_Tag) ===';
SELECT s.Id, s.Name
FROM dbo.SkillTags st
JOIN dbo.Skills s ON s.Id = st.SkillId AND s.IsDeleted = 0
WHERE st.TagId = (SELECT Id FROM dbo.Tags WHERE Name = N'Онлайн')
ORDER BY s.Name;
GO

PRINT N'=== 4. Навички категорії зі сторінками по 2 записи (запит під IX_Skills_Category) ===';
SELECT s.Id, s.Name
FROM dbo.Skills s
WHERE s.CategoryId = 2 AND s.IsDeleted = 0
ORDER BY s.Name
OFFSET 0 ROWS FETCH NEXT 2 ROWS ONLY;
GO

PRINT N'=== 5. Унікальність назви навички (очікується помилка 2601) ===';
BEGIN TRY
    INSERT dbo.Skills (CategoryId, Name, Slug) VALUES (1, N'Програмування C#', N'csharp-duplicate');
    PRINT N'ПОМИЛКА: рядок вставлено.';
END TRY
BEGIN CATCH
    PRINT CONCAT(N'OK, помилка ', ERROR_NUMBER(), N': ', ERROR_MESSAGE());
END CATCH
GO

PRINT N'=== 6. CHECK: рівень 9 і slug у верхньому регістрі (очікуються помилки 547) ===';
BEGIN TRY
    INSERT dbo.SkillLevels (SkillId, Level, Title) VALUES (1, 9, N'Неправильний рівень');
    PRINT N'ПОМИЛКА: рядок вставлено.';
END TRY
BEGIN CATCH
    PRINT CONCAT(N'OK, помилка ', ERROR_NUMBER(), N': ', ERROR_MESSAGE());
END CATCH
BEGIN TRY
    INSERT dbo.Categories (Name, Slug) VALUES (N'Тест', N'UPPER');
    PRINT N'ПОМИЛКА: рядок вставлено.';
END TRY
BEGIN CATCH
    PRINT CONCAT(N'OK, помилка ', ERROR_NUMBER(), N': ', ERROR_MESSAGE());
END CATCH
GO

PRINT N'=== 7. М''яке видалення звільняє назву для повторного використання (у транзакції з відкотом) ===';
BEGIN TRANSACTION;
UPDATE dbo.Skills SET IsDeleted = 1, DeletedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME() WHERE Id = 6;
INSERT dbo.Skills (CategoryId, Name, Slug) VALUES (6, N'Приготування випічки', N'baking-new');
SELECT Id, Name, Slug, IsDeleted FROM dbo.Skills WHERE Name = N'Приготування випічки' ORDER BY Id;
ROLLBACK TRANSACTION;
GO

PRINT N'=== 8. Ідемпотентність seed: кількість рядків після повторного запуску seed.sql не змінюється ===';
SELECT (SELECT COUNT(*) FROM dbo.Categories) AS Categories, (SELECT COUNT(*) FROM dbo.Skills) AS Skills,
       (SELECT COUNT(*) FROM dbo.SkillDetails) AS SkillDetails, (SELECT COUNT(*) FROM dbo.SkillLevels) AS SkillLevels,
       (SELECT COUNT(*) FROM dbo.Tags) AS Tags, (SELECT COUNT(*) FROM dbo.SkillTags) AS SkillTags;
-- очікується: 6, 6, 6, 30, 6, 15
GO
