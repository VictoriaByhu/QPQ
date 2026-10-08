SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
USE QPQ_Sessions;
GO

MERGE dbo.SessionFormats AS t
USING (VALUES
    (1, N'Online',   N'Онлайн'),
    (2, N'InPerson', N'Особиста зустріч')
) AS s (Id, Code, Name)
ON t.Id = s.Id
WHEN NOT MATCHED THEN
    INSERT (Id, Code, Name) VALUES (s.Id, s.Code, s.Name);
GO

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

DECLARE @U1 UNIQUEIDENTIFIER = '3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01';
DECLARE @U2 UNIQUEIDENTIFIER = '7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02';
DECLARE @U3 UNIQUEIDENTIFIER = 'b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03';

MERGE dbo.AcceptedSwaps AS t
USING (VALUES
    (2, @U2, @U3, N'Accepted'),
    (3, @U3, @U1, N'Completed')
) AS s (SwapId, InitiatorId, PartnerId, Status)
ON t.SwapId = s.SwapId
WHEN NOT MATCHED THEN
    INSERT (SwapId, InitiatorId, PartnerId, Status)
    VALUES (s.SwapId, s.InitiatorId, s.PartnerId, s.Status);
GO

DECLARE @U2 UNIQUEIDENTIFIER = '7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02';
DECLARE @U3 UNIQUEIDENTIFIER = 'b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03';

SET IDENTITY_INSERT dbo.Sessions ON;
MERGE dbo.Sessions AS t
USING (VALUES
    (1, 2, 2, CAST('2026-10-22T17:00:00' AS DATETIME2), 90,  NULL,                                    N'Чернівці, центр', N'Planned',   @U2),
    (2, 2, 1, CAST('2026-10-29T17:00:00' AS DATETIME2), 90,  N'https://meet.google.com/abc-defg-hij', NULL,               N'Planned',   @U3),
    (3, 3, 1, CAST('2026-10-05T19:00:00' AS DATETIME2), 120, N'https://zoom.us/j/1234567890',         NULL,               N'Completed', @U3)
) AS s (Id, SwapId, FormatId, ScheduledAt, DurationMinutes, Link, Location, Status, CreatedByUserId)
ON t.Id = s.Id
WHEN NOT MATCHED THEN
    INSERT (Id, SwapId, FormatId, ScheduledAt, DurationMinutes, Link, Location, Status, CreatedByUserId)
    VALUES (s.Id, s.SwapId, s.FormatId, s.ScheduledAt, s.DurationMinutes, s.Link, s.Location, s.Status, s.CreatedByUserId);
SET IDENTITY_INSERT dbo.Sessions OFF;
GO

MERGE dbo.SessionSkills AS t
USING (VALUES
    (1, 2, 45), (1, 3, 45),
    (2, 2, 45), (2, 3, 45),
    (3, 4, 60), (3, 1, 60)
) AS s (SessionId, SkillId, PlannedMinutes)
ON t.SessionId = s.SessionId AND t.SkillId = s.SkillId
WHEN NOT MATCHED THEN
    INSERT (SessionId, SkillId, PlannedMinutes)
    VALUES (s.SessionId, s.SkillId, s.PlannedMinutes);
GO

MERGE dbo.SessionOutcomes AS t
USING (VALUES
    (3, N'Консультація з C# та огляд макета у Figma', 115, CAST('2026-10-05T21:00:00' AS DATETIME2))
) AS s (SessionId, Summary, ActualDurationMinutes, CompletedAt)
ON t.SessionId = s.SessionId
WHEN NOT MATCHED THEN
    INSERT (SessionId, Summary, ActualDurationMinutes, CompletedAt)
    VALUES (s.SessionId, s.Summary, s.ActualDurationMinutes, s.CompletedAt);
GO