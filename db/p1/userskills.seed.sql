SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
USE QPQ_UserSkills;
GO

-- Ідентифікатори навичок збігаються з довідником Catalog (db/p2/catalog.seed.sql) та з іншими контекстами QPQ.
MERGE dbo.Skills AS t
USING (VALUES
    (1, N'Програмування C#'),
    (2, N'Англійська розмовна'),
    (3, N'Гра на гітарі'),
    (4, N'UI-дизайн'),
    (5, N'Фотографія'),
    (6, N'Приготування випічки')
) AS s (Id, Name)
ON t.Id = s.Id
WHEN NOT MATCHED THEN
    INSERT (Id, Name) VALUES (s.Id, s.Name);
GO

MERGE dbo.Languages AS t
USING (VALUES
    (N'uk', N'Українська'),
    (N'en', N'Англійська'),
    (N'pl', N'Польська'),
    (N'de', N'Німецька')
) AS s (Code, Name)
ON t.Code = s.Code
WHEN NOT MATCHED THEN
    INSERT (Code, Name) VALUES (s.Code, s.Name);
GO

-- Користувачі U1-U4 ті самі, що в інших контекстах QPQ (їх видає Identity).
DECLARE @U1 UNIQUEIDENTIFIER = '3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01';
DECLARE @U2 UNIQUEIDENTIFIER = '7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02';
DECLARE @U3 UNIQUEIDENTIFIER = 'b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03';
DECLARE @U4 UNIQUEIDENTIFIER = 'e8d05b44-9f7a-4c21-a6b3-1d2e4f7a8b04';

SET IDENTITY_INSERT dbo.UserSkills ON;
MERGE dbo.UserSkills AS t
USING (VALUES
    (1,  @U1, 1, N'Offer', 4, N'Active'),
    (2,  @U1, 2, N'Want',  2, N'Active'),
    (3,  @U2, 2, N'Offer', 5, N'Active'),
    (4,  @U2, 1, N'Want',  2, N'Active'),
    (5,  @U2, 3, N'Want',  1, N'Active'),
    (6,  @U3, 3, N'Offer', 4, N'Active'),
    (7,  @U3, 4, N'Offer', 3, N'Active'),
    (8,  @U3, 1, N'Want',  2, N'Paused'),
    (9,  @U4, 5, N'Offer', 4, N'Active'),
    (10, @U4, 1, N'Want',  1, N'Archived')
) AS s (Id, UserId, SkillId, Type, Level, Status)
ON t.Id = s.Id
WHEN NOT MATCHED THEN
    INSERT (Id, UserId, SkillId, Type, Level, Status, CreatedBy)
    VALUES (s.Id, s.UserId, s.SkillId, s.Type, s.Level, s.Status, s.UserId);
SET IDENTITY_INSERT dbo.UserSkills OFF;
GO

MERGE dbo.UserSkillDetails AS t
USING (VALUES
    (1,  N'Пишу на C# та .NET, допоможу з основами й практикою', 5,    N'https://github.com/example/csharp-notes'),
    (2,  N'Хочу підтягнути розмовну англійську',                 NULL, NULL),
    (3,  N'Викладаю розмовну англійську, рівень C2',             8,    NULL),
    (4,  N'Потрібна допомога з основами C#',                     NULL, NULL),
    (5,  N'Хочу навчитися грати прості пісні',                   NULL, NULL),
    (6,  N'Акустична гітара, вчу акорди та бій',                 6,    NULL),
    (7,  N'Макети інтерфейсів у Figma',                          3,    N'https://www.behance.net/example'),
    (8,  N'Хочу почати з C#, але зараз немає часу',              NULL, NULL),
    (9,  N'Портретна та вулична фотографія',                     4,    N'https://example.com/portfolio'),
    (10, N'Більше не актуально',                                 NULL, NULL)
) AS s (UserSkillId, Description, ExperienceYears, PortfolioUrl)
ON t.UserSkillId = s.UserSkillId
WHEN NOT MATCHED THEN
    INSERT (UserSkillId, Description, ExperienceYears, PortfolioUrl)
    VALUES (s.UserSkillId, s.Description, s.ExperienceYears, s.PortfolioUrl);
GO

MERGE dbo.UserSkillLanguages AS t
USING (VALUES
    (1, N'uk', 5), (1, N'en', 3),
    (2, N'uk', 5),
    (3, N'uk', 5), (3, N'en', 5), (3, N'pl', 3),
    (4, N'uk', 5),
    (5, N'uk', 5),
    (6, N'uk', 5),
    (7, N'uk', 5), (7, N'en', 4),
    (8, N'uk', 5),
    (9, N'uk', 5), (9, N'en', 3),
    (10, N'uk', 5)
) AS s (UserSkillId, LanguageCode, Proficiency)
ON t.UserSkillId = s.UserSkillId AND t.LanguageCode = s.LanguageCode
WHEN NOT MATCHED THEN
    INSERT (UserSkillId, LanguageCode, Proficiency)
    VALUES (s.UserSkillId, s.LanguageCode, s.Proficiency);
GO

MERGE dbo.AvailabilitySlots AS t
USING (VALUES
    (1, 1, CAST('18:00' AS TIME(0)), CAST('20:00' AS TIME(0))),
    (1, 3, CAST('18:00' AS TIME(0)), CAST('20:00' AS TIME(0))),
    (2, 6, CAST('10:00' AS TIME(0)), CAST('12:00' AS TIME(0))),
    (3, 2, CAST('17:00' AS TIME(0)), CAST('19:00' AS TIME(0))),
    (3, 4, CAST('17:00' AS TIME(0)), CAST('19:00' AS TIME(0))),
    (4, 1, CAST('18:00' AS TIME(0)), CAST('20:00' AS TIME(0))),
    (5, 4, CAST('17:00' AS TIME(0)), CAST('19:00' AS TIME(0))),
    (6, 5, CAST('16:00' AS TIME(0)), CAST('18:00' AS TIME(0))),
    (7, 7, CAST('12:00' AS TIME(0)), CAST('14:00' AS TIME(0))),
    (8, 3, CAST('19:00' AS TIME(0)), CAST('21:00' AS TIME(0))),
    (9, 6, CAST('12:00' AS TIME(0)), CAST('14:00' AS TIME(0)))
) AS s (UserSkillId, DayOfWeek, StartTime, EndTime)
ON t.UserSkillId = s.UserSkillId AND t.DayOfWeek = s.DayOfWeek AND t.StartTime = s.StartTime
WHEN NOT MATCHED THEN
    INSERT (UserSkillId, DayOfWeek, StartTime, EndTime)
    VALUES (s.UserSkillId, s.DayOfWeek, s.StartTime, s.EndTime);
GO

DECLARE @U1 UNIQUEIDENTIFIER = '3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01';
DECLARE @U2 UNIQUEIDENTIFIER = '7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02';
DECLARE @U3 UNIQUEIDENTIFIER = 'b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03';
DECLARE @U4 UNIQUEIDENTIFIER = 'e8d05b44-9f7a-4c21-a6b3-1d2e4f7a8b04';

MERGE dbo.UserSkillStatusHistory AS t
USING (VALUES
    (1,  NULL,       N'Active',   @U1, N'User skill created'),
    (2,  NULL,       N'Active',   @U1, N'User skill created'),
    (3,  NULL,       N'Active',   @U2, N'User skill created'),
    (4,  NULL,       N'Active',   @U2, N'User skill created'),
    (5,  NULL,       N'Active',   @U2, N'User skill created'),
    (6,  NULL,       N'Active',   @U3, N'User skill created'),
    (7,  NULL,       N'Active',   @U3, N'User skill created'),
    (8,  NULL,       N'Active',   @U3, N'User skill created'),
    (8,  N'Active',  N'Paused',   @U3, N'No time right now'),
    (9,  NULL,       N'Active',   @U4, N'User skill created'),
    (10, NULL,       N'Active',   @U4, N'User skill created'),
    (10, N'Active',  N'Archived', @U4, N'No longer needed')
) AS s (UserSkillId, OldStatus, NewStatus, ChangedByUserId, Comment)
ON t.UserSkillId = s.UserSkillId AND t.NewStatus = s.NewStatus
WHEN NOT MATCHED THEN
    INSERT (UserSkillId, OldStatus, NewStatus, ChangedByUserId, Comment)
    VALUES (s.UserSkillId, s.OldStatus, s.NewStatus, s.ChangedByUserId, s.Comment);
GO
