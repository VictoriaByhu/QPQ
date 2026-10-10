SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
USE QPQ_Swaps;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CreateSwap
    @InitiatorId      UNIQUEIDENTIFIER,
    @PartnerId        UNIQUEIDENTIFIER,
    @OfferedSkillId   INT,
    @RequestedSkillId INT,
    @Message          NVARCHAR(1000) = NULL,
    @Location         NVARCHAR(200)  = NULL,
    @ProposedDate     DATETIME2      = NULL,
    @DurationMinutes  INT            = NULL,
    @NewSwapId        INT            OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        IF @InitiatorId = @PartnerId
            THROW 50003, N'Initiator and partner must be different users.', 1;
        IF @OfferedSkillId = @RequestedSkillId
            THROW 50004, N'Offered and requested skills must be different.', 1;

        BEGIN TRANSACTION;

        IF (SELECT COUNT(*) FROM dbo.Skills WHERE Id IN (@OfferedSkillId, @RequestedSkillId) AND IsDeleted = 0) <> 2
            THROW 50002, N'The skill was not found or has been deleted.', 1;

        INSERT dbo.Swaps (InitiatorId, PartnerId, Status, CreatedBy)
        VALUES (@InitiatorId, @PartnerId, N'Pending', @InitiatorId);
        SET @NewSwapId = SCOPE_IDENTITY();

        INSERT dbo.SwapDetails (SwapId, Message, Location, ProposedDate, DurationMinutes)
        VALUES (@NewSwapId, @Message, @Location, @ProposedDate, @DurationMinutes);

        INSERT dbo.SwapSkills (SwapId, SkillId, Role)
        VALUES (@NewSwapId, @OfferedSkillId, N'Offered'),
               (@NewSwapId, @RequestedSkillId, N'Requested');

        INSERT dbo.SwapStatusHistory (SwapId, OldStatus, NewStatus, ChangedByUserId, Comment)
        VALUES (@NewSwapId, NULL, N'Pending', @InitiatorId, N'Swap created');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_ChangeSwapStatus
    @SwapId          INT,
    @NewStatus       NVARCHAR(20),
    @ChangedByUserId UNIQUEIDENTIFIER,
    @RowVersion      BINARY(8),
    @Comment         NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @OldStatus NVARCHAR(20), @InitiatorId UNIQUEIDENTIFIER, @PartnerId UNIQUEIDENTIFIER,
                @CurrentRowVersion BINARY(8), @IsDeleted BIT;

        SELECT @OldStatus = Status, @InitiatorId = InitiatorId, @PartnerId = PartnerId,
               @CurrentRowVersion = RowVersion, @IsDeleted = IsDeleted
        FROM dbo.Swaps WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = @SwapId;

        IF @OldStatus IS NULL OR @IsDeleted = 1
            THROW 50010, N'Swap not found.', 1;
        IF @CurrentRowVersion <> @RowVersion
            THROW 50011, N'Parallel editing conflict: swap has already been modified.', 1;

        DECLARE @Allowed BIT = 0;
        IF @OldStatus = N'Pending' AND @NewStatus IN (N'Accepted', N'Rejected') AND @ChangedByUserId = @PartnerId
            SET @Allowed = 1;
        IF @OldStatus = N'Pending' AND @NewStatus = N'Cancelled' AND @ChangedByUserId = @InitiatorId
            SET @Allowed = 1;
        IF @OldStatus = N'Accepted' AND @NewStatus IN (N'Completed', N'Cancelled')
           AND @ChangedByUserId IN (@InitiatorId, @PartnerId)
            SET @Allowed = 1;

        IF @Allowed = 0
            THROW 50012, N'This status transition is not allowed for this user.', 1;

        UPDATE dbo.Swaps
        SET Status = @NewStatus, UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @ChangedByUserId
        WHERE Id = @SwapId;

        INSERT dbo.SwapStatusHistory (SwapId, OldStatus, NewStatus, ChangedByUserId, Comment)
        VALUES (@SwapId, @OldStatus, @NewStatus, @ChangedByUserId, @Comment);

        COMMIT TRANSACTION;

        SELECT RowVersion FROM dbo.Swaps WHERE Id = @SwapId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetSwapById
    @SwapId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT s.Id, s.InitiatorId, s.PartnerId, s.Status, s.CreatedAt, s.CreatedBy, s.UpdatedAt, s.UpdatedBy, s.RowVersion
    FROM dbo.Swaps s
    WHERE s.Id = @SwapId AND s.IsDeleted = 0;

    SELECT d.SwapId, d.Message, d.Location, d.ProposedDate, d.DurationMinutes
    FROM dbo.SwapDetails d
    JOIN dbo.Swaps s ON s.Id = d.SwapId
    WHERE d.SwapId = @SwapId AND s.IsDeleted = 0;

    SELECT ss.SkillId, sk.Name AS SkillName, ss.Role
    FROM dbo.SwapSkills ss
    JOIN dbo.Swaps s ON s.Id = ss.SwapId
    JOIN dbo.Skills sk ON sk.Id = ss.SkillId
    WHERE ss.SwapId = @SwapId AND s.IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetUserSwaps
    @UserId     UNIQUEIDENTIFIER,
    @Role       NVARCHAR(10) = NULL,
    @Status     NVARCHAR(20) = NULL,
    @PageNumber INT = 1,
    @PageSize   INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    SELECT s.Id, s.InitiatorId, s.PartnerId, s.Status, s.CreatedAt, s.RowVersion
    FROM dbo.Swaps s
    WHERE s.IsDeleted = 0
      AND (   (@Role = N'Initiator' AND s.InitiatorId = @UserId)
           OR (@Role = N'Partner'   AND s.PartnerId   = @UserId)
           OR (@Role IS NULL AND (s.InitiatorId = @UserId OR s.PartnerId = @UserId)))
      AND (@Status IS NULL OR s.Status = @Status)
    ORDER BY s.CreatedAt DESC, s.Id DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_SoftDeleteSwap
    @SwapId          INT,
    @DeletedByUserId UNIQUEIDENTIFIER,
    @RowVersion      BINARY(8)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Swaps WHERE Id = @SwapId AND IsDeleted = 0)
        THROW 50010, N'Swap not found.', 1;

    UPDATE dbo.Swaps
    SET IsDeleted = 1, DeletedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @DeletedByUserId
    WHERE Id = @SwapId AND IsDeleted = 0 AND RowVersion = @RowVersion;

    IF @@ROWCOUNT = 0
        THROW 50011, N'Parallel editing conflict: swap has already been modified.', 1;
END
GO