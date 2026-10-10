SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
USE QPQ_Catalog;
GO

-- Ідентифікатори навичок 1-6 збігаються з копіями в UserSkills, Swaps і Sessions.
SET IDENTITY_INSERT dbo.Categories ON;
MERGE dbo.Categories AS t
USING (VALUES
    (1, N'Програмування', N'programming', 10),
    (2, N'Мови',          N'languages',   20),
    (3, N'Музика',        N'music',       30),
    (4, N'Дизайн',        N'design',      40),
    (5, N'Фото',          N'photo',       50),
    (6, N'Кулінарія',     N'cooking',     60)
) AS s (Id, Name, Slug, SortOrder)
ON t.Id = s.Id
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Slug, SortOrder) VALUES (s.Id, s.Name, s.Slug, s.SortOrder);
SET IDENTITY_INSERT dbo.Categories OFF;
GO

SET IDENTITY_INSERT dbo.Skills ON;
MERGE dbo.Skills AS t
USING (VALUES
    (1, 1, N'Програмування C#',     N'csharp'),
    (2, 2, N'Англійська розмовна',  N'english-speaking'),
    (3, 3, N'Гра на гітарі',        N'guitar'),
    (4, 4, N'UI-дизайн',            N'ui-design'),
    (5, 5, N'Фотографія',           N'photography'),
    (6, 6, N'Приготування випічки', N'baking')
) AS s (Id, CategoryId, Name, Slug)
ON t.Id = s.Id
WHEN NOT MATCHED THEN
    INSERT (Id, CategoryId, Name, Slug) VALUES (s.Id, s.CategoryId, s.Name, s.Slug);
SET IDENTITY_INSERT dbo.Skills OFF;
GO

MERGE dbo.SkillDetails AS t
USING (VALUES
    (1, N'Основи C#, платформа .NET, ООП, робота з даними та веб-API',          N'https://cdn.example.com/icons/csharp.svg'),
    (2, N'Розмовна англійська: діалоги, вимова, підготовка до співбесід',       N'https://cdn.example.com/icons/english.svg'),
    (3, N'Акустична гітара: акорди, бій, перші пісні',                          N'https://cdn.example.com/icons/guitar.svg'),
    (4, N'Проєктування інтерфейсів: макети, компоненти, прототипи у Figma',     N'https://cdn.example.com/icons/ui.svg'),
    (5, N'Знімання, композиція, світло та базова обробка фото',                 N'https://cdn.example.com/icons/photo.svg'),
    (6, N'Тісто, креми, випікання: від печива до пирогів',                      N'https://cdn.example.com/icons/baking.svg')
) AS s (SkillId, Description, IconUrl)
ON t.SkillId = s.SkillId
WHEN NOT MATCHED THEN
    INSERT (SkillId, Description, IconUrl) VALUES (s.SkillId, s.Description, s.IconUrl);
GO

-- Для кожної навички п'ять рівнів на єдиній шкалі 1..5, на яку посилається UserSkills.Level.
MERGE dbo.SkillLevels AS t
USING (
    SELECT sk.Id AS SkillId, lv.Level, lv.Title
    FROM dbo.Skills sk
    CROSS JOIN (VALUES
        (1, N'Початківець'),
        (2, N'Базовий'),
        (3, N'Середній'),
        (4, N'Просунутий'),
        (5, N'Експерт')
    ) AS lv (Level, Title)
) AS s
ON t.SkillId = s.SkillId AND t.Level = s.Level
WHEN NOT MATCHED THEN
    INSERT (SkillId, Level, Title) VALUES (s.SkillId, s.Level, s.Title);
GO

SET IDENTITY_INSERT dbo.Tags ON;
MERGE dbo.Tags AS t
USING (VALUES
    (1, N'.NET'),
    (2, N'Практика'),
    (3, N'Онлайн'),
    (4, N'Для початківців'),
    (5, N'Творчість'),
    (6, N'Мови')
) AS s (Id, Name)
ON t.Id = s.Id
WHEN NOT MATCHED THEN
    INSERT (Id, Name) VALUES (s.Id, s.Name);
SET IDENTITY_INSERT dbo.Tags OFF;
GO

MERGE dbo.SkillTags AS t
USING (VALUES
    (1, 1), (1, 2), (1, 3),
    (2, 6), (2, 3), (2, 4),
    (3, 5), (3, 4), (3, 2),
    (4, 5), (4, 3),
    (5, 5), (5, 2),
    (6, 5), (6, 4)
) AS s (SkillId, TagId)
ON t.SkillId = s.SkillId AND t.TagId = s.TagId
WHEN NOT MATCHED THEN
    INSERT (SkillId, TagId) VALUES (s.SkillId, s.TagId);
GO
