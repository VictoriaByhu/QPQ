-- 1. Створення нової скарги (Create)
CREATE OR ALTER PROCEDURE dbo.usp_CreateReport
    @TargetUserId BIGINT,
    @TargetUserDisplayName NVARCHAR(100),
    @FullDescription NVARCHAR(MAX),
    @NewReportId BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        INSERT INTO dbo.Reports (TargetUserId, TargetUserDisplayName)
        VALUES (@TargetUserId, @TargetUserDisplayName);
        
        SET @NewReportId = SCOPE_IDENTITY();
        
        INSERT INTO dbo.ReportDetails (ReportId, FullDescription)
        VALUES (@NewReportId, @FullDescription);
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- 2. Отримання скарги (Read)
CREATE OR ALTER PROCEDURE dbo.usp_GetReportById
    @ReportId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT r.Id, r.TargetUserDisplayName, r.Status, d.FullDescription
    FROM dbo.Reports r
    LEFT JOIN dbo.ReportDetails d ON r.Id = d.ReportId
    WHERE r.Id = @ReportId AND r.IsDeleted = 0;
END;
GO

-- 3. М'яке видалення скарги (Soft Delete)
CREATE OR ALTER PROCEDURE dbo.usp_SoftDeleteReport
    @ReportId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Reports
    SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @ReportId;
END;
GO

-- 4. ТРАНЗАКЦІЙНА ЗМІНА СТАТУСУ (Найголовніша процедура за критеріями)
CREATE OR ALTER PROCEDURE dbo.usp_ChangeReportStatus
    @ReportId BIGINT,
    @NewStatus NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON; -- Будь-яка помилка миттєво відкочує транзакцію
    
    BEGIN TRANSACTION;
    
    DECLARE @CurrentStatus NVARCHAR(30);
    
    -- UPDLOCK блокує рядок до кінця транзакції (захист від паралельного перезапису)
    SELECT @CurrentStatus = Status 
    FROM dbo.Reports WITH (UPDLOCK) 
    WHERE Id = @ReportId AND IsDeleted = 0;
    
    IF @CurrentStatus IS NULL
        THROW 50001, 'Report not found or deleted', 1;
        
    -- Бізнес-правило: Не можна змінювати статус вже закритої скарги
    IF @CurrentStatus IN ('Resolved', 'Dismissed')
        THROW 50002, 'Cannot change status of an already closed report', 1;
        
    -- Бізнес-правило: Можна перевести тільки у валідний статус
    IF @NewStatus NOT IN ('InProgress', 'Resolved', 'Dismissed')
        THROW 50003, 'Invalid status transition', 1;
        
    UPDATE dbo.Reports
    SET Status = @NewStatus, UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @ReportId;
    
    COMMIT TRANSACTION;
END;
GO