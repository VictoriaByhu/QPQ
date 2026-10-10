SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
IF DB_ID(N'QPQ_Catalog') IS NULL CREATE DATABASE QPQ_Catalog;
GO
USE QPQ_Catalog;
GO

-- Категорії навичок. Власник даних: Catalog.
IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
CREATE TABLE dbo.Categories (
    Id         INT           NOT NULL IDENTITY(1,1) CONSTRAINT PK_Categories PRIMARY KEY,
    Name       NVARCHAR(100) NOT NULL,
    Slug       NVARCHAR(100) NOT NULL,
    SortOrder  INT           NOT NULL CONSTRAINT DF_Categories_SortOrder DEFAULT 0,
    CreatedAt  DATETIME2     NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt  DATETIME2     NULL,
    IsDeleted  BIT           NOT NULL CONSTRAINT DF_Categories_IsDeleted DEFAULT 0,
    DeletedAt  DATETIME2     NULL,
    RowVersion ROWVERSION    NOT NULL,
    CONSTRAINT CK_Categories_Slug CHECK (LEN(Slug) > 0 AND Slug COLLATE Latin1_General_BIN2 = LOWER(Slug) COLLATE Latin1_General_BIN2)
);
GO

-- Навичка (SkillId у решті контекстів QPQ). 1:N Categories -> Skills.
IF OBJECT_ID(N'dbo.Skills', N'U') IS NULL
CREATE TABLE dbo.Skills (
    Id         INT           NOT NULL IDENTITY(1,1) CONSTRAINT PK_Skills PRIMARY KEY,
    CategoryId INT           NOT NULL CONSTRAINT FK_Skills_Category REFERENCES dbo.Categories(Id),
    Name       NVARCHAR(100) NOT NULL,
    Slug       NVARCHAR(100) NOT NULL,
    CreatedAt  DATETIME2     NOT NULL CONSTRAINT DF_Skills_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt  DATETIME2     NULL,
    IsDeleted  BIT           NOT NULL CONSTRAINT DF_Skills_IsDeleted DEFAULT 0,
    DeletedAt  DATETIME2     NULL,
    RowVersion ROWVERSION    NOT NULL,
    CONSTRAINT CK_Skills_Slug CHECK (LEN(Slug) > 0 AND Slug COLLATE Latin1_General_BIN2 = LOWER(Slug) COLLATE Latin1_General_BIN2)
);
GO

-- 1:1 Skills <-> SkillDetails: FK є водночас PK.
IF OBJECT_ID(N'dbo.SkillDetails', N'U') IS NULL
CREATE TABLE dbo.SkillDetails (
    SkillId     INT            NOT NULL CONSTRAINT PK_SkillDetails PRIMARY KEY
                               CONSTRAINT FK_SkillDetails_Skill REFERENCES dbo.Skills(Id),
    Description NVARCHAR(2000) NULL,
    IconUrl     NVARCHAR(500)  NULL,
    UpdatedAt   DATETIME2      NULL,
    RowVersion  ROWVERSION     NOT NULL
);
GO

-- 1:N Skills -> SkillLevels: назви рівнів 1..5 для кожної навички (Levels у вимогах викладача).
IF OBJECT_ID(N'dbo.SkillLevels', N'U') IS NULL
CREATE TABLE dbo.SkillLevels (
    Id          INT           NOT NULL IDENTITY(1,1) CONSTRAINT PK_SkillLevels PRIMARY KEY,
    SkillId     INT           NOT NULL CONSTRAINT FK_SkillLevels_Skill REFERENCES dbo.Skills(Id),
    Level       TINYINT       NOT NULL,
    Title       NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    CONSTRAINT CK_SkillLevels_Level CHECK (Level BETWEEN 1 AND 5),
    CONSTRAINT UQ_SkillLevels_Skill_Level UNIQUE (SkillId, Level)
);
GO

IF OBJECT_ID(N'dbo.Tags', N'U') IS NULL
CREATE TABLE dbo.Tags (
    Id   INT          NOT NULL IDENTITY(1,1) CONSTRAINT PK_Tags PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL CONSTRAINT UQ_Tags_Name UNIQUE
);
GO

-- M:N Skills <-> Tags із власним полем AddedAt.
IF OBJECT_ID(N'dbo.SkillTags', N'U') IS NULL
CREATE TABLE dbo.SkillTags (
    SkillId INT       NOT NULL CONSTRAINT FK_SkillTags_Skill REFERENCES dbo.Skills(Id),
    TagId   INT       NOT NULL CONSTRAINT FK_SkillTags_Tag   REFERENCES dbo.Tags(Id),
    AddedAt DATETIME2 NOT NULL CONSTRAINT DF_SkillTags_AddedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_SkillTags PRIMARY KEY (SkillId, TagId)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Categories_Name' AND object_id = OBJECT_ID(N'dbo.Categories'))
    CREATE UNIQUE INDEX UX_Categories_Name ON dbo.Categories(Name) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Categories_Slug' AND object_id = OBJECT_ID(N'dbo.Categories'))
    CREATE UNIQUE INDEX UX_Categories_Slug ON dbo.Categories(Slug) WHERE IsDeleted = 0;
-- Назва навички унікальна в усьому довіднику: локальні копії в інших базах мають UNIQUE по Name.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Skills_Name' AND object_id = OBJECT_ID(N'dbo.Skills'))
    CREATE UNIQUE INDEX UX_Skills_Name ON dbo.Skills(Name) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Skills_Slug' AND object_id = OBJECT_ID(N'dbo.Skills'))
    CREATE UNIQUE INDEX UX_Skills_Slug ON dbo.Skills(Slug) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Skills_Category' AND object_id = OBJECT_ID(N'dbo.Skills'))
    CREATE INDEX IX_Skills_Category ON dbo.Skills(CategoryId) INCLUDE (Name, Slug) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SkillTags_Tag' AND object_id = OBJECT_ID(N'dbo.SkillTags'))
    CREATE INDEX IX_SkillTags_Tag ON dbo.SkillTags(TagId, SkillId);
GO
