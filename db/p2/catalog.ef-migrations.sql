-- Згенеровано: dotnet ef migrations script --idempotent --project Catalog.Dal --startup-project Catalog.Api
-- Ідемпотентний скрипт міграцій QPQ.Catalog (EF Core). Цільова база: QPQ_CatalogEf (створіть її заздалегідь).
-- Запуск: sqlcmd -S <server> -d QPQ_CatalogEf -E -i db/p2/catalog.ef-migrations.sql
-- Фільтровані індекси вимагають QUOTED_IDENTIFIER ON (sqlcmd за замовчуванням OFF), тому SET нижче обов'язковий.
SET QUOTED_IDENTIFIER ON;
GO

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
    WHERE [MigrationId] = N'20261011161809_InitialCreate'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Slug] nvarchar(100) NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Categories_Slug] CHECK (LEN([Slug]) > 0 AND [Slug] COLLATE Latin1_General_BIN2 = LOWER([Slug]) COLLATE Latin1_General_BIN2)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011161809_InitialCreate'
)
BEGIN
    CREATE TABLE [Tags] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_Tags] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011161809_InitialCreate'
)
BEGIN
    CREATE TABLE [Skills] (
        [Id] int NOT NULL IDENTITY,
        [CategoryId] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Slug] nvarchar(100) NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Skills] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Skills_Slug] CHECK (LEN([Slug]) > 0 AND [Slug] COLLATE Latin1_General_BIN2 = LOWER([Slug]) COLLATE Latin1_General_BIN2),
        CONSTRAINT [FK_Skills_Category] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011161809_InitialCreate'
)
BEGIN
    CREATE TABLE [SkillDetails] (
        [SkillId] int NOT NULL,
        [Description] nvarchar(2000) NULL,
        [IconUrl] nvarchar(500) NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SkillDetails] PRIMARY KEY ([SkillId]),
        CONSTRAINT [FK_SkillDetails_Skill] FOREIGN KEY ([SkillId]) REFERENCES [Skills] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011161809_InitialCreate'
)
BEGIN
    CREATE TABLE [SkillLevels] (
        [Id] int NOT NULL IDENTITY,
        [SkillId] int NOT NULL,
        [Level] tinyint NOT NULL,
        [Title] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        CONSTRAINT [PK_SkillLevels] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SkillLevels_Level] CHECK ([Level] BETWEEN 1 AND 5),
        CONSTRAINT [FK_SkillLevels_Skill] FOREIGN KEY ([SkillId]) REFERENCES [Skills] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011161809_InitialCreate'
)
BEGIN
    CREATE TABLE [SkillTags] (
        [SkillId] int NOT NULL,
        [TagId] int NOT NULL,
        [AddedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_SkillTags] PRIMARY KEY ([SkillId], [TagId]),
        CONSTRAINT [FK_SkillTags_Skill] FOREIGN KEY ([SkillId]) REFERENCES [Skills] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SkillTags_Tag] FOREIGN KEY ([TagId]) REFERENCES [Tags] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011161809_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'DeletedAt', N'IsDeleted', N'Name', N'Slug', N'SortOrder', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] ON;
    EXEC(N'INSERT INTO [Categories] ([Id], [CreatedAt], [DeletedAt], [IsDeleted], [Name], [Slug], [SortOrder], [UpdatedAt])
    VALUES (1, ''2026-10-01T00:00:00.0000000Z'', NULL, CAST(0 AS bit), N''Програмування'', N''programming'', 10, NULL),
    (2, ''2026-10-01T00:00:00.0000000Z'', NULL, CAST(0 AS bit), N''Мови'', N''languages'', 20, NULL),
    (3, ''2026-10-01T00:00:00.0000000Z'', NULL, CAST(0 AS bit), N''Музика'', N''music'', 30, NULL),
    (4, ''2026-10-01T00:00:00.0000000Z'', NULL, CAST(0 AS bit), N''Дизайн'', N''design'', 40, NULL),
    (5, ''2026-10-01T00:00:00.0000000Z'', NULL, CAST(0 AS bit), N''Фото'', N''photo'', 50, NULL),
    (6, ''2026-10-01T00:00:00.0000000Z'', NULL, CAST(0 AS bit), N''Кулінарія'', N''cooking'', 60, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'DeletedAt', N'IsDeleted', N'Name', N'Slug', N'SortOrder', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011161809_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Tags]'))
        SET IDENTITY_INSERT [Tags] ON;
    EXEC(N'INSERT INTO [Tags] ([Id], [Name])
    VALUES (1, N''.NET''),
    (2, N''Практика''),
    (3, N''Онлайн''),
    (4, N''Для початківців''),
    (5, N''Творчість''),
    (6, N''Мови'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Tags]'))
        SET IDENTITY_INSERT [Tags] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011161809_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CategoryId', N'CreatedAt', N'DeletedAt', N'IsDeleted', N'Name', N'Slug', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Skills]'))
        SET IDENTITY_INSERT [Skills] ON;
    EXEC(N'INSERT INTO [Skills] ([Id], [CategoryId], [CreatedAt], [DeletedAt], [IsDeleted], [Name], [Slug], [UpdatedAt])
    VALUES (1, 1, ''2026-10-01T00:00:00.0000000Z'', NULL, CAST(0 AS bit), N''Програмування C#'', N''csharp'', NULL),
    (2, 2, ''2026-10-01T00:00:00.0000000Z'', NULL, CAST(0 AS bit), N''Англійська розмовна'', N''english-speaking'', NULL),
    (3, 3, ''2026-10-01T00:00:00.0000000Z'', NULL, CAST(0 AS bit), N''Гра на гітарі'', N''guitar'', NULL),
    (4, 4, ''2026-10-01T00:00:00.0000000Z'', NULL, CAST(0 AS bit), N''UI-дизайн'', N''ui-design'', NULL),
    (5, 5, ''2026-10-01T00:00:00.0000000Z'', NULL, CAST(0 AS bit), N''Фотографія'', N''photography'', NULL),
    (6, 6, ''2026-10-01T00:00:00.0000000Z'', NULL, CAST(0 AS bit), N''Приготування випічки'', N''baking'', NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CategoryId', N'CreatedAt', N'DeletedAt', N'IsDeleted', N'Name', N'Slug', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Skills]'))
        SET IDENTITY_INSERT [Skills] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011161809_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Categories_Name] ON [Categories] ([Name]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011161809_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Categories_Slug] ON [Categories] ([Slug]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011161809_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_SkillLevels_Skill_Level] ON [SkillLevels] ([SkillId], [Level]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011161809_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Skills_Category] ON [Skills] ([CategoryId]) INCLUDE ([Name], [Slug]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011161809_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Skills_Name] ON [Skills] ([Name]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011161809_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Skills_Slug] ON [Skills] ([Slug]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011161809_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SkillTags_Tag] ON [SkillTags] ([TagId], [SkillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011161809_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Tags_Name] ON [Tags] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011161809_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261011161809_InitialCreate', N'10.0.12');
END;

COMMIT;
GO

