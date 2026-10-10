SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
IF DB_ID(N'QPQ_UserSkills') IS NULL CREATE DATABASE QPQ_UserSkills;
GO
USE QPQ_UserSkills;
GO

-- Локальна копія довідника навичок з Catalog (власник даних: Catalog).
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

-- Довідник мов, якими користувач може проводити навчання (власник: UserSkills).
IF OBJECT_ID(N'dbo.Languages', N'U') IS NULL
CREATE TABLE dbo.Languages (
    Code NVARCHAR(10) NOT NULL CONSTRAINT PK_Languages PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL
);
GO

-- Типи для табличних параметрів процедур (у Dapper передаються як DataTable.AsTableValuedParameter).
IF TYPE_ID(N'dbo.UserSkillLanguageList') IS NULL
CREATE TYPE dbo.UserSkillLanguageList AS TABLE (
    LanguageCode NVARCHAR(10) NOT NULL PRIMARY KEY,
    Proficiency  TINYINT      NOT NULL CHECK (Proficiency BETWEEN 1 AND 5)
);
GO

IF TYPE_ID(N'dbo.AvailabilitySlotList') IS NULL
CREATE TYPE dbo.AvailabilitySlotList AS TABLE (
    DayOfWeek TINYINT NOT NULL,
    StartTime TIME(0) NOT NULL,
    EndTime   TIME(0) NOT NULL,
    PRIMARY KEY (DayOfWeek, StartTime),
    CHECK (DayOfWeek BETWEEN 1 AND 7 AND EndTime > StartTime)
);
GO

-- Навичка користувача: пропоную (Offer) або хочу вивчити (Want). UserId видає Identity, FK на нього немає.
IF OBJECT_ID(N'dbo.UserSkills', N'U') IS NULL
CREATE TABLE dbo.UserSkills (
    Id         INT              NOT NULL IDENTITY(1,1) CONSTRAINT PK_UserSkills PRIMARY KEY,
    UserId     UNIQUEIDENTIFIER NOT NULL,
    SkillId    INT              NOT NULL CONSTRAINT FK_UserSkills_Skill REFERENCES dbo.Skills(Id),
    Type       NVARCHAR(10)     NOT NULL,
    Level      TINYINT          NOT NULL CONSTRAINT DF_UserSkills_Level DEFAULT 1,
    Status     NVARCHAR(20)     NOT NULL CONSTRAINT DF_UserSkills_Status DEFAULT N'Active',
    CreatedAt  DATETIME2        NOT NULL CONSTRAINT DF_UserSkills_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy  UNIQUEIDENTIFIER NOT NULL,
    UpdatedAt  DATETIME2        NULL,
    UpdatedBy  UNIQUEIDENTIFIER NULL,
    IsDeleted  BIT              NOT NULL CONSTRAINT DF_UserSkills_IsDeleted DEFAULT 0,
    DeletedAt  DATETIME2        NULL,
    RowVersion ROWVERSION       NOT NULL,
    CONSTRAINT CK_UserSkills_Type   CHECK (Type IN (N'Offer', N'Want')),
    CONSTRAINT CK_UserSkills_Level  CHECK (Level BETWEEN 1 AND 5),
    CONSTRAINT CK_UserSkills_Status CHECK (Status IN (N'Active', N'Paused', N'Archived'))
);
GO

-- 1:1 з UserSkills: FK є водночас PK.
IF OBJECT_ID(N'dbo.UserSkillDetails', N'U') IS NULL
CREATE TABLE dbo.UserSkillDetails (
    UserSkillId     INT            NOT NULL CONSTRAINT PK_UserSkillDetails PRIMARY KEY
                                   CONSTRAINT FK_UserSkillDetails_UserSkill REFERENCES dbo.UserSkills(Id),
    Description     NVARCHAR(1000) NULL,
    ExperienceYears TINYINT        NULL,
    PortfolioUrl    NVARCHAR(500)  NULL,
    CreatedAt       DATETIME2      NOT NULL CONSTRAINT DF_UserSkillDetails_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2      NULL,
    RowVersion      ROWVERSION     NOT NULL,
    CONSTRAINT CK_UserSkillDetails_Experience CHECK (ExperienceYears IS NULL OR ExperienceYears <= 80)
);
GO

-- 1:N до UserSkills: вікна доступності (1 = понеділок ... 7 = неділя).
IF OBJECT_ID(N'dbo.AvailabilitySlots', N'U') IS NULL
CREATE TABLE dbo.AvailabilitySlots (
    Id          INT       NOT NULL IDENTITY(1,1) CONSTRAINT PK_AvailabilitySlots PRIMARY KEY,
    UserSkillId INT       NOT NULL CONSTRAINT FK_AvailabilitySlots_UserSkill REFERENCES dbo.UserSkills(Id),
    DayOfWeek   TINYINT   NOT NULL,
    StartTime   TIME(0)   NOT NULL,
    EndTime     TIME(0)   NOT NULL,
    CreatedAt   DATETIME2 NOT NULL CONSTRAINT DF_AvailabilitySlots_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_AvailabilitySlots_Day   CHECK (DayOfWeek BETWEEN 1 AND 7),
    CONSTRAINT CK_AvailabilitySlots_Range CHECK (EndTime > StartTime),
    CONSTRAINT UQ_AvailabilitySlots_Slot  UNIQUE (UserSkillId, DayOfWeek, StartTime)
);
GO

-- M:N між UserSkills і Languages з власним полем Proficiency (рівень володіння мовою).
IF OBJECT_ID(N'dbo.UserSkillLanguages', N'U') IS NULL
CREATE TABLE dbo.UserSkillLanguages (
    UserSkillId  INT          NOT NULL CONSTRAINT FK_UserSkillLanguages_UserSkill REFERENCES dbo.UserSkills(Id),
    LanguageCode NVARCHAR(10) NOT NULL CONSTRAINT FK_UserSkillLanguages_Language  REFERENCES dbo.Languages(Code),
    Proficiency  TINYINT      NOT NULL CONSTRAINT DF_UserSkillLanguages_Proficiency DEFAULT 3,
    CreatedAt    DATETIME2    NOT NULL CONSTRAINT DF_UserSkillLanguages_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_UserSkillLanguages PRIMARY KEY (UserSkillId, LanguageCode),
    CONSTRAINT CK_UserSkillLanguages_Proficiency CHECK (Proficiency BETWEEN 1 AND 5)
);
GO

-- 1:N до UserSkills: журнал змін статусу.
IF OBJECT_ID(N'dbo.UserSkillStatusHistory', N'U') IS NULL
CREATE TABLE dbo.UserSkillStatusHistory (
    Id              BIGINT           NOT NULL IDENTITY(1,1) CONSTRAINT PK_UserSkillStatusHistory PRIMARY KEY,
    UserSkillId     INT              NOT NULL CONSTRAINT FK_UserSkillStatusHistory_UserSkill REFERENCES dbo.UserSkills(Id),
    OldStatus       NVARCHAR(20)     NULL,
    NewStatus       NVARCHAR(20)     NOT NULL,
    ChangedByUserId UNIQUEIDENTIFIER NOT NULL,
    Comment         NVARCHAR(500)    NULL,
    ChangedAt       DATETIME2        NOT NULL CONSTRAINT DF_UserSkillStatusHistory_ChangedAt DEFAULT SYSUTCDATETIME()
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Skills_Name' AND object_id = OBJECT_ID(N'dbo.Skills'))
    CREATE UNIQUE INDEX UX_Skills_Name ON dbo.Skills(Name) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_UserSkills_Live' AND object_id = OBJECT_ID(N'dbo.UserSkills'))
    CREATE UNIQUE INDEX UX_UserSkills_Live ON dbo.UserSkills(UserId, SkillId, Type) WHERE IsDeleted = 0 AND Status <> N'Archived';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserSkills_User_Status' AND object_id = OBJECT_ID(N'dbo.UserSkills'))
    CREATE INDEX IX_UserSkills_User_Status ON dbo.UserSkills(UserId, Status) INCLUDE (SkillId, Type, Level, CreatedAt) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserSkills_Skill_Type' AND object_id = OBJECT_ID(N'dbo.UserSkills'))
    CREATE INDEX IX_UserSkills_Skill_Type ON dbo.UserSkills(SkillId, Type) INCLUDE (UserId, Level) WHERE IsDeleted = 0 AND Status = N'Active';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserSkillLanguages_Language' AND object_id = OBJECT_ID(N'dbo.UserSkillLanguages'))
    CREATE INDEX IX_UserSkillLanguages_Language ON dbo.UserSkillLanguages(LanguageCode, UserSkillId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserSkillStatusHistory_UserSkill' AND object_id = OBJECT_ID(N'dbo.UserSkillStatusHistory'))
    CREATE INDEX IX_UserSkillStatusHistory_UserSkill ON dbo.UserSkillStatusHistory(UserSkillId, ChangedAt);
GO
