-- 1. Таблиця модераторів (Довідник)
CREATE TABLE dbo.Moderators (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    RowVersion ROWVERSION -- Оптимістична конкурентність
);

-- 2. Таблиця скарг (Зв'язок 1:N від Moderators)
CREATE TABLE dbo.Reports (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    -- Поля з іншого мікросервісу (Користувачі/Репутація) БЕЗ FOREIGN KEY
    TargetUserId BIGINT NOT NULL, 
    TargetUserDisplayName NVARCHAR(100) NOT NULL, -- Знімок на момент створення скарги
    
    Status NVARCHAR(30) NOT NULL DEFAULT 'New'
        CHECK (Status IN ('New', 'InProgress', 'Resolved', 'Dismissed')),
    
    AssignedModeratorId BIGINT NULL REFERENCES dbo.Moderators(Id),
    
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    RowVersion ROWVERSION
);

-- Індекс під конкретний запит: "Отримати всі активні скарги конкретного модератора"
CREATE INDEX IX_Reports_AssignedModerator_Active 
ON dbo.Reports (AssignedModeratorId, Status) 
WHERE IsDeleted = 0;

-- 3. Деталі скарги (Зв'язок 1:1 з Reports)
CREATE TABLE dbo.ReportDetails (
    ReportId BIGINT PRIMARY KEY REFERENCES dbo.Reports(Id) ON DELETE CASCADE,
    FullDescription NVARCHAR(MAX) NOT NULL,
    EvidenceLinks NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    RowVersion ROWVERSION
);

-- 4. Типи порушень (Довідник)
CREATE TABLE dbo.ViolationTypes (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    SeverityLevel INT NOT NULL CHECK (SeverityLevel BETWEEN 1 AND 5),
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    IsDeleted BIT NOT NULL DEFAULT 0
);

-- 5. Проміжна таблиця (Зв'язок M:N між Reports та ViolationTypes із власним полем)
CREATE TABLE dbo.ReportViolations (
    ReportId BIGINT NOT NULL REFERENCES dbo.Reports(Id) ON DELETE CASCADE,
    ViolationTypeId BIGINT NOT NULL REFERENCES dbo.ViolationTypes(Id),
    Notes NVARCHAR(500) NULL, -- Власне поле проміжної таблиці
    PRIMARY KEY (ReportId, ViolationTypeId)
);