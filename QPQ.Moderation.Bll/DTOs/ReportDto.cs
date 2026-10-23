namespace QPQ.Moderation.Bll.DTOs;

// DTO для повернення даних
public record ReportDto(int Id, string Title, string Status);

// DTO для створення скарги
public record CreateReportDto(string Title);