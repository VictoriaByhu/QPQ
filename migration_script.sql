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
GO

CREATE TABLE [Moderators] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Moderators] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Reports] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Reports] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Attachments] (
    [Id] int NOT NULL IDENTITY,
    [ReportId] int NOT NULL,
    [FileUrl] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Attachments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Attachments_Reports_ReportId] FOREIGN KEY ([ReportId]) REFERENCES [Reports] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ReportAssignments] (
    [ReportId] int NOT NULL,
    [ModeratorId] int NOT NULL,
    [AssignedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_ReportAssignments] PRIMARY KEY ([ReportId], [ModeratorId]),
    CONSTRAINT [FK_ReportAssignments_Moderators_ModeratorId] FOREIGN KEY ([ModeratorId]) REFERENCES [Moderators] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ReportAssignments_Reports_ReportId] FOREIGN KEY ([ReportId]) REFERENCES [Reports] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ReportDetails] (
    [ReportId] int NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [SystemLogs] nvarchar(max) NULL,
    CONSTRAINT [PK_ReportDetails] PRIMARY KEY ([ReportId]),
    CONSTRAINT [FK_ReportDetails_Reports_ReportId] FOREIGN KEY ([ReportId]) REFERENCES [Reports] ([Id]) ON DELETE CASCADE
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Moderators]'))
    SET IDENTITY_INSERT [Moderators] ON;
INSERT INTO [Moderators] ([Id], [Name])
VALUES (1, N'Admin'),
(2, N'Support');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Moderators]'))
    SET IDENTITY_INSERT [Moderators] OFF;
GO

CREATE INDEX [IX_Attachments_ReportId] ON [Attachments] ([ReportId]);
GO

CREATE INDEX [IX_ReportAssignments_ModeratorId] ON [ReportAssignments] ([ModeratorId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261021204802_InitialCreate', N'8.0.0');
GO

COMMIT;
GO

