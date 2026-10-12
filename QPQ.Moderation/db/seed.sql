-- Додаємо модераторів
IF NOT EXISTS (SELECT 1 FROM dbo.Moderators WHERE Username = 'admin_volodymyr')
BEGIN
    INSERT INTO dbo.Moderators (Username, Email) 
    VALUES ('admin_volodymyr', 'volodymyr@qpq.ua');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Moderators WHERE Username = 'moderator_01')
BEGIN
    INSERT INTO dbo.Moderators (Username, Email) 
    VALUES ('moderator_01', 'mod1@qpq.ua');
END;

-- Додаємо типи порушень
IF NOT EXISTS (SELECT 1 FROM dbo.ViolationTypes WHERE Name = 'Spam')
BEGIN
    INSERT INTO dbo.ViolationTypes (Name, SeverityLevel) VALUES ('Spam', 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.ViolationTypes WHERE Name = 'Hate Speech')
BEGIN
    INSERT INTO dbo.ViolationTypes (Name, SeverityLevel) VALUES ('Hate Speech', 4);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.ViolationTypes WHERE Name = 'Scam')
BEGIN
    INSERT INTO dbo.ViolationTypes (Name, SeverityLevel) VALUES ('Scam', 5);
END;

-- Додаємо тестову скаргу (з перевіркою)
IF NOT EXISTS (SELECT 1 FROM dbo.Reports WHERE TargetUserId = 999)
BEGIN
    DECLARE @RepId BIGINT;
    
    -- Викликаємо нашу процедуру для створення
    EXEC dbo.usp_CreateReport 
        @TargetUserId = 999, 
        @TargetUserDisplayName = 'BadSeller123', 
        @FullDescription = 'This user is sending spam messages in DM.',
        @NewReportId = @RepId OUTPUT;
        
    -- Прив'язуємо порушення до скарги
    INSERT INTO dbo.ReportViolations (ReportId, ViolationTypeId, Notes)
    SELECT @RepId, Id, 'Auto-assigned by script'
    FROM dbo.ViolationTypes WHERE Name = 'Spam';
END;