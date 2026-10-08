SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
IF DB_ID(N'QPQ_Sessions') IS NULL CREATE DATABASE QPQ_Sessions;
GO
USE QPQ_Sessions;
GO

IF OBJECT_ID(N'dbo.SessionFormats', N'U') IS NULL
CREATE TABLE dbo.SessionFormats (
    Id   TINYINT      NOT NULL CONSTRAINT PK_SessionFormats PRIMARY KEY,
    Code NVARCHAR(20) NOT NULL CONSTRAINT UQ_SessionFormats_Code UNIQUE,
    Name NVARCHAR(50) NOT NULL
);
GO

IF OBJECT_ID(N'dbo.Skills', N'U') IS NULL
CREATE TABLE dbo.Skills (
    Id          INT           NOT NULL CONSTRAINT PK_Skills PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL,
    CreatedAt   DATETIME2     NOT NULL CONSTRAINT DF_Skills_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt   DATETIME2     NULL,
    IsDeleted   BIT           NOT NULL CONSTRAINT DF_Skills_IsDeleted DEFAULT 0,
    DeletedAt   DATETIME2     NULL,
    RowVersion  ROWVERSION    NOT NULL
);
GO

IF OBJECT_ID(N'dbo.AcceptedSwaps', N'U') IS NULL
CREATE TABLE dbo.AcceptedSwaps (
    SwapId      INT              NOT NULL CONSTRAINT PK_AcceptedSwaps PRIMARY KEY,
    InitiatorId UNIQUEIDENTIFIER NOT NULL,
    PartnerId   UNIQUEIDENTIFIER NOT NULL,
    Status      NVARCHAR(20)     NOT NULL CONSTRAINT DF_AcceptedSwaps_Status DEFAULT N'Accepted',
    CreatedAt   DATETIME2        NOT NULL CONSTRAINT DF_AcceptedSwaps_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt   DATETIME2        NULL,
    RowVersion  ROWVERSION       NOT NULL,
    CONSTRAINT CK_AcceptedSwaps_Status CHECK (Status IN (N'Accepted', N'Completed', N'Cancelled')),
    CONSTRAINT CK_AcceptedSwaps_Parties CHECK (InitiatorId <> PartnerId)
);
GO

IF OBJECT_ID(N'dbo.Sessions', N'U') IS NULL
CREATE TABLE dbo.Sessions (
    Id              INT              NOT NULL IDENTITY(1,1) CONSTRAINT PK_Sessions PRIMARY KEY,
    SwapId          INT              NOT NULL CONSTRAINT FK_Sessions_AcceptedSwap REFERENCES dbo.AcceptedSwaps(SwapId),
    FormatId        TINYINT          NOT NULL CONSTRAINT FK_Sessions_Format REFERENCES dbo.SessionFormats(Id),
    ScheduledAt     DATETIME2        NOT NULL,
    DurationMinutes INT              NOT NULL CONSTRAINT DF_Sessions_Duration DEFAULT 60,
    Link            NVARCHAR(500)    NULL,
    Location        NVARCHAR(200)    NULL,
    Status          NVARCHAR(20)     NOT NULL CONSTRAINT DF_Sessions_Status DEFAULT N'Planned',
    CreatedByUserId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt       DATETIME2        NOT NULL CONSTRAINT DF_Sessions_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2        NULL,
    IsDeleted       BIT              NOT NULL CONSTRAINT DF_Sessions_IsDeleted DEFAULT 0,
    DeletedAt       DATETIME2        NULL,
    RowVersion      ROWVERSION       NOT NULL,
    CONSTRAINT CK_Sessions_Duration CHECK (DurationMinutes > 0),
    CONSTRAINT CK_Sessions_Status CHECK (Status IN (N'Planned', N'Completed', N'Cancelled')),
    CONSTRAINT CK_Sessions_Place CHECK (
        (FormatId = 1 AND Link IS NOT NULL) OR
        (FormatId = 2 AND Location IS NOT NULL))
);
GO

IF OBJECT_ID(N'dbo.SessionSkills', N'U') IS NULL
CREATE TABLE dbo.SessionSkills (
    SessionId      INT       NOT NULL CONSTRAINT FK_SessionSkills_Session REFERENCES dbo.Sessions(Id),
    SkillId        INT       NOT NULL CONSTRAINT FK_SessionSkills_Skill   REFERENCES dbo.Skills(Id),
    PlannedMinutes INT       NOT NULL CONSTRAINT DF_SessionSkills_Minutes DEFAULT 30,
    AddedAt        DATETIME2 NOT NULL CONSTRAINT DF_SessionSkills_AddedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_SessionSkills PRIMARY KEY (SessionId, SkillId),
    CONSTRAINT CK_SessionSkills_Minutes CHECK (PlannedMinutes > 0)
);
GO

IF OBJECT_ID(N'dbo.SessionOutcomes', N'U') IS NULL
CREATE TABLE dbo.SessionOutcomes (
    SessionId             INT            NOT NULL CONSTRAINT PK_SessionOutcomes PRIMARY KEY
                                         CONSTRAINT FK_SessionOutcomes_Session REFERENCES dbo.Sessions(Id),
    Summary               NVARCHAR(1000) NULL,
    ActualDurationMinutes INT            NOT NULL,
    CompletedAt           DATETIME2      NOT NULL CONSTRAINT DF_SessionOutcomes_CompletedAt DEFAULT SYSUTCDATETIME(),
    RowVersion            ROWVERSION     NOT NULL,
    CONSTRAINT CK_SessionOutcomes_Duration CHECK (ActualDurationMinutes > 0)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Skills_Name' AND object_id = OBJECT_ID(N'dbo.Skills'))
    CREATE UNIQUE INDEX UX_Skills_Name ON dbo.Skills(Name) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AcceptedSwaps_Initiator' AND object_id = OBJECT_ID(N'dbo.AcceptedSwaps'))
    CREATE INDEX IX_AcceptedSwaps_Initiator ON dbo.AcceptedSwaps(InitiatorId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AcceptedSwaps_Partner' AND object_id = OBJECT_ID(N'dbo.AcceptedSwaps'))
    CREATE INDEX IX_AcceptedSwaps_Partner ON dbo.AcceptedSwaps(PartnerId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sessions_Swap_ScheduledAt' AND object_id = OBJECT_ID(N'dbo.Sessions'))
    CREATE INDEX IX_Sessions_Swap_ScheduledAt ON dbo.Sessions(SwapId, ScheduledAt) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Sessions_Swap_Time' AND object_id = OBJECT_ID(N'dbo.Sessions'))
    CREATE UNIQUE INDEX UX_Sessions_Swap_Time ON dbo.Sessions(SwapId, ScheduledAt) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SessionSkills_Skill' AND object_id = OBJECT_ID(N'dbo.SessionSkills'))
    CREATE INDEX IX_SessionSkills_Skill ON dbo.SessionSkills(SkillId, SessionId);
GO