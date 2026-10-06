
SET QUOTED_IDENTIFIER ON;
GO
IF DB_ID(N'QPQ_Swaps') IS NULL CREATE DATABASE QPQ_Swaps;
GO
USE QPQ_Swaps;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
CREATE TABLE dbo.Users (
    Id          INT           NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    DisplayName NVARCHAR(100) NOT NULL,
    Email       NVARCHAR(256) NOT NULL,
    CreatedAt   DATETIME2     NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt   DATETIME2     NULL,
    IsDeleted   BIT           NOT NULL CONSTRAINT DF_Users_IsDeleted DEFAULT 0,
    DeletedAt   DATETIME2     NULL,
    RowVersion  ROWVERSION    NOT NULL
);
GO

IF OBJECT_ID(N'dbo.Skills', N'U') IS NULL
CREATE TABLE dbo.Skills (
    Id          INT           NOT NULL CONSTRAINT PK_Skills PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL,
    Category    NVARCHAR(100) NULL,
    CreatedAt   DATETIME2     NOT NULL CONSTRAINT DF_Skills_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt   DATETIME2     NULL,
    IsDeleted   BIT           NOT NULL CONSTRAINT DF_Skills_IsDeleted DEFAULT 0,
    DeletedAt   DATETIME2     NULL,
    RowVersion  ROWVERSION    NOT NULL
);
GO

IF OBJECT_ID(N'dbo.Swaps', N'U') IS NULL
CREATE TABLE dbo.Swaps (
    Id          INT           NOT NULL IDENTITY(1,1) CONSTRAINT PK_Swaps PRIMARY KEY,
    RequesterId INT           NOT NULL CONSTRAINT FK_Swaps_Requester REFERENCES dbo.Users(Id),
    ProviderId  INT           NOT NULL CONSTRAINT FK_Swaps_Provider  REFERENCES dbo.Users(Id),
    Status      NVARCHAR(20)  NOT NULL CONSTRAINT DF_Swaps_Status DEFAULT N'Pending',
    CreatedAt   DATETIME2     NOT NULL CONSTRAINT DF_Swaps_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt   DATETIME2     NULL,
    IsDeleted   BIT           NOT NULL CONSTRAINT DF_Swaps_IsDeleted DEFAULT 0,
    DeletedAt   DATETIME2     NULL,
    RowVersion  ROWVERSION    NOT NULL,
    CONSTRAINT CK_Swaps_Status CHECK (Status IN (N'Pending', N'Accepted', N'Completed', N'Rejected', N'Cancelled')),
    CONSTRAINT CK_Swaps_Parties CHECK (RequesterId <> ProviderId)
);
GO

IF OBJECT_ID(N'dbo.SwapDetails', N'U') IS NULL
CREATE TABLE dbo.SwapDetails (
    SwapId          INT            NOT NULL CONSTRAINT PK_SwapDetails PRIMARY KEY
                                   CONSTRAINT FK_SwapDetails_Swap REFERENCES dbo.Swaps(Id),
    Message         NVARCHAR(1000) NULL,
    Location        NVARCHAR(200)  NULL,
    ProposedDate    DATETIME2      NULL,
    DurationMinutes INT            NULL,
    CreatedAt       DATETIME2      NOT NULL CONSTRAINT DF_SwapDetails_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2      NULL,
    RowVersion      ROWVERSION     NOT NULL,
    CONSTRAINT CK_SwapDetails_Duration CHECK (DurationMinutes IS NULL OR DurationMinutes > 0)
);
GO

IF OBJECT_ID(N'dbo.SwapSkills', N'U') IS NULL
CREATE TABLE dbo.SwapSkills (
    SwapId    INT          NOT NULL CONSTRAINT FK_SwapSkills_Swap  REFERENCES dbo.Swaps(Id),
    SkillId   INT          NOT NULL CONSTRAINT FK_SwapSkills_Skill REFERENCES dbo.Skills(Id),
    Role      NVARCHAR(10) NOT NULL,
    CreatedAt DATETIME2    NOT NULL CONSTRAINT DF_SwapSkills_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_SwapSkills PRIMARY KEY (SwapId, SkillId, Role),
    CONSTRAINT CK_SwapSkills_Role CHECK (Role IN (N'Offered', N'Requested'))
);
GO

IF OBJECT_ID(N'dbo.SwapStatusHistory', N'U') IS NULL
CREATE TABLE dbo.SwapStatusHistory (
    Id              BIGINT        NOT NULL IDENTITY(1,1) CONSTRAINT PK_SwapStatusHistory PRIMARY KEY,
    SwapId          INT           NOT NULL CONSTRAINT FK_SwapStatusHistory_Swap REFERENCES dbo.Swaps(Id),
    OldStatus       NVARCHAR(20)  NULL,
    NewStatus       NVARCHAR(20)  NOT NULL,
    ChangedByUserId INT           NOT NULL CONSTRAINT FK_SwapStatusHistory_User REFERENCES dbo.Users(Id),
    Comment         NVARCHAR(500) NULL,
    ChangedAt       DATETIME2     NOT NULL CONSTRAINT DF_SwapStatusHistory_ChangedAt DEFAULT SYSUTCDATETIME()
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Users_Email' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Swaps_Requester_Status' AND object_id = OBJECT_ID(N'dbo.Swaps'))
    CREATE INDEX IX_Swaps_Requester_Status ON dbo.Swaps(RequesterId, Status) INCLUDE (CreatedAt) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Swaps_Provider_Status' AND object_id = OBJECT_ID(N'dbo.Swaps'))
    CREATE INDEX IX_Swaps_Provider_Status ON dbo.Swaps(ProviderId, Status) INCLUDE (CreatedAt) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SwapSkills_Skill' AND object_id = OBJECT_ID(N'dbo.SwapSkills'))
    CREATE INDEX IX_SwapSkills_Skill ON dbo.SwapSkills(SkillId, SwapId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SwapStatusHistory_Swap' AND object_id = OBJECT_ID(N'dbo.SwapStatusHistory'))
    CREATE INDEX IX_SwapStatusHistory_Swap ON dbo.SwapStatusHistory(SwapId, ChangedAt);
GO