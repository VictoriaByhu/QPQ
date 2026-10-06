SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
USE QPQ_Swaps;
GO

MERGE dbo.Users AS t
USING (VALUES
    (1, N'Олена Коваль',    N'olena@example.com'),
    (2, N'Андрій Мельник',  N'andriy@example.com'),
    (3, N'Марія Шевченко',  N'maria@example.com'),
    (4, N'Іван Бондаренко', N'ivan@example.com')
) AS s (Id, DisplayName, Email)
ON t.Id = s.Id
WHEN NOT MATCHED THEN
    INSERT (Id, DisplayName, Email) VALUES (s.Id, s.DisplayName, s.Email);
GO


MERGE dbo.Skills AS t
USING (VALUES
    (1, N'Програмування C#',        N'IT'),
    (2, N'Англійська розмовна',     N'Мови'),
    (3, N'Гра на гітарі',           N'Музика'),
    (4, N'UI-дизайн',               N'Дизайн'),
    (5, N'Фотографія',              N'Мистецтво'),
    (6, N'Приготування випічки',    N'Кулінарія')
) AS s (Id, Name, Category)
ON t.Id = s.Id
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Category) VALUES (s.Id, s.Name, s.Category);
GO

SET IDENTITY_INSERT dbo.Swaps ON;
MERGE dbo.Swaps AS t
USING (VALUES
    (1, 1, 2, N'Pending'),
    (2, 2, 3, N'Accepted'),
    (3, 3, 1, N'Completed'),
    (4, 4, 1, N'Rejected'),
    (5, 2, 4, N'Cancelled')
) AS s (Id, RequesterId, ProviderId, Status)
ON t.Id = s.Id
WHEN NOT MATCHED THEN
    INSERT (Id, RequesterId, ProviderId, Status) VALUES (s.Id, s.RequesterId, s.ProviderId, s.Status);
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

MERGE dbo.SwapStatusHistory AS t
USING (VALUES
    (1, NULL,       N'Pending',   1, N'Swap created'),
    (2, NULL,       N'Pending',   2, N'Swap created'),
    (2, N'Pending', N'Accepted',  3, N'Accepted'),
    (3, NULL,       N'Pending',   3, N'Swap created'),
    (3, N'Pending', N'Accepted',  1, N'Accepted'),
    (3, N'Accepted',N'Completed', 3, N'Completed'),
    (4, NULL,       N'Pending',   4, N'Swap created'),
    (4, N'Pending', N'Rejected',  1, N'No time'),
    (5, NULL,       N'Pending',   2, N'Swap created'),
    (5, N'Pending', N'Cancelled', 2, N'Plans changed')
) AS s (SwapId, OldStatus, NewStatus, ChangedByUserId, Comment)
ON t.SwapId = s.SwapId AND t.NewStatus = s.NewStatus
WHEN NOT MATCHED THEN
    INSERT (SwapId, OldStatus, NewStatus, ChangedByUserId, Comment)
    VALUES (s.SwapId, s.OldStatus, s.NewStatus, s.ChangedByUserId, s.Comment);
GO