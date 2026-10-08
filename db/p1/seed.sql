SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
USE QPQ_Swaps;
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
DECLARE @U4 UNIQUEIDENTIFIER = 'e8d05b44-9f7a-4c21-a6b3-1d2e4f7a8b04';

SET IDENTITY_INSERT dbo.Swaps ON;
MERGE dbo.Swaps AS t
USING (VALUES
    (1, @U1, @U2, N'Pending',   @U1),
    (2, @U2, @U3, N'Accepted',  @U2),
    (3, @U3, @U1, N'Completed', @U3),
    (4, @U4, @U1, N'Rejected',  @U4),
    (5, @U2, @U4, N'Cancelled', @U2)
) AS s (Id, InitiatorId, PartnerId, Status, CreatedBy)
ON t.Id = s.Id
WHEN NOT MATCHED THEN
    INSERT (Id, InitiatorId, PartnerId, Status, CreatedBy)
    VALUES (s.Id, s.InitiatorId, s.PartnerId, s.Status, s.CreatedBy);
SET IDENTITY_INSERT dbo.Swaps OFF;
GO

MERGE dbo.SwapDetails AS t
USING (VALUES
    (1, N'Допоможу з C#, а мені потрібна практика англійської', N'Онлайн (Google Meet)', CAST('2026-10-20T18:00:00' AS DATETIME2), 60),
    (2, N'Обмін англійської на гітару',                         N'Чернівці, центр',      CAST('2026-10-22T17:00:00' AS DATETIME2), 90),
    (3, N'Макет у Figma в обмін на консультацію з C#',          N'Онлайн (Zoom)',        CAST('2026-10-05T19:00:00' AS DATETIME2), 120),
    (4, N'Фотосесія в обмін на допомогу з кодом',               N'Чернівці, парк',       CAST('2026-10-25T12:00:00' AS DATETIME2), 60),
    (5, N'Урок гітари в обмін на майстер-клас з випічки',       N'Чернівці',             CAST('2026-10-28T16:00:00' AS DATETIME2), 90)
) AS s (SwapId, Message, Location, ProposedDate, DurationMinutes)
ON t.SwapId = s.SwapId
WHEN NOT MATCHED THEN
    INSERT (SwapId, Message, Location, ProposedDate, DurationMinutes)
    VALUES (s.SwapId, s.Message, s.Location, s.ProposedDate, s.DurationMinutes);
GO

MERGE dbo.SwapSkills AS t
USING (VALUES
    (1, 1, N'Offered'), (1, 2, N'Requested'),
    (2, 2, N'Offered'), (2, 3, N'Requested'),
    (3, 4, N'Offered'), (3, 1, N'Requested'),
    (4, 5, N'Offered'), (4, 1, N'Requested'),
    (5, 3, N'Offered'), (5, 6, N'Requested')
) AS s (SwapId, SkillId, Role)
ON t.SwapId = s.SwapId AND t.SkillId = s.SkillId AND t.Role = s.Role
WHEN NOT MATCHED THEN
    INSERT (SwapId, SkillId, Role) VALUES (s.SwapId, s.SkillId, s.Role);
GO

DECLARE @U1 UNIQUEIDENTIFIER = '3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01';
DECLARE @U2 UNIQUEIDENTIFIER = '7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02';
DECLARE @U3 UNIQUEIDENTIFIER = 'b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03';
DECLARE @U4 UNIQUEIDENTIFIER = 'e8d05b44-9f7a-4c21-a6b3-1d2e4f7a8b04';

MERGE dbo.SwapStatusHistory AS t
USING (VALUES
    (1, NULL,        N'Pending',   @U1, N'Swap created'),
    (2, NULL,        N'Pending',   @U2, N'Swap created'),
    (2, N'Pending',  N'Accepted',  @U3, N'Accepted'),
    (3, NULL,        N'Pending',   @U3, N'Swap created'),
    (3, N'Pending',  N'Accepted',  @U1, N'Accepted'),
    (3, N'Accepted', N'Completed', @U3, N'Completed'),
    (4, NULL,        N'Pending',   @U4, N'Swap created'),
    (4, N'Pending',  N'Rejected',  @U1, N'No time'),
    (5, NULL,        N'Pending',   @U2, N'Swap created'),
    (5, N'Pending',  N'Cancelled', @U2, N'Plans changed')
) AS s (SwapId, OldStatus, NewStatus, ChangedByUserId, Comment)
ON t.SwapId = s.SwapId AND t.NewStatus = s.NewStatus
WHEN NOT MATCHED THEN
    INSERT (SwapId, OldStatus, NewStatus, ChangedByUserId, Comment)
    VALUES (s.SwapId, s.OldStatus, s.NewStatus, s.ChangedByUserId, s.Comment);
GO