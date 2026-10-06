SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
USE QPQ_Swaps;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CreateSwap
    @RequesterId      INT,
    @ProviderId       INT,
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
        BEGIN TRANSACTION;

        IF (SELECT COUNT(*) FROM dbo.Users WHERE Id IN (@RequesterId, @ProviderId) AND IsDeleted = 0) <> 2
            THROW 50001, N'User not found or deleted.', 1;
        IF (SELECT COUNT(*) FROM dbo.Skills WHERE Id IN (@OfferedSkillId, @RequestedSkillId) AND IsDeleted = 0)
           <> (CASE WHEN @OfferedSkillId = @RequestedSkillId THEN 1 ELSE 2 END)
            THROW 50002, N'The skill was not found or has been deleted.', 1;

        INSERT dbo.Swaps (RequesterId, ProviderId, Status)
        VALUES (@RequesterId, @ProviderId, N'Pending');
        SET @NewSwapId = SCOPE_IDENTITY();

        INSERT dbo.SwapDetails (SwapId, Message, Location, ProposedDate, DurationMinutes)
        VALUES (@NewSwapId, @Message, @Location, @ProposedDate, @DurationMinutes);

        INSERT dbo.SwapSkills (SwapId, SkillId, Role)
        VALUES (@NewSwapId, @OfferedSkillId, N'Offered'),
               (@NewSwapId, @RequestedSkillId, N'Requested');

        INSERT dbo.SwapStatusHistory (SwapId, OldStatus, NewStatus, ChangedByUserId, Comment)
        VALUES (@NewSwapId, NULL, N'Pending', @RequesterId, N'Swap created');

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
    @ChangedByUserId INT,
    @RowVersion      BINARY(8),
    @Comment         NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @OldStatus NVARCHAR(20), @RequesterId INT, @ProviderId INT,
                @CurrentRowVersion BINARY(8), @IsDeleted BIT;

        SELECT @OldStatus = Status, @RequesterId = RequesterId, @ProviderId = ProviderId,
               @CurrentRowVersion = RowVersion, @IsDeleted = IsDeleted
        FROM dbo.Swaps WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = @SwapId;

        IF @OldStatus IS NULL OR @IsDeleted = 1
            THROW 50010, N'Swap not found.', 1;
        IF @CurrentRowVersion <> @RowVersion
            THROW 50011, N'Parallel editing conflict: swap has already been modified.', 1;

        DECLARE @Allowed BIT = 0;
        IF @OldStatus = N'Pending'  AND @NewStatus IN (N'Accepted', N'Rejected') AND @ChangedByUserId = @ProviderId
            SET @Allowed = 1;
        IF @OldStatus = N'Pending'  AND @NewStatus = N'Cancelled' AND @ChangedByUserId = @RequesterId
            SET @Allowed = 1;
        IF @OldStatus = N'Accepted' AND @NewStatus IN (N'Completed', N'Cancelled')
           AND @ChangedByUserId IN (@RequesterId, @ProviderId)
            SET @Allowed = 1;

        IF @Allowed = 0
            THROW 50012, N'This status transition is not allowed for this user.', 1;

        UPDATE dbo.Swaps
        SET Status = @NewStatus, UpdatedAt = SYSUTCDATETIME()
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

    SELECT s.Id, s.RequesterId, ur.DisplayName AS RequesterName,
           s.ProviderId, up.DisplayName AS ProviderName,
           s.Status, s.CreatedAt, s.UpdatedAt, s.RowVersion
    FROM dbo.Swaps s
    JOIN dbo.Users ur ON ur.Id = s.RequesterId
    JOIN dbo.Users up ON up.Id = s.ProviderId
    WHERE s.Id = @SwapId AND s.IsDeleted = 0;

    SELECT SwapId, Message, Location, ProposedDate, DurationMinutes
    FROM dbo.SwapDetails
    WHERE SwapId = @SwapId;

    SELECT ss.SkillId, sk.Name AS SkillName, ss.Role
    FROM dbo.SwapSkills ss
    JOIN dbo.Skills sk ON sk.Id = ss.SkillId
    WHERE ss.SwapId = @SwapId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetUserSwaps
    @UserId     INT,
    @Role       NVARCHAR(10) = NULL,
    @Status     NVARCHAR(20) = NULL,
    @PageNumber INT = 1,
    @PageSize   INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    SELECT s.Id, s.RequesterId, s.ProviderId, s.Status, s.CreatedAt, s.RowVersion
    FROM dbo.Swaps s
    WHERE s.IsDeleted = 0
      AND (   (@Role = N'Requester' AND s.RequesterId = @UserId)
           OR (@Role = N'Provider'  AND s.ProviderId  = @UserId)
           OR (@Role IS NULL AND (s.RequesterId = @UserId OR s.ProviderId = @UserId)))
      AND (@Status IS NULL OR s.Status = @Status)
    ORDER BY s.CreatedAt DESC, s.Id DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_SoftDeleteSwap
    @SwapId     INT,
    @RowVersion BINARY(8)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Swaps WHERE Id = @SwapId AND IsDeleted = 0)
        THROW 50010, N'Swap not found.', 1;

    UPDATE dbo.Swaps
    SET IsDeleted = 1, DeletedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @SwapId AND IsDeleted = 0 AND RowVersion = @RowVersion;

    IF @@ROWCOUNT = 0
        THROW 50011, N'Parallel editing conflict: swap has already been modified.', 1;
END
GO