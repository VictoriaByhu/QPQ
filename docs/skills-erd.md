# ERD: піддомен Skills

Піддомен складається з трьох API, кожен має власну базу. Між базами немає FOREIGN KEY: зв'язок забезпечують ідентифікатори (`SkillId`, `UserId`) та локальні копії, які оновлюються подіями.

## UserSkills (Проєкт №1, SQL Server, ADO.NET + Dapper), база QPQ_UserSkills

```mermaid
erDiagram
    UserSkills ||--|| UserSkillDetails : "1:1"
    UserSkills ||--o{ AvailabilitySlots : "1:N"
    UserSkills ||--o{ UserSkillStatusHistory : "1:N"
    UserSkills ||--o{ UserSkillLanguages : "M:N"
    Languages ||--o{ UserSkillLanguages : "M:N"
    Skills ||--o{ UserSkills : "1:N"

    Skills {
        int Id PK
        nvarchar Name UK
        datetime2 CreatedAt
        datetime2 UpdatedAt
        bit IsDeleted
        datetime2 DeletedAt
        rowversion RowVersion
    }
    Languages {
        nvarchar Code PK
        nvarchar Name
    }
    UserSkills {
        int Id PK
        uniqueidentifier UserId
        int SkillId FK
        nvarchar Type
        tinyint Level
        nvarchar Status
        datetime2 CreatedAt
        uniqueidentifier CreatedBy
        datetime2 UpdatedAt
        uniqueidentifier UpdatedBy
        bit IsDeleted
        datetime2 DeletedAt
        rowversion RowVersion
    }
    UserSkillDetails {
        int UserSkillId PK, FK
        nvarchar Description
        tinyint ExperienceYears
        nvarchar PortfolioUrl
        datetime2 CreatedAt
        datetime2 UpdatedAt
        rowversion RowVersion
    }
    AvailabilitySlots {
        int Id PK
        int UserSkillId FK
        tinyint DayOfWeek
        time StartTime
        time EndTime
        datetime2 CreatedAt
    }
    UserSkillLanguages {
        int UserSkillId PK, FK
        nvarchar LanguageCode PK, FK
        tinyint Proficiency
        datetime2 CreatedAt
    }
    UserSkillStatusHistory {
        bigint Id PK
        int UserSkillId FK
        nvarchar OldStatus
        nvarchar NewStatus
        uniqueidentifier ChangedByUserId
        nvarchar Comment
        datetime2 ChangedAt
    }
```

## Catalog (Проєкт №2, SQL Server, EF Core), база QPQ_Catalog

```mermaid
erDiagram
    Categories ||--o{ Skills : "1:N"
    Skills ||--|| SkillDetails : "1:1"
    Skills ||--o{ SkillLevels : "1:N"
    Skills ||--o{ SkillTags : "M:N"
    Tags ||--o{ SkillTags : "M:N"

    Categories {
        int Id PK
        nvarchar Name UK
        nvarchar Slug UK
        int SortOrder
        datetime2 CreatedAt
        datetime2 UpdatedAt
        bit IsDeleted
        datetime2 DeletedAt
        rowversion RowVersion
    }
    Skills {
        int Id PK
        int CategoryId FK
        nvarchar Name UK
        nvarchar Slug UK
        datetime2 CreatedAt
        datetime2 UpdatedAt
        bit IsDeleted
        datetime2 DeletedAt
        rowversion RowVersion
    }
    SkillDetails {
        int SkillId PK, FK
        nvarchar Description
        nvarchar IconUrl
        datetime2 UpdatedAt
        rowversion RowVersion
    }
    SkillLevels {
        int Id PK
        int SkillId FK
        tinyint Level
        nvarchar Title
        nvarchar Description
    }
    Tags {
        int Id PK
        nvarchar Name UK
    }
    SkillTags {
        int SkillId PK, FK
        int TagId PK, FK
        datetime2 AddedAt
    }
```

## Matching (Проєкт №3, MongoDB), база QPQ_Matching

```mermaid
erDiagram
    userSkillIndex ||--o{ languages : "embedded"
    userSkillIndex ||--o{ availability : "embedded"
    matches ||--|| offer : "embedded"
    matches ||--o| counterOffer : "embedded (mutual)"
    matches }o--|| userSkillIndex : "offer.indexRef (reference)"
    skillStats }o--|| userSkillIndex : "computed from skillId"

    userSkillIndex {
        objectId _id PK
        number userSkillId UK
        string userId
        number skillId
        string skillName
        string type "Offer or Want"
        number level
        string status
        number experienceYears "only Offer"
        array languages
        array availability
        date updatedAt
    }
    matches {
        objectId _id PK
        string kind "oneWay or mutual"
        string status
        number score
        array userIds
        object offer
        object counterOffer "only mutual"
        array commonLanguages
        date createdAt
        date expiresAt "TTL"
    }
    skillStats {
        number _id PK "skillId"
        string skillName
        number offersCount
        number wantsCount
        array topLanguages
        date updatedAt
    }
    languages {
        string code
        number proficiency
    }
    availability {
        number dayOfWeek
        string start
        string end
    }
    offer {
        string fromUserId
        string toUserId
        number skillId
        string skillName
        objectId indexRef
    }
    counterOffer {
        string fromUserId
        string toUserId
        number skillId
        string skillName
        objectId indexRef
    }
```

## Зв'язки між базами

Пунктирні зв'язки нижче є лише логічними посиланнями за ідентифікатором, без FOREIGN KEY. Поруч зберігаються копії полів.

```mermaid
flowchart LR
    Identity[(Identity: UserId)]
    Catalog[(QPQ_Catalog: Skills)]
    UserSkills[(QPQ_UserSkills: Skills copy, UserSkills)]
    Matching[(QPQ_Matching: userSkillIndex, matches, skillStats)]

    Catalog -. "SkillChanged: Id, Name" .-> UserSkills
    Catalog -. "SkillChanged: skillName" .-> Matching
    UserSkills -. "UserSkillsChanged" .-> Matching
    Identity -. "UserId in UserSkills.UserId, userSkillIndex.userId" .-> UserSkills
    Identity -. "UserDeleted" .-> Matching
```
