IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022123205_InitialCreate'
)
BEGIN
    CREATE TABLE [AcceptedSwaps] (
        [SwapId] int NOT NULL,
        [InitiatorId] uniqueidentifier NOT NULL,
        [PartnerId] uniqueidentifier NOT NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT N'Accepted',
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AcceptedSwaps] PRIMARY KEY ([SwapId]),
        CONSTRAINT [CK_AcceptedSwaps_Parties] CHECK ([InitiatorId] <> [PartnerId]),
        CONSTRAINT [CK_AcceptedSwaps_Status] CHECK ([Status] IN (N'Accepted', N'Completed', N'Cancelled'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022123205_InitialCreate'
)
BEGIN
    CREATE TABLE [SessionFormats] (
        [Id] tinyint NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_SessionFormats] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022123205_InitialCreate'
)
BEGIN
    CREATE TABLE [Skills] (
        [Id] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [DeletedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Skills] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022123205_InitialCreate'
)
BEGIN
    CREATE TABLE [Sessions] (
        [Id] int NOT NULL IDENTITY,
        [SwapId] int NOT NULL,
        [FormatId] tinyint NOT NULL,
        [ScheduledAt] datetime2 NOT NULL,
        [DurationMinutes] int NOT NULL DEFAULT 60,
        [Link] nvarchar(500) NULL,
        [Location] nvarchar(200) NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT N'Planned',
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [DeletedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Sessions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Sessions_Duration] CHECK ([DurationMinutes] > 0),
        CONSTRAINT [CK_Sessions_Place] CHECK (([FormatId] = 1 AND [Link] IS NOT NULL) OR ([FormatId] = 2 AND [Location] IS NOT NULL)),
        CONSTRAINT [CK_Sessions_Status] CHECK ([Status] IN (N'Planned', N'Completed', N'Cancelled')),
        CONSTRAINT [FK_Sessions_AcceptedSwap] FOREIGN KEY ([SwapId]) REFERENCES [AcceptedSwaps] ([SwapId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Sessions_Format] FOREIGN KEY ([FormatId]) REFERENCES [SessionFormats] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022123205_InitialCreate'
)
BEGIN
    CREATE TABLE [SessionOutcomes] (
        [SessionId] int NOT NULL,
        [Summary] nvarchar(1000) NULL,
        [ActualDurationMinutes] int NOT NULL,
        [CompletedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SessionOutcomes] PRIMARY KEY ([SessionId]),
        CONSTRAINT [CK_SessionOutcomes_Duration] CHECK ([ActualDurationMinutes] > 0),
        CONSTRAINT [FK_SessionOutcomes_Session] FOREIGN KEY ([SessionId]) REFERENCES [Sessions] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022123205_InitialCreate'
)
BEGIN
    CREATE TABLE [SessionSkills] (
        [SessionId] int NOT NULL,
        [SkillId] int NOT NULL,
        [PlannedMinutes] int NOT NULL DEFAULT 30,
        [AddedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_SessionSkills] PRIMARY KEY ([SessionId], [SkillId]),
        CONSTRAINT [CK_SessionSkills_Minutes] CHECK ([PlannedMinutes] > 0),
        CONSTRAINT [FK_SessionSkills_Session] FOREIGN KEY ([SessionId]) REFERENCES [Sessions] ([Id]),
        CONSTRAINT [FK_SessionSkills_Skill] FOREIGN KEY ([SkillId]) REFERENCES [Skills] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022123205_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Name') AND [object_id] = OBJECT_ID(N'[SessionFormats]'))
        SET IDENTITY_INSERT [SessionFormats] ON;
    EXEC(N'INSERT INTO [SessionFormats] ([Id], [Code], [Name])
    VALUES (CAST(1 AS tinyint), N''Online'', N''Онлайн''),
    (CAST(2 AS tinyint), N''InPerson'', N''Особиста зустріч'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Name') AND [object_id] = OBJECT_ID(N'[SessionFormats]'))
        SET IDENTITY_INSERT [SessionFormats] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022123205_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'DeletedAt', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Skills]'))
        SET IDENTITY_INSERT [Skills] ON;
    EXEC(N'INSERT INTO [Skills] ([Id], [CreatedAt], [DeletedAt], [Name], [UpdatedAt])
    VALUES (1, ''2026-10-01T00:00:00.0000000Z'', NULL, N''Програмування C#'', NULL),
    (2, ''2026-10-01T00:00:00.0000000Z'', NULL, N''Англійська розмовна'', NULL),
    (3, ''2026-10-01T00:00:00.0000000Z'', NULL, N''Гра на гітарі'', NULL),
    (4, ''2026-10-01T00:00:00.0000000Z'', NULL, N''UI-дизайн'', NULL),
    (5, ''2026-10-01T00:00:00.0000000Z'', NULL, N''Фотографія'', NULL),
    (6, ''2026-10-01T00:00:00.0000000Z'', NULL, N''Приготування випічки'', NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'DeletedAt', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Skills]'))
        SET IDENTITY_INSERT [Skills] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022123205_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AcceptedSwaps_Initiator] ON [AcceptedSwaps] ([InitiatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022123205_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AcceptedSwaps_Partner] ON [AcceptedSwaps] ([PartnerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022123205_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_SessionFormats_Code] ON [SessionFormats] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022123205_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Sessions_FormatId] ON [Sessions] ([FormatId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022123205_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Sessions_Swap_Time] ON [Sessions] ([SwapId], [ScheduledAt]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022123205_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SessionSkills_Skill] ON [SessionSkills] ([SkillId], [SessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022123205_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Skills_Name] ON [Skills] ([Name]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022123205_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022123205_InitialCreate', N'10.0.13');
END;

COMMIT;
GO

